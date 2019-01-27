using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HamtaroController : MonoBehaviour
{
    public GameObject ball;
    private Vector3 prevPos;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 currPos = ball.transform.position;
        this.transform.position = currPos;
        // prevPos = currPos;

        var ballrb = ball.GetComponent<Rigidbody>();
        var xzmove = ballrb.velocity;
        xzmove.y = 0;
        animator.SetFloat("MoveSpeed", xzmove.magnitude);

        this.transform.LookAt(this.transform.position + xzmove);
    }
}
