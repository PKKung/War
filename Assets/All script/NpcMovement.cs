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
    private NavMeshAgent agent; // 🟢 แก้ไข: ตัวแปรสำหรับคุม NavMeshAgent
    private NavMeshPath path;
    private int currentCorner = 0;
    private bool hasPath = false;
    private Animator animator;
    private Vector3 lastPosition;
    private float currentSpeed;
    private bool wasRunningToSafeZone = false;
    public bool isSafe = false;

    private NPCHealth healthScript;
    private NPCHealth.State lastHealthState;
    private Outline outlineComponent;

    void Start()
    {
        // 🛠️ 1. ดึง Component Agent มาเก็บไว้ก่อน กัน Error
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
        animator = GetComponent<Animator>();
        healthScript = GetComponent<NPCHealth>();

        if (healthScript != null) lastHealthState = healthScript.currentState;

        lastPosition = transform.position;
        currentSpeed = walkSpeed;

        outlineComponent = GetComponent<Outline>();
        if (outlineComponent != null) outlineComponent.enabled = false;

        // เริ่มต้นให้สุ่มทางเดินทันที
        SetRandomDestination();
    }

    void Update()
    {
        if (healthScript == null) return;
        UpdateOutlineStatus();

        // 🛑 ถ้าถูกอุ้มอยู่ ไม่ต้องทำอะไรเลย
        if (animator.GetBool("isBeingCarried")) return;

        // 🛑 เช็กสถานะวิกฤต (ล้ม/ตาย) หรือปลอดภัยแล้วแต่ถึงที่หมายแล้ว -> หยุดเดิน
        // แก้ไข: เอา isSafe ออกจากตรงนี้เพื่อให้มันเดินไปจุดสุ่มข้างในก่อนค่อยหยุด
        if (healthScript.currentState == NPCHealth.State.Down || healthScript.currentState == NPCHealth.State.Dead)
        {
            StopMovement();
            return;
        }

        // ✨ ระบบตรวจจับการเปลี่ยนสถานะเลือด
        if (healthScript.currentState != lastHealthState)
        {
            OnHealthStateChanged();
            lastHealthState = healthScript.currentState;
        }

        // 🏃 อัปเดตตรรกะการเคลื่อนที่
        HandleMissileState();
        MoveAlongPath();
        UpdateAnimation();
        
    }

    void OnHealthStateChanged()
    {
        if (healthScript.currentState == NPCHealth.State.Down || healthScript.currentState == NPCHealth.State.Dead)
        {
            StopMovement();
            return;
        }

        // ถ้าปลอดภัยแล้ว ไม่ต้องสุ่มจุดใหม่ตามสถานะเลือด
        if (isSafe) return;

        if (wasRunningToSafeZone) SetSafeZoneDestination();
        else SetRandomDestination();
    }

    void HandleMissileState()
    {
        if (isSafe) return; // ถ้าปลอดภัยแล้ว ไม่ต้องวิ่งหนีระเบิดอีก

        bool isMissile = MissileRain.isMissileActive;

        if (isMissile)
        {
            if (healthScript.currentState == NPCHealth.State.Injured)
            {
                currentSpeed = walkSpeed * 0.5f;
                animator.SetBool("isRunning", false);
            }
            else
            {
                if (hasPath)
                {
                    currentSpeed = runSpeed;
                    animator.SetBool("isRunning", true);
                }
                else
                {
                    currentSpeed = 0;
                    animator.SetBool("isRunning", false);
                }
            }

            if (!wasRunningToSafeZone)
            {
                wasRunningToSafeZone = true;
                CancelInvoke(nameof(SetRandomDestination));
                SetSafeZoneDestination();
            }
        }
    }

    void MoveAlongPath()
    {
        // ถ้านิ่งแล้ว หรือไม่มีทาง ก็ไม่ต้องเคลื่อนที่
        if (!hasPath || path.corners.Length == 0) return;

        Vector3 nextPoint = path.corners[currentCorner];
        Vector3 direction = (nextPoint - transform.position).normalized;

        transform.position += direction * currentSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                10f * Time.deltaTime
            );
        }

        if (Vector3.Distance(transform.position, nextPoint) < 0.3f)
        {
            currentCorner++;
            if (currentCorner >= path.corners.Length)
            {
                hasPath = false;

                // 🟢 ถ้าถึงที่หมายตอน Safe แล้ว ให้หยุดนิ่งถาวร
                if (isSafe)
                {
                    StopMovement();
                }
                else if (!MissileRain.isMissileActive)
                {
                    SetRandomDestination();
                }
            }
        }
    }

    public void StopMovement()
    {
        hasPath = false;
        CancelInvoke(nameof(SetRandomDestination));

        // 🛠️ ปิด Agent และหยุดความเร็วทั้งหมด
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("isRunning", false);
        }
    }

    // 🟢 ฟังก์ชันใหม่: จัดการตอนเข้า Safe Zone (แบบที่นายต้องการ)
    public void EnterSafeZone(Vector3 zoneCenter, bool wasCarried)
    {
        if (isSafe) return; // กันรันซ้ำ
        isSafe = true;

        if (wasCarried)
        {
            // 🛑 อุ้มมาวาง = หยุดตรงนั้นเลย
            StopMovement();
            Debug.Log(gameObject.name + " [Dropped] Standing still.");
        }
        else
        {
            // 🚶 เดินมาเอง = สุ่มจุดลึกเข้าไปข้างใน
            float randomRadius = 5f;
            Vector3 randomPos = zoneCenter + (Random.insideUnitSphere * randomRadius);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, randomRadius, NavMesh.AllAreas))
            {
                // ใช้ระบบ Path เดิมของนายในการเดินไปจุดสุ่ม
                if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                {
                    currentCorner = 0;
                    hasPath = true;
                    currentSpeed = walkSpeed; // เดินไปชิลๆ ข้างใน
                    Debug.Log(gameObject.name + " [Walk-in] Moving deeper...");
                }
            }
        }
    }

    // --- ส่วนสุ่มทางเดิมของนาย ---
    void SetRandomDestination()
    {
        if (isSafe) return;
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
                        return;
                    }
                }
            }
        }
        hasPath = false;
        Invoke(nameof(SetRandomDestination), 1f);
    }

    void SetSafeZoneDestination()
    {
        if (isSafe || safeZone == null) return;
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
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        float smoothSpeed = Mathf.Lerp(animator.GetFloat("Speed"), speed, 10f * Time.deltaTime);
        animator.SetFloat("Speed", smoothSpeed);
        lastPosition = transform.position;
    }

    void UpdateOutlineStatus()
    {
        if (outlineComponent == null || healthScript == null) return;
        if (healthScript.currentState == NPCHealth.State.Down || healthScript.currentState == NPCHealth.State.Injured)
            outlineComponent.enabled = true;
        else
            outlineComponent.enabled = false;
    }
}