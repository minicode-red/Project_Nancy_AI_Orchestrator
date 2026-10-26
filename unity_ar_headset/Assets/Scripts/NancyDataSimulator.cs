using UnityEngine;

public class NancyDataSimulator : MonoBehaviour
{
    [Header("Simulation Settings")]
    [Tooltip("How often (in seconds) should we test data?")]
    [SerializeField] private float updateInterval = 1.0f; 

    private float timer;

    void Start()
    {
        // This prints a message the exact millisecond you press Play
        Debug.Log("<color=cyan>[NANCY SYSTEM]</color> Simulation started. Awaiting hardware connection...");
    }

    void Update()
    {
        //Debug.Log("Iron Man Coming ...");
        timer += Time.deltaTime;

        // Trigger our test data printout every time the interval passes
        if (timer >= updateInterval)
        {
            SimulateSmartBoxData();
            timer = 0f; // Reset the clock
        }
    }

    void SimulateSmartBoxData()
    {
        Debug.Log("Iron Man Coming ... 2");

        // 1. Generate fake, moving data numbers using random ranges
        float simulatedTemp = Random.Range(21.5f, 24.8f);
        int simulatedBattery = Random.Range(85, 99);
        float simulatedSignal = Random.Range(-65f, -40f); // RSSI Signal strength

        // 2. The Log Commands: Print them cleanly to the console window
        // The <color> tags make it look like a high-tech terminal in your console!
        Debug.Log($"<color=orange>[NANCY DATA]</color> Temp: {simulatedTemp:F1}°C | Battery: {simulatedBattery}% | Signal: {simulatedSignal}dBm");

        // 3. Conditional logs (Test alerts)
        if (simulatedBattery < 88)
        {
            Debug.LogWarning("<color=yellow>[SYS_WARN]</color> Battery levels fluctuating below optimal charge!");
        }
    }
}
