using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI; // ต้องมีตัวนี้เพราะเราจะใช้ Warp

public class NPCSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    public List<GameObject> npcPrefabs; // ลากตัวละคร 10 แบบมาใส่
    public int spawnCount = 10;

    [Header("Spawn Area")]
    public GameObject warZone; // Object ที่มี Box Collider

    void Start()
    {
        SpawnNPCs();
    }

    void SpawnNPCs()
    {
        if (warZone == null || npcPrefabs.Count == 0) return;

        BoxCollider collider = warZone.GetComponent<BoxCollider>();
        if (collider == null) return;

        Bounds bounds = collider.bounds;

        for (int i = 0; i < spawnCount; i++)
        {
            // 1. สุ่ม X และ Z ภายในขอบเขตของกล่อง
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            // --- ส่วนที่แก้ไข: การสุ่ม Y Position ---
            // บังคับให้จุดต่ำสุดไม่น้อยกว่า 0 (แก้ปัญหา Y ติดลบ)
            float minY = Mathf.Max(bounds.min.y, 3f);
            // บังคับให้จุดสูงสุดไม่เกิน 6
            float maxY = Mathf.Min(bounds.max.y, 6f);

            // สุ่มค่า Y ระหว่างช่วงที่กำหนดไว้ข้างบน
            float randomY = Random.Range(minY, maxY);
            // ---------------------------------------

            Vector3 spawnPos = new Vector3(randomX, randomY, randomZ);

            // 3. สุ่มเลือกตัวละคร
            GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Count)];

            // 4. สร้างตัวละคร
            GameObject npc = Instantiate(prefab, spawnPos, Quaternion.identity);

            // 5. วาร์ปลง NavMesh
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(spawnPos);
            }
        }
    }
}
    
