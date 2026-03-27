using UnityEngine;

public class MissileRain : MonoBehaviour
{
    public BoxCollider zone;
    public GameObject missilePrefab;

    public float spawnHeight = 150f;
    public int missileCount = 10;

    void Start()
    {
        for (int i = 0; i < missileCount; i++)
        {
            SpawnMissile();
        }
    }

    void SpawnMissile()
    {
        Bounds bounds = zone.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);

        Vector3 spawnPos = new Vector3(x, bounds.max.y + spawnHeight, z);

        GameObject missile = Instantiate(missilePrefab, spawnPos, Quaternion.identity);
        Rigidbody rb = missile.GetComponent<Rigidbody>();

        // ให้ตกลง + หมุนให้เข้าแนว
        rb.linearVelocity = Vector3.down * 50f;
        missile.transform.rotation = Quaternion.LookRotation(Vector3.down);
    }
}