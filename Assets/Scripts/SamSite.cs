using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SamSite : MonoBehaviour, IInterceptor
{
    public string type => "sam";
    public bool isAvailable => true;

    public int interceptionPrice => 2000;
    [SerializeField]public float InnerLimitRadius => 50f;
    [SerializeField] public float OuterLimitRadius => 200f;
    public GameObject interceptorPrefab;
    public GameObject destroiedSaPrefab;
    public AudioClip lunchSFX;
    
    public GameObject currentTarget;
    private GameObject interceptor;
    private HomingMissile instance;
    private AudioSource audioSource;
    private void Start()
    {
        GameManager.Instance?.RegisterInterceptor(this);
        audioSource = this.GetComponent<AudioSource>();
        if (audioSource == null)
            Debug.LogError("Missing Audio Source!");
    }
    private void Update()
    {
        //reload
        if (interceptor == null) 
        {
            interceptor = Instantiate(interceptorPrefab, transform.position, transform.rotation);
            instance = interceptor.GetComponent<HomingMissile>();
            if (instance == null)
            {
                interceptor.AddComponent<HomingMissile>();
                instance = interceptor.GetComponent<HomingMissile>();
            }
            interceptor.transform.GetChild(0).gameObject.SetActive(false);
            interceptor.transform.GetChild(1).gameObject.SetActive(false);


        }

        //on object detection
        if (currentTarget != null)
        {
            interceptor.transform.GetChild(0).gameObject.SetActive(true);
            interceptor.transform.GetChild(1).gameObject.SetActive(true);

            audioSource.PlayOneShot(lunchSFX);
            instance.SetTarget(currentTarget);
            instance.Activate();
            currentTarget = null;
            interceptor = null;
        }
    }

    public bool NewTarget(IThreat _target)
    {
        if (currentTarget == null)
        {
            currentTarget = _target.GetGameObject();
            currentTarget.gameObject.GetComponent<IThreat>().isIntercepted = true;
            return true;
        }
        return false;

    }
    public GameObject GetGameObject() { return gameObject; }
    private void OnDrawGizmos()
    {
        if (TryGetComponent<IInterceptor>(out var interceptor))
        {
            Vector3 center = interceptor.GetGameObject().transform.position;

            float outerR = interceptor.OuterLimitRadius;
            float innerR = interceptor.InnerLimitRadius;
            float height = 100f; // vertical size of the ring (tweak for looks)

            Gizmos.color = new Color(0.5f, 0f, 0f, 0.6f); // dark red
            DrawWireCylinder(center, outerR, height);

            Gizmos.color = new Color(0.3f, 0f, 0f, 0.6f); // darker red
            DrawWireCylinder(center, innerR, height);
        }
    }


    private void DrawWireCylinder(Vector3 center, float radius, float height, int segments = 32)
    {
        float halfHeight = height / 2f;
        Vector3 top = center + Vector3.up * halfHeight;
        Vector3 bottom = center - Vector3.up * halfHeight;

        // Draw circles at top and bottom
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * Mathf.PI * 2f;
            float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

            Vector3 offset1 = new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
            Vector3 offset2 = new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;

            // top ring
            Gizmos.DrawLine(top + offset1, top + offset2);
            // bottom ring
            Gizmos.DrawLine(bottom + offset1, bottom + offset2);
            // vertical lines
            Gizmos.DrawLine(top + offset1, bottom + offset1);
        }
    }

    void OnDestroy()
    {
        GameManager.Instance?.UnregisterInterceptor(this);
    }

}
