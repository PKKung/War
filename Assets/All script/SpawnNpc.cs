using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    public List<GameObject> npcPrefabs;
    public int spawnCount = 10;

    [Header("Spawn Points Settings")]
    // ลาก SpawnPointHolder มาใส่ หรือจะให้มันหาเองก็ได้
    public List<Transform> spawnPoints = new List<Transform>();

    void Start()
    {
        // ถ้าไม่ได้ลากใส่ไว้ ให้มันหาลูกๆ ของมันเองอัตโนมัติ
        if (spawnPoints.Count == 0)
        {
            foreach (Transform child in transform)
            {
                spawnPoints.Add(child);
            }
        }

        SpawnNPCs();
    }

    void SpawnNPCs()
    {
        if (spawnPoints.Count == 0 || npcPrefabs.Count == 0)
        {
            Debug.LogWarning("ไม่มีจุดเกิดหรือไม่มี Prefab นะจ๊ะ!");
            return;
        }

        // สร้าง List สำรองเพื่อไม่ให้ NPC เกิดซ้ำจุดเดิม (ถ้าจุดเกิดมีมากกว่าจำนวน NPC)
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePoints.Count == 0) break; // จุดเกิดหมดแล้ว

            // 1. สุ่มเลือกจุดเกิดจาก List
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform targetPoint = availablePoints[randomIndex];

            // 2. สุ่มเลือก NPC
            GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Count)];

            // 3. สร้าง NPC
            GameObject npc = Instantiate(prefab, targetPoint.position, targetPoint.rotation);

            // 4. เซ็ต NavMeshAgent ให้วาร์ปไปจุดนั้น
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(targetPoint.position);
            }

            // 5. เอาจุดที่ใช้แล้วออก (เพื่อไม่ให้เกิดซ้อนกัน)
            availablePoints.RemoveAt(randomIndex);
        }
    }
}