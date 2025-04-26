using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public interface IInterceptor
{
    string type { get; } 
    int interceptionPrice { get; }
    float InnerLimitRadius { get; }
    float OuterLimitRadius { get; }
    bool isAvailable { get; }


    bool NewTarget(IThreat target);
    public GameObject GetGameObject();

}

