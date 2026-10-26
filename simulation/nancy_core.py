import os
import sys
import time
import cv2
import socket
import struct
import threading
from http.server import BaseHTTPRequestHandler, HTTPServer

CI = os.getenv("CI", "").lower() in {"1", "true", "yes"}

# Enforce strict MAVLink 2.0 framing configurations
os.environ["MAVLINK20"] = "1"
from pymavlink import mavutil

# Operational Absolute Directories Setup
RECORDINGS_DIR = "/home/minicode/Nancy/spatial_intent_middleware/simulation/recordings/"
os.makedirs(RECORDINGS_DIR, exist_ok=True)

# Hardware Control Variables
SERIAL_PORT = '/dev/serial0'
BAUD_RATE = 115200         # Formatted to handle TM4C123G high-speed registers
GROUND_STATION_IP = "127.0.0.1"
QGC_PORT = 14550

# ==================================================================
# GLOBAL NETWORK STREAMING BUFFER TIER
# ==================================================================
class StreamingBuffer:
    """Provides a thread-safe, high-velocity single-frame ring buffer."""
    def __init__(self):
        self.frame = None
        self.condition = threading.Condition()

    def write(self, frame_bytes):
        with self.condition:
            self.frame = frame_bytes
            self.condition.notify_all()

global_video_buffer = StreamingBuffer()

class StreamHandler(BaseHTTPRequestHandler):
    """Serves the MJPEG boundary matrix over a local HTTP connection."""
    def do_GET(self):
        if self.path == '/stream.mjpg':
            self.send_response(200)
            self.send_header('Age', 0)
            self.send_header('Cache-Control', 'no-cache, private')
            self.send_header('Pragma', 'no-cache')
            self.send_header('Content-Type', 'multipart/x-mixed-replace; boundary=FRAME')
            self.end_headers()
            try:
                while True:
                    with global_video_buffer.condition:
                        global_video_buffer.condition.wait()
                        frame = global_video_buffer.frame
                    self.wfile.write(b'--FRAME\r\n')
                    self.send_header('Content-Type', 'image/jpeg')
                    self.send_header('Content-Length', len(frame))
                    self.end_headers()
                    self.wfile.write(frame)
                    self.wfile.write(b'\r\n')
            except Exception as e:
                pass
        else:
            self.send_error(404)

# ==================================================================
# UTILITY MATH INTERFACES
# ==================================================================
def calculate_fletcher16(data: bytes) -> int:
    sum1 = 0
    sum2 = 0
    for byte in data:
        sum1 = (sum1 + byte) % 255
        sum2 = (sum2 + sum1) % 255
    return (sum2 << 8) | sum1


class ProjectNancyUSBBridge:
    def __init__(self):
        if CI:
            raise RuntimeError("Hardware bridge disabled in CI mode")

        print("============================================================")
        print("   PROJECT NANCY: PLUG-AND-PLAY USB AVIONICS BRIDGE CORE    ")
        print("============================================================")

        self.udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.mav_link = mavutil.mavlink_connection(
            f'udpout:{GROUND_STATION_IP}:{QGC_PORT}',
            source_system=1,
            source_component=1
        )

        print(f"[INIT] Opening serial data streams to TM4C on {SERIAL_PORT}...")
        self.serial_link = mavutil.mavserial(SERIAL_PORT, baud=BAUD_RATE)
        
        # update fixed 1
        self.serial_link.port.timeout = 0.1

        self.serial_link.port.reset_input_buffer()
        self.serial_link.port.reset_output_buffer()

        self.serial_lock = threading.Lock()

        # Asynchronous shared-memory frames buffers to decouple pipeline delays
        self.frame_lock = threading.Lock()
        self.latest_raw_frame = None
        self.latest_annotated_frame = None
        self.running = True

        # Launch the high-speed background HTTP video broadcast engine on port 8080
        self.start_web_server()

    def start_web_server(self):
        """Spawns an asynchronous web network listener on port 8080."""
        print("[INIT] Starting background live HTTP stream listener node on port 8080...")
        server = HTTPServer(('0.0.0.0', 8080), StreamHandler)
        server_thread = threading.Thread(target=server.serve_forever)
        server_thread.daemon = True
        server_thread.start()

    def send_command_to_tm4c(self, command_byte: int):
        with self.serial_lock:
            try:
                self.serial_link.port.write(bytes([command_byte]))
            except Exception as e:
                pass

    def run_telemetry_router(self):
        print("[THREAD 1] Telemetry router and watchdog monitor online.")
        data_buffer = bytearray()
        PACKET_SIZE = 20
        last_watchdog_feed = 0

        #log_file = open(os.path.join(RECORDINGS_DIR, "project_nancy_flight_coordinates.csv"), "w")
        log_file = open("project_nancy_flight_coordinates.csv", "w")
        log_file.write("SystemTime_ms,Latitude,Longitude,Altitude_m\n")

        while self.running:
            current_time = time.time()

            # Watchdog Signal: Feed the microchip every 1000ms
            if current_time - last_watchdog_feed >= 1.0:
                self.send_command_to_tm4c(0x00) # Reset TM4C Watchdog
                self.mav_link.mav.heartbeat_send(
                    type=mavutil.mavlink.MAV_TYPE_QUADROTOR,
                    autopilot=mavutil.mavlink.MAV_AUTOPILOT_ARDUPILOTMEGA,
                    base_mode=mavutil.mavlink.MAV_MODE_FLAG_SAFETY_ARMED,
                    custom_mode=0,
                    system_status=mavutil.mavlink.MAV_STATE_ACTIVE
                )
                last_watchdog_feed = current_time

            try:
                if self.serial_link.port.in_waiting > 0:
                    #data_buffer.extend(self.serial_link.port.read(self.serial_link.port.in_waiting))
                    incoming = self.serial_link.port.read(self.serial_link.port.in_waiting)
                    #print(f"RAW INCOMING SERIAL BYTES: {incoming.hex()}")
                    data_buffer.extend(incoming)
            except Exception as e:
                print(f"❌ [SERIAL ERROR] Failed to read from port: {e}") #update fixed2
                break

            while len(data_buffer) >= PACKET_SIZE:
                if data_buffer[0] == 0xAA and data_buffer[1] == 0xBB:
                    raw_packet = bytes(data_buffer[:PACKET_SIZE])
                    del data_buffer[:PACKET_SIZE]

                    received_checksum = struct.unpack("<H", raw_packet[18:20])[0]
                    computed_checksum = calculate_fletcher16(raw_packet[0:18])

                    if received_checksum != computed_checksum:
                        #print("⚠️ [CHECKSUM ERROR] Packet corrupted, skipping frame.")
                        print("[BRIDGE WARN] Fletcher-16 Checksum Mismatch from TM4C!")
                        #del data_buffer[0]
                        continue

                    _, _, timestamp, lat, lon, alt = struct.unpack("<BBLfff", raw_packet[0:18])
                    mav_lat = int(lat * 1E7)
                    mav_lon = int(lon * 1E7)
                    mav_alt_mm = int(alt * 1E3)

                    self.mav_link.mav.global_position_int_send(
                        time_boot_ms=timestamp, lat=mav_lat, lon=mav_lon, alt=mav_alt_mm,
                        relative_alt=mav_alt_mm, vx=0, vy=0, vz=0, hdg=0
                    )

                    log_file.write(f"{timestamp},{lat},{lon},{alt}\n")
                    log_file.flush()
                else:
                    del data_buffer[0]

            time.sleep(0.005)
        log_file.close()

    def run_hardware_frame_grabber(self):
        """THREAD 3: Fast-track grabber loop to stream EMEET C960 frames at unblocked 30 FPS."""
        print("[THREAD 3] EMEET C960 Fast-Track Video Ingestion Thread Online.")

        cap = cv2.VideoCapture(0, cv2.CAP_V4L2)
        if not cap.isOpened():
            cap = cv2.VideoCapture(0)

        if not cap.isOpened():
            print("❌ [CRITICAL ERROR] No USB camera detected on hardware index 0!")
            self.running = False
            return

        print("[HW] Waiting for USB camera initialization...")
        time.sleep(2.0)

        # Apply production profile metrics for crisp 1080p recording quality
        FRAME_WIDTH, FRAME_HEIGHT, TARGET_FPS = 1920, 1080, 10
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
        cap.set(cv2.CAP_PROP_FPS, TARGET_FPS)

        output_filename = os.path.join(RECORDINGS_DIR, "Find_nancy_d1_7min.avi")
        fourcc = cv2.VideoWriter_fourcc(*'MJPG')
        video_writer = cv2.VideoWriter(output_filename, fourcc, float(TARGET_FPS), (640, 480))

        # Signal TM4C to start green camera indicator blink loops (Command 0x11)
        self.send_command_to_tm4c(0x11)

        while self.running:
            ret, frame = cap.read()
            if not ret:
                time.sleep(0.005)
                continue

            # Save untouched full-resolution frames straight to disk reel
            video_writer.write(frame)

            with self.frame_lock:
                self.latest_raw_frame = frame.copy()
                if self.latest_annotated_frame is None:
                    self.latest_annotated_frame = frame.copy()

            time.sleep(0.005)

        self.send_command_to_tm4c(0x22) # Safe shutdown to TM4C
        video_writer.release()
        cap.release()
        print("[HARDWARE] Camera node cleanly detached.")

    def run_vision_scanner(self):
        """THREAD 2: Processes crash-proof YOLO targets asynchronously in the background."""
        print("[THREAD 2] Asynchronous Universal Multi-Class Object Tracking AI Core Running...")

        from ultralytics import YOLO
        model = YOLO("yolov8n.pt")
        model.to('cpu')

        RECORDING_DURATION_SECONDS = 420  # 7-Minute flight layout
        start_time = time.time()
        end_time = start_time + RECORDING_DURATION_SECONDS

        try:
            while self.running:
                current_time = time.time()
                elapsed_time = current_time - start_time

                if current_time >= end_time:
                    print(f"\n[SUCCESS] Exact 7-minute mission demo video captured.")
                    break

                frame_processing = None
                with self.frame_lock:
                    if self.latest_raw_frame is not None:
                        frame_processing = self.latest_raw_frame.copy()

                if frame_processing is None:
                    time.sleep(0.01)
                    continue

                # Downscale resolution internally ONLY for the AI matrix math to fix lag
                frame_resized = cv2.resize(frame_processing, (640, 480))

                # Track multiple target classes simultaneously: Human(0), Cat(15), Dog(16)
                predictions = model(frame_resized, stream=True, verbose=False, classes=[0, 15, 16], conf=0.35)
                target_counter = 0

                for r in predictions:
                    if r.boxes is not None:
                        for box in r.boxes:
                            try:
                                # FIXED TENSOR BOUNDS: Unpack coordinate matrix via safe 1D row mapping loops
                                x1, y1, x2, y2 = map(int, box.xyxy[0])

                                # FIXED item() EXTRACTORS: Unpack multi-element arrays to raw int/float scalars
                                cls = int(box.cls.item())
                                conf = float(box.conf.item())

                                target_counter += 1

                                # Draw visual target boundaries directly on screen
                                cv2.rectangle(frame_resized, (x1, y1), (x2, y2), (0, 255, 0), 2)
                                cv2.putText(frame_resized, f"ID:{cls} {conf:.2f}", (x1, y1 - 10),
                                            cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)
                            except Exception as tensor_err:
                                continue

                # TRIGGER SYSTEM ALERT STROBE (RED LED): If target matches tracking indices
                if target_counter > 0:
                    self.send_command_to_tm4c(0x99)

                minutes_left = int((RECORDING_DURATION_SECONDS - elapsed_time) // 60)
                seconds_left = int((RECORDING_DURATION_SECONDS - elapsed_time) % 60)

                # Draw telemetry matrices directly onto the output presentation frames
                cv2.putText(frame_resized, f"MISSION REEL REMAINING: {minutes_left:02d}:{seconds_left:02d}", (20, 40),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)
                cv2.putText(frame_resized, f"RESCUE TARGETS SPOTTED: {target_counter}", (20, 70),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)

                # Compress downscaled frame to JPEG 70 quality to beat network traffic lag completely
                _, jpeg_bytes = cv2.imencode('.jpg', frame_resized, [int(cv2.IMWRITE_JPEG_QUALITY), 70])
                global_video_buffer.write(jpeg_bytes.tobytes())

                with self.frame_lock:
                    self.latest_annotated_frame = frame_resized

                print(f"\r[NANCY HUD] Time Remaining: {minutes_left:02d}:{seconds_left:02d} | Targets: {target_counter} | Watchdog: FED", end="", flush=True)
                time.sleep(0.03)

        except Exception as e:
            print(f"\n❌ [VISION CRASH] Exception inside script: {e}")

    def start(self):
        """Launches all threads concurrently in parallel memory tracks."""
        self.t1 = threading.Thread(target=self.run_telemetry_router)
        self.t_cam = threading.Thread(target=self.run_hardware_frame_grabber)
        self.t2 = threading.Thread(target=self.run_vision_scanner)

        self.t1.start()
        self.t_cam.start()
        self.t2.start()

        try:
            while self.t2.is_alive():
                time.sleep(0.5)
        except KeyboardInterrupt:
            print("\n[SHUTDOWN] Safety override received. Powering down payload...")
        finally:
            self.running = False
            self.t1.join()
            self.t_cam.join()
            self.t2.join()
            self.serial_link.close()
            self.udp_socket.close()
            print("[SHUTDOWN] System threads terminated securely.")


if __name__ == "__main__":
    if CI:
        print("CI mode: skipping hardware bridge startup")
        raise SystemExit(0)

    bridge = ProjectNancyUSBBridge()
    bridge.start()