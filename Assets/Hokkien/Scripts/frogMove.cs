using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class frogMove : MonoBehaviour
{
    private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private bool movementPaused;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        if (movementPaused)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            if (animator != null)
                animator.SetBool("isWalking", false);

            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

        rb.linearVelocity = moveInput * moveSpeed;
        animator.SetBool("isWalking", moveInput != Vector2.zero);

        if (moveInput != Vector2.zero)
        {
            animator.SetFloat("InputX", moveInput.x);
            animator.SetFloat("InputY", moveInput.y);
        }
        else
        {
            animator.SetFloat("LastInputX", animator.GetFloat("InputX"));
            animator.SetFloat("LastInputY", animator.GetFloat("InputY"));
        }

    }

    public void SetMovementPaused(bool isPaused)
    {
        movementPaused = isPaused;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (!movementPaused)
            return;

        moveInput = Vector2.zero;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetBool("isWalking", false);
    }
}
