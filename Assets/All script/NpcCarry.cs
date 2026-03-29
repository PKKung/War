using UnityEngine;

public class NPC_Carriable : MonoBehaviour
{
    private Animator animator;
    private MonoBehaviour movementScript; // อ้างอิงสคริปต์เดิน
    private Collider npcCollider;
    private Rigidbody rb;
    private NPCHealth health;

    void Awake()
    {
        animator = GetComponent<Animator>();
        npcCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        health = GetComponent<NPCHealth>();

        // ดึงสคริปต์เดินมาเก็บไว้ (ชื่อคลาสต้องตรงกับไฟล์สคริปต์เดินของนาย)
        movementScript = GetComponent("NPC_QueryMovement") as MonoBehaviour;
    }

    // ฟังก์ชันเช็คว่าแบกได้ไหม
    public bool CanBePickedUp()
    {
        if (health == null) return false;
        // แบกได้เฉพาะตอนบาดเจ็บ (Injured) หรือ ล้ม (Down)
        return (health.currentState == NPCHealth.State.Injured || health.currentState == NPCHealth.State.Down);
    }

    public void OnBeingPickedUp(Transform socket)
    {
        // 1. ปิดระบบเดิน
        if (movementScript != null) movementScript.enabled = false;

        // 2. ปิดฟิสิกส์และการชน
        if (npcCollider != null) npcCollider.enabled = false;
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 3. เข้าไปติดที่ไหล่ผู้เล่น
        transform.SetParent(socket);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 4. เล่น Animation โดนแบก
        if (animator != null)
        {
            animator.SetBool("isBeingCarried", true);
            animator.SetFloat("Speed", 0);

        }
        Debug.Log("NPC: I am being carried now!");
        animator.SetBool("isBeingCarried", true);
    }

    public void OnBeingDropped()
    {
        // 1. ออกจากไหล่ผู้เล่น
        transform.SetParent(null);

        // 2. เปิดการชนและฟิสิกส์คืน
        if (npcCollider != null) npcCollider.enabled = true;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // 3. เปิดระบบเดินคืน (มันจะไปเช็คสถานะเลือดเองว่าควรเดินหรือนอนต่อ)
        if (movementScript != null) movementScript.enabled = true;

        // 4. เลิกเล่นท่าโดนแบก
        if (animator != null)
        {
            animator.SetBool("isBeingCarried", false);
        }
    }
}