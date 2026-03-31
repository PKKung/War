using System.Collections;
using UnityEngine;

public class SimpleBGMController : MonoBehaviour
{
    public static SimpleBGMController Instance;
    public AudioSource bgmSource;

    [Header("Settings")]
    public float maxVolume = 1.0f;    // ความดังปกติ
    public float lowVolume = 0.2f;    // ความดังตอนมีระเบิด (เบาๆ คลอไว้)
    public float fadeSpeed = 0.5f;    // ความเร็วในการปรับเสียง (ยิ่งน้อยยิ่งช้า)

    private Coroutine fadeCoroutine;

    void Awake()
    {
        Instance = this;
        if (bgmSource != null) bgmSource.volume = maxVolume;
    }

    // ฟังก์ชันสำหรับ "ลดเสียง" (เรียกตอนไซเรนดัง)
    public void FadeToLow()
    {
        StartFade(lowVolume);
    }

    // ฟังก์ชันสำหรับ "เร่งเสียงกลับ" (เรียกตอนระเบิดจบ)
    public void FadeToMax()
    {
        StartFade(maxVolume);
    }

    private void StartFade(float targetVolume)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(targetVolume));
    }

    private IEnumerator FadeRoutine(float target)
    {
        while (!Mathf.Approximately(bgmSource.volume, target))
        {
            // ค่อยๆ ปรับ Volume ไปยังเป้าหมาย
            bgmSource.volume = Mathf.MoveTowards(bgmSource.volume, target, fadeSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        bgmSource.volume = target;
    }
}