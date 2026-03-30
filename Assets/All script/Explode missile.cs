using UnityEngine;

public class MissileExplosion : MonoBehaviour
{
    public GameObject explosionPrefab;
    public AudioClip explosionSound;

    [Header("Explosion Damage Settings")]
    public float explosionRadius = 7f; // รัศมีทำดาเมจ (ตาย/เจ็บ)
    public float maxDamage = 100f;      // ดาเมจแรงสุด

    [Header("Sanity Panic Settings")]
    [Tooltip("ระยะที่ระเบิดตกแล้วยังทำให้ผู้เล่นเสียสติ (ควรมากกว่ารัศมีดาเมจ)")]
    public float sanityPanicRadius = 20f;
    [Tooltip("ค่าสติที่ลดลงมากที่สุดเมื่อระเบิดลงข้างตัวพอดี")]
    public float maxSanityLoss = 20f;
    [Tooltip("ค่าสติที่ลดลงน้อยที่สุดเมื่ออยู่เกือบสุดขอบรัศมี")]
    public float minSanityLoss = 2f;

    AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        float height = transform.position.y;
        float volume = Mathf.Clamp01(1f - (height / 150f));
        if (audioSource != null) audioSource.volume = volume;
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    void Explode()
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        // 1. คำนวณดาเมจให้ NPC และ Player (ของเดิม)
        ApplyDamage();

        // 2. ✨ คำนวณการลดค่าสติของผู้เล่นตามระยะห่าง (เพิ่มใหม่)
        ApplySanityPanic();

        Destroy(gameObject);
    }

    void ApplyDamage()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in colliders)
        {
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            float damageMultiplier = 1f - Mathf.Clamp01(distance / explosionRadius);
            float finalDamage = maxDamage * damageMultiplier;

            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(Mathf.Max(finalDamage, 5f));
                continue;
            }

            NPCHealth npcHealth = hit.GetComponent<NPCHealth>();
            if (npcHealth != null)
            {
                npcHealth.TakeDamage(Mathf.Max(finalDamage, 5f));
            }
        }
    }

    // 🔥 ฟังก์ชันใหม่: คำนวณการลดค่าสติแบบละเอียด
    void ApplySanityPanic()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // เช็กว่าอยู่ในรัศมีที่ทำให้สติลดไหม
        if (distToPlayer <= sanityPanicRadius)
        {
            // คำนวณค่า T (0 = ใกล้มาก, 1 = ขอบวง)
            float t = Mathf.Clamp01(distToPlayer / sanityPanicRadius);

            // ใช้ Lerp เพื่อหาค่าลดสติ: ยิ่งใกล้ ยิ่งลดเข้าใกล้ maxSanityLoss
            float finalSanityLoss = Mathf.Lerp(maxSanityLoss, minSanityLoss, t);

            if (SanityManager.Instance != null)
            {
                SanityManager.Instance.currentSanity -= finalSanityLoss;

                // ป้องกันสติไม่ให้ติดลบ
                if (SanityManager.Instance.currentSanity < 0)
                    SanityManager.Instance.currentSanity = 0;

                Debug.Log($"<color=yellow>[Explosion Panic] ระยะ: {distToPlayer:F1}m | ลดสติ: {finalSanityLoss:F1}</color>");
            }

            // ส่งข้อมูลไปให้ AI วิเคราะห์ (ความกดดันจากสิ่งแวดล้อม)
            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.LogEvent("EXPLOSION_PANIC", "Explosive", SanityManager.Instance.currentSanity, distToPlayer);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // วงกลมสีแดง = รัศมีดาเมจ (อันตรายถึงชีวิต)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // วงกลมสีเหลือง = รัศมีลดค่าสติ (ความกดดันทางจิตใจ)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sanityPanicRadius);
    }
}