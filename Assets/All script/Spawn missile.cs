using UnityEngine;
using System.Collections;

public class MissileRain : MonoBehaviour
{
    public BoxCollider zone;
    public GameObject missilePrefab;

    public float spawnHeight = 150f;
    public int missilePerWave = 10;     // ลูกต่อชุด
    public int totalWaves = 10;         // จำนวนรอบ
    public float delayBetweenWaves = 3f;

    public void StartMissileRain()
    {
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        for (int wave = 0; wave < totalWaves; wave++)
        {
            // ยิง 10 ลูก
            for (int i = 0; i < missilePerWave; i++)
            {
                SpawnMissile();
                yield return new WaitForSeconds(0.1f);
                
            }

            // รอก่อนรอบถัดไป
            yield return new WaitForSeconds(delayBetweenWaves);
        }

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

        rb.linearVelocity = Vector3.down * 50f;
        missile.transform.rotation = Quaternion.LookRotation(Vector3.down);
    }
}