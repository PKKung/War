using UnityEngine;
using TMPro;

public class MissionManager : MonoBehaviour
{
    [Header("UI Settings")]
    public TextMeshProUGUI savedCountText;
    public string prefixText = "NPC Saved: ";

    private NPC_QueryMovement[] allNPCs;
    private int lastCount = -1;

    void Start()
    {
        // 🔍 จุดเช็คที่ 1: ลองหา NPC แบบละเอียดขึ้น
        FindAllNPCs();

        InvokeRepeating(nameof(CountSavedNPCs), 0.5f, 0.5f);
    }

    void FindAllNPCs()
    {
        allNPCs = Object.FindObjectsByType<NPC_QueryMovement>(FindObjectsSortMode.None);
        Debug.Log($"<color=cyan>[Mission] เจอ NPC ทั้งหมดในฉาก: {allNPCs.Length} ตัว</color>");

        if (allNPCs.Length == 0)
        {
            Debug.LogError("!!! ไม่เจอ NPC เลยแม้แต่ตัวเดียว เช็คว่า NPC มีสคริปต์ NPC_QueryMovement หรือยัง?");
        }
    }

    void CountSavedNPCs()
    {
        // 🔍 จุดเช็คที่ 2: ถ้าตอนเริ่มหาไม่เจอ ให้ลองหาใหม่เรื่อยๆ (เผื่อ NPC โหลดมาช้า)
        if (allNPCs == null || allNPCs.Length == 0)
        {
            FindAllNPCs();
            return;
        }

        int currentSaved = 0;
        foreach (var npc in allNPCs)
        {
            if (npc != null && npc.isSafe)
            {
                currentSaved++;
            }
        }

        if (currentSaved != lastCount)
        {
            lastCount = currentSaved;
            UpdateUI(currentSaved);
        }
    }

    void UpdateUI(int count)
    {
        Debug.Log($"<color=green>[Mission] กำลังส่งค่าไป UI: {count}</color>");

        if (savedCountText != null)
        {
            savedCountText.text = prefixText + count.ToString();
        }
        else
        {
            Debug.LogError("!!! ลืมลาก TextMeshPro ในหน้า Inspector มาใส่ช่อง Saved Count Text หรือเปล่า?");
        }
    }
}