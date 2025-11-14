using System.Collections.Generic;
using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    [SerializeField] private List<GameObject> _predator_spawn_areas;
    [SerializeField] private List<GameObject> _prey_spawn_areas;

    private void Awake()
    {
        // Validate spawn areas on initialization
        if (_predator_spawn_areas == null || _predator_spawn_areas.Count == 0)
        {
            Debug.LogError("SpawnArea: No predator spawn areas configured!", this);
        }

        if (_prey_spawn_areas == null || _prey_spawn_areas.Count == 0)
        {
            Debug.LogError("SpawnArea: No prey spawn areas configured!", this);
        }
    }

    public void PlacePredator(GameObject predator)
    {
        int spawnIndex = Random.Range(0, _predator_spawn_areas.Count);
        PlaceObject(predator, _predator_spawn_areas[spawnIndex]);
    }

    public void PlacePrey(GameObject prey)
    {
        int spawnIndex = Random.Range(0, _prey_spawn_areas.Count);
        PlaceObject(prey, _prey_spawn_areas[spawnIndex]);
    }

    private void PlaceObject(GameObject objectToPlace, GameObject spawnArea)
    {
        var spawnTransform = spawnArea.transform;
        var xRange = spawnTransform.localScale.x / 2.1f;
        var zRange = spawnTransform.localScale.z / 2.1f;

        // Use spawn area's Y position instead of hardcoded value
        objectToPlace.transform.position = spawnTransform.position + new Vector3(
            Random.Range(-xRange, xRange),
            0f,
            Random.Range(-zRange, zRange)
        );

        // Reset rotation on x-z plane only
        objectToPlace.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }
}
