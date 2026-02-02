using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VehicleSpawnData
{
    public GameObject prefab;
    public int count;
}

public class NPCSpawner : MonoBehaviour
{
    [SerializeField] private float minDist = 80f;
    [SerializeField] private List<VehicleSpawnData> vehiclesToSpawn = new List<VehicleSpawnData>();
    public RoadGraph graph;
    [SerializeField] private Transform player;
    private List<Vector3> usedPositions = new List<Vector3>();

    void Start()
    {
        StartCoroutine(SpawnVehiclesAfterGraphReady());
    }

    IEnumerator SpawnVehiclesAfterGraphReady()
    {
        if (graph == null)
        {
            Debug.LogError("RoadGraph not assigned");
            yield break;
        }

        // Load graph if needed
        yield return StartCoroutine(graph.LoadGraphCoroutine());
        yield return null;

        if (graph.Nodes == null || graph.Nodes.Count == 0)
        {
            Debug.LogError("RoadGraph failed to load or is empty.");
            yield break;
        }

        // Spawn all vehicle types
        foreach (var v in vehiclesToSpawn)
        {
            SpawnVehicles(v.count, v.prefab);
        }
    }

    void SpawnVehicles(int count, GameObject prefab)
    {
        int tries = 0;
        int spawned = 0;
        int maxTries = 1000;

        while (spawned < count && tries < maxTries)
        {
            tries++;

            LaneNode node = graph.Nodes[Random.Range(0, graph.Nodes.Count)];
            if (node.Outgoing.Count == 0) continue;

            Vector3 spawnPos = node.Position;
            // Prevent spawning too close to the player
            if (player != null)
            {
                float playerDistance = Vector3.Distance(player.position, spawnPos);
                if (playerDistance < minDist)
                    continue;
            }
            // Minimum spacing check
            bool tooClose = false;
            foreach (var pos in usedPositions)
            {
                if (Vector3.Distance(pos, spawnPos) < minDist)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Force prefab’s original Y height
            spawnPos.y = prefab.transform.position.y;

            // Face the next node
            Vector3 direction = node.Outgoing[0].Position - node.Position;
            direction.y = 0;
            Quaternion rotation = Quaternion.LookRotation(direction);

            GameObject vehicle = Instantiate(prefab, spawnPos, rotation);
            vehicle.name = $"{prefab.name}_{spawned}";

            usedPositions.Add(spawnPos);
            spawned++;
        }

        if (tries >= maxTries)
        {
            Debug.LogWarning($"Could not spawn all {count} of {prefab.name} (max tries reached).");
        }
    }
}
