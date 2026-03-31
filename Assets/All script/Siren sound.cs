using UnityEngine;
using System.Collections;

public class SirenManager : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField] private AudioClip sirenSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Missile Settings")]
    [SerializeField] private MissileRain missileRain;
    [SerializeField] private float delayBeforeMissile = 3f; // รอก่อนให้มิสไซล์ตก

    [Header("Time Settings (Minutes)")]
    [SerializeField] private float minInterval = 3f;  // นาทีต่ำสุด
    [SerializeField] private float maxInterval = 5f;  // นาทีสูงสุด

    private float nextSirenTime;

    void Start()
    {
        ScheduleNextSiren();
    }

    void Update()
    {
        if (Time.time >= nextSirenTime)
        {
            PlaySiren();
            ScheduleNextSiren();
        }
    }

    void PlaySiren()
    {
        if (audioSource != null && sirenSound != null)
        {
            if (SimpleBGMController.Instance != null) SimpleBGMController.Instance.FadeToLow();
            audioSource.PlayOneShot(sirenSound);
            Debug.Log("🚨 ไซเรนดัง!");

            // ⏳ รอแล้วค่อยยิงมิสไซล์
            StartCoroutine(StartMissileAfterDelay());
        }
        else
        {
            Debug.LogWarning("❗ ยังไม่ได้ใส่เสียงหรือ AudioSource");
        }
    }

    IEnumerator StartMissileAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeMissile);

        if (missileRain != null)
        {
            missileRain.StartMissileRain();
            Debug.Log("🚀 เริ่มยิงมิสไซล์!");
        }
        else
        {
            Debug.LogWarning("❗ ยังไม่ได้เชื่อม MissileRain");
        }
    }

    void ScheduleNextSiren()
    {
        float randomDelay = Random.Range(minInterval * 60f, maxInterval * 60f);
        nextSirenTime = Time.time + randomDelay;
    }
}