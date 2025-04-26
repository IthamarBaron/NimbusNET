using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Transform threatListContainer;
    [SerializeField] private GameObject threatButtonPrefab;
    [SerializeField] private TextMeshProUGUI selectedThreatInfoText;

    private List<GameObject> currentThreatButtons = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Update()
    {
        UpdateStatusPanel();
        UpdateThreatList();
        UpdateSelectedThreatInfo();
    }

    private void UpdateStatusPanel()
    {
        float timeScale = Time.timeScale;
        int activeThreats = GameManager.Instance.GetActiveThreats().Count;
        string selectedName =
            GameManager.Instance.SelectedThreat != null && GameManager.Instance.SelectedThreat.GetGameObject() != null
            ? GameManager.Instance.SelectedThreat.GetGameObject().name
            : "None";
        statusText.text = $"Time Scale: {timeScale:F2}\n" +
                          $"Threats: {activeThreats}\n" +
                          $"Selected: {selectedName}";
    }

    private void UpdateThreatList()
    {
        var threats = GameManager.Instance.GetActiveThreats();
        var validThreats = threats.Where(t => t != null && t.GetGameObject() != null).ToList();
        // Rebuild only if count changed
        if (threats.Count != currentThreatButtons.Count)
        {
            foreach (var go in currentThreatButtons)
                Destroy(go);

            currentThreatButtons.Clear();

            foreach (var threat in threats)
            {
                GameObject btnObj = Instantiate(threatButtonPrefab, threatListContainer);
                currentThreatButtons.Add(btnObj);

                var button = btnObj.GetComponent<Button>();
                var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                text.text = threat.GetGameObject().name;

                button.onClick.AddListener(() =>
                {
                    GameManager.Instance.SelectThreat(threat);
                    FreeFlyCamera.Instance?.SnapTo(threat.GetGameObject().transform.position);
                });
            }
        }
    }

    private void UpdateSelectedThreatInfo()
    {
        var threat = GameManager.Instance.SelectedThreat;
        if (threat == null || threat.GetGameObject() == null)
        {
            selectedThreatInfoText.text = "No threat selected";
            return;
        }

        Vector3 targetPos = threat.PredictImpactPoint();
        selectedThreatInfoText.text = $"Threat: {threat.GetGameObject().name}\n" +
                                      $"Target @ {targetPos:F2}";
    }
}
