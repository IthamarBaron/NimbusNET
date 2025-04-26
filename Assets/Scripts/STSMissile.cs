using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


//!!!!!!!!!!!!!!!!!!!!!!!DEPRECATED!!!!!!!!!!!!!!!!!!!!!!!


[RequireComponent(typeof(Rigidbody))]
public class STSMissile : MonoBehaviour
{
    public bool isIntercepted { get; set; }

    [field: SerializeField]
    public GameObject explosionObject { get; set; }


    public Transform target; // The target the missile will aim at
    public float stage1BoostForce = 500f; // Force for stage 1 boost
    public float stage1Duration = 0.5f; // Duration for stage 1 boost
    public float rotationForce = 200f; // Force for stage 2 rotation
    public float stage2Duration = 0.5f; // Duration for stage 2 rotation burn
    public float stage3ThrustForce = 1000f; // Force for stage 3 main engine burn
    public float stage3Duration = 2f; // Duration for stage 3 thrust burn
    public float stageCooldown = 0.5f; // Cooldown duration between stages

    public float angularDrag = 2f; // Angular drag to stabilize rotation in early stages
    public float linearDrag = 0.1f; // Linear drag to simulate air resistance
    public Vector3 centerOfMassOffset = new Vector3(0, 0.5f, 0); // Offset for making the missile nose-heavy
    public float stabilizationTorqueMultiplier = 1f; // Strength of the stabilization torque

    private Rigidbody rb;
    private int currentStage = 0; // Tracks the current stage of the missile's operation
    private float stageTimer = 0f;
    private bool inCooldown = false; // Tracks whether the missile is in a cooldown period
    Collider col = null;
    public ParticleSystem fire;

    void Start()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody is missing from the missile.");
            return;
        }

        // Set the center of mass to make the missile nose-heavy
        rb.centerOfMass = centerOfMassOffset;

        // Set drag values for rotation and movement stabilization
        rb.angularDrag = angularDrag;
        rb.drag = linearDrag;

        //Debug.Log("Center of Mass set to: " + rb.centerOfMass);
    }

    void FixedUpdate()
    {
        // Trigger stage 1 when "L" is pressed
        if (Input.GetKeyDown(KeyCode.L))
        {

            StartStage(1);
        }

        // Apply stabilization torque in all stages except Stage 4
        if (rb.velocity.magnitude > 0.1f && currentStage != 4)
        {
            Vector3 velocityDirection = rb.velocity.normalized;
            Vector3 stabilizationTorque = Vector3.Cross(transform.up, velocityDirection); // Torque to align forward direction with velocity
            rb.AddTorque(stabilizationTorque * stabilizationTorqueMultiplier, ForceMode.Acceleration);

        }
        /*
        // Apply stabilization with clamping in Stage 4 to prevent over-rotation
        if (currentStage == 4 && rb.velocity.magnitude > 0.1f)
        {
            Vector3 velocityDirection = rb.velocity.normalized;

            // Calculate the angle between the missile's heading (up direction) and its velocity
            float angle = Vector3.Angle(transform.up, velocityDirection);

            // Apply stabilization torque if the angle exceeds the threshold
            if (angle > 2f)
            {
                Vector3 stabilizationTorque = Vector3.Cross(transform.up, velocityDirection); // Torque to align up direction with velocity
                rb.AddTorque(stabilizationTorque * stabilizationTorqueMultiplier, ForceMode.Acceleration);
            }
            else
            {
                // Dampen angular velocity to prevent excessive rotation
                rb.angularVelocity *= 0.9f;
            }
        }*/

        // Debug lines to visualize the missile's velocity and forward direction
        Debug.DrawLine(transform.position, transform.position + rb.velocity.normalized * 2, Color.yellow); // Velocity direction
        Debug.DrawLine(transform.position, transform.position + transform.forward * 2, Color.red);         // Forward direction

        if (inCooldown) return; // Skip further updates if in cooldown period

        stageTimer += Time.fixedDeltaTime;

        switch (currentStage)
        {
            case 1: // Stage 1: Launch
                if (stageTimer < stage1Duration)
                {
                    fire.Play();
                    col.enabled = false;
                    // Apply upward force to simulate launch
                    rb.AddForce(Vector3.up * stage1BoostForce * Time.fixedDeltaTime, ForceMode.Acceleration);
                }
                else
                {
                    StartCooldown(2); // Transition to Stage 2
                }
                break;

            case 2: // Stage 2: Rotation burn
                if (stageTimer < stage2Duration)
                {
                    fire.Stop();
                    col.isTrigger = true;
                    // Calculate the direction vector to the target
                    Vector3 directionToTarget = (target.position - transform.position).normalized;
                    // Calculate the angle between the missile's forward direction and the direction to the target
                    float angleToTarget = Vector3.Angle(transform.up, directionToTarget);


                    // Apply torque if the angle is significant (prevents unnecessary small rotations)
                    if (angleToTarget > 1f) // Use a small threshold to avoid jitter
                    {
                        Vector3 rotationTorque = Vector3.Cross(transform.up, directionToTarget);
                        rb.AddTorque(rotationTorque * rotationForce * Time.fixedDeltaTime, ForceMode.Acceleration);
                    }
                }
                else
                {
                    StartCooldown(3); // Transition to Stage 3
                }
                break;

            case 3: // Stage 3: Main engine burn
                if (stageTimer < stage3Duration)
                {
                    fire.Play();

                    col.enabled = true;
                    // Apply force in the missile's heading direction (up)
                    rb.AddForce(transform.up * stage3ThrustForce * Time.fixedDeltaTime, ForceMode.Acceleration);
                    stageCooldown = 1.5f;
                }
                else
                {
                    fire.Stop();
                    StartCooldown(4); // Transition to Stage 4
                }
                break;

            case 4: // Stage 4: Main engine cutoff
                    // Reduce drag to allow natural free-fall behavior
                rb.angularDrag = 0.1f; // Allow natural rotation
                rb.drag = 0f;          // Remove linear drag for freefall behavior

                // Calculate horizontal speed (ignoring vertical speed)
                float horizontalSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;

                // Simulate lift force based on horizontal speed
                float liftForce = horizontalSpeed * 0.0001f; // Subtle lift force proportional to horizontal speed
                Vector3 liftDirection = -Physics.gravity.normalized; // Direction opposite to gravity

                rb.AddForceAtPosition(liftDirection * liftForce * 0.5f, transform.TransformPoint(rb.centerOfMass), ForceMode.Force);
                rb.AddForceAtPosition(liftDirection * liftForce , transform.TransformPoint(rb.centerOfMass)-centerOfMassOffset, ForceMode.Force);
                //rb.AddForceAtPosition(transform.up * liftForce*2, transform.TransformPoint(rb.centerOfMass)-(centerOfMassOffset*2), ForceMode.Force);

                // Debug: Draw the lift vector in yellow
                Debug.DrawLine(transform.TransformPoint(rb.centerOfMass), transform.TransformPoint(rb.centerOfMass) + liftDirection * liftForce * 0.1f, Color.yellow);

                break;

        }
    }

    void StartCooldown(int nextStage)
    {
        inCooldown = true;
        StartCoroutine(CooldownCoroutine(nextStage));
    }

    IEnumerator CooldownCoroutine(int nextStage)
    {
        //Debug.Log($"Cooldown before Stage {nextStage}...");
        yield return new WaitForSeconds(stageCooldown); // Wait for the cooldown duration
        StartStage(nextStage); // Proceed to the next stage
    }

    void StartStage(int stage)
    {
        currentStage = stage;
        stageTimer = 0f;
        inCooldown = false;

        Debug.Log($"Stage {stage} initiated.");
    }

    void OnDrawGizmos()
    {
        if (rb != null)
        {
            // Set the gizmo color to yellow for the center of mass
            Gizmos.color = Color.yellow;

            // Calculate the world position of the center of mass
            Vector3 centerOfMassWorldPosition = transform.TransformPoint(rb.centerOfMass);

            // Draw a small sphere at the center of mass
            Gizmos.DrawSphere(centerOfMassWorldPosition, 0.2f); // Adjust the size as needed
            Debug.DrawLine(transform.position, target.position, Color.blue); // Line pointing to the target
            Debug.DrawLine(transform.position, transform.position + transform.up * 10, Color.red); // Missile's heading direction
        }
    }

    public GameObject GetGameObject() { return gameObject; }
    public void Explode()
    {
        Instantiate(explosionObject, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }

    public Vector3 PredictImpactPoint()
    {
        // Predict based on current velocity and gravity
        Rigidbody rb = GetComponent<Rigidbody>();

        // Estimate time until hit ground
        float t = Mathf.Abs(transform.position.y / rb.velocity.y); // if it works dont touch it
        Vector3 displacement = rb.velocity * t;

        return transform.position + displacement;
    }

    public Vector3 GetVelocity()
    {
        throw new System.NotImplementedException();
    }
}
