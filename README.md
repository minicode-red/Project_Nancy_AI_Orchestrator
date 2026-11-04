# Project_Nancy_Showcase
Meta Horizon Start Competition Showcase. Features the Unity v207 Spatial HUD Interface and autonomous companion simulation pipeline for Project Nancy.

=================================================================
Flight Control Interface Specifications: Spatial Gesture Commands
==================================================================

This document establishes the standardized operational procedures for the high-priority spatial gesture uplink system controlling the unmanned aerial vehicle (UAV). All pilots must adhere strictly to these physical hand orientations to ensure deterministic flight performance.

1. Launch & Recovery System Configuration (Left Hand)

• Operational Execution:
	• Takeoff Protocol: Extend the index finger and pinky finger simultaneously while keeping the middle and ring fingers closed (the "Sign of the Horns" / Rock and Roll gesture). This initiates the automatic arming sequence and commands an ultra-smooth vertical climb to a stabilized altitude.
	• Precision Landing: Transition the left hand into a fully closed fist. This overrides current flight routines to initiate a soft, managed vertical descent to touchdown.

2. Directional Flight Steering & Vector Tracking (Right Hand)

• Physical Hand Layout: Extend either the index and middle fingers together OR the index finger alone. All remaining digits must be fully closed into the palm. Position the palm face-down, parallel to the ground (analogous to placing your hand on a steering wheel).
• Operational Execution:
	• Pitch Forward: Push the hand forward along the longitudinal axis to command the aircraft to accelerate forward along your hand's alignment path.
	• Lateral Roll: Translate the hand sideways along the lateral axis to command the aircraft to drift left or right in tandem with your movement.
	• Retract/Pitch Backward: Draw the hand backward toward the operator's chest to command the aircraft to reverse direction and retreat toward the pilot.

3. Dynamic Braking & Immediate Halt (Right Hand)

• Physical Hand Layout: Transition the right steering hand immediately into a fully closed fist (all digits retracted into the palm).
• Operational Execution:
	• Active Stop: Intercepting this gesture neutralizes all active horizontal velocity inputs instantly, initiating programmatic aerodynamic braking to bring the aircraft to an immediate, stable hover.

4. Strategic Recall & Aerodynamic Position Lock (Left Hand)

• Physical Hand Layout: Utilize the left hand with fully extended digits to issue systemic overrides.
• Operational Execution:
	• Recall to Base (Return-to-Pilot): Extend all fingers fully in an upright position (resembling a standard "stop sign") with the hand positioned away from the face. This commands the aircraft to initiate autonomous recall routines and return directly to the pilot's coordinates.
	• Flight Loiter Freeze: Extend all fingers fully with the palm facing directly toward the sky. This overrides active avionics subsystems to lock the aircraft into a rigid GPS Loiter position hold.

========================================================================
System Deployment & Operational Procedures: Project Find Nancy
========================================================================

Follow these steps to initialize and run the Find Nancy (Nancy Smart Box) system architecture. The core can be executed either via a simulated environment or through live physical deployment with Meta Quest hardware tracking.


Step 1: Initialize the Edge Core (Python Backend)

The primary telemetry router, live ingestion node, and local object detection pipeline run from your local terminal workspace.
1. Navigate to your project directory.
2. Launch the asynchronous controller script to open serial data streams and start the background live HTTP stream listener:

bash
python main.py

3. Verify that the terminal outputs show the telemetry router, video ingestion thread, and localized object tracking AI core are running in an [ONLINE] state.

Step 2: Deploy the Pilot Interface (Hardware Tracking or Simulation)

To send high-priority spatial gesture flight commands to the Python core backend on port 5007, initialize the operator uplink using one of the following two deployment methods:

Method A: Physical Hardware Deployment (Meta Quest)

1. Compile and Export: Build the gesture tracking package into an Android application file (.apk).
2. Install the Application: Side-load the file onto your Meta Quest headset using an installation tool like SideQuest or via the Android Debug Bridge CLI:

bash
adb install -r nancy_gesture_core.apk

3. Launch the Core: Power on the headset, launch the application from your unknown sources directory, and stand away from your face to calibrate spatial tracking bounds.

Method B: Local Simulation Environment (Testing Profile)

1. If you are operating without physical VR hardware, initiate the local software-in-the-loop gesture emulation panel.
2. Run the simulation driver file in a secondary terminal window to map your computer keyboard or standard mouse movements to gesture parameters:

Once step 1 and step 2 are linked, the console will display [QUEST UPLINK RECEIVED] indicators, confirming that manual gesture overrides are actively commanding the MAVLink flight deck.
