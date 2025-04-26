using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour, IExplode
{

    [field: SerializeField]
    public GameObject explosionObject { get; set; }

    public void Explode()
    {
        Instantiate(explosionObject, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<IThreat>() != null)
        {
            Explode();
        }

    }

}
