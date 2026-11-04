using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

public class NancyMuseVisionTester : MonoBehaviour
{
    [Header("--- Target Video Stream ---")]
    [Tooltip("Drag your working YOLO RawImage or UI Display component here.")]
    [SerializeField] private RawImage yoloStreamDisplay;

    [Header("--- UI Output Terminal ---")]
    [Tooltip("Drag your TextMeshPro text box here to display the AI's answer.")]
    [SerializeField] private TextMeshProUGUI aiAnalysisOutputText;

    [Header("--- Meta AI Credentials ---")]
    [SerializeField] private string metaBearerToken = "LLM_1686390322836457_Bj0IuPhzQVmAMtpYOxhSov100NE";

    private const string META_SPARK_URL = "https://meta.ai";
    private bool isProcessingInference = false;

    /// <summary>
    /// CALL THIS METHOD FROM A UI BUTTON CLICK OR AN INSTANT TIMER TO RUN THE TEST
    /// </summary>
    public async void TriggerLiveMuseAnalysis()
    {
        if (isProcessingInference) return;
        if (yoloStreamDisplay == null || yoloStreamDisplay.texture == null)
        {
            Debug.LogWarning("[MUSE VISION] Cannot test: YOLO stream texture is missing.");
            return;
        }

        isProcessingInference = true;
        if (aiAnalysisOutputText != null) aiAnalysisOutputText.text = "🤖 MUSE SPARK 1.3: ANALYZING LIVE DRONE STREAM FEED...";

        // 1. Extract bytes safely from your working texture stream
        byte[] imageBytes = ConvertTextureToPNGBytes(yoloStreamDisplay.texture);
        string base64Image = Convert.ToBase64String(imageBytes);

        // 2. Build the production multi-modal JSON frame payload
        string jsonPayload = "{" +
            "\"model\": \"muse-spark-1.3\"," +
            "\"messages\": [{" +
                "\"role\": \"user\"," +
                "\"content\": [" +
                    "{\"type\": \"text\", \"text\": \"Analyze this drone vision frame for human or animal rescue validation. Provide a concise, professional situational report for the rescue operator.\"}," +
                    "{\"type\": \"image_url\", \"image_url\": {\"url\": \"data:image/png;base64," + base64Image + "\"}}" +
                "]" +
            "}]" +
        "}";

        // 3. Send over the web request tunnel
        using (UnityWebRequest request = new UnityWebRequest(META_SPARK_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {metaBearerToken}");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            // 4. Output response feedback metrics cleanly onto your VR canvas panel
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (aiAnalysisOutputText != null) aiAnalysisOutputText.text = request.downloadHandler.text;
                Debug.Log($"<color=lime>[MUSE SUCCESS]</color> Scene interpreted: {request.downloadHandler.text}");
            }
            else
            {
                if (aiAnalysisOutputText != null) aiAnalysisOutputText.text = $"❌ Error {request.responseCode}: Unable to complete reasoning.";
                Debug.LogError($"[MUSE API EXCEPTION] Code: {request.responseCode} | Error: {request.error}");
            }
        }

        isProcessingInference = false;
    }

    private byte[] ConvertTextureToPNGBytes(Texture sourceTex)
    {
        RenderTexture tempRT = RenderTexture.GetTemporary(sourceTex.width, sourceTex.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(sourceTex, tempRT);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = tempRT;

        Texture2D readableTex = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(tempRT);

        byte[] bytes = readableTex.EncodeToPNG();
        Destroy(readableTex);
        return bytes;
    }
}
