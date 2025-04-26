using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles detection of threats using trigger colliders. Each Radar manages its own interceptors.
/// </summary>
public class Radar : MonoBehaviour
{
    [Header("Radar Setup")]
    [SerializeField] private GameObject scanDish;
    [Header("Interceptors Setup")]
    [SerializeField] public GameObject[] interceptorGameobjects;
    private List<IInterceptor> managedInterceptors = new(); // Interceptors managed by this radar

    private Collider[] scanColliders;

    // Threats that are detected and prioritized
    private PriorityQueue<IThreat, float> trackedThreats = new();
    //private HashSet<IThreat> knownThreats = new(); // For fast duplicate prevention
    private TopsisRunner TOPSISRunnerInstance = null;
    private void Start()
    {
        TOPSISRunnerInstance = new TopsisRunner();
        // Ensure colliders are set
        scanColliders = scanDish.GetComponentsInChildren<MeshCollider>();
        if (scanColliders.Length != 2)
        {
            Debug.LogError($"Radar {name} is missing scan dish colliders or has incorrect setup.");
        }

        foreach (var interceptor in interceptorGameobjects)
        {
            IInterceptor _interceptor = interceptor.GetComponent<IInterceptor>();
            if (_interceptor!=null)
            {
                managedInterceptors.Add(_interceptor);
            }
            else
            {
                Debug.LogWarning($"[Radar: {gameObject.name}] Interceptor {interceptor.gameObject.name} is missing IInterceptor!");
            }
        }

        // Ensure trigger + kinematic setup
        Rigidbody rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var col in scanColliders)
        {
            col.isTrigger = true;
            var forwarder = col.gameObject.AddComponent<RadarTriggerForwarder>();
            forwarder.Initialize(this);
        }
    }


    private void Update()
    {
        if (trackedThreats.Count >0)
        {
            IThreat threat = trackedThreats.Dequeue();
            var interceptorArray = GetAvailableInterceptors().ToArray();
            IInterceptor interceptor = TOPSISRunnerInstance.GetBestInterceptor(interceptorArray, threat);
            interceptor.NewTarget(threat);
            Debug.Log($"[RADAR] Selected interceptor {interceptor.GetGameObject().name} for threat {threat.GetGameObject().name}");
        }
    }
    /// <summary>
    /// Called by RadarTriggerForwarder when a threat enters.
    /// </summary>  
    public void OnTriggerFromChildEnter(Collider other)
    {
        IThreat threat = other.GetComponent<IThreat>();
        if (threat == null || threat.isTracked || threat.isIntercepted)
            return;

        threat.isTracked = true;

        if (CriteriaCalculator.Instance.CalcDangerLevel(threat) > 0.1)
        {
            //handle threat
            float priority = CriteriaCalculator.Instance.CalcPriorityScore(threat);
            Debug.Log($"[Radar] Detected new threat: {threat.GetGameObject().name} | Priority: {priority:F2}");
            trackedThreats.Enqueue(threat, priority);
        }
        else
        {
            Debug.Log($"[Radar] Ignoring threat: {threat.GetGameObject().name} | DL: {CriteriaCalculator.Instance.CalcDangerLevel(threat):F2}");
        }
        
    }

    /// <summary>
    /// Called by RadarTriggerForwarder when a threat exits.
    /// </summary>
    public void OnTriggerFromChildExit(Collider other)
    {
        IThreat threat = other.GetComponent<IThreat>();
        if (threat != null)
        {
            Debug.Log($"[Radar] Threat exited range: {threat.GetGameObject().name}");
        }
    }
    public List<IInterceptor> GetAvailableInterceptors()
    {
        return managedInterceptors.FindAll(i => i.isAvailable);
    }

    public bool HasPendingThreats() => trackedThreats.Count > 0;

    public IThreat GetNextThreat()
    {
        if (trackedThreats.Count == 0) return null;
        return trackedThreats.Dequeue();
    }

    private void TEMPCalcCriteria(IThreat SelectedThreat)
    {
        foreach (var interceptor in managedInterceptors)
        {
            float ct = CriteriaCalculator.Instance.CalcCriticalTime(interceptor, SelectedThreat);
            float dl = CriteriaCalculator.Instance.CalcDangerLevel(SelectedThreat);
            float alt = CriteriaCalculator.Instance.CalcAltCompatibility(interceptor, SelectedThreat);
            float dev = CriteriaCalculator.Instance.CalcPathDeviation(interceptor, SelectedThreat);
            float er = CriteriaCalculator.Instance.CalcEffectiveRange(interceptor, SelectedThreat);

            Debug.Log($"====Interceptor [{interceptor.GetGameObject().name}]====");
            Debug.Log($" - CriticalTime: {ct:F2}");
            Debug.Log($" - DangerLevel: {dl:F2}");
            Debug.Log($" - AltCompatibility: {alt:F2}");
            Debug.Log($" - PathDeviation: {dev:F2}");
            Debug.Log($" - Effective Range: {er:F2}");
            Debug.Log($"====Interceptor [{interceptor.GetGameObject().name}]====");

        }
    }
}
