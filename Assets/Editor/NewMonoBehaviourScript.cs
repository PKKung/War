using UnityEngine;
using UnityEditor;

public class BulkColliderTool : EditorWindow
{
    [MenuItem("Tools/My Custom Tools/Add Mesh Colliders to Selection")]
    public static void AddMeshColliders()
    {
        // ดึง Object ทั้งหมดที่เราเลือกไว้ใน Hierarchy
        GameObject[] selectedObjects = Selection.gameObjects;
        int count = 0;

        foreach (GameObject obj in selectedObjects)
        {
            // หา MeshRenderer ในตัวมันเองและลูกๆ ของมัน (Children)
            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer renderer in renderers)
            {
                // ถ้ามี Mesh แต่ยังไม่มี Collider ให้ใส่เข้าไป
                if (renderer.GetComponent<MeshCollider>() == null)
                {
                    renderer.gameObject.AddComponent<MeshCollider>();
                    count++;
                }
            }
        }

        Debug.Log($"เพิ่ม Mesh Collider เสร็จเรียบร้อยทั้งหมด {count} ชิ้น!");
    }
}