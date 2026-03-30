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
    public NavMeshAgent agent; // 🟢 แก้ไข: ตัวแปรสำหรับคุม NavMeshAgent
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
    [Header("Sanity Check")]
    public bool isEscortedByPlayer = false;
    [Header("Carried Settings")]
    private Rigidbody rb;

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
            rb.linearVelocity = Vector3.zero;
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

        // --- 1. ระบบจัดการค่าสติ (Sanity) และการบันทึกข้อมูล (Analytics) ---
        if (wasCarried || isEscortedByPlayer)
        {
            if (SanityManager.Instance != null)
            {
                // เช็กว่าเป็นคนในครอบครัวหรือไม่
                if (gameObject.CompareTag("Family"))
                {
                    // ช่วยครอบครัว (เรียก 2 รอบเพื่อให้รางวัลเยอะกว่าปกติ)
                    SanityManager.Instance.IncreaseSanity();
                    SanityManager.Instance.IncreaseSanity();
                    Debug.Log($"<color=cyan>FAMILY SAVED: {gameObject.name}! ค่าสติฟื้นฟูอย่างมาก</color>");
                }
                else
                {
                    SanityManager.Instance.IncreaseSanity();
                    Debug.Log($"<color=green>NPC SAVED: {gameObject.name}! ค่าสติเพิ่มขึ้น</color>");
                }
            }

            // 🔥 ส่วนการบันทึก Log แบบละเอียด (6 พารามิเตอร์)
            if (AnalyticsManager.Instance != null)
            {
                // 1. ระยะห่าง (เข้า Safe Zone แล้วให้เป็น 0)
                float dist = 0f;

                // 2. นับจำนวนครอบครัวที่เหลือในฉาก (รวมตัวที่เพิ่งช่วยได้นี้ด้วย)
                int famLeft = GameObject.FindGameObjectsWithTag("Family").Length;

                // 3. ดึงเลือดของผู้เล่น (ใส่ 100f ไว้ก่อน หรือดึงจากสคริปต์เลือดของนาย)
                float pHP = 100f;
                /* ถ้ามีสคริปต์เลือดผู้เล่น ให้ใช้แบบนี้:
                   PlayerHealth player = FindObjectOfType<PlayerHealth>();
                   if(player != null) pHP = player.currentHealth;
                */

                // ส่งข้อมูลชุดใหญ่ไปที่ Analytics
                AnalyticsManager.Instance.LogEvent(
                    "SAVED_BY_PLAYER",
                    gameObject.tag,
                    SanityManager.Instance.currentSanity,
                    dist,
                    famLeft,
                    pHP
                );
            }
        }
        else
        {
            // กรณีเดินเข้าโซนเองโดยที่ผู้เล่นไม่ได้ช่วย
            Debug.Log($"<color=yellow>{gameObject.name} entered alone. No Sanity reward.</color>");

            if (AnalyticsManager.Instance != null)
            {
                int famLeft = GameObject.FindGameObjectsWithTag("Family").Length;

                // ส่งข้อมูลชุดใหญ่ (แม้รอดเองก็ต้องเก็บสถิติ)
                AnalyticsManager.Instance.LogEvent(
                    "SAVED_ALONE",
                    gameObject.tag,
                    SanityManager.Instance.currentSanity,
                    0f,
                    famLeft,
                    100f
                );
            }
        }

        // --- 2. ระบบจัดการการเคลื่อนที่หลังเข้าโซนปลอดภัย ---
        isEscortedByPlayer = false; // รีเซ็ตค่าการเดินตาม

        if (wasCarried)
        {
            // ถ้าถูกอุ้มมาวาง ให้หยุดนิ่งตรงที่วางเลย
            StopMovement();
            Debug.Log(gameObject.name + " [Dropped] Standing still in Safe Zone.");
        }
        else
        {
            // ถ้าเดินมาเอง ให้สุ่มจุดเดินลึกเข้าไปข้างในโซนอีกนิดเพื่อให้ดูเป็นธรรมชาติ
            float randomRadius = 5f;
            Vector3 randomPos = zoneCenter + (Random.insideUnitSphere * randomRadius);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, randomRadius, NavMesh.AllAreas))
            {
                if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                {
                    currentCorner = 0;
                    hasPath = true;
                    currentSpeed = walkSpeed; // ใช้ความเร็วเดินปกติ
                    Debug.Log(gameObject.name + " [Walk-in] Moving to a safe spot inside.");
                }
            }
        }
    }

    // --- ส่วนสุ่มทางเดิมของนาย ---
    void SetRandomDestination()
    {
        if (isSafe) return;
        int maxAttempts = 15;
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

    // แก้ไขฟังก์ชันใน NPC_QueryMovement.cs
    void UpdateOutlineStatus()
    {
        if (outlineComponent == null || healthScript == null) return;

        bool shouldShowOutline = (healthScript.currentState == NPCHealth.State.Down || healthScript.currentState == NPCHealth.State.Injured);

        if (outlineComponent.enabled != shouldShowOutline)
        {
            outlineComponent.enabled = shouldShowOutline;

            // ✨ บังคับให้ Mesh อัปเดตขอบเขตใหม่ทุกครั้งที่เปิด Outline
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.allowOcclusionWhenDynamic = false; // ปิดการซ่อนตัวอัตโนมัติ
            }
        }
    }
    public void StartEscorting()
    {
        isEscortedByPlayer = true;
        Debug.Log(gameObject.name + " is now being escorted by Player.");
    }
    [Header("Physics Settings")]
    private Collider npcCollider; // ตัวแปรเก็บ Collider

    // เพิ่มฟังก์ชันนี้เข้าไปใน NPC_QueryMovement
    private void OnTransformParentChanged()
    {
        // ถ้าถูกจับไปเป็นลูกของอะไรบางอย่าง (ถูกอุ้ม)
        if (transform.parent != null)
        {
            Debug.Log("NPC: ตรวจพบการโดนอุ้ม! กำลังปิดระบบเดิน...");

            // เรียกใช้คำสั่งปิดที่เราเขียนไว้
            // (ส่งค่า parent ไปเป็น socket เลย)
            OnPickedUp(transform.parent);
        }
        else
        {
            Debug.Log("NPC: ตรวจพบการปล่อยวาง!");
            OnDropped();
        }
    }

    // ปรับปรุง OnPickedUp เล็กน้อยเพื่อให้รองรับการเรียกแบบนี้
    public void OnPickedUp(Transform socket)
    {
        // เช็ค Component ให้ชัวร์
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (npcCollider == null) npcCollider = GetComponent<Collider>();

        // 1. หยุด NavMesh แน่นอน 100%
        if (agent != null)
        {
            agent.enabled = false;
            Debug.Log("NPC: NavMeshAgent ปิดตัวลงแล้ว");
        }

        // 2. ตั้งค่าฟิสิกส์
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }

        // 3. ปิด Collider กันดีดเข้าตัวผู้เล่น
        if (npcCollider != null) npcCollider.enabled = false;

        // 4. จัดตำแหน่ง (ถ้า socket ไม่ใช่ตัวมันเอง)
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (animator != null) animator.SetBool("isBeingCarried", true);
    }
    // ก๊อปปี้ส่วนนี้ไปวางใน NPC_QueryMovement นะครับ
    public void OnDropped()
    {
        // 1. คืนค่า Collider ให้กลับมาชนได้ปกติ
        if (npcCollider == null) npcCollider = GetComponent<Collider>();
        if (npcCollider != null) npcCollider.enabled = true;

        // 2. คืนค่าฟิสิกส์ ให้ตกตามแรงโน้มถ่วง
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // 3. ปล่อยจากการเป็นลูก (Parent)
        transform.SetParent(null);

        // 4. เปิดระบบเดิน NavMesh กลับมา (ถ้ายังไม่ปลอดภัย)
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null && !isSafe)
        {
            agent.enabled = true;
            // สั่งให้น้องหาที่ไปใหม่ทันที
            SetRandomDestination();
        }

        // 5. บอก Animator ว่าเลิกโดนอุ้มแล้ว
        if (animator != null) animator.SetBool("isBeingCarried", false);
    }
}