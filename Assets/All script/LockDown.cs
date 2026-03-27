using UnityEngine;

public class LookDown : MonoBehaviour
{
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(Vector3.down);
    }
}