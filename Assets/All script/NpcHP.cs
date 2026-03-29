using UnityEngine;

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
        if (currentHP <= 0) currentState = State.Dead;
        else if (currentHP < 50) currentState = State.Down;
        else if (currentHP < 75) currentState = State.Injured;
        else currentState = State.Normal;
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