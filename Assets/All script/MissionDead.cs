using UnityEngine;
using TMPro;
using UnityEngine.Rendering;

public class DeathCounter : MonoBehaviour
{
    [Header("UI Settings")]
    public TextMeshProUGUI deadText;
    public string prefix = "NPC Dead: ";

    private NPCHealth[] allNPCHealths;
    private int lastCount = -1;
    
    void Start()
    {
        FindHealthScripts();
        InvokeRepeating(nameof(UpdateDeathStatus), 0.5f, 0.5f);
    }

    void FindHealthScripts()
    {
        allNPCHealths = Object.FindObjectsByType<NPCHealth>(FindObjectsSortMode.None);
        Debug.Log($"<color=magenta>[DeathCounter] ตรวจพบสคริปต์เลือดทั้งหมด: {allNPCHealths.Length} ชุด</color>");
    }

    void UpdateDeathStatus()
    {
        if (allNPCHealths == null || allNPCHealths.Length == 0) { FindHealthScripts(); return; }

        int count = 0;
        foreach (var health in allNPCHealths)
        {
            // เช็กสถานะ Dead จาก NPCHealth
            if (health != null && health.currentState == NPCHealth.State.Dead) count++;
        }

        if (count != lastCount)
        {
            lastCount = count;
            if (deadText != null)
            {
                deadText.text = prefix + count.ToString();
                Debug.Log($"<color=red>[DeathCounter] อัปเดตยอดคนตาย: {count}</color>");
            }
            else
            {
                Debug.LogError("!!! ลืมลาก UI Text ใส่ช่อง Dead Text ใน Inspector");
            }
        }
    }
}