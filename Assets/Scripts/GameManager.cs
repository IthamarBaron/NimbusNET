using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Simulation Time Control")]
    [SerializeField] private float timeStep = 0.1f;
    [SerializeField] private float minTime = 0f;
    [SerializeField] private float maxTime = 5f;

    [SerializeField] private LayerMask threatLayerMask;

    public float CurrentTimeScale => Time.timeScale;

    private List<IThreat> activeThreats = new();
    private List<IInterceptor> activeInterceptors = new();
    private int selectedThreatIndex = -1;

    public IThreat SelectedThreat { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectThreatUnderCursor(threatLayerMask);
        }
        HandleTimeInput();
        HandleThreatSelectionInput();
    }

    private void HandleTimeInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
            Time.timeScale = 0f;
        if (Input.GetKeyDown(KeyCode.Alpha1))
            Time.timeScale = 1f;
        if (Input.GetKeyDown(KeyCode.UpArrow))
            Time.timeScale = Mathf.Clamp(Time.timeScale + timeStep, minTime, maxTime);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            Time.timeScale = Mathf.Clamp(Time.timeScale - timeStep, minTime, maxTime);
    }

    private void HandleThreatSelectionInput()
    {
        if (activeThreats.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            selectedThreatIndex = (selectedThreatIndex + 1) % activeThreats.Count;
            SelectedThreat = activeThreats[selectedThreatIndex];
            Debug.Log($"[GameManager] Selected Threat: {SelectedThreat.GetGameObject().name}");

            // Optional: Snap camera to threat here if needed
        }

        if (Input.GetKeyDown(KeyCode.F) && SelectedThreat != null)
        {
            var targetPos = SelectedThreat.GetGameObject().transform.position;
            FreeFlyCamera.Instance?.SnapTo(targetPos);
        }

        if (Input.GetKeyDown(KeyCode.T) && SelectedThreat != null)
        {
            var impact = SelectedThreat.PredictImpactPoint();
            FreeFlyCamera.Instance?.SnapTo(impact);
        }

        if (Input.GetKeyDown(KeyCode.C) && SelectedThreat != null)
        {
            Debug.Log("=== Calculating Criteria ===");
            foreach (var interceptor in activeInterceptors)
            {
                float ct = CriteriaCalculator.Instance.CalcCriticalTime(interceptor, SelectedThreat);
                float dl = CriteriaCalculator.Instance.CalcDangerLevel(SelectedThreat);
                float alt = CriteriaCalculator.Instance.CalcAltCompatibility(interceptor, SelectedThreat);
                float dev = CriteriaCalculator.Instance.CalcPathDeviation(interceptor, SelectedThreat);

                Debug.Log($"==Interceptor [{interceptor.GetGameObject().name}]==");
                Debug.Log($" - CriticalTime: {ct:F2}");
                Debug.Log($" - DangerLevel: {dl:F2}");
                Debug.Log($" - AltCompatibility: {alt:F2}");
                Debug.Log($" - PathDeviation: {dev:F2}");
            }
        }
    }
    public void TrySelectThreatUnderCursor(LayerMask threatLayerMask)
    {
        if (!Cursor.visible) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, threatLayerMask))
        {
            IThreat threat = hit.collider.GetComponentInParent<IThreat>();
            if (threat != null)
            {
                SelectedThreat = threat;
                Debug.Log($"[GameManager] Selected threat via click: {threat.GetGameObject().name}");
            }
        }
    }

    // === Registration API ===

    public void RegisterThreat(IThreat threat)
    {
        if (!activeThreats.Contains(threat))
            activeThreats.Add(threat);
    }

    public void UnregisterThreat(IThreat threat)
    {
        activeThreats.Remove(threat);
    }

    public void RegisterInterceptor(IInterceptor interceptor)
    {
        if (!activeInterceptors.Contains(interceptor))
            activeInterceptors.Add(interceptor);
    }

    public void UnregisterInterceptor(IInterceptor interceptor)
    {
        activeInterceptors.Remove(interceptor);
    }

    public IReadOnlyList<IThreat> GetActiveThreats() => activeThreats;
    public IReadOnlyList<IInterceptor> GetActiveInterceptors() => activeInterceptors;

    public void SelectThreat(IThreat threat)
    {
        if (threat == null || threat.GetGameObject() == null) return;

        SelectedThreat = threat;
        selectedThreatIndex = activeThreats.IndexOf(threat);
    }
}
