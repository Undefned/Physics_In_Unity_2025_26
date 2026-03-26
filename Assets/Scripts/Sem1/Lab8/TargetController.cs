using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetController : MonoBehaviour
{
    [Header("Настройки мишени")]
    public float rotationSpeed = 30f;
    // public ParticleSystem hitParticles;
    public TMP_Text scoreText;
    
    private int score = 0;
    
    void Update()
    {
        // Вращение мишени для наглядности
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Particle"))
        {
            // Частица попала в мишень
            score++;
            UpdateScore();
            Destroy(gameObject);
            
            // Эффект попадания
            // if (hitParticles != null)
            // {
            //     Instantiate(hitParticles, transform.position, Quaternion.identity);
            // }
            
            // Уничтожаем частицу
            // Destroy(other.gameObject);
        }
    }
    
    void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }
    
    public void ResetScore()
    {
        score = 0;
        UpdateScore();
    }
}