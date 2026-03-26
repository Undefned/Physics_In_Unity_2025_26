using UnityEngine;

public class BilliardBallDragController : MonoBehaviour
{
    public float minVelocity = 0.1f; // Минимальная скорость для остановки
    public float dragMultiplier = 0.98f; // Множитель замедления

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity.magnitude > minVelocity)
        {
            // Постепенно уменьшаем скорость
            rb.linearVelocity *= dragMultiplier;
            rb.angularVelocity *= dragMultiplier;
        }
        else
        {
            // Полная остановка при малой скорости
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // rb.Sleep();
        }
    }
}