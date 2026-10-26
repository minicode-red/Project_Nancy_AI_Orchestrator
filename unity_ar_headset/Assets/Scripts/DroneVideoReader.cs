using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DroneVideoReader : MonoBehaviour
{
    [Header("Network Configuration")]
    [SerializeField] private string streamUrl = "http://10.94.70.75:8080/stream.mjpg";

    [Header("UI Target Allocation")]
    [SerializeField] private RawImage displayImage;

    [Header("Diagnostic Heartbeat Indicator")]
    [SerializeField] private MeshRenderer diagnosticCubeRenderer;

    private Texture2D _videoTexture;
    private HttpClient _httpClient;
    private CancellationTokenSource _cts;
    private byte[] _latestFrameBytes;
    private bool _frameReady = false;
    private readonly object _lockObject = new object();

    private enum StreamStatus { Idle, Connected, Failed }
    private StreamStatus _currentStatus = StreamStatus.Idle;
    private bool _statusChanged = false;

    void Start()
    {
        _videoTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (displayImage != null)
        {
            displayImage.texture = _videoTexture;
        }

        UpdateCubeColor(Color.yellow);

        _cts = new CancellationTokenSource();
        Task.Run(() => StreamConsumerLoop(_cts.Token));
    }

    void Update()
    {
        if (_frameReady)
        {
            byte[] bytesToLoad = null;
            lock (_lockObject)
            {
                bytesToLoad = _latestFrameBytes;
                _frameReady = false;
            }

            if (bytesToLoad != null && _videoTexture != null)
            {
                _videoTexture.LoadImage(bytesToLoad);
                _videoTexture.Apply();
            }
        }

        if (_statusChanged)
        {
            _statusChanged = false;
            switch (_currentStatus)
            {
                case StreamStatus.Idle:
                    UpdateCubeColor(Color.yellow);
                    break;
                case StreamStatus.Connected:
                    UpdateCubeColor(Color.green);
                    break;
                case StreamStatus.Failed:
                    UpdateCubeColor(Color.red);
                    break;
            }
        }
    }

    private async Task StreamConsumerLoop(CancellationToken token)
    {
        try
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);

            using (var response = await _httpClient.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead, token))
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                MemoryStream frameBuffer = new MemoryStream();
                
                // FIXED TYPO: Properly allocated array size buffer blocks
                byte[] buffer = new byte[65536]; 

                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0) break;

                    frameBuffer.Write(buffer, 0, bytesRead);
                    byte[] currentBuffer = frameBuffer.ToArray();

                    int soi = FindBytes(currentBuffer, new byte[] { 0xFF, 0xD8 });
                    int eoi = FindBytes(currentBuffer, new byte[] { 0xFF, 0xD9 }, soi);

                    if (soi != -1 && eoi != -1)
                    {
                        if (_currentStatus != StreamStatus.Connected)
                        {
                            _currentStatus = StreamStatus.Connected;
                            _statusChanged = true;
                        }

                        int frameLength = (eoi + 2) - soi;
                        byte[] completeFrame = new byte[frameLength];
                        Array.Copy(currentBuffer, soi, completeFrame, 0, frameLength);

                        lock (_lockObject)
                        {
                            _latestFrameBytes = completeFrame;
                            _frameReady = true;
                        }

                        frameBuffer.SetLength(0);
                        int remaining = currentBuffer.Length - (eoi + 2);
                        if (remaining > 0)
                        {
                            frameBuffer.Write(currentBuffer, eoi + 2, remaining);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[NETWORK ERROR] Exception caught: {ex.Message}");
            _currentStatus = StreamStatus.Failed;
            _statusChanged = true;
        }
    }

    private void UpdateCubeColor(Color color)
    {
        if (diagnosticCubeRenderer != null && diagnosticCubeRenderer.material != null)
        {
            diagnosticCubeRenderer.material.color = color;
        }
    }

    private int FindBytes(byte[] src, byte[] find, int startIdx = 0)
    {
        if (src == null || find == null || startIdx == -1 || startIdx >= src.Length) return -1;
        for (int i = startIdx; i <= src.Length - find.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < find.Length; j++)
            {
                if (src[i + j] != find[j]) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    void OnDestroy()
    {
        _cts?.Cancel();
        _httpClient?.Dispose();
        if (_videoTexture != null) Destroy(_videoTexture);
    }
}
