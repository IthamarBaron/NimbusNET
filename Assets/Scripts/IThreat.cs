using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public interface IThreat: IExplode
{
    public bool isIntercepted { get; set; }
    public bool isTracked { get; set; }

    Vector3 GetVelocity();

    GameObject GetGameObject();
    Vector3 PredictImpactPoint();
    float GetTimeToImpact();


}

