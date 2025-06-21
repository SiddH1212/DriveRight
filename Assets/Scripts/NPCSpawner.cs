using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [SerializeField] private int nCars = 0, nTrucks = 0;
    [SerializeField] private float minDist = 80f;
    [SerializeField] private GameObject carPrefabBlue, carPrefabRed;
    [SerializeField] private GameObject truckPrefab;
    public RoadGraph graph;

    private List<Vector3> usedPositions = new List<Vector3>();

    void Start()
    {
        // if (graph == null || graph.Nodes.Count == 0 || graph.Nodes[0].Outgoing.Count == 0 || graph.Nodes[0].Position == Vector3.zero){
        //     Debug.Log("Graph loaded from npc script");
        //     graph.Load();
        // }
        // if (graph == null || graph.Nodes.Count == 0)
        // {
        //     Debug.LogError("RoadGraph not assigned or empty!");
        //     return;
        // }

        // int blues = Random.Range(0, nCars+1);

        // SpawnVehicles(blues, carPrefabBlue);
        // SpawnVehicles(nTrucks, truckPrefab);
        // SpawnVehicles(nCars - blues, carPrefabRed);
        StartCoroutine(SpawnVehiclesAfterGraphReady());
    }
    IEnumerator SpawnVehiclesAfterGraphReady()
    {
        if (graph == null)
        {
            Debug.LogError("RoadGraph not assigned");
            yield break;
        }
        yield return StartCoroutine(graph.LoadGraphCoroutine());
        yield return null;
        if (graph.Nodes == null || graph.Nodes.Count == 0)
        {
            Debug.LogError("RoadGraph failed to load or is empty.");
            yield break;
        }
        int blues = Random.Range(0, nCars+1);

        SpawnVehicles(blues, carPrefabBlue);
        SpawnVehicles(nTrucks, truckPrefab);
        SpawnVehicles(nCars - blues, carPrefabRed);
    }
    void SpawnVehicles(int count, GameObject prefab)
    {
        int tries = 0;
        int spawned = 0;
        int maxTries = 1000;

        while (spawned < count && tries < maxTries)
        {
            tries++;

            // Get a random node with at least one outgoing connection
            LaneNode node = graph.Nodes[Random.Range(0, graph.Nodes.Count)];
            if (node.Outgoing.Count == 0) continue;

            Vector3 spawnPos = node.Position;
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

            // Use Y position of prefab
            float prefabY = prefab.transform.position.y;
            spawnPos.y = prefabY;

            // Face the next node
            Vector3 direction = node.Outgoing[0].Position - node.Position;
            direction.y = 0;
            Quaternion rotation = Quaternion.LookRotation(direction.normalized);

            // Instantiate and configure
            GameObject vehicle = Instantiate(prefab, spawnPos, rotation);
            NPCController npc = vehicle.GetComponent<NPCController>();
            // if (npc != null)
            // {
            //     npc.roadGraph = graph;
            // }

            usedPositions.Add(spawnPos);
            spawned++;
            vehicle.name = $"{prefab.name}_{spawned}";
        }

        if (tries >= maxTries)
        {
            Debug.LogWarning("Reached max tries while spawning. Some vehicles may not have spawned.");
        }
    }
}
