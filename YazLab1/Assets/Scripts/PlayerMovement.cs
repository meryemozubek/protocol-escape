using UnityEngine;

// Basit hareket icin konumu dogrudan Transform uzerinden degistir.
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(x, y).normalized;

        // Hareketi gecen sureyle carp; kare hizi degisince yurume hizi degismesin.
        transform.Translate(movement * speed * Time.deltaTime);
    }
}