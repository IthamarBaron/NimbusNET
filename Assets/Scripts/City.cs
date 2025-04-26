using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour
{
    public Collider dangerZoneCollider;
    public Transform centerPoint; // set manually or auto from collider.bounds.center

    public Vector3 GetCenter()
    {
        return centerPoint != null ? centerPoint.position : dangerZoneCollider.bounds.center;
    }
}
