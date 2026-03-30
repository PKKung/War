using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIGraphVisualizer : MonoBehaviour
{
    public RectTransform graphContainer; // พื้นที่สีเทา
    public GameObject dotPrefab;        // Prefab จุด (Image)
    public Color graphColor = Color.green;

    public void ShowGraph()
    {
        // 1. ล้างของเก่าทิ้งก่อน
        if (graphContainer != null)
        {
            foreach (Transform child in graphContainer) Destroy(child.gameObject);
        }

        // 2. ใช้ท่า "ค้นหา" แทนการเรียก Instance ตรงๆ (กันเหนียวกรณี Instance เป็น null)
        AnalyticsManager manager = FindObjectOfType<AnalyticsManager>();

        if (manager == null)
        {
            Debug.LogError("❌ [Graph] หา AnalyticsManager ไม่เจอในฉากเลยนาย! ลืมวางหรือเปล่า?");
            return;
        }

        // 3. ดึงข้อมูลมาเช็ค
        List<GameDataPoint> data = manager.sessionHistory;

        if (data == null || data.Count == 0)
        {
            Debug.LogWarning("⚠️ [Graph] เจอ Manager นะ แต่ข้อมูลใน List ว่างเปล่า! (กำลังวาดจุด Test ให้ดูแทน)");
            // ถ้าไม่มีข้อมูลจริงๆ จะวาดจุดมั่ว 5 จุดให้ดูว่าระบบวาด "ยังปกติดี"
            for (int i = 0; i < 5; i++) CreateDot(new Vector2(i * 50, i * 50));
            return;
        }

        Debug.Log($"✅ [Graph] กำลังวาดข้อมูลจริง {data.Count} จุด");

        // 4. ตั้งค่าขนาดพื้นที่วาด
        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;

        // เช็คว่าลืมตั้งขนาด UI ไหม
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("❌ [Graph] กราฟขนาดเป็น 0! ไปเช็ค Width/Height ของ GraphContainer ใน Inspector ด่วน");
            return;
        }

        // 5. คำนวณจุดและลากเส้น
        float maxTime = data[data.Count - 1].time;
        if (maxTime <= 0) maxTime = 1f; // กัน Error หารด้วย 0

        Vector2 lastPos = Vector2.zero;

        for (int i = 0; i < data.Count; i++)
        {
            // คำนวณพิกัด (สัดส่วน 0-1 แล้วคูณขนาดจริง)
            float xNorm = data[i].time / maxTime;
            float yNorm = Mathf.Clamp01(data[i].sanity / 100f);

            Vector2 currentPos = new Vector2(xNorm * width, yNorm * height);

            // สร้างจุด
            CreateDot(currentPos);

            // ถ้าไม่ใช่จุดแรก ให้ลากเส้นเชื่อมจากจุดที่แล้ว
            if (i > 0)
            {
                CreateLine(lastPos, currentPos);
            }
            lastPos = currentPos;
        }
    }

    // ฟังก์ชันสร้างจุด
    GameObject CreateDot(Vector2 pos)
    {
        GameObject go = Instantiate(dotPrefab, graphContainer);
        RectTransform rt = go.GetComponent<RectTransform>();

        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(6, 6); // บังคับขนาดจุดให้เล็ก (กันเขียวเต็มจอ)
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        return go;
    }

    // ฟังก์ชันลากเส้น
    void CreateLine(Vector2 dotA, Vector2 dotB)
    {
        // สร้างวัตถุใหม่ภายใต้ GraphContainer (ซึ่งเป็น UI)
        GameObject go = new GameObject("GraphLine", typeof(Image));
        go.transform.SetParent(graphContainer, false); // false คือห้ามใช้พิกัดโลก
        go.transform.SetAsFirstSibling(); // ให้เส้นอยู่ข้างหลังจุด

        Image img = go.GetComponent<Image>();
        img.color = new Color(0, 1, 0, 0.5f); // สีเขียวใสๆ หน่อย

        RectTransform rt = go.GetComponent<RectTransform>();

        // ตั้งค่า Anchor ให้เริ่มจากซ้ายล่างของ UI
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;

        Vector2 dir = (dotB - dotA).normalized;
        float distance = Vector2.Distance(dotA, dotB);

        // ปรับขนาดเส้น (กว้าง = ระยะห่าง, สูง = ความหนา 3 พิกเซล)
        rt.sizeDelta = new Vector2(distance, 3f);
        rt.anchoredPosition = dotA; // วางตำแหน่งเริ่มที่จุด A
        rt.pivot = new Vector2(0, 0.5f); // ให้จุดหมุนอยู่ต้นเส้น

        // หมุนเส้นให้ชี้ไปหาจุด B
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0, 0, angle);
    }
}