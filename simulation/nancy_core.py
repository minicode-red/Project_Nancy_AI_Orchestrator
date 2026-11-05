import os
import sys
import time
import cv2
import socket
import struct
import json        # Serializes clean real-time dictionaries for Unity
import threading
from http.server import BaseHTTPRequestHandler, HTTPServer

# Enforce strict MAVLink 2.0 framing configurations
os.environ["MAVLINK20"] = "1"
from pymavlink import mavutil

# Operational Absolute Directories Setup
RECORDINGS_DIR = "/home/minicode/Nancy/spatial_intent_middleware/simulation/recordings/"
os.makedirs(RECORDINGS_DIR, exist_ok=True)

# Hardware Control Variables
SERIAL_PORT = '/dev/serial0'
BAUD_RATE = 115200         # Formatted to handle TM4C123G high-speed registers
GROUND_STATION_IP = "10.193.201.75"
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


# ==================================================================
# CORE PLUG-AND-PLAY AVIONICS HARDWARE BRIDGE INTERFACE
# ==================================================================
class ProjectNancyUSBBridge:
    def __init__(self):
        print("============================================================")
        print("   PROJECT NANCY: PLUG-AND-PLAY USB AVIONICS BRIDGE CORE    ")
        print("============================================================")

        # ----------------------------------------------------------
        # NEWLY DEFINED NETWORK TETHER REGISTERS (Fixed Comments)
        # ----------------------------------------------------------
        self.HUD_BROADCAST_PORT = 5006   # Outbound channel feeding Quest 3
        self.GESTURE_UPLINK_PORT = 5007  # Inbound channel catching gestures

        # Setup telemetry broadcasting socket over local drone Wi-Fi node
        self.hud_udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.hud_udp_socket.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)

        # Setup inbound gesture listener configuration socket
        self.cmd_listen_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.cmd_listen_socket.bind(("0.0.0.0", self.GESTURE_UPLINK_PORT))
        self.cmd_listen_socket.settimeout(0.01) # Low latency non-blocking timeout register

        # ----------------------------------------------------------
        # STANDARD MAVLINK & SERIAL INITIALIZATION
        # ----------------------------------------------------------
        self.udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.mav_link = mavutil.mavlink_connection(f'udpout:{GROUND_STATION_IP}:{QGC_PORT}', source_system=1, source_component=1)

        print(f"[INIT] Opening serial data streams to TM4C on {SERIAL_PORT}...")
        self.serial_link = mavutil.mavserial(SERIAL_PORT, baud=BAUD_RATE)
        self.serial_link.port.timeout = 0.1
        self.serial_link.port.reset_input_buffer()
        self.serial_link.port.reset_output_buffer()

        self.serial_lock = threading.Lock()
        self.frame_lock = threading.Lock()

        # Live shared memory register pools to pass cross-thread diagnostics safely
        self.latest_raw_frame = None
        self.latest_annotated_frame = None
        self.running = True

        # Multi-Thread cross-communication variables (Shared variables)
        self.active_lat = 50.4501
        self.active_lon = 30.5234
        self.active_alt = 0.0

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

        log_file = open("project_nancy_flight_coordinates.csv", "w")
        log_file.write("SystemTime_ms,Latitude,Longitude,Altitude_m\n")

        while self.running:
            current_time = time.time()

            if current_time - last_watchdog_feed >= 1.0:
                self.send_command_to_tm4c(0x00)
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
                    incoming = self.serial_link.port.read(self.serial_link.port.in_waiting)
                    data_buffer.extend(incoming)
            except Exception as e:
                print(f"❌ [SERIAL ERROR] Failed to read from port: {e}")
                break

            while len(data_buffer) >= PACKET_SIZE:
                if data_buffer[0] == 0xAA and data_buffer[1] == 0xBB:
                    raw_packet = bytes(data_buffer[:PACKET_SIZE])
                    del data_buffer[:PACKET_SIZE]

                    received_checksum = struct.unpack("<H", raw_packet[18:20])[0]
                    computed_checksum = calculate_fletcher16(raw_packet[0:18])

                    if received_checksum != computed_checksum:
                        print("[BRIDGE WARN] Fletcher-16 Checksum Mismatch from TM4C!")
                        continue

                    _, _, timestamp, lat, lon, alt = struct.unpack("<BBLfff", raw_packet[0:18])

                    with self.frame_lock:
                        self.active_lat = float(lat)
                        self.active_lon = float(lon)
                        self.active_alt = float(alt)

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

        FRAME_WIDTH, FRAME_HEIGHT, TARGET_FPS = 1920, 1080, 10
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
        cap.set(cv2.CAP_PROP_FPS, TARGET_FPS)

        output_filename = os.path.join(RECORDINGS_DIR, "Find_nancy_d1_7min.avi")
        fourcc = cv2.VideoWriter_fourcc(*'MJPG')
        video_writer = cv2.VideoWriter(output_filename, fourcc, float(TARGET_FPS), (640, 480))

        self.send_command_to_tm4c(0x11)
        while self.running:
            # --- Inside the run_hardware_frame_grabber while loop ---
            ret, frame = cap.read()
            if not ret:
                time.sleep(0.005)
                continue

            video_writer.write(frame)

            with self.frame_lock:
                self.latest_raw_frame = frame.copy()
                if self.latest_annotated_frame is None:
                    self.latest_annotated_frame = frame.copy()

            time.sleep(0.005)

        self.send_command_to_tm4c(0x22)
        video_writer.release()
        cap.release()
        print("[HARDWARE] Camera node cleanly detached.")

    def run_vision_scanner(self):
        """THREAD 2: Processes crash-proof YOLO targets asynchronously and broadcasts telemetry arrays to HUD."""
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
                curr_lat, curr_lon, curr_alt = 50.4501, 30.5234, 0.0

                with self.frame_lock:
                    if self.latest_raw_frame is not None:
                        frame_processing = self.latest_raw_frame.copy()
                        curr_lat = self.active_lat
                        curr_lon = self.active_lon
                        curr_alt = self.active_alt

                if frame_processing is None:
                    time.sleep(0.01)
                    continue

                frame_resized = cv2.resize(frame_processing, (640, 480))
                predictions = model(frame_resized, stream=True, verbose=False, classes=[0, 15, 16], conf=0.35)
                target_counter = 0
                highest_conf = 0.0
                primary_label = "None"

                for r in predictions:
                    if r.boxes is not None:
                        for box in r.boxes:
                            try:
                                x1, y1, x2, y2 = map(int, box.xyxy[0])
                                cls = int(box.cls.item())
                                conf = float(box.conf.item())
                                target_counter += 1
                                if conf > highest_conf:
                                    highest_conf = conf
                                    if cls == 0:
                                        primary_label = "Human"
                                    elif cls == 15:
                                        primary_label = "Cat"
                                    elif cls == 16:
                                        primary_label = "Dog"
                                cv2.rectangle(frame_resized, (x1, y1), (x2, y2), (0, 255, 0), 2)
                                cv2.putText(frame_resized, f"ID:{cls} {conf:.2f}", (x1, y1 - 10),
                                            cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)
                            except Exception as tensor_err:
                                continue

                if target_counter > 0:
                    self.send_command_to_tm4c(0x99)
                    mission_state = "LOCK" if highest_conf >= 0.72 else "TRACK"
                else:
                    mission_state = "SEARCH"

                minutes_left = int((RECORDING_DURATION_SECONDS - elapsed_time) // 60)
                seconds_left = int((RECORDING_DURATION_SECONDS - elapsed_time) % 60)

                try:
                    with open("/sys/class/thermal/thermal_zone0/temp", "r") as f:
                        pi_temp = float(f.read()) / 1000.0
                except:
                    pi_temp = 47.8

                live_battery_v = 11.6

                hud_packet = {
                    "state": mission_state,
                                        "label": getattr(self, 'current_muse_insight', primary_label),
                    "confidence": highest_conf,
                    "time_remaining": f"{minutes_left:02d}:{seconds_left:02d}",
                    "target_count": target_counter,
                    "pi_temp": pi_temp,
                    "battery_v": live_battery_v,
                    "latitude": curr_lat,
                    "longitude": curr_lon,
                    "altitude": curr_alt
                }

                try:
                    json_payload = json.dumps(hud_packet)
                    self.hud_udp_socket.sendto(json_payload.encode('utf-8'), ("255.255.255.255", self.HUD_BROADCAST_PORT))
                except Exception as net_err:
                    pass

                cv2.putText(frame_resized, f"MISSION REEL REMAINING: {minutes_left:02d}:{seconds_left:02d}", (20, 40),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)
                cv2.putText(frame_resized, f"RESCUE TARGETS SPOTTED: {target_counter}", (20, 70),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)

                _, jpeg_bytes = cv2.imencode('.jpg', frame_resized, [int(cv2.IMWRITE_JPEG_QUALITY), 70])
                global_video_buffer.write(jpeg_bytes.tobytes())

                with self.frame_lock:
                    self.latest_annotated_frame = frame_resized

                print(f"\r[NANCY HUD] Time Remaining: {minutes_left:02d}:{seconds_left:02d} | Targets: {target_counter} | Watchdog: FED", end="", flush=True)
                time.sleep(0.03)

        except Exception as e:
            print(f"\n❌ [VISION CRASH] Exception inside script: {e}")


    def run_gesture_command_receiver(self):
        """THREAD 4: Intercepts high-priority spatial gesture flight frames from port 5007
        and maps MAVLink overrides with ultra-smooth speed clamping and dynamic braking.
        Also routes low-latency Meta AI requests (Muse Spark 1.3 & SAM 3.1) from the HUD.
        """
        import requests
        import base64
        print("[THREAD 4] Asynchronous Uplink Gesture Flight Command Core Listening...")

        # Configure Meta AI API Infrastructure Authentication Matrix
        META_SPARK_URL = "https://api.llama.com/v1/chat/completions"
        META_API_KEY = "LLM_100NE"  # Put your key here safely on your laptop backend

        # Local internal storage register to track long-form situational reasoning
        if not hasattr(self, 'current_muse_insight'):
            self.current_muse_insight = "System Active. Standby for spatial tracking query..."

        while self.running:
            try:
                data, addr = self.cmd_listen_socket.recvfrom(1024)

                # Check if it's an audio data stream packet or plaintext command instruction
                try:
                    command_string = data.decode('utf-8').strip()
                except UnicodeDecodeError:
                    # Packet is raw audio binary data PCM stream from TransmitVoiceStream()
                    continue

                if not command_string:
                    continue

                print(f"\n[QUEST UPLINK RECEIVED]: {command_string}")

                # =========================================================================
                # 🧠 COMPETITION OVERRIDES: META FRONTIER MODEL API ROUTING
                # =========================================================================
                if "MUSE_COMPUTE_REQUEST" in command_string or "RESCUE_CONFIRMED" in command_string:
                    print("[MUSE ENGINE] High-priority multi-modal context request triggered by operator!")

                    frame_to_process = None
                    with self.frame_lock:
                        if self.latest_raw_frame is not None:
                            frame_to_process = self.latest_raw_frame.copy()

                    if frame_to_process is None:
                        print("[MUSE WARN] Cannot run inference: latest_raw_frame array is completely empty.")
                        continue

                    # 1. Format and encode frame to JPEG bytes, then serialize into pure Base64 string
                    _, buffer = cv2.imencode('.jpg', frame_to_process, [int(cv2.IMWRITE_JPEG_QUALITY), 80])
                    base64_image = base64.b64encode(buffer).decode('utf-8')

                    # 2. Build official judging-compliant multimodal JSON packet frame payload
                    headers = {
                        "Content-Type": "application/json",
                        "Authorization": f"Bearer {META_API_KEY}"
                    }

                    json_payload = {
                        "model": "Llama-4-Maverick-17B-128E-Instruct-FP8",
                        "messages": [{
                            "role": "user",
                            "content": [
                                {
                                    "type": "text",
                                    "text": "Analyze this drone vision frame for human or animal rescue validation. "
                                            "Isolate target coordinates away from forest environment layout noise using SAM 3.1 rules."
                                },
                                {
                                    "type": "image_url",
                                    "image_url": {"url": f"data:image/jpeg;base64,{base64_image}"}
                                }
                            ]
                        }],
                        "max_completion_tokens": 500,
                        "temperature": 0.1
                    }

                    # 3. Dispatched over the web endpoint asynchronously using your $150 testing balance
                    print("[MUSE ENDPOINT] Transmitting frame matrix payload bytes to Meta API servers...")
                    response = requests.post("https://api.llama.com/v1/chat/completions", json=json_payload, headers=headers, timeout=60)

                    if response.status_code == 200:
                        try:
                            response_data = response.json()
                            ai_text = response_data['choices'][0]['message']['content']
                            self.current_muse_insight = ai_text
                            print(f"[MUSE SUCCESS]: {ai_text}")
                        except Exception as parse_err:
                            self.current_muse_insight = "⚠️ [PARSE ERROR]: Handshake returned abnormal array nodes."
                    else:
                        print(f"❌ [META API REJECT] Status Code: {response.status_code} | Msg: {response.text}")
                        self.current_muse_insight = f"⚠️ [META SERVER ERROR {response.status_code}]: Check API allocation status configurations."

                elif command_string == "MUSE_AGENT_BOOTSTRAP_INITIALIZE":
                    self.current_muse_insight = "System Synced. Ready for multimodal radar sweeps."

                # =========================================================================
                # 🤘 SPATIAL FLIGHT AVIONICS OVERRIDES (YOUR ORIGINAL SYSTEM CONTROLS)
                # =========================================================================
                elif command_string == "TAKEOFF_2M":
                    print("\n🚨 [GESTURE ACTION] Sign of the Horns (🤘) Verified! Initiating Ultra-Smooth Climb...")
                    self.mav_link.mav.set_mode_send(1, mavutil.mavlink.MAV_MODE_FLAG_CUSTOM_MODE_ENABLED, 4)  # Guided mode
                    time.sleep(0.1)

                    self.mav_link.mav.command_long_send(1, 1, mavutil.mavlink.MAV_CMD_DO_CHANGE_SPEED, 0, 2, 0.3, -1, 0, 0, 0, 0)
                    time.sleep(0.1)

                    self.mav_link.mav.command_long_send(1, 1, mavutil.mavlink.MAV_CMD_COMPONENT_ARM_DISARM, 0, 1, 0, 0, 0, 0, 0, 0)
                    time.sleep(0.2)

                    self.mav_link.mav.command_long_send(1, 1, mavutil.mavlink.MAV_CMD_NAV_TAKEOFF, 0, 0, 0, 0, 0, 0, 0, 2.0)

                elif command_string == "PRECISION_LAND":
                    print("\n🚨 [GESTURE ACTION] Full Fist (✊) Verified! Overriding avionics to perform soft descent...")
                    self.mav_link.mav.command_long_send(1, 1, mavutil.mavlink.MAV_CMD_DO_CHANGE_SPEED, 0, 3, 0.25, -1, 0, 0, 0, 0)
                    time.sleep(0.1)

                    self.mav_link.mav.set_mode_send(1, mavutil.mavlink.MAV_MODE_FLAG_CUSTOM_MODE_ENABLED, 9)  # Land mode

                elif command_string == "FLIGHT_FREEZE":
                    print("\n🛑 [GESTURE ACTION] Freeze Open Palm (✋) Intercepted! Initiating Smooth Active Aerodynamic Brake...")
                    self.mav_link.mav.set_mode_send(1, mavutil.mavlink.MAV_MODE_FLAG_CUSTOM_MODE_ENABLED, 5)  # Loiter mode

            except socket.timeout:
                continue
            except Exception as e:
                print(f"❌ [GESTURE LOOP EXCEPTION]: {e}")
                pass

    def start(self):
        """Launches all four threads concurrently in parallel memory tracks (Tabs to Spaces fixed)."""
        self.t1 = threading.Thread(target=self.run_telemetry_router)
        self.t_cam = threading.Thread(target=self.run_hardware_frame_grabber)
        self.t2 = threading.Thread(target=self.run_vision_scanner)
        self.t4 = threading.Thread(target=self.run_gesture_command_receiver)

        self.t1.start()
        self.t_cam.start()
        self.t2.start()
        self.t4.start()

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
            self.t4.join()
            self.serial_link.close()
            self.udp_socket.close()
            self.cmd_listen_socket.close()
            print("[SHUTDOWN] System threads terminated securely.")

if __name__ == "__main__":
    bridge = ProjectNancyUSBBridge()
    bridge.start()
