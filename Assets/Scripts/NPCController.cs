using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    public float speed = 10f;
    public float rotationSpeed = 1f;
    public float nodeReachThreshold = 1.5f;
    public float unsafeDistance = 5f;
    public float minTailgatingDistance = 2.3f, midTailgatingDistance = 3.5f, maxTailgatingDistance = 5f;
    public float minTailgatingPenalty = 5f, midTailgatingPenalty = 10f, maxTailgatingPenalty = 20f;
    public float minStoppingDistance = 2f;
    public float decelerationFactor = 30f;
    public float accelerationFactor = 2f;

    private float unpredictability = 100f;

    private LaneNode currentNode;
    private LaneNode targetNode;
    private LaneNode previousNode;
    private Rigidbody rb;

    [SerializeField] private RoadGraph roadGraph;
    [SerializeField] private Transform rayOrigin; // ✅ NEW

    private bool TailgatingMax = false, TailgatingMid = false, TailgatingMin = false;
    private float currentSpeed;

    public GameManagerBase gameManager;
    private int vehicleLayerMask;
    private NPCIndicator npcIndicator = null;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        TryGetComponent<NPCIndicator>(out npcIndicator);

        if (rayOrigin == null)
        {
            Debug.LogWarning($"{name}: RayOrigin not assigned, using transform.position");
        }

        currentSpeed = speed;
        vehicleLayerMask = LayerMask.GetMask("Vehicles");

        StartCoroutine(InitNPC());
    }

    IEnumerator InitNPC()
    {
        if (roadGraph == null)
        {
            Debug.LogError("No roadgraph assigned");
            yield break;
        }

        yield return StartCoroutine(roadGraph.LoadGraphCoroutine());
        yield return null;

        if (roadGraph.Nodes == null || roadGraph.Nodes.Count == 0)
        {
            Debug.LogError("No nodes in RoadGraph");
            yield break;
        }

        currentNode = roadGraph.GetClosestNode(transform.position, 20f);
        if (currentNode != null && currentNode.Outgoing.Count > 0)
        {
            targetNode = currentNode.Outgoing[Random.Range(0, currentNode.Outgoing.Count)];
        }

        if (gameObject.name.Contains("Truck"))
        {
            Debug.Log("Initialized NPC truck with graph size: " + roadGraph.Nodes.Count);
        }
    }

    void FixedUpdate()
    {
        if (!gameManager.gameplayActive)
            return;

        if (currentNode == null || targetNode == null)
            return;

        MoveTowardsTarget();
        CheckUnsafeDistance();
    }

    void MoveTowardsTarget()
    {
        Vector3 toTarget = targetNode.Position - transform.position;
        toTarget.y = 0;

        if (toTarget.magnitude <= nodeReachThreshold)
        {
            previousNode = currentNode;
            currentNode = targetNode;

            List<LaneNode> options = currentNode.Outgoing.ToList();

            if (options.Count > 0)
            {
                targetNode = options[Random.Range(0, options.Count)];

                if (Random.Range(0, 1000) < unpredictability)
                {
                    List<LaneNode> prospects =
                        roadGraph.GetNodesFacing(
                            roadGraph.GetNearbyNodes(
                                currentNode.Position,
                                targetNode.Position,
                                15f,
                                0.9f),
                            targetNode.Position - currentNode.Position,
                            0.9f);

                    if (prospects.Count > 0)
                        targetNode = prospects[Random.Range(0, prospects.Count)];
                }
            }
            else
            {
                targetNode = null;
            }

            return;
        }

        Vector3 dir = toTarget.normalized;

        Vector3 horizontalVelocity = dir * currentSpeed;
        horizontalVelocity.y = rb.velocity.y;
        rb.velocity = horizontalVelocity;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
    }

    void CheckUnsafeDistance()
    {
        Vector3 origin = rayOrigin != null ? rayOrigin.position : transform.position;

        Ray rayBack   = new Ray(origin, -transform.forward);
        Ray rayFront  = new Ray(origin, transform.forward);
        Ray rayFrontL = new Ray(origin, transform.forward + 0.2f * Vector3.Cross(transform.up, transform.forward));
        Ray rayFrontR = new Ray(origin, transform.forward - 0.2f * Vector3.Cross(transform.up, transform.forward));

        if (checkHit(rayBack, minTailgatingDistance, vehicleLayerMask))
        {
            if (!TailgatingMin)
                gameManager.UpdateScore(-(int)maxTailgatingPenalty, "Driving too close behind another vehicle: severity 3/3");
            TailgatingMin = true;
        }
        else TailgatingMin = false;

        if (checkHit(rayBack, midTailgatingDistance, vehicleLayerMask))
        {
            if (!TailgatingMid)
                gameManager.UpdateScore(-(int)midTailgatingPenalty, "Driving too close behind another vehicle: severity 2/3");
            TailgatingMid = true;
        }
        else TailgatingMid = false;

        if (checkHit(rayBack, maxTailgatingDistance, vehicleLayerMask))
        {
            if (!TailgatingMax)
                gameManager.UpdateScore(-(int)minTailgatingPenalty, "Driving too close behind another vehicle: severity 1/3");
            TailgatingMax = true;
        }
        else TailgatingMax = false;

        float frontCheckDistance = (1 + rb.velocity.magnitude / speed) * unsafeDistance;

        List<float> hitDistances = new List<float>();
        RaycastHit hit;

        if (Physics.Raycast(rayFront, out hit, frontCheckDistance, vehicleLayerMask))
            hitDistances.Add(hit.distance);
        if (Physics.Raycast(rayFrontL, out hit, frontCheckDistance, vehicleLayerMask))
            hitDistances.Add(hit.distance);
        if (Physics.Raycast(rayFrontR, out hit, frontCheckDistance, vehicleLayerMask))
            hitDistances.Add(hit.distance);

        bool frontHit = hitDistances.Count > 0;
        float minDistance = frontHit ? hitDistances.Min() : float.MaxValue;

        if (npcIndicator != null)
        {
            if (frontHit) npcIndicator.TurnOnLights();
            else npcIndicator.TurnOffLights();
        }

        float targetSpeed;

        if (frontHit && minDistance <= minStoppingDistance)
        {
            targetSpeed = 0f;
            currentSpeed = 0f;
        }
        else if (frontHit)
        {
            float t = (minDistance - minStoppingDistance) / (frontCheckDistance - minStoppingDistance);
            targetSpeed = speed * Mathf.Clamp01(t);
        }
        else
        {
            targetSpeed = speed;
        }

        if (targetSpeed < currentSpeed)
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, decelerationFactor * Time.fixedDeltaTime);
        else
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, accelerationFactor * Time.fixedDeltaTime);

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, speed);

        Debug.DrawRay(rayBack.origin, rayBack.direction * minTailgatingDistance, Color.red);
        Debug.DrawRay(rayBack.origin + Vector3.up * 0.1f, rayBack.direction * midTailgatingDistance, Color.yellow);
        Debug.DrawRay(rayBack.origin + Vector3.up * 0.2f, rayBack.direction * maxTailgatingDistance, Color.green);
        Debug.DrawRay(rayFront.origin, rayFront.direction * frontCheckDistance, Color.blue);
        Debug.DrawRay(rayFrontL.origin, rayFrontL.direction * frontCheckDistance, Color.blue);
        Debug.DrawRay(rayFrontR.origin, rayFrontR.direction * frontCheckDistance, Color.blue);
    }

    bool checkHit(Ray rayBack, float unsafeDistance, int VehicleLayerMask)
    {
        RaycastHit hit;
        return Physics.Raycast(rayBack, out hit, unsafeDistance, vehicleLayerMask) &&
               hit.collider.tag == "Player" &&
               hit.collider.GetComponentInParent<Rigidbody>().velocity != Vector3.zero;
    }
}
