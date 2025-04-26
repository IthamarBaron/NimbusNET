using System.Collections.Generic;
using UnityEngine;

public class CityGridManager : MonoBehaviour
{
    public static CityGridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 50f;

    [Header("City References")]
    [SerializeField] private Transform cityParent;

    public Dictionary<Vector2Int, List<City>> cityGrid = new();
    private List<City> allCities = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeGrid();
    }

    private void InitializeGrid()
    {
        // Loop over all child objects under cityParent
        foreach (Transform child in cityParent)
        {
            City city = child.GetComponent<City>();
            if (city == null) continue;

            allCities.Add(city);

            // Get bounding box of the city's trigger collider
            Bounds bounds = city.dangerZoneCollider.bounds;

            // Convert world bounds to grid cell range
            int minX = Mathf.FloorToInt(bounds.min.x / cellSize);
            int maxX = Mathf.FloorToInt(bounds.max.x / cellSize);
            int minZ = Mathf.FloorToInt(bounds.min.z / cellSize);
            int maxZ = Mathf.FloorToInt(bounds.max.z / cellSize);

            // Register this city into all overlapping cells
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector2Int cell = new(x, z);

                    if (!cityGrid.ContainsKey(cell))
                        cityGrid[cell] = new List<City>();

                    cityGrid[cell].Add(city);
                }
            }
        }
    }

    public City GetClosestCity(Vector3 position)
    {
        Vector2Int key = WorldToGrid(position);
        List<City> candidates = new();

        // Check current cell and its 8 neighbors
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                Vector2Int check = key + new Vector2Int(dx, dz);
                if (cityGrid.TryGetValue(check, out var cities))
                    candidates.AddRange(cities);
            }
        }

        // Find the closest city from collected candidates
        City closest = null;
        float minDist = float.MaxValue;

        foreach (var city in candidates)
        {
            float dist = Vector3.Distance(position, city.GetCenter());
            if (dist < minDist)
            {
                minDist = dist;
                closest = city;
            }
        }

        return closest;
    }

    public Vector2Int WorldToGrid(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.z / cellSize)
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (cityGrid == null || cityGrid.Count == 0)
            return;

        Gizmos.color = Color.yellow;

        foreach (var cell in cityGrid)
        {
            Vector2Int gridPos = cell.Key;
            Vector3 worldPos = new Vector3(
                gridPos.x * cellSize + cellSize / 2f,
                0f,
                gridPos.y * cellSize + cellSize / 2f
            );

            // Draw a box for this cell
            Vector3 size = new Vector3(cellSize, 0.1f, cellSize);
            Gizmos.DrawWireCube(worldPos, size);

            // Optional: Color cells with cities
            if (cell.Value.Count > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(worldPos + Vector3.up * 0.05f, size * 0.95f);
                Gizmos.color = Color.yellow; // reset for next cell
            }
        }
    }



}
