using UnityEngine;
using UnityEngine.AI;

public class NPCHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Bleeding Settings")]
    public float bleedDamage = 1f; // ลดวิละ 1
    private float bleedTimer = 0f;

    public enum State { Normal, Injured, Down, Dead }

    [Header("Current Status")]
    public State currentState;
    private State lastState;
    private Animator animator;
    private NPC_QueryMovement moveScript; // อ้างอิงสคริปต์เคลื่อนที่เพื่อเช็ก isSafe

    void Start()
    {
        currentHP = maxHP;
        currentState = State.Normal;
        lastState = State.Normal;
        animator = GetComponent<Animator>();
        moveScript = GetComponent<NPC_QueryMovement>(); // ดึงสคริปต์มาเก็บไว้
    }

    void Update()
    {
        UpdateState();
        HandleBleeding();
        SyncAnimator();
    }

    void UpdateState()
    {
        // 1. คำนวณ State ที่ควรจะเป็นในเฟรมนี้ก่อน
        State nextState;

        if (currentHP <= 0)
            nextState = State.Dead;
        else if (currentHP < 50)
            nextState = State.Down;
        else if (currentHP < 75)
            nextState = State.Injured;
        else
            nextState = State.Normal;

        // 2. 🔥 ตรวจสอบ: ถ้า State ใหม่ ไม่เหมือนเดิม ถึงจะเริ่มทำงาน (ป้องกันการรันรัวๆ)
        if (nextState != currentState)
        {
            // บันทึก State เก่าไว้เผื่อใช้ (ถ้าจำเป็น)
            lastState = currentState;
            // อัปเดต State ปัจจุบัน
            currentState = nextState;

            // 3. รัน Logic เฉพาะตอนที่ "เพิ่งเปลี่ยนสถานะ" ครั้งเดียว
            HandleStateChange(currentState);
        }
    }

    // แยก Logic การทำงานออกมาเพื่อให้โค้ดสะอาดและไม่รันซ้ำซ้อน
    void HandleStateChange(State newState)
    {
        switch (newState)
        {
            case State.Dead:
                OnNPCDied();
                break;

            case State.Down:
                Debug.Log(gameObject.name + " เข้าสู่สถานะ Down");
                // สั่งปิดระบบเดินทันทีที่ล้ม
                if (moveScript != null && moveScript.agent != null)
                    moveScript.agent.enabled = false;
                break;

            case State.Injured:
                Debug.Log(gameObject.name + " บาดเจ็บ");
                break;

            case State.Normal:
                Debug.Log(gameObject.name + " กลับสู่สถานะปกติ");
                break;
        }
    }

    // Logic ตอนตาย (ย้ายมาจาก UpdateState เดิม)
    void OnNPCDied()
    {
        float distToPlayer = 0f;
        float sanityTraumaRadius = 25f;
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            distToPlayer = Vector3.Distance(transform.position, player.transform.position);
        }

        // จัดการค่า Sanity
        if (gameObject.CompareTag("Family"))
        {
            SanityManager.Instance.DecreaseSanityFamily();
            Debug.Log($"<color=red>เสียใจรุนแรง: ครอบครัวตาย!</color>");
        }
        else
        {
            if (distToPlayer <= sanityTraumaRadius)
            {
                SanityManager.Instance.DecreaseSanity();
                Debug.Log($"<color=orange>คนแปลกหน้าตายต่อหน้า</color>");
            }
        }

        // Analytics
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.LogEvent("DIED", gameObject.tag, SanityManager.Instance.currentSanity, distToPlayer);
        }
    }

    void HandleBleeding()
    {
        if (currentState == State.Down && currentHP > 0)
        {
            bleedTimer += Time.deltaTime;
            if (bleedTimer >= 1f)
            {
                currentHP -= bleedDamage;
                if (currentHP < 0) currentHP = 0;
                bleedTimer = 0f;
            }
        }
    }

    // --- ฟังก์ชัน Heal ที่เพิ่มเข้ามา ---
    public void Heal(float amount)
    {
        if (currentState == State.Dead) return; // ตายแล้วไม่ฟื้น

        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;

        // ??? เงื่อนไขพิเศษ: ถ้าอยู่ใน Safe Zone และเลือดพ้นสถานะ Down (> 50) ให้ลุกทันที
        if (moveScript != null && moveScript.isSafe && currentHP >= 50f)
        {
            if (animator != null && currentState == State.Down)
            {
                // สั่งให้ข้ามไปท่า Idle (หรือชื่อท่าปกติของคุณ) ทันที
                animator.CrossFadeInFixedTime("Idle", 0.2f);

                // ถ้าคุณมี Parameter แบบ Bool ใน Animator ให้ Reset ตรงนี้ด้วย
                animator.SetBool("isDown", false);
            }
        }
    }

    void SyncAnimator()
    {
        if (currentState != lastState)
        {
            if (animator != null)
                animator.SetInteger("State", (int)currentState);
            lastState = currentState;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;
    }
}