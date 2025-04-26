using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CitySplitter : EditorWindow
{
    [MenuItem("Tools/Split City Into Quadrants")]
    public static void ShowWindow()
    {
        GetWindow<CitySplitter>("City Splitter");
    }

    private GameObject cityParent;
    private Vector3 centerPoint = Vector3.zero;

    void OnGUI()
    {
        GUILayout.Label("Split City Model Into 4 Quadrants", EditorStyles.boldLabel);
        cityParent = (GameObject)EditorGUILayout.ObjectField("City Parent", cityParent, typeof(GameObject), true);
        centerPoint = EditorGUILayout.Vector3Field("Center Point", centerPoint);

        if (GUILayout.Button("Split City"))
        {
            if (cityParent == null)
            {
                Debug.LogError("City parent not assigned!");
                return;
            }

            SplitCity();
        }
    }

    void SplitCity()
    {
        // Create quadrant containers
        GameObject q1 = new GameObject("City_Q1"); // +X, +Z
        GameObject q2 = new GameObject("City_Q2"); // -X, +Z
        GameObject q3 = new GameObject("City_Q3"); // -X, -Z
        GameObject q4 = new GameObject("City_Q4"); // +X, -Z

        q1.transform.parent = cityParent.transform.parent;
        q2.transform.parent = cityParent.transform.parent;
        q3.transform.parent = cityParent.transform.parent;
        q4.transform.parent = cityParent.transform.parent;

        int moved = 0;

        // Get all descendants (recursive)
        List<Transform> allChildren = new List<Transform>();
        GetAllChildren(cityParent.transform, allChildren);

        foreach (Transform child in allChildren)
        {
            if (child == null || child == cityParent.transform) continue;

            Vector3 worldPos = child.position;

            // Optional: skip objects with no mesh (comment this line if you want everything)
            if (child.GetComponent<MeshRenderer>() == null) continue;

            if (worldPos.x >= centerPoint.x && worldPos.z >= centerPoint.z)
                child.parent = q1.transform;
            else if (worldPos.x < centerPoint.x && worldPos.z >= centerPoint.z)
                child.parent = q2.transform;
            else if (worldPos.x < centerPoint.x && worldPos.z < centerPoint.z)
                child.parent = q3.transform;
            else
                child.parent = q4.transform;

            moved++;
        }

        Debug.Log($"Moved {moved} objects to quadrants.");
    }

    void GetAllChildren(Transform parent, List<Transform> list)
    {
        foreach (Transform child in parent)
        {
            list.Add(child);
            GetAllChildren(child, list); // recursion
        }
    }
}
