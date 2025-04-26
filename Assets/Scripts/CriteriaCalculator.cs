using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CriteriaCalculator : MonoBehaviour
{
    public IInterceptor interceptor;
    public IThreat threat;

    private Vector3 lastImpactPoint;
    private Vector3 lastCityCenter;
    private float lastDangerLevel;
    public float laserPerfaredRange = 100f;


    public static CriteriaCalculator Instance { get; private set; }


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }


    /// <summary>
    /// amount of tme it will take Threat to go beyond the Interceptor's range (not effective) anymore
    /// </summary>
    /// <param name="interceptor">interceptor Station</param>
    /// <param name="threat">incomming threat</param>
    /// <returns>Critical Time</returns>
    public float CalcCriticalTime(IInterceptor interceptor, IThreat threat)
    {
        Vector3 threatWorldPos = threat.GetGameObject().transform.position;
        Vector3 interceptorWorldPos = interceptor.GetGameObject().transform.position;
        Vector3 velocity = threat.GetVelocity();
        float speed = velocity.magnitude;

        if (speed < 0.01f)
        {
            Debug.Log("Threat velocity too low, skipping.");
            return 0f;
        }

        // Project to XZ plane
        Vector2 threatPos = new Vector2(threatWorldPos.x, threatWorldPos.z);
        Vector2 interceptorPos = new Vector2(interceptorWorldPos.x, interceptorWorldPos.z);
        Vector2 direction = new Vector2(velocity.x, velocity.z).normalized;

        float innerR = interceptor.InnerLimitRadius;
        float outerR = interceptor.OuterLimitRadius;

        Vector3 impact3D = threat.PredictImpactPoint();
        Vector2 impact = new Vector2(impact3D.x, impact3D.z);
        float tImpact = Vector2.Distance(threatPos, impact) / speed;

        float distNow = Vector2.Distance(threatPos, interceptorPos);
        bool isInsideNow = distNow >= innerR && distNow <= outerR;

       // Debug.Log($"Threat is currently {(isInsideNow ? "INSIDE" : "OUTSIDE")} the interceptable zone");

        float tEnterOuter = -1, tExitOuter = -1;
        float tEnterInner = -1, tExitInner = -1;

        float exposureStart = -1f;
        float exposureEnd = -1f;

        foreach (float radius in new[] { outerR, innerR })
        {
            Vector2 d = direction * speed;
            Vector2 f = threatPos - interceptorPos;

            float a = Vector2.Dot(d, d);
            float b = 2 * Vector2.Dot(f, d);
            float c = Vector2.Dot(f, f) - radius * radius;

            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
             //   Debug.Log($"Threat will NOT intersect radius {radius}");
                continue;
            }

            float sqrtD = Mathf.Sqrt(discriminant);
            float t1 = (-b - sqrtD) / (2 * a);
            float t2 = (-b + sqrtD) / (2 * a);

            float enter = Mathf.Min(t1, t2);
            float exit = Mathf.Max(t1, t2);

            if (Mathf.Approximately(radius, outerR))
            {
                tEnterOuter = enter;
                tExitOuter = exit;
               // Debug.Log($"Threat will ENTER outer ring in {enter:F2}s, EXIT in {exit:F2}s");

                exposureStart = Mathf.Max(0, enter);
                exposureEnd = Mathf.Min(tImpact, exit);
            }
            else
            {
                tEnterInner = enter;
                tExitInner = exit;
               // Debug.Log($"Threat will ENTER inner ring in {enter:F2}s, EXIT in {exit:F2}s");
                exposureEnd = Mathf.Min(exposureEnd, enter);
            }
        }

        if (tExitInner < 0 && tEnterInner < 0 && tExitOuter > 0) 
        {
            float finalCriticalTime = Mathf.Min(tImpact, tExitOuter);
            finalCriticalTime = Mathf.Max(0, finalCriticalTime);
            return finalCriticalTime;
        }
        if (tExitInner < 0 && tEnterInner < 0 && tExitOuter <0 && tEnterOuter <0)
        {
            return 0f;
        }
        float rawExposure = exposureEnd - exposureStart;
        rawExposure = Mathf.Clamp(rawExposure, 0, tImpact);
        return rawExposure;

    }

    /// <summary>
    /// calculates the chances of threat hitting a populated/ imporntant area
    /// </summary>
    /// <param name="threat">incomming threat</param>
    /// <returns>0-1 cance of hitting imporntant area</returns>
    public float CalcDangerLevel(IThreat threat)
    {
        //Predict the impact point
        Vector3 impactPoint = threat.PredictImpactPoint();
        lastImpactPoint = impactPoint;

        //  Convert to grid position 
        Vector2Int cell = CityGridManager.Instance.WorldToGrid(impactPoint);

        //  Check current cell
        List<City> candidateCities = null;
        if (CityGridManager.Instance.cityGrid.ContainsKey(cell))
        {
            candidateCities = CityGridManager.Instance.cityGrid[cell];
        }

        // === If not found, check neighbors ===
        if (candidateCities == null || candidateCities.Count == 0)
        {
            candidateCities = new List<City>();
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(cell.x + 1, cell.y),
                new Vector2Int(cell.x - 1, cell.y),
                new Vector2Int(cell.x, cell.y + 1),
                new Vector2Int(cell.x, cell.y - 1),
                new Vector2Int(cell.x + 1, cell.y + 1),
                new Vector2Int(cell.x - 1, cell.y - 1),
                new Vector2Int(cell.x + 1, cell.y - 1),
                new Vector2Int(cell.x - 1, cell.y + 1)
            };

            foreach (var neighbor in neighbors)
            {
                if (CityGridManager.Instance.cityGrid.ContainsKey(neighbor))
                {
                    candidateCities.AddRange(CityGridManager.Instance.cityGrid[neighbor]);
                }
            }
        }

        // No cities found
        if (candidateCities.Count == 0)
        {
           // Debug.Log("No city found near impact point. DL = 0");
            lastDangerLevel = 0f;
            lastCityCenter = Vector3.zero;
            return 0f;
        }

        // Find closest city from candidates
        City closestCity = null;
        float minDist = float.MaxValue;

        foreach (City city in candidateCities)
        {
            Vector3 impactXZ = new Vector3(impactPoint.x, 0f, impactPoint.z);
            Vector3 centerXZ = new Vector3(city.GetCenter().x, 0f, city.GetCenter().z);
            float dist = Vector3.Distance(impactXZ, centerXZ);
            if (dist < minDist)
            {
                minDist = dist;
                closestCity = city;
            }
        }

        //Calculate normalized danger level 
        Vector3 cityCenter = closestCity.GetCenter();
        float cityRadius = closestCity.dangerZoneCollider.bounds.extents.magnitude;

        float danger = 1f - (minDist / cityRadius);
        float clamped = Mathf.Clamp01(danger);

        // Store for debug draw
        lastCityCenter = cityCenter;
        lastDangerLevel = clamped;

       // Debug.Log($"City: {closestCity.name} | Dist: {minDist:F1} | DL: {clamped:F2}");
        return clamped;
    }

    /// <summary>
    /// calculates the copatibility for threat to interceptor based on altitude
    /// </summary>
    /// <param name="interceptor">intreception site</param>
    /// <param name="threat">incomimng threat</param>
    /// <returns>compatibility precent (0-1)</returns>
    public float CalcAltCompatibility(IInterceptor interceptor, IThreat threat)
    {
        float threatAlt = threat.GetGameObject().transform.position.y;
        string type = interceptor.type;

        if (type == "laser")
        {
            // Soft falloff after 100m, not a cliff
            float value = Mathf.Exp(-threatAlt / 75f); // faster falloff
            float clamped = Mathf.Clamp01(value);
           // Debug.Log($"[AltC] Laser @ {threatAlt:F1}m → {clamped:F2}");
            return clamped;
        }
        else if (type == "sam")
        {
            // Soft rise after 100m
            float value = 1f - Mathf.Exp(-(threatAlt - laserPerfaredRange) / 100f);
            float clamped = Mathf.Clamp01(value);
            //Debug.Log($"[AltC] SAM @ {threatAlt:F1}m → {clamped:F2}");
            return clamped;
        }

        Debug.LogWarning($"[AltC] Unknown interceptor type: {interceptor.type}");
        return 0f;
    }

    /// <summary>
    /// calculates the Effective distance between threat and interceptor
    /// Effective is because we take into consideration the impact point
    /// </summary>
    /// <param name="interceptor">interception site</param>
    /// <param name="threat">incomming threat</param>
    /// <returns></returns>
    public float CalcEffectiveRange(IInterceptor interceptor, IThreat threat)
    {
        Vector3 interceptorPos = interceptor.GetGameObject().transform.position;
        Vector3 threatPos = threat.GetGameObject().transform.position;
        Vector3 impactPoint = threat.PredictImpactPoint();

        float distToThreat = Vector3.Distance(interceptorPos, threatPos);
        return distToThreat;

    }

    /// <summary>
    /// Calculates the Interceptor's Deviation from the threats path
    /// </summary>
    /// <param name="interceptor"> the interceptor site we are testing</param>
    /// <param name="threat">the threat</param>
    /// <returns></returns>
    public float CalcPathDeviation(IInterceptor interceptor, IThreat threat)
    {
        Vector3 threatStart = threat.GetGameObject().transform.position;
        Vector3 interceptorPos = interceptor.GetGameObject().transform.position;
        Vector3 impactPoint = threat.PredictImpactPoint();

        Vector3 pathDir = (impactPoint - threatStart).normalized;
        float totalPathLength = Vector3.Distance(threatStart, impactPoint);
        float distanceToInterceptor = Vector3.Distance(threatStart, interceptorPos);

        // Projection
        Vector3 toInterceptor = interceptorPos - threatStart;
        float projectionLength = Vector3.Dot(toInterceptor, pathDir);
        float clampedLength = Mathf.Clamp(projectionLength, 0f, totalPathLength);

        Vector3 closestPoint = threatStart + pathDir * clampedLength;
        float deviation = Vector3.Distance(interceptorPos, closestPoint);

        // Normalize deviation to [0,1]
        float normalized = Mathf.Clamp01(deviation / totalPathLength);

        // Boost deviation if interceptor is behind threat
        float behindness = Vector3.Dot(toInterceptor.normalized, pathDir); // < 0 means behind
        if (behindness < 0)
        {
            normalized = Mathf.Clamp01(normalized * 1.5f + 0.3f); // bump it up significantly
           // Debug.Log("[PathDev] Interceptor is behind the threat, boosting deviation.");
        }

        // If threat won't reach the interceptor (i.e., impact is before it)
        if (projectionLength > totalPathLength)
        {
            normalized = 1f;
            //Debug.Log("[PathDev] Interceptor is beyond impact point. Max deviation applied.");
        }

       // Debug.Log($"[PathDev] Deviation: {deviation:F2}, normalized: {normalized:F2}");
        return normalized;
    }

    /// <summary>
    /// Calculates Priority based on Time To Imact and Danger Level
    /// </summary>
    /// <param name="threat">Threat to analyse</param>
    /// <returns></returns>
    public float CalcPriorityScore(IThreat threat)
    {
        float dangerLevel = CalcDangerLevel(threat);
        float tti = threat.GetTimeToImpact();

        float dangerWeight = 0.65f;
        float ttiWeight = 0.35f;

        // For now: just hardcoded max time
        float maxTime = 120f;
        float normTTI = Mathf.Clamp01(1f - tti / maxTime);

        float score = dangerWeight * dangerLevel + ttiWeight * normTTI;

       // Debug.Log($"[Priority Score] DL={dangerLevel:F2}, TTI={tti:F2}, normTTI={normTTI:F2}, Score={score:F2}");

        return score;
    }

    void OnDrawGizmos()
    {
        if (interceptor == null || threat == null) return;

        Vector3 threatPos3D = threat.GetGameObject().transform.position;
        Vector3 interceptorPos3D = interceptor.GetGameObject().transform.position;

        Vector3 velocity = threat.GetVelocity();
        float speed = velocity.magnitude;

        if (speed < 0.01f) return;

        Vector2 threatPos = new Vector2(threatPos3D.x, threatPos3D.z);
        Vector2 interceptorPos = new Vector2(interceptorPos3D.x, interceptorPos3D.z);
        Vector2 reverseDir = -new Vector2(velocity.x, velocity.z).normalized;
        float innerR = interceptor.InnerLimitRadius;
        float outerR = interceptor.OuterLimitRadius;

        Vector3 revLineEnd = threatPos3D + new Vector3(reverseDir.x, 0, reverseDir.y) * (outerR - innerR);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(threatPos3D, revLineEnd);

        Vector2 dRev = reverseDir * speed;
        Vector2 fRev = threatPos - interceptorPos;

        float aRev = Vector2.Dot(dRev, dRev);
        float bRev = 2 * Vector2.Dot(fRev, dRev);
        float cRev = Vector2.Dot(fRev, fRev) - innerR * innerR;
        float discriminantRev = bRev * bRev - 4 * aRev * cRev;

        if (discriminantRev >= 0)
        {
            float sqrtD = Mathf.Sqrt(discriminantRev);
            float t1 = (-bRev - sqrtD) / (2 * aRev);
            float t2 = (-bRev + sqrtD) / (2 * aRev);
            float intersectionTime = Mathf.Min(t1, t2);

            float backTravelDistance = Mathf.Abs(intersectionTime * speed);
            float maxAllowed = outerR - innerR;

            if (intersectionTime > 0 && backTravelDistance <= maxAllowed)
            {
                Vector2 intersection2D = threatPos + reverseDir * speed * intersectionTime;
                Vector3 point3D = new Vector3(intersection2D.x, threatPos3D.y, intersection2D.y);

                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(point3D, 1f);
            }
        }
    }

}


