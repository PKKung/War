using UnityEngine;

public class MissileExplosion : MonoBehaviour
{
    public GameObject explosionPrefab;
    public AudioClip explosionSound;

    [Header("Explosion Settings")]
    public float explosionRadius = 7f; // รัศมีระเบิด
    public float maxDamage = 100f;      // ดาเมจแรงสุด (ตรงกลาง)

    AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        float height = transform.position.y;
        // ยิ่งใกล้พื้น → เสียงดังขึ้น (อิงตามความสูง 150m ตามที่คุณตั้งไว้)
        float volume = Mathf.Clamp01(1f - (height / 150f));
        if (audioSource != null) audioSource.volume = volume;
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    void Explode()
    {
        // 1. สร้าง Effect ระเบิด
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // 2. เสียงระเบิด
        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        // 3. ✨ ระบบสร้างดาเมจแบบรัศมี (Area of Effect)
        ApplyDamage();

        // 4. ทำลายลูกระเบิด
        Destroy(gameObject);
    }

    void ApplyDamage()
    {
        // หาวัตถุที่มี Collider ทั้งหมดในรัศมีระเบิด
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in colliders)
        {
            // คำนวณความห่างและดาเมจพื้นฐาน
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            float damageMultiplier = 1f - Mathf.Clamp01(distance / explosionRadius);
            float finalDamage = maxDamage * damageMultiplier;

            // --- 1. เช็กว่าเป็น Player หรือไม่ ---
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // ส่งดาเมจไปที่สคริปต์ PlayerHealth (ขั้นต่ำ 5)
                playerHealth.TakeDamage(Mathf.Max(finalDamage, 5f));
                Debug.Log($"<color=cyan>Hit Player!</color> Damage: {finalDamage}");
                continue; // ถ้าเจอ Player แล้วให้ข้ามไปเช็กตัวถัดไปเลย ไม่ต้องเช็ก NPC ซ้ำในตัวเดียวกัน
            }

            // --- 2. เช็กว่าเป็น NPC หรือไม่ ---
            NPCHealth npcHealth = hit.GetComponent<NPCHealth>();
            if (npcHealth != null)
            {
                npcHealth.TakeDamage(Mathf.Max(finalDamage, 5f));
                Debug.Log($"Hit NPC: {hit.name} | Damage: {finalDamage}");
            }
        }
    }

    // 🎨 วาดวงกลมสีแดงในหน้า Scene เพื่อให้ Dev เห็นรัศมีระเบิดง่ายขึ้น
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}