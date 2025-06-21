using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ViolationDetector : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public GameManager gameManager;
    private CarIndicator carIndicator;
    private float relaxationTime = 2f;
    private char indication = 'F'; // forward

    [Header("Detection Settings")]
    [Tooltip("How far below the car we raycast to find the road")]
    public float groundCheckDistance = 2f;
    [Tooltip("LayerMask for your road meshes")]
    public LayerMask roadLayer;

    [Tooltip("Dot-product threshold for wrong-way")]
    public float thresh = 0.1f;
    [SerializeField] private float heightOffset = 0f;
    public float threshTurnAngle = 30;


    private RoadGenerator currentRoad;
    private List<float3> points = new List<float3>();
    private bool correctWay = true;
    // private bool leftLane = true;
    public bool onRoad = true, onIntersection = false;
    private Vector3 prevRot;
    public int lane = 0;
    private int prevLane = 0;
    float prevTime, prevTimeRoad;

    void Start()
    {
        carIndicator = GetComponent<CarIndicator>();
        prevRot = transform.forward;
        prevTime = Time.time;
        prevTimeRoad = Time.time;
    }
    void FixedUpdate()
    {
        // Raycast down to find which RoadGenerator we're over
        if (Physics.Raycast(player.transform.position + Vector3.up * heightOffset - Vector3.down * 0.2f, Vector3.down, out var hit, groundCheckDistance, roadLayer))
        {
            var rg = (RoadGenerator)null;
            if (!onRoad && Time.time > prevTimeRoad + relaxationTime)
            {
                onRoad = true;
                prevTimeRoad = Time.time;
            }
            if (hit.collider.name == "Plane" && onRoad)
            {
                gameManager.UpdateScore(-10, "Driving outside the road");
                onRoad = false;
                prevTimeRoad = Time.time;
                return;
            }

            else if (hit.collider.name != "Plane" && !onRoad)
            {
                // gameManager.UpdateScore(10, "Back on the road");
                onRoad = true;
            }
            if (hit.collider.name.Contains("Intersection") && !onIntersection)
            {
                onIntersection = true;
                prevRot = transform.forward;
                if (carIndicator.rightOn) indication = 'R';
                else if (carIndicator.leftOn) indication = 'L';
                else indication = 'F';
            }
            if (hit.collider.name.Contains("Intersection")) return;

            if (hit.collider.GetComponentInParent<RoadGenerator>() != null)
            {
                rg = hit.collider.GetComponentInParent<RoadGenerator>();
            }

            // Debug.Log(hit.collider.name);
            if (rg != null)
            {
                var prevRoad = currentRoad;
                // Switched to a new road → rebuild point list
                if (rg != prevRoad)
                {
                    correctWay = true;  // reset per‑road
                    currentRoad = rg;
                    points.Clear();
                    foreach (var p in currentRoad.pIn) points.Add(p);
                    foreach (var p in currentRoad.pOut) points.Add(p);
                    
                }

                // also check if the user indicated correctly if he just came from an intersection
                if (onIntersection)
                {
                    onIntersection = false;
                    lane = GetLane();
                    Vector3 prevDir = new Vector3(prevRot.x, 0f, prevRot.z).normalized;
                    Vector3 currDir = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                    float turnAngle = Vector3.SignedAngle(currDir, prevDir, Vector3.up);
                    // Debug.Log("Angle Turned: " + turnAngle);
                    carIndicator.TurnOffIndicators();
                    if (prevRoad == currentRoad && (turnAngle > 180 - threshTurnAngle || turnAngle < -180 + threshTurnAngle))
                    {
                        if (indication == 'F') gameManager.UpdateScore(-5, "Did not indicate before U-turn");
                        else if (indication != 'R') gameManager.UpdateScore(-5, "Wrong Indication before U-turn");
                        else if (prevLane != prevRoad.n_lanes - 1) gameManager.UpdateScore(-3, "U-turn should be made from the right lane");
                        else gameManager.UpdateScore(+1, "Correctly indicated U-turn");
                    }
                    else if (turnAngle > threshTurnAngle && (indication == 'L'))
                    {
                        if (prevLane != 0) gameManager.UpdateScore(-3, "Left turn should be made from the left lane");
                        else gameManager.UpdateScore(+1, "Correctly indicated left turn");
                    }
                    else if (turnAngle < -threshTurnAngle && (indication == 'R'))
                    {
                        if (prevLane != prevRoad.n_lanes - 1) gameManager.UpdateScore(-3, "Right turn should be made from the right lane");
                        else gameManager.UpdateScore(+1, "Correctly indicated right turn");
                    }
                    else if (turnAngle >= threshTurnAngle || turnAngle <= -threshTurnAngle)
                    {
                        if (indication == 'F') gameManager.UpdateScore(-5, "Did not indicate appropriately before turning");
                        else gameManager.UpdateScore(-5, "Wrong indication before turning");
                    }
                    else if (indication != 'F')
                    {
                        gameManager.UpdateScore(-5, "Indicated without turning");
                    }

                    prevLane = lane;
                }
            }
        }
        else
        {
            // No road found underneath
            return;
        }

        if (points.Count == 0) return;

        // Find the closest point in the merged pIn/pOut list
        float minDist = float.MaxValue;
        int minIdx = 0;
        Vector3 carPos = player.transform.position;
        for (int i = 0; i < points.Count; i++)
        {
            float d = Vector3.Distance(carPos, (Vector3)points[i]);
            if (d < minDist)
            {
                minDist = d;
                minIdx = i;
            }
        }

        // Debug.Log(perLaneWidth + " " + minDist);
        // Debug.Log("The lane number: " + lane);
        // if (minDist > currentRoad.width / 2)
        // {
        //     lane = 1;
        // }
        // else
        // {
        //     lane = 0;
        // }

        // Compute the "correct" direction to drive for that segment
        Vector3 correctDir;
        int split = currentRoad.pIn.Count;
        if (minIdx < split)
        {
            // left lane
            // leftLane = true;
            if (minIdx == 0) correctDir = points[1] - points[0];
            else correctDir = points[minIdx] - points[minIdx - 1];

        }
        else
        {
            // right lane
            // leftLane = false;
            int idxOut = minIdx - split;
            if (idxOut == 0) correctDir = -(points[split + 1] - points[split]);
            else correctDir = -(points[minIdx] - points[minIdx - 1]);
        }

        // To check car alignment
        float alignment = Vector3.Dot(player.transform.forward.normalized, -correctDir.normalized);

        // Report violation or return to correct
        if (onRoad && !correctWay && Time.time > prevTime + relaxationTime)
        {
            Debug.Log($"Greater: {prevTime}+{relaxationTime} than {Time.time}");
            prevTime = Time.time;
            correctWay = true;
        }
        if (onRoad && correctWay && (alignment < -thresh || Vector3.Dot(transform.gameObject.GetComponent<Rigidbody>().velocity, -transform.forward) > 0.5f))
        {
            Debug.Log($"Correct Way: {correctWay}");
            correctWay = false;
            gameManager.UpdateScore(-5, "Wrong Way");
            prevTime = Time.time;
            Debug.Log($"Correct Way: {correctWay}");
            Debug.Log($"Time: {Time.time}");
        }
        else if (onRoad && !correctWay && alignment > thresh && Vector3.Dot(transform.gameObject.GetComponent<Rigidbody>().velocity, -transform.forward) < 0.5f)
        {
            correctWay = true;
            prevLane = lane;
            Debug.Log($"OnRoad, Lane {lane} and {prevLane}");
            // gameManager.UpdateScore(+10, "Right Way");
        }

        lane = GetLane();
        if (onRoad && prevLane != lane)
        {
            if (prevLane < lane && !carIndicator.rightOn) gameManager.UpdateScore(-5, "Changed Lane to right without indicating");
            else if (prevLane > lane && !carIndicator.leftOn) gameManager.UpdateScore(-5, "Changed Lane to left without indicating");
        }

        prevLane = lane;
    }

    int GetLane()
    {
        float minDist = float.MaxValue;
        Vector3 carPos = player.transform.position;
        for (int i = 0; i < points.Count; i++)
        {
            float d = Vector3.Distance(carPos, (Vector3)points[i]);
            if (d < minDist)
            {
                minDist = d;
            }
        }

        int perLaneWidth = (int)(currentRoad.width / (currentRoad.bidirectional ? 1 : 2)) / currentRoad.n_lanes;
        lane = (int)minDist / perLaneWidth;
        lane = Mathf.Min(lane, currentRoad.n_lanes - 1);

        return lane;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the ground‑ray
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine((player.transform.position + Vector3.up * heightOffset - Vector3.down * 0.2f),
                            player.transform.position + Vector3.down * groundCheckDistance);
        }
    }
}