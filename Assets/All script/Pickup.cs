using UnityEngine;

public class PlayerCarry : MonoBehaviour
{
    [Header("Settings")]
    public Transform carrySocket;
    public float interactRange = 2.5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Status")]
    public bool isCarrying = false;
    private NPC_Carriable currentNPC;
    private Animator playerAnimator;

    void Start()
    {
        // ?? เทคนิค: Starter Assets มักจะใส่ Animator ไว้ที่ตัว Model ลูก 
        // ลองใช้ GetComponentInChildren ถ้า GetComponent ปกติหาไม่เจอ
        playerAnimator = GetComponentInChildren<Animator>();
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (!isCarrying) TryPickUp();
            else DropNPC();
        }
    }

    void TryPickUp()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactRange);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("NPC"))
            {
                NPC_Carriable carriable = hit.GetComponent<NPC_Carriable>();
                if (carriable != null && carriable.CanBePickedUp())
                {
                    currentNPC = carriable;
                    PickUp();
                    break;
                }
            }
        }
    }

    void PickUp()
    {
        isCarrying = true;
        currentNPC.OnBeingPickedUp(carrySocket);
        NPC_QueryMovement npcMovement = currentNPC.GetComponent<NPC_QueryMovement>();
        if (npcMovement != null)
        {
            npcMovement.StartEscorting();
        }
        if (playerAnimator != null)
        {
            // ? สั่ง SetBool
            playerAnimator.SetBool("isCarrying", true);

            // ?? บังคับกระโดดไปที่ State ท่าแบกทันที (ชื่อในกล่อง Animator ของนาย)
            // ถ้าใน Animator นายตั้งชื่อสถานะว่า "Carry_Idle" ให้ใส่ชื่อนั้น
            playerAnimator.CrossFadeInFixedTime("Carry_Idle", 0.1f);
        }
    }

    public void DropNPC()
    {
        if (currentNPC == null) return;

        currentNPC.OnBeingDropped();

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isCarrying", false);
        }

        isCarrying = false;
        currentNPC = null;
    }
}