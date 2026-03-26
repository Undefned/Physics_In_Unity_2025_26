using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BilliardBallController : MonoBehaviour
{
    public Transform spawnPoint;

    public void RespawnBall(string tag)
    {
        // Всегда ищем мяч по тегу, а не храним ссылку
        GameObject ball = GameObject.FindWithTag(tag);
        
        if (ball != null)
        {
            // Перемещаем существующий биток
            ball.transform.position = spawnPoint.position;
            
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            Debug.LogError("CueBall not found with tag: " + tag);
        }
    }
}