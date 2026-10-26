using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class HandlerMovement : MonoBehaviour
{
    [Header("Keyboard Controls Tuning")]
    public float movementSpeed = 10.0f;
    private CharacterController controller; 

    [Header("🛸 Iron Man Avionics Network Link")]
    // 📲 TYPE YOUR PHONE HOTSPOT IP HERE IN THE INSPECTOR (hostname -I)
    public string piHotspotIpAddress = "10.16.50.75";
    public int controlTxPort = 15000;
    public Transform questCenterEyeAnchor; // Assign your Main Quest Camera eye channel here

    private UdpClient txClient;
    private IPEndPoint targetPiNode;
    private float nextPacingFrameTime = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        try
        {
            txClient = new UdpClient();
            targetPiNode = new IPEndPoint(IPAddress.Parse(piHotspotIpAddress), controlTxPort);
            Debug.Log($"[FLIGHT CONTROLLER] Transmitter Active targeting Pi at {piHotspotIpAddress}:{controlTxPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NETWORK ERROR] Failed to initialize TX Client: {e.Message}");
        }
    }

    void Update()
    {
        // 1. Keyboard simulation fallback tracking
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    move += Vector3.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  move += Vector3.back;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  move += Vector3.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += Vector3.right;

        if (controller != null && move != Vector3.zero)
        {
            controller.Move(move.normalized * movementSpeed * Time.deltaTime);
        }

        // 2. Head-tilt streaming pacing loop limited strictly to 50 Hz
        if (Time.time >= nextPacingFrameTime)
        {
            nextPacingFrameTime = Time.time + 0.02f;
            ExecuteSpatialTransmission();
        }
    }

    private void ExecuteSpatialTransmission()
    {
        if (questCenterEyeAnchor == null || txClient == null) return;

        // Process physical head-tilt gyroscopic properties
        Vector3 headTilt = questCenterEyeAnchor.localEulerAngles;
        float rawPitch = headTilt.x > 180 ? headTilt.x - 360 : headTilt.x;
        float rawRoll  = headTilt.z > 180 ? headTilt.z - 360 : headTilt.z;

        // Scale throw limits to standard joystick fields (-1.0 to +1.0)
        float normalizedPitch = Mathf.Clamp(rawPitch / 25.0f, -1.0f, 1.0f);
        float normalizedRoll  = Mathf.Clamp(-rawRoll / 25.0f, -1.0f, 1.0f);

        // Fall back to virtual axes mapping for flight testing
        float normalizedThrottle = Input.GetAxis("Vertical");
        float normalizedYaw      = Input.GetAxis("Horizontal");

        try
        {
            // FIXED: Instantiated rigid 20-byte structural boundary buffer arrays
            byte[] controlBlockBytes = new byte[20];
            
            // Unpack data into memory tracks sequentially
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)0xCCDD), 0, controlBlockBytes, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(normalizedPitch), 0, controlBlockBytes, 2, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(normalizedRoll), 0, controlBlockBytes, 6, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(normalizedThrottle), 0, controlBlockBytes, 10, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(normalizedYaw), 0, controlBlockBytes, 14, 4);

            // Calculate validation byte checks
            byte validationSum = 0;
            for (int i = 0; i < 19; i++) validationSum += controlBlockBytes[i];
            
            // FIXED: Targeted final byte array index explicitly
            controlBlockBytes[19] = validationSum;

            // Flash packets over the cellular link channel
            txClient.Send(controlBlockBytes, controlBlockBytes.Length, targetPiNode);
        }
        catch (Exception ex) { Debug.LogWarning($"[LINK DISRUPT] Packet drop: {ex.Message}"); }
    }

    void OnDestroy()
    {
        if (txClient != null) txClient.Close();
    }
}
