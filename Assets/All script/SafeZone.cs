using UnityEngine;
using System.Collections.Generic; // เพิ่มตัวนี้เพื่อใช้ List

public class SafeZoneTrigger : MonoBehaviour
{
    [Header("Heal Settings")]
    public float healAmount = 2f;
    public float healInterval = 1f;
    private float timer = 0f;

    // เก็บรายชื่อคนติดอยู่ในโซน
    private List<Collider> entitiesInZone = new List<Collider>();

    void Update()
    {
        // นับเวลาถอยหลังใน Update แทน เพื่อให้แม่นยำ
        timer += Time.deltaTime;

        if (timer >= healInterval)
        {
            HealAllInZone();
            timer = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("NPC") || other.CompareTag("Player") || other.CompareTag("Family"))
        {
            if (!entitiesInZone.Contains(other)) entitiesInZone.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (entitiesInZone.Contains(other)) entitiesInZone.Remove(other);
    }

    private void HealAllInZone()
    {
        // วนลูปฮีลทุกคนที่อยู่ใน List
        for (int i = entitiesInZone.Count - 1; i >= 0; i--)
        {
            Collider other = entitiesInZone[i];

            // ถ้า Object ถูกลบไปแล้ว (เช่น NPC ตายแล้วหายไป) ให้เอาออกจากลิสต์
            if (other == null)
            {
                entitiesInZone.RemoveAt(i);
                continue;
            }

            // ฮีล NPC
            if (other.CompareTag("NPC") || other.CompareTag("Family"))
            {
                var health = other.GetComponentInParent<NPCHealth>();
                if (health == null) health = other.GetComponentInChildren<NPCHealth>();
                if (health != null) health.Heal(healAmount);
            }

            // ฮีล Player
            if (other.CompareTag("Player"))
            {
                var pHealth = other.GetComponentInParent<PlayerHealth>();
                if (pHealth == null) pHealth = other.GetComponentInChildren<PlayerHealth>();
                if (pHealth != null) pHealth.Heal(healAmount);
            }
        }
    }

    // --- ระบบจัดการ NPC (ใส่ไว้ใน OnTriggerStay เหมือนเดิมได้) ---
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("NPC") || other.CompareTag("Family"))
        {
            HandleNPCInSafeZone(other);
        }
    }

    private void HandleNPCInSafeZone(Collider other)
    {
        var moveScript = other.GetComponentInParent<NPC_QueryMovement>();
        if (moveScript == null) moveScript = other.GetComponentInChildren<NPC_QueryMovement>();

        var animator = other.GetComponentInParent<Animator>();
        if (animator == null) animator = other.GetComponentInChildren<Animator>();

        if (moveScript != null && animator != null)
        {
            bool isBeingCarried = animator.GetBool("isBeingCarried");

            if (!moveScript.isSafe && !isBeingCarried)
            {
                // ถ้าไม่ได้ถูกอุ้ม และยังไม่ปลอดภัย (ทั้งเดินมาเอง หรือ เพิ่งถูกวาง)
                // เราส่ง wasCarried ตามสถานะจริง ถ้าเพิ่งถูกวาง ตำแหน่งมันจะนิ่ง
                moveScript.EnterSafeZone(transform.position, false);
            }
        }
    }
}