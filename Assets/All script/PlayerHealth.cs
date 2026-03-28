using UnityEngine;
using StarterAssets;
using Unity.Cinemachine; // สำหรับคุมกล้องเวอร์ชันใหม่

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float crawlThreshold = 50f;

    [Header("Camera Settings")]
    [Tooltip("ลาก PlayerCameraRoot มาใส่ที่นี่")]
    public GameObject cameraRoot;
    [Tooltip("ลาก PlayerFollowCamera มาใส่ที่นี่ (ถ้าใช้ Cinemachine)")]
    public CinemachineCamera vCam;

    public float normalCameraHeight = 1.375f;
    public float crawlCameraHeight = 0.4f;
    public float fallTiltZ = -15f;
    public float transitionSpeed = 6f;

    [Header("First Person Offset")]
    public float normalZOffset = 0f;
    public float crawlZOffset = 0.2f;

    private Animator _animator;
    private FirstPersonController _controller;
    private CharacterController _charController;
    private StarterAssetsInputs _input; // ตัวแปรสำหรับคุมปุ่ม Shift
    private CinemachineThirdPersonFollow _vCamFollow;

    private float _targetCameraY;
    private float _targetCameraZ;
    private bool _isDown = false;

    void Awake()
    {
        currentHealth = maxHealth;
        _animator = GetComponent<Animator>();
        _controller = GetComponent<FirstPersonController>();
        _charController = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();

        if (vCam != null)
        {
            _vCamFollow = vCam.GetComponent<CinemachineThirdPersonFollow>();
        }

        _targetCameraY = normalCameraHeight;
        _targetCameraZ = 0f;
    }

    void Update()
    {
        // 1. ระบบตรวจจับสถานะจากเลือด
        if (currentHealth < crawlThreshold && !_isDown)
        {
            SetCrawlState(true);
        }
        else if (currentHealth >= crawlThreshold && _isDown)
        {
            SetCrawlState(false);
        }

        // 2. แก้ปัญหา Shift แล้ววิ่งเร็ว: ถ้าคลานอยู่ บังคับให้ปุ่ม Sprint เป็น false ตลอดเวลา
        if (_isDown && _input != null)
        {
            _input.sprint = false;
        }

        // 3. จัดการการเคลื่อนที่ของกล้อง
        HandleCameraEffects();
    }

    void HandleCameraEffects()
    {
        if (cameraRoot == null) return;

        // เลื่อนตำแหน่ง Y และ Z ของ Camera Root
        float newY = Mathf.Lerp(cameraRoot.transform.localPosition.y, _targetCameraY, Time.deltaTime * transitionSpeed);
        float targetZOffset = _isDown ? crawlZOffset : normalZOffset;
        float newZOffset = Mathf.Lerp(cameraRoot.transform.localPosition.z, targetZOffset, Time.deltaTime * transitionSpeed);

        cameraRoot.transform.localPosition = new Vector3(cameraRoot.transform.localPosition.x, newY, newZOffset);

        // เอียงกล้อง (Z Tilt)
        Vector3 currentRotation = cameraRoot.transform.localEulerAngles;
        float currentZ = currentRotation.z;
        if (currentZ > 180) currentZ -= 360;

        float newZ = Mathf.Lerp(currentZ, _targetCameraZ, Time.deltaTime * (transitionSpeed * 0.5f));
        cameraRoot.transform.localRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, newZ);

        // ถ้าใช้ Cinemachine ให้รีเซ็ต Shoulder Offset เป็น 0 เพื่อให้มันเชื่อฟัง CameraRoot
        if (_vCamFollow != null)
        {
            _vCamFollow.ShoulderOffset = Vector3.zero;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (_animator != null) _animator.SetTrigger("Hit");
        if (currentHealth <= 0) Die();
    }

    void SetCrawlState(bool isCrawl)
    {
        _isDown = isCrawl;

        if (_animator != null) _animator.SetBool("isCrawling", isCrawl);

        if (isCrawl)
        {
            // --- สถานะคลาน ---
            _targetCameraY = crawlCameraHeight;
            _targetCameraZ = fallTiltZ;

            _controller.MoveSpeed = 0.7f;
            _controller.SprintSpeed = 0.7f; // ล็อคความเร็ววิ่งให้เท่ากับเดิน
            _controller.JumpHeight = 0f;

            _charController.height = 0.5f;
            _charController.center = new Vector3(0, 0.25f, 0);
        }
        else
        {
            // --- สถานะปกติ ---
            _targetCameraY = normalCameraHeight;
            _targetCameraZ = 0f;

            _controller.MoveSpeed = 4.0f;
            _controller.SprintSpeed = 6.0f;
            _controller.JumpHeight = 1.2f;

            _charController.height = 2.0f;
            _charController.center = new Vector3(0, 1.0f, 0);
        }
    }

    void Die()
    {
        Debug.Log("Player Dead");
    }
}