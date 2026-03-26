using UnityEngine;
using TMPro;

public class BilliardCueShoot : MonoBehaviour
{
    [Header("Launch Settings")]
    private string cueBallTag = "CueBall";
    public float power = 1f;
    

    void Start()
    {
        // Проверяем что коллайдер настроен как триггер
        Collider collider = GetComponent<Collider>();
        if (collider != null && !collider.isTrigger)
        {
            collider.isTrigger = true;
        }
    }

    public void OnTriggerEnter(Collider other) {
        if (other.CompareTag(cueBallTag)) {
            Rigidbody ballRb = other.GetComponent<Rigidbody>();
            Vector3 forceDirection = transform.forward * power;
            ballRb.AddForce(forceDirection, ForceMode.Impulse);
        }
    }
}