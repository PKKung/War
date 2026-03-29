using UnityEngine;

public class SafeZoneTrigger : MonoBehaviour
{
    [Header("Heal Settings")]
    public float healAmount = 2f;      // เพิ่มเลือดวิละ 2
    public float healInterval = 1f;    // ระยะเวลาห่างกัน (1 วินาที)
    private float nextHealTime = 0f;

    private void OnTriggerStay(Collider other)
    {
        // --- 1. ระบบ Heal Over Time (ทำงานทุกๆ 1 วินาที) ---
        if (Time.time >= nextHealTime)
        {
            HandleHeal(other);
            nextHealTime = Time.time + healInterval;
        }

        // --- 2. ระบบจัดการ NPC (เข้าโซน/สุ่มจุด/ล็อคตำแหน่ง) ---
        if (other.CompareTag("NPC"))
        {
            HandleNPCInSafeZone(other);
        }
    }

    private void HandleHeal(Collider other)
    {
        // ฮีล NPC
        if (other.CompareTag("NPC"))
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

    private void HandleNPCInSafeZone(Collider other)
    {
        // หา MoveScript และ Animator จากตัวพ่อหรือลูก
        var moveScript = other.GetComponentInParent<NPC_QueryMovement>();
        if (moveScript == null) moveScript = other.GetComponentInChildren<NPC_QueryMovement>();

        var animator = other.GetComponentInParent<Animator>();
        if (animator == null) animator = other.GetComponentInChildren<Animator>();

        if (moveScript != null && animator != null)
        {
            bool isBeingCarried = animator.GetBool("isBeingCarried");

            // ถ้ายังไม่ได้ถูกล็อคเป็น IsSafe
            if (!moveScript.isSafe)
            {
                // เคส A: เดินเข้ามาเอง (ไม่ได้ถูกอุ้ม) -> สั่งสุ่มจุดเดินลึกเข้าไป
                if (!isBeingCarried)
                {
                    moveScript.EnterSafeZone(transform.position, false);
                    Debug.Log("<color=cyan>NPC Walk-in:</color> " + other.name + " is finding a spot.");
                }
                // เคส B: ถูกอุ้มอยู่แล้วเพิ่งถูกปล่อยวางลงพื้น -> สั่งหยุดนิ่งตรงนั้นเลย
                else if (isBeingCarried == false) // เช็คซ้ำเพื่อความชัวร์ตอนจังหวะปล่อย
                {
                    // Logic นี้จะทำงานในเฟรมที่ปล่อยพอดี
                }
            }

            // ตรวจจับจังหวะ "เพิ่งปล่อยวาง" ภายในโซน
            if (!isBeingCarried && !moveScript.isSafe)
            {
                moveScript.EnterSafeZone(transform.position, true); // ส่ง true = หยุดนิ่งตรงที่วาง
                Debug.Log("<color=yellow>NPC Dropped:</color> " + other.name + " locked at current position.");
            }
        }
    }
}