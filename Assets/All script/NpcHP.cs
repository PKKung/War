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

    void Start()
    {
        currentHP = maxHP;
        currentState = State.Normal;
        lastState = State.Normal;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        UpdateState();
        HandleBleeding(); // ? เพิ่มระบบเลือดไหล
        SyncAnimator();
    }

    void UpdateState()
    {
        if (currentHP <= 0) currentState = State.Dead;
        else if (currentHP < 50) currentState = State.Down;
        else if (currentHP < 75) currentState = State.Injured;
        else currentState = State.Normal;
    }

    // ? ฟังก์ชันจัดการเลือดไหล
    void HandleBleeding()
    {
        // ถ้าสถานะคือ Down (เลือดต่ำกว่า 50) และยังไม่ตาย
        if (currentState == State.Down && currentHP > 0)
        {
            bleedTimer += Time.deltaTime;

            if (bleedTimer >= 1f) // ครบ 1 วินาที
            {
                currentHP -= bleedDamage; // ลดเลือด
                if (currentHP < 0) currentHP = 0;
                bleedTimer = 0f; // รีเซ็ตตัวนับเวลา

                Debug.Log($"NPC is bleeding... Remaining HP: {currentHP}");
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