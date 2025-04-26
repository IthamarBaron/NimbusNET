using UnityEngine;
using UnityEditor;

public class RemoveBoxCollidersWindow : EditorWindow
{
    GameObject targetObject;

    [MenuItem("Tools/Remove BoxColliders")]
    public static void ShowWindow()
    {
        GetWindow<RemoveBoxCollidersWindow>("Remove BoxColliders");
    }

    private void OnGUI()
    {
        GUILayout.Label("Remove BoxColliders From Hierarchy", EditorStyles.boldLabel);

        targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);

        if (targetObject != null)
        {
            if (GUILayout.Button("Remove Box Colliders"))
            {
                RemoveBoxColliders(targetObject);
                Debug.Log("BoxColliders removed from " + targetObject.name + " and its children.");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please assign a GameObject or prefab.", MessageType.Info);
        }
    }

    private void RemoveBoxColliders(GameObject root)
    {
        BoxCollider[] colliders = root.GetComponentsInChildren<BoxCollider>(true);

        int removedCount = 0;
        foreach (BoxCollider col in colliders)
        {
            DestroyImmediate(col);
            removedCount++;
        }

        Debug.Log($"Removed {removedCount} BoxCollider(s) from {root.name}.");
    }
}
