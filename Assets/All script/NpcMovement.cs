using UnityEngine;
using UnityEngine.AI;

public class NPC_QueryMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 8f;
    public float walkRadius = 20f;

    [Header("Target")]
    public GameObject safeZone;

    private NavMeshPath path;
    private int currentCorner = 0;
    private bool hasPath = false;

    private Animator animator;
    private Vector3 lastPosition;

    private AudioSource sirenAudioSource;
    private bool wasSirenPlaying = false;

    private float currentSpeed;

    void Start()
    {
        path = new NavMeshPath();
        animator = GetComponent<Animator>();

        lastPosition = transform.position;
        currentSpeed = walkSpeed;

        GameObject sirenObj = GameObject.Find("Siren");
        if (sirenObj != null)
            sirenAudioSource = sirenObj.GetComponent<AudioSource>();

        SetRandomDestination();
    }

    void Update()
    {
        HandleSiren();
        MoveAlongPath();
        UpdateAnimation();
    }

    // =========================
    // 🚨 Siren Logic
    // =========================
    void HandleSiren()
    {
        bool isSirenPlaying = (sirenAudioSource != null && sirenAudioSource.isPlaying);

        if (isSirenPlaying)
        {
            if (!wasSirenPlaying)
            {
                wasSirenPlaying = true;

                if (safeZone != null)
                {
                    currentSpeed = runSpeed;
                    animator.SetBool("isRunning", true);

                    CalculatePath(safeZone.transform.position);
                }
            }
        }
        else
        {
            if (wasSirenPlaying)
            {
                wasSirenPlaying = false;

                currentSpeed = walkSpeed;
                animator.SetBool("isRunning", false);

                SetRandomDestination();
            }
        }
    }

    // =========================
    // 🚶 Movement
    // =========================
    void MoveAlongPath()
    {
        if (!hasPath || path.corners.Length == 0) return;

        Vector3 nextPoint = path.corners[currentCorner];
        Vector3 direction = (nextPoint - transform.position).normalized;

        // เคลื่อนที่เอง
        transform.position += direction * currentSpeed * Time.deltaTime;

        // หมุนตัว
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                10f * Time.deltaTime
            );
        }

        // ถึง corner ยัง
        if (Vector3.Distance(transform.position, nextPoint) < 0.3f)
        {
            currentCorner++;

            if (currentCorner >= path.corners.Length)
            {
                if (!wasSirenPlaying)
                {
                    SetRandomDestination();
                }
                else
                {
                    hasPath = false;
                }
            }
        }
    }

    // =========================
    // 🎯 Pathfinding
    // =========================
    void CalculatePath(Vector3 target)
    {
        if (NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, path))
        {
            currentCorner = 0;
            hasPath = true;
        }
    }

    void SetRandomDestination()
    {
        Vector2 random2D = Random.insideUnitCircle * walkRadius;

        Vector3 randomPos = new Vector3(
            random2D.x,
            0f,
            random2D.y
        ) + transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPos, out hit, 25f, NavMesh.AllAreas))
        {
            CalculatePath(hit.position);
        }
    }

    // =========================
    // 🎬 Animation
    // =========================
    void UpdateAnimation()
    {
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;

        // ทำให้เนียน
        float smoothSpeed = Mathf.Lerp(animator.GetFloat("Speed"), speed, 10f * Time.deltaTime);

        animator.SetFloat("Speed", smoothSpeed);

        lastPosition = transform.position;
    }
}