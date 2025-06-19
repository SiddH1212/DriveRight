using UnityEngine;

public class PedestrianMovement : MonoBehaviour
{
    public float speed = 1.5f;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

    }

    void Update()
    {
        if (animator.GetBool("isWalking"))
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }
}
