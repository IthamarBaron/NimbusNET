using UnityEngine;

public class LaserInterceptor : MonoBehaviour, IInterceptor
{
    public string type => "laser";
    public bool isAvailable { get; set; }

    public int interceptionPrice => 50;

    [SerializeField] public float InnerLimitRadius => 0.1f;

    [SerializeField] public float OuterLimitRadius => 200f;

    [Header("Laser Settings")]
    public Transform firePoint;
    public LineRenderer laserLine;
    public float laserDamageMultiplier = 1f;

    private Transform currentTarget;
    private float targetDestroyTimer;
    private bool firing = false;

    private void Start()
    {
        GameManager.Instance?.RegisterInterceptor(this);
        isAvailable = true;

    }

    void Update()
    {
        if (currentTarget)
        {
            FireLaserAtTarget();
        }
        else
        {
            DisableLaser();
        }
    }

    
    // Called externally by Radar 
    public bool NewTarget(IThreat target)
    {
        if (!currentTarget)
        {
            currentTarget = target.GetGameObject().transform;

            // Calculate destroy timer based on distance and size
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            float targetSize = currentTarget.GetComponent<Collider>().bounds.size.magnitude;
            targetDestroyTimer = (distance / 200f) * targetSize * (1/laserDamageMultiplier);
            return true;
        }
        return false;

    }

    void FireLaserAtTarget()
    {
        isAvailable = false;
        firing = true;
        laserLine.enabled = true;


        laserLine.SetPosition(0, firePoint.position);
        laserLine.SetPosition(1, currentTarget.position);

            
        targetDestroyTimer -= Time.deltaTime;
        if (targetDestroyTimer <= 0)
        {
            var threat = currentTarget.GetComponent<IThreat>();
            if (threat != null)
                threat.Explode();
            else
                Destroy(currentTarget.gameObject);

            currentTarget = null;
            DisableLaser();
        }
    }

    void DisableLaser()
    {
        if (firing)
        {
            laserLine.enabled = false;
            firing = false;
            isAvailable = true;
        }
    }

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


    public GameObject GetGameObject() { return gameObject; }
    void OnDestroy()
    {
        GameManager.Instance?.UnregisterInterceptor(this);
    }
}
