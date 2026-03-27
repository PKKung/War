using UnityEngine;

public class MissileExplosion : MonoBehaviour
{
    public GameObject explosionPrefab;
    public AudioClip explosionSound;

    AudioSource audioSource; // 🔊 เสียงตอนตก

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        float height = transform.position.y;

        // ยิ่งใกล้พื้น → เสียงดังขึ้น
        float volume = Mathf.Clamp01(1f - (height / 150f));
        audioSource.volume = volume;
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    void Explode()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // 🔊 เสียงระเบิด
        AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        Destroy(gameObject);
    }
}