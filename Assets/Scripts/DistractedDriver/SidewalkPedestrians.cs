using UnityEngine;

public class SidewalkPedestrians : MonoBehaviour
{
    public float walkSpeed = 1.5f;
    public Animator animator;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        animator.SetBool("isWalking", true); // Always play walking animation
    }

    void Update()
    {
        // Move forward in local space
        transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime);
    }
}
