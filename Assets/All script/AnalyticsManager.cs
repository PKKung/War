using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class GameDataPoint
{
    public float time;
    public string eventType; // เพิ่มอันนี้
    public string npcTag;    // เพิ่มอันนี้
    public float sanity;
    public float distanceToSafe; // เพิ่มอันนี้
    public int familyLeft;
    public float playerHP;   // เพิ่มอันนี้
}

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance;

    [Header("Statistics")]
    public int familySavedCount = 0;
    public int deadCount = 0;
    public int totalProcessedNPC = 0;

    [Header("AI Analytics Settings")]
    public int initialFamilyCount = 4;
    private int currentFamilyInScene;

    private string filePath;
    public List<GameDataPoint> sessionHistory = new List<GameDataPoint>();

    void Awake()
    {
        // แก้ไขระบบ Singleton ให้ถูกต้อง ตัว Manager จะไม่หายไปตอนเริ่มเกม
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ล็อกตัวนี้ให้อยู่ยาวทุกฉาก

            // ตั้งค่าเริ่มต้น (รันแค่ครั้งเดียวตอนเปิดเกมครั้งแรก)
            currentFamilyInScene = initialFamilyCount;
            filePath = Application.dataPath + "/PlayerBehaviorLog.csv";

            if (!File.Exists(filePath))
            {
                string header = "Time,EventType,NPCTag,Sanity,DistanceToSafe,FamilyLeft,PlayerHP\n";
                File.WriteAllText(filePath, header);
            }
            Debug.Log("<color=green>✅ [Analytics] Instance Created & Locked.</color>");
        }
        else if (Instance != this)
        {
            // ถ้าโหลดฉากใหม่แล้วเจอตัวซ้ำ ให้ลบตัวที่เพิ่งเกิดทิ้งไป
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // เพิ่มจุดเริ่มต้น (Point 0) เพื่อให้กราฟมีข้อมูลวาดทันที
        if (sessionHistory.Count == 0)
        {
            sessionHistory.Add(new GameDataPoint
            {
                time = 0,
                sanity = 100f,
                familyLeft = initialFamilyCount
            });
            Debug.Log("<color=white>[Analytics] Initialized first data point at Time 0.</color>");
        }
    }

    public void LogEvent(string eventType, string npcTag, float sanity, float distance = 0f, int unusedParam = 0, float playerHP = 0f)
    {
        // 1. คำนวณจำนวนครอบครัวที่เหลือ
        if (npcTag.Contains("Family"))
        {
            if (eventType.Contains("SAVED") || eventType.Contains("DIED"))
            {
                currentFamilyInScene = Mathf.Max(0, currentFamilyInScene - 1);
            }
        }

        string timestamp = Time.time.ToString("F2");

        // 2. บันทึกลงไฟล์ CSV
        string logEntry = $"{timestamp},{eventType},{npcTag},{sanity:F1},{distance:F2},{currentFamilyInScene},{playerHP:F1}";
        try
        {
            File.AppendAllText(filePath, logEntry + "\n");
            Debug.Log($"<color=cyan>[Analytics] Logged to CSV: {logEntry}</color>");
            HandleCounting(eventType, npcTag);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Analytics] CSV Error: {e.Message}");
        }

        // 3. ✨ ส่วนสำคัญ: เพิ่มข้อมูลลง List เพื่อส่งให้กราฟวาด
        sessionHistory.Add(new GameDataPoint
        {
            time = Time.time,
            eventType = eventType,
            npcTag = npcTag,
            sanity = sanity,
            distanceToSafe = distance,
            familyLeft = currentFamilyInScene,
            playerHP = playerHP
        });

        Debug.Log($"<color=yellow>[Analytics] Data added to List. Total points: {sessionHistory.Count}</color>");
    }

    private void HandleCounting(string eventType, string npcTag)
    {
        if (eventType.Contains("DIED"))
        {
            deadCount++;
            totalProcessedNPC++;
        }
        else if (eventType.Contains("SAVED"))
        {
            if (npcTag.Contains("Family")) familySavedCount++;
            totalProcessedNPC++;
        }
    }

    public void ResetCounter()
    {
        totalProcessedNPC = 0;
        familySavedCount = 0;
        deadCount = 0;
        currentFamilyInScene = initialFamilyCount;
    }
}