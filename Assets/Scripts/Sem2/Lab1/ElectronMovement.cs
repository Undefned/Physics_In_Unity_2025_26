using UnityEngine;

public class ElectronMovement : MonoBehaviour
{
    public float velocity = 10f; // скорость в условных единицах
    public Transform anode;
    public static float voltage = 0f; // задерживающее напряжение
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        
        rb.useGravity = false;
        rb.linearVelocity = transform.right * velocity;
        
        // Эффект свечения электрона
        Material mat = GetComponent<Renderer>().material;
        if (mat != null)
            mat.color = Color.yellow;
    }
    
    void Update()
    {
        if (anode != null)
        {
            // Простая симуляция влияния напряжения
            // Положительное напряжение притягивает, отрицательное отталкивает
            Vector3 direction = (anode.position - transform.position).normalized;
            float force = voltage * 5f;
            rb.AddForce(direction * force);
        }
        
        // Добавляем эффект траектории (опционально)
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Anode"))
        {
            // Электрон достиг анода — увеличиваем счётчик тока
            FindObjectOfType<Anode>()?.CollectElectron();
            Destroy(gameObject);
        }
    }
}