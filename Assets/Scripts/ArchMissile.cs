using UnityEngine;

public class ArcMissile : MonoBehaviour, IThreat
{
    [Header("Targeting")]
    public Transform target;

    [Header("Missile Parameters")]
    public float speed = 50f;             // Movement speed
    public float arcFactor = 0.2f;        // Curve height relative to target distance
    [field: SerializeField] public GameObject explosionObject { get; set; }
    public bool isIntercepted { get; set; }
    public bool isTracked { get; set; }

    
    private Vector3 lastPosition;
    private float lastTime;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private float arcHeight;
    private float flightDuration;
    private float startTime;

    private bool launched = false;

    public void Explode()
    {
        Instantiate(explosionObject, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }

    void Start()
    {
        GameManager.Instance?.RegisterThreat(this);
        if (target != null)
            StartLaunch(target.position);
        lastPosition = transform.position;
        lastTime = Time.time;
    }

    public void StartLaunch(Vector3 targetPosition)
    {
        startPoint = transform.position;
        endPoint = targetPosition;

        float flatDistance = Vector3.Distance(
            new Vector3(startPoint.x, 0, startPoint.z),
            new Vector3(endPoint.x, 0, endPoint.z)
        );

        arcHeight = flatDistance * arcFactor;
        flightDuration = flatDistance / speed;
        startTime = Time.time;
        launched = true;

        lastTime = Time.time;
        lastPosition = transform.position;
    }

    

    void Update()
    {
        if (!launched) return;
        float t = (Time.time - startTime) / flightDuration;
        if (this.gameObject.transform.position.y < 0 && t < 1)
        {

        }

        Vector3 flatStart = new Vector3(startPoint.x, 0, startPoint.z);
        Vector3 flatEnd = new Vector3(endPoint.x, 0, endPoint.z);

        Vector3 moveDir;

        if (t < 1f)
        {
            // Follow the calculated parabolic arc (red line)
            Vector3 flatPos = Vector3.Lerp(flatStart, flatEnd, t);
            float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
            Vector3 newPos = new Vector3(flatPos.x, startPoint.y + height, flatPos.z);

            moveDir = newPos - transform.position;
            transform.position = newPos;
        }
        else
        {
            // After arc ends, continue flying in same direction (blue line)
            moveDir = (endPoint - flatStart).normalized;
            transform.position += moveDir * speed * Time.deltaTime;
        }

        // Rotate to match direction
        if (moveDir != Vector3.zero)
            transform.up = moveDir.normalized;
    }

    public Vector3 PredictImpactPoint()
    {
        float inaccuracyRadius = 7f;
        Vector2 randomOffset = Random.insideUnitCircle * inaccuracyRadius;

        return new Vector3(
            endPoint.x + randomOffset.x,
            endPoint.y,
            endPoint.z + randomOffset.y
        );
    }
    public GameObject GetGameObject() => this == null ? null : gameObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("radarDish")) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("city"))
        {
            //TODO UPDATE COUNTER:
            if (other.gameObject.GetComponent<IExplode>() != null)
            {
                other.gameObject.GetComponent<IExplode>().Explode();
            }
            Explode();
        }
    }

    public Vector3 GetVelocity()
    {
        Vector3 currentPosition = transform.position;
        float currentTime = Time.time;

        // Avoid divide-by-zero
        float deltaTime = Mathf.Max(currentTime - lastTime, 0.0001f);
        Vector3 velocity = (currentPosition - lastPosition) / deltaTime;

        return velocity;
    }
    public float GetTimeToImpact()
    {
        float distance = Vector3.Distance(transform.position, PredictImpactPoint());
        float speed = GetVelocity().magnitude;
        if (speed <0.001f)
        {
            Debug.LogWarning($"Threat {gameObject.name} is mooving suspeciusly slow");
            return Mathf.Infinity;
        }
        return distance / speed;
    }
    void OnDestroy()
    {
        GameManager.Instance?.UnregisterThreat(this);
    }
}
