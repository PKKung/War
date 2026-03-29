using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPC_QueryMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 8f;
    public float walkRadius = 20f;

    [Header("Safe Zone")]
    public GameObject safeZone;

    private NavMeshPath path;
    private int currentCorner = 0;
    private bool hasPath = false;
    private Animator animator;
    private Vector3 lastPosition;
    private float currentSpeed;
    private bool wasRunningToSafeZone = false;

    private NPCHealth healthScript;
    private NPCHealth.State lastHealthState;

    void Start()
    {
        path = new NavMeshPath();
        animator = GetComponent<Animator>();
        healthScript = GetComponent<NPCHealth>();

        if (healthScript != null) lastHealthState = healthScript.currentState;

        lastPosition = transform.position;
        currentSpeed = walkSpeed;

        // เริ่มต้นให้สุ่มทางเดินทันที
        SetRandomDestination();
    }

    void Update()
    {
        // 🛑 ป้องกัน Error ถ้าลืมใส่ NPCHealth
        if (healthScript == null) return;
        if (animator.GetBool("isBeingCarried")) return;

        // 🛑 1. เช็กสถานะวิกฤต (ล้ม/ตาย) -> ต้องหยุดเดินทันที
        if (healthScript.currentState == NPCHealth.State.Down || healthScript.currentState == NPCHealth.State.Dead)
        {
            StopMovement();
            return;
        }

        // ✨ 2. ระบบตรวจจับการเปลี่ยนสถานะเลือด (เช่น เพิ่งโดนระเบิดจนบาดเจ็บ)
        if (healthScript.currentState != lastHealthState)
        {
            OnHealthStateChanged();
            lastHealthState = healthScript.currentState;
        }

        // 🏃 3. อัปเดตตรรกะการวิ่งหนีหรือเดินปกติ
        HandleMissileState();
        MoveAlongPath();
        UpdateAnimation();
    }

    void OnHealthStateChanged()
    {
        // เมื่อสถานะเปลี่ยน (เช่น Normal -> Injured) ให้หาทางใหม่ทันทีด้วยความเร็วใหม่
        if (wasRunningToSafeZone) SetSafeZoneDestination();
        else SetRandomDestination();

        Debug.Log($"{gameObject.name}: Health state changed to {healthScript.currentState}. Recalculating...");
    }

    void HandleMissileState()
    {
        bool isMissile = MissileRain.isMissileActive;

        if (isMissile)
        {
            // --- ช่วงมีระเบิดลง ---
            if (healthScript.currentState == NPCHealth.State.Injured)
            {
                currentSpeed = walkSpeed * 0.5f;
                animator.SetBool("isRunning", false);
            }
            else
            {
                // ✅ เพิ่มเงื่อนไข: สั่งวิ่งเฉพาะตอนที่ "ยังมีทางให้ไป" เท่านั้น
                if (hasPath)
                {
                    currentSpeed = runSpeed;
                    animator.SetBool("isRunning", true);
                }
                else
                {
                    // ถ้าถึงที่หมาย (Safe Zone) แล้ว ให้หยุดวิ่ง
                    currentSpeed = 0;
                    animator.SetBool("isRunning", false);
                }
            }

            if (!wasRunningToSafeZone)
            {
                wasRunningToSafeZone = true;
                SetSafeZoneDestination();
            }
        }
        else
        {
            // ... (ส่วนช่วงปกติเหมือนเดิม) ...
            if (wasRunningToSafeZone)
            {
                wasRunningToSafeZone = false;
                animator.SetBool("isRunning", false);
                currentSpeed = (healthScript.currentState == NPCHealth.State.Injured) ? walkSpeed * 0.5f : walkSpeed;
                SetRandomDestination();
            }
        }
    }

    void MoveAlongPath()
    {
        if (!hasPath || path.corners.Length == 0) return;

        Vector3 nextPoint = path.corners[currentCorner];
        Vector3 direction = (nextPoint - transform.position).normalized;

        // เคลื่อนที่ตามความเร็วปัจจุบัน
        transform.position += direction * currentSpeed * Time.deltaTime;

        // หมุนตัวไปตามทิศทาง
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                10f * Time.deltaTime
            );
        }

        // เช็กว่าถึงจุดหักมุมหรือยัง
        if (Vector3.Distance(transform.position, nextPoint) < 0.3f)
        {
            currentCorner++;
            if (currentCorner >= path.corners.Length)
            {
                // ถ้าเดินปกติให้สุ่มต่อ ถ้าหนีระเบิดถึงเซฟโซนแล้วให้หยุด
                if (!MissileRain.isMissileActive) SetRandomDestination();
                else hasPath = false;
                if (animator != null)
                {
                    animator.SetBool("isRunning", false);
                    animator.SetFloat("Speed", 0f);
                }
            }
        }
    }

    void StopMovement()
    {
        hasPath = false;
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("isRunning", false);
        }
    }

    // ==========================================
    // 🎯 ระบบสุ่มทางแบบ "ตื้อไม่เลิก" (จาก Code 1)
    // ==========================================
    void SetRandomDestination()
    {
        int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * walkRadius;
            Vector3 randomPos = new Vector3(random2D.x, 0f, random2D.y) + transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 40f, NavMesh.AllAreas))
            {
                if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                {
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        currentCorner = 0;
                        hasPath = true;
                        return; // เจอทางแล้ว ออกจากฟังก์ชันเลย
                    }
                }
            }
        }

        // ถ้าวน Loop 10 รอบแล้วยังไม่เจอทาง ให้รอ 1 วินาทีแล้วลองใหม่ (นี่คือจุดที่ทำให้เดินครบทุกตัว)
        hasPath = false;
        Invoke(nameof(SetRandomDestination), 1f);
    }

    void SetSafeZoneDestination()
    {
        if (safeZone == null) return;

        BoxCollider box = safeZone.GetComponent<BoxCollider>();
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 center = box.center;
            Vector3 size = box.size;
            float x = Random.Range(-size.x / 2f, size.x / 2f);
            float z = Random.Range(-size.z / 2f, size.z / 2f);
            Vector3 randomPos = safeZone.transform.TransformPoint(new Vector3(x, 0f, z) + center);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 20f, NavMesh.AllAreas))
            {
                if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                {
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        currentCorner = 0;
                        hasPath = true;
                        return;
                    }
                }
            }
        }

        hasPath = false;
    }

    void UpdateAnimation()
    {
        if (animator == null) return;

        // คำนวณความเร็วที่เคลื่อนที่จริง
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        float smoothSpeed = Mathf.Lerp(animator.GetFloat("Speed"), speed, 10f * Time.deltaTime);

        animator.SetFloat("Speed", smoothSpeed);
        lastPosition = transform.position;
    }
}