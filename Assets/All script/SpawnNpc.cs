using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    public List<GameObject> npcPrefabs;
    public int spawnCount = 10;

    [Header("Spawn Points Settings")]
    public List<Transform> spawnPoints = new List<Transform>();

    // 🟢 เพิ่มตัวแปรสำหรับจัดการ Family
    [Header("Family Settings")]
    public int familyCount = 4;
    private List<GameObject> allSpawnedNPCs = new List<GameObject>();

    void Start()
    {
        if (spawnPoints.Count == 0)
        {
            foreach (Transform child in transform)
            {
                spawnPoints.Add(child);
            }
        }

        SpawnNPCs();

        // 🟢 หลังจากเสกครบแล้ว ให้สุ่มเลือก Family
        AssignFamilyTags();
    }

    void SpawnNPCs()
    {
        if (spawnPoints.Count == 0 || npcPrefabs.Count == 0)
        {
            Debug.LogWarning("ไม่มีจุดเกิดหรือไม่มี Prefab นะจ๊ะ!");
            return;
        }

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePoints.Count == 0) break;

            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform targetPoint = availablePoints[randomIndex];
            GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Count)];

            GameObject npc = Instantiate(prefab, targetPoint.position, targetPoint.rotation);

            // 🟢 เก็บ NPC ที่เสกออกมาเข้า List กลาง
            allSpawnedNPCs.Add(npc);

            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(targetPoint.position);
            }

            availablePoints.RemoveAt(randomIndex);
        }
    }

    // 🟢 ฟังก์ชันใหม่สำหรับสุ่ม Tag Family
    void AssignFamilyTags()
    {
        if (allSpawnedNPCs.Count == 0) return;

        int actualFamilyToSpawn = Mathf.Min(familyCount, allSpawnedNPCs.Count);
        List<GameObject> pool = new List<GameObject>(allSpawnedNPCs);

        for (int i = 0; i < actualFamilyToSpawn; i++)
        {
            int rndIndex = Random.Range(0, pool.Count);
            GameObject member = pool[rndIndex]; // ดึงตัวที่สุ่มได้ออกมา

            member.tag = "Family";
            member.name += " [FAMILY]";

            // 🔥 ย้ายส่วนนี้เข้ามาข้างใน Loop เพื่อให้เปลี่ยนสีทุกตัวที่ถูกเลือก
            Outline outline = member.GetComponent<Outline>();
            if (outline != null)
            {
                outline.OutlineColor = Color.yellow;
                outline.OutlineWidth = 5f; // เพิ่มความหนาหน่อยจะได้เห็นชัดๆ
                outline.enabled = true;
            }

            pool.RemoveAt(rndIndex);
        } // 👈 ปีกกาปิด Loop ต้องอยู่ตรงนี้

        Debug.Log($"<color=yellow>สุ่ม Family เรียบร้อยแล้ว {actualFamilyToSpawn} ตัว</color>");
    }
}