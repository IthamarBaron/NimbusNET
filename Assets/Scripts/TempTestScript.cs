using UnityEngine;

public class TempTestScript : MonoBehaviour
{
    [SerializeField] private MonoBehaviour interceptorObject; // must implement IInterceptor
    [SerializeField] private MonoBehaviour threatObject;      // must implement IThreat

    private IInterceptor interceptor;
    private IThreat threat;

    void Start()
    {
        interceptor = interceptorObject as IInterceptor;
        threat = threatObject as IThreat;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            float ct = CriteriaCalculator.Instance.CalcPriorityScore(threat);
            Debug.Log(ct);
            
        }
    }
}
