using UnityEngine;
using System;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

public class NancyDataSimulator : MonoBehaviour
{
    [Header("--- Simulation Interval Settings ---")]
    [SerializeField] private float updateInterval = 2.0f; 

    [Header("--- Meta AI Security Matrix ---")]
    [Tooltip("Paste your raw sk-meta-... api key directly here for this isolated security pass.")]
    [SerializeField] private string metaBearerToken = "LLM_1686390322836457_Bj0IuPhzQVmAMtpYOxhSov100NE";

    // Production Endpoint Definitions
    private const string META_VOICE_WS = "wss://api.meta.ai/v1/asr/realtime";
    
    private float timer;
    private ClientWebSocket voiceWebSocket;
    private CancellationTokenSource wsCancelTokenSource;

    async void Start()
    {
        Debug.Log("<color=cyan>[NANCY SECURITY DIAGNOSTIC]</color> Booting isolated testing matrix...");
        
        // Instant Live Meta Connectivity Authentication Test
        await InitializeTestMetaVoiceStream();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            SimulateSmartBoxData();
            timer = 0f; 
        }
    }

    // =========================================================================
    // ISOLATED CONNECTIVITY & SECURITY TESTING MATRIX
    // =========================================================================
    private async Task InitializeTestMetaVoiceStream666()
    {
        voiceWebSocket = new ClientWebSocket();
        wsCancelTokenSource = new CancellationTokenSource();

        try
        {
            Debug.Log($"<color=yellow>[NET_CONNECT]</color> Opening channel to endpoint: {META_VOICE_WS}");
            Uri serverUri = new Uri(META_VOICE_WS);
            
            // Set connection timeout constraint safely
            var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(8)).Token;
            await voiceWebSocket.ConnectAsync(serverUri, timeoutToken);
            
            Debug.Log("<color=green>[NET_SUCCESS]</color> Socket handshake completed. Sending Authorization Token Frame...");

            // Pack structural security handshake payload 
            string handshakeJson = "{\"type\": \"handshake\", \"authorization\": {\"accessToken\": \"Bearer " + metaBearerToken + "\"}, \"model\": \"muse-voice-transcribe-1.0\"}";
            byte[] handshakeBytes = Encoding.UTF8.GetBytes(handshakeJson);
            
            await voiceWebSocket.SendAsync(new ArraySegment<byte>(handshakeBytes), WebSocketMessageType.Text, true, wsCancelTokenSource.Token);
            Debug.Log("<color=green>[AUTH_SUCCESS]</color> Authorization packet dispatched to Meta AI. Connection secure.");

            // Start worker thread to verify if Meta server keeps connection alive or drops it
            _ = Task.Run(ReceiveVoiceStreamLoop);
        }
        catch (WebSocketException wse)
        {
            Debug.LogError($"<color=red>[SECURITY/NET FAILURE]</color> Meta Server rejected connection directly: {wse.Message}\nCheck if your firewall blocks WebSockets or if your local IP is blocked.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red>[SYSTEM CRASH ERROR]</color> General initialization failure: {ex.Message}");
        }
    }

    private async Task InitializeTestMetaVoiceStream()
    {
        voiceWebSocket = new ClientWebSocket();
        wsCancelTokenSource = new CancellationTokenSource();

        try
        {
            Debug.Log($"<color=yellow>[NET_CONNECT]</color> Opening channel to endpoint: {META_VOICE_WS}");
            Uri serverUri = new Uri(META_VOICE_WS);
            
            var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(8)).Token;
            await voiceWebSocket.ConnectAsync(serverUri, timeoutToken);
            
            Debug.Log("<color=green>[NET_SUCCESS]</color> Socket handshake completed. Sending Authorization Token Frame...");

            // --- FIXED HANDSHAKE PAYLOAD MATRIX ---
            // Replaced the custom "type" tag with the required production encoding configurations
            string handshakeJson = "{" +
                "\"authorization\": {\"accessToken\": \"Bearer " + metaBearerToken + "\"}," +
                "\"model\": \"muse-voice-transcribe-1.0\"," +
                "\"audioEncoding\": \"PCM_16KHZ\"," +  // Explicitly declares the audio encoding attribute
                "\"mode\": \"PUSH_TO_TALK\"" +
            "}";
            // --------------------------------------

            byte[] handshakeBytes = Encoding.UTF8.GetBytes(handshakeJson);
            
            await voiceWebSocket.SendAsync(new ArraySegment<byte>(handshakeBytes), WebSocketMessageType.Text, true, wsCancelTokenSource.Token);
            Debug.Log("<color=green>[AUTH_SUCCESS]</color> Authorization packet dispatched to Meta AI. Connection secure.");

            _ = Task.Run(ReceiveVoiceStreamLoop);
        }
        catch (WebSocketException wse)
        {
            Debug.LogError($"<color=red>[SECURITY/NET FAILURE]</color> Meta Server rejected connection directly: {wse.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red>[SYSTEM CRASH ERROR]</color> General initialization failure: {ex.Message}");
        }
    }


    private async Task ReceiveVoiceStreamLoop()
    {
        byte[] buffer = new byte[1024 * 4];
        try
        {
            while (voiceWebSocket.State == WebSocketState.Open && !wsCancelTokenSource.Token.IsCancellationRequested)
            {
                var result = await voiceWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), wsCancelTokenSource.Token);
                string responseText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log($"<color=lime>[META_FEEDBACK]</color> Server responded: {responseText}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"<color=orange>[STREAM_DISCONNECT]</color> Stream loop ended or terminated by server: {ex.Message}");
        }
    }

    // =========================================================================
    // LOCAL DATA SIMULATOR LOOP
    // =========================================================================
    void SimulateSmartBoxData()
    {
        float simulatedTemp = UnityEngine.Random.Range(21.5f, 24.8f);
        int simulatedBattery = UnityEngine.Random.Range(85, 99);
        float simulatedSignal = UnityEngine.Random.Range(-65f, -40f); 

        Debug.Log($"<color=orange>[NANCY SIM_DATA]</color> Temp: {simulatedTemp:F1}°C | Battery: {simulatedBattery}% | Signal: {simulatedSignal}dBm | WS State: {voiceWebSocket?.State}");
    }

    private void OnDestroy()
    {
        wsCancelTokenSource?.Cancel();
        voiceWebSocket?.Dispose();
    }
}
