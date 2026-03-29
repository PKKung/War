using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SanityManager : MonoBehaviour
{
    public static SanityManager Instance; // ทำเป็น Singleton ให้สคริปต์อื่นเรียกใช้ง่ายๆ

    [Header("Sanity Settings")]
    public float maxSanity = 100f;
    public float currentSanity;
    public float penaltyPerDeath = 10f;  // ตาย 1 คน ลด 10
    public float rewardPerSave = 5f;     // รอด 1 คน เพิ่ม 5

    [Header("UI Settings")]
    public Slider sanitySlider;         // หลอดเลือดค่าสติ
    public TextMeshProUGUI sanityText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentSanity = maxSanity;
        UpdateUI();
    }

    // ฟังก์ชันสำหรับ "ลด" ค่าสติ (เรียกใช้ตอน NPC ตาย)
    public void DecreaseSanity()
    {
        currentSanity -= penaltyPerDeath;
        currentSanity = Mathf.Clamp(currentSanity, 0, maxSanity); // ไม่ให้ต่ำกว่า 0
        UpdateUI();
        Debug.Log("<color=red>[Sanity] NPC ตาย! ค่าสติลดลง</color>");
    }

    // ฟังก์ชันสำหรับ "เพิ่ม" ค่าสติ (เรียกใช้ตอน NPC รอด)
    public void IncreaseSanity()
    {
        currentSanity += rewardPerSave;
        currentSanity = Mathf.Clamp(currentSanity, 0, maxSanity); // ไม่ให้เกิน Max
        UpdateUI();
        Debug.Log("<color=green>[Sanity] ช่วย NPC ได้! ค่าสติเพิ่มขึ้น</color>");
    }

    void UpdateUI()
    {
        if (sanitySlider != null) sanitySlider.value = currentSanity / maxSanity;
        if (sanityText != null) sanityText.text = $"Sanity: {Mathf.RoundToInt(currentSanity)}%";

        // ถ้าค่าสติหมด (ตัวอย่างการจบเกม)
        if (currentSanity <= 0)
        {
            Debug.LogError("Mental Breakdown! Game Over");
        }
    }
}