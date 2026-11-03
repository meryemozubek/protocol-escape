using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Oyuncuyu Rigidbody2D uzerinden hareket ettir.
public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarlari")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // Capraz giderken hiz artmasin diye yon vektorunu normalize et.
        moveInput = new Vector2(moveX, moveY).normalized;
    }

    private void FixedUpdate()
    {
        
        // Tuslari Update icinde oku, fizik hizini sabit zaman adiminda uygula.
        rb.velocity = moveInput * moveSpeed;
    }
}