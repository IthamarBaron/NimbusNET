using UnityEngine;

public class RadarTriggerForwarder : MonoBehaviour
{
    private Radar radar;

    public void Initialize(Radar radarReference)
    {
        radar = radarReference;
    }

    private void OnTriggerEnter(Collider other)
    {
        radar?.OnTriggerFromChildEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        radar?.OnTriggerFromChildExit(other);
    }
}
