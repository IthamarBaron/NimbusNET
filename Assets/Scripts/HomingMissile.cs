using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    public Transform target = null;
    public Rigidbody rocketRB;
    public GameObject explosionObject;
    private bool isActive = false;
    public float turn=10f;
    public float predictionCoefficient = 1f;
    public float velocity=250f;
    private Collider col;
    private float SelfDestructTimer = 3f;
    private float selfDestructTimerCounter = 0f;


    private void Start()
    {
        col = this.gameObject.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("NO COLLIDER ON HOMING MISSILE!");
        }
        col.enabled = false;
        rocketRB.useGravity = false;
    }
    private void FixedUpdate()
    {
        if (Input.GetKeyUp(KeyCode.G) && !isActive)
        {
                Activate();
        }
        if (target != null && rocketRB != null && isActive)
        {
            Rigidbody targetRB = target.GetComponent<Rigidbody>();

            Vector3 targetVelocity = target.GetComponent<IThreat>().GetVelocity();

            // Calculate distance to target
            Vector3 directionToTarget = target.position - transform.position;
            float distance = directionToTarget.magnitude;

            // Estimate time to reach target
            float timeToReachTarget = distance / velocity;

            // Predict future position
            Vector3 futurePosition = target.position + (targetVelocity * timeToReachTarget)*0.5f;

            // Adjust missile direction
            Quaternion targetRotation = Quaternion.LookRotation(futurePosition - transform.position);

            rocketRB.velocity = transform.forward * velocity;
            rocketRB.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, turn));
        }
        else if (isActive && target == null)
        {
            selfDestructTimerCounter += Time.fixedDeltaTime;

            // Rotate smoothly upwards
            Quaternion upwardsRotation = Quaternion.LookRotation(Vector3.up);
            rocketRB.MoveRotation(Quaternion.RotateTowards(transform.rotation, upwardsRotation, turn));

            // Apply velocity upwards
            rocketRB.velocity = rocketRB.transform.forward * velocity;

            if (selfDestructTimerCounter >= SelfDestructTimer)
            {
                SelfDestruct();
            }
        }



    }


    public void Activate()
    {
        col = this.gameObject.GetComponent<Collider>();//physics frame might cause Start() to get called after Activate()
        this.gameObject.SetActive(true);
        this.isActive = true;
        this.rocketRB.useGravity = true;
        this.col.enabled = true;

    }

    private void SelfDestruct()
    {
        Debug.Log("Self Destructing, object is NULL");
        Instantiate(explosionObject, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("radarDish")) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("city"))
        {
            SelfDestruct();
            other.gameObject.SetActive(false);
        }


    }

    public void SetTarget(GameObject _target)
    {
        this.target = _target.transform;
    }
}
