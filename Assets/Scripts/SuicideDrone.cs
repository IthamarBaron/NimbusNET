using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SuicideDrone : MonoBehaviour, IThreat
{
    public enum FlightPhase { Launch, Cruise, Terminal }
    public bool isIntercepted { get; set; }
    public bool isTracked { get; set; }

    [field: SerializeField]
    public GameObject explosionObject { get; set; }

    [Header("Drone Settings")]
    public Transform target;
    public float launchAngle = 30f;
    public float launchForce = 1000f;
    public float flightSpeed = 50f;
    public float cruiseAltitude = 50f;
    public float maxRotationDegreesPerSecond = 45f;

    [Header("Explosion Settings")]
    public float detonationDistance = 2f;

    private FlightPhase currentPhase = FlightPhase.Launch;
    private bool targetReached = false;
    private Rigidbody rb;

    private Vector3 lastPosition;
    private float lastTime;
    void Start()
    {
        GameManager.Instance?.RegisterThreat(this);
        lastPosition = transform.position;
        lastTime = Time.time;

        rb = gameObject.GetComponent<Rigidbody>();
        rb.useGravity = true;

        transform.rotation = Quaternion.Euler(-launchAngle, transform.rotation.eulerAngles.y, 0);
        rb.AddForce(transform.forward * launchForce);

        Invoke("EndLaunchPhase", 2f);
    }

    void EndLaunchPhase()
    {
        //Destroy(rb);
        lastTime = Time.time;
        lastPosition = transform.position;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        currentPhase = FlightPhase.Cruise;
    }

    void Update()
    {
        if (!target || targetReached || currentPhase == FlightPhase.Launch)
            return;

        switch (currentPhase)
        {
            case FlightPhase.Cruise:
                CruisePhase();
                break;

            case FlightPhase.Terminal:
                TerminalPhase();
                break;
        }

        transform.position += transform.forward * flightSpeed * Time.deltaTime;
    }

    void CruisePhase()
    {

        lastTime = Time.time;
        lastPosition = transform.position;
        Vector3 cruisePosition = new Vector3(target.position.x, cruiseAltitude, target.position.z);
        Vector3 directionToTarget = (cruisePosition - transform.position).normalized;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        float requiredDiveDistance = (transform.position.y - target.position.y) + (flightSpeed * 1.5f);

        if (distanceToTarget <= requiredDiveDistance)
            currentPhase = FlightPhase.Terminal;

        RotateGradually(directionToTarget);
    }

    void TerminalPhase()
    {
        lastTime = Time.time;
        lastPosition = transform.position;

        Vector3 diveDirection = (target.position - transform.position).normalized;
        RotateGradually(diveDirection);

        if (Vector3.Distance(transform.position, target.position) <= detonationDistance)
            Explode();
    }

    void RotateGradually(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxRotationDegreesPerSecond * Time.deltaTime);
    }


    public GameObject GetGameObject() => this == null ? null : gameObject;

    public void Explode()
    {
        Instantiate(explosionObject, transform.position, Quaternion.identity);
        targetReached = true;
        Destroy(gameObject);
    }

    public Vector3 PredictImpactPoint()
    {
        //giving it a bit of randomness
        Vector3 targetPos = this.target.position;
        float radius = 4f; 
        Vector2 randomOffset = Random.insideUnitCircle * radius;

        return new Vector3(targetPos.x + randomOffset.x, targetPos.y, targetPos.z + randomOffset.y);
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
        if (speed < 0.001f)
        {
            UnityEngine.Debug.LogWarning($"Threat {gameObject.name} is mooving suspeciusly slow");
            return Mathf.Infinity;
        }
        return distance / speed;
    }

    void OnDestroy()
    {
        GameManager.Instance?.UnregisterThreat(this);
    }

}
