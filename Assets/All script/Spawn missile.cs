using UnityEngine;
using System.Collections;

public class MissileRain : MonoBehaviour
{
    public BoxCollider zone;
    public GameObject missilePrefab;

    public float spawnHeight = 150f;
    public int missilePerWave = 10;
    public int totalWaves = 10;
    public float delayBetweenWaves = 3f;

    // 🔥 ตัวแปร global บอกว่า "ระเบิดยังทำงานอยู่"
    public static bool isMissileActive = false;

    public void StartMissileRain()
    {
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        isMissileActive = true; // 🚨 เริ่มระเบิด
        TargetRandomFamilyMember();

        for (int wave = 0; wave < totalWaves; wave++)
        {
            for (int i = 0; i < missilePerWave; i++)
            {
                SpawnMissile();
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(delayBetweenWaves);
        }

        isMissileActive = false; // ✅ ระเบิดจบแล้ว
        if (SimpleBGMController.Instance != null) SimpleBGMController.Instance.FadeToMax();
        Debug.Log("ยิงครบแล้ว!");
    }

    void SpawnMissile()
    {
        Bounds bounds = zone.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        Vector3 spawnPos = new Vector3(x, bounds.max.y + spawnHeight, z);

        GameObject missile = Instantiate(missilePrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = missile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // ✅ เช็กให้ชัวร์ว่าตัวกระสุนเองไม่ได้เผลอตั้งเป็น Kinematic ใน Prefab
            if (rb.isKinematic) rb.isKinematic = false;

            rb.linearVelocity = Vector3.down * 50f;
        }
        missile.transform.rotation = Quaternion.LookRotation(Vector3.down);
    }
    public void TargetRandomFamilyMember()
    {
        // 1. หา NPC ทั้งหมดที่มี Tag ว่า Family
        GameObject[] familyMembers = GameObject.FindGameObjectsWithTag("Family");

        if (familyMembers.Length > 0)
        {
            // 2. สุ่มเลือกมา 1 ตัวเลข index
            int randomIndex = Random.Range(0, familyMembers.Length);
            GameObject victim = familyMembers[randomIndex];

            // 3. ดึงสคริปต์เลือดออกมา (สมมติว่าชื่อ NPC_Health)
            // ถ้าของนายใช้ชื่ออื่น ให้เปลี่ยนตรงนี้นะครับ
            NPCHealth healthScript = victim.GetComponent<NPCHealth>();

            if (healthScript != null)
            {
                // 4. ลดเลือด 51 (หรือตามที่นายต้องการเพื่อให้ Down)
                healthScript.TakeDamage(51f);

                Debug.Log("ระเบิดสุ่มโดน: " + victim.name + " จนล้มลง!");
            }
        }
        else
        {
            Debug.LogWarning("ไม่พบคนในครอบครัว (Tag: Family) ให้สุ่มเลย!");
        }
    }
}