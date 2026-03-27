using UnityEngine;

public class ElectronMovement : MonoBehaviour
{
    public float velocity = 0.25f; // скорость в условных единицах (уменьшено в 4 раза для лучшей видимости)
    public Transform anode;
    public static float voltage = 0f; // задерживающее напряжение

    private Rigidbody rb;
    private Vector3 direction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearDamping = 0.5f; // небольшое сопротивление для стабильности

        // Направление движения - от катода к аноду (в мировом пространстве)
        if (anode != null)
        {
            direction = (anode.position - transform.position).normalized;
            // Убеждаемся что направление корректное (от катода к аноду)
            if (direction.x < 0) direction = -direction;
        }
        else
        {
            direction = Vector3.right; // мировое направление +X
        }

        // Начальная скорость
        rb.linearVelocity = direction * velocity;

        Debug.Log($"[Electron] Spawned at {transform.position}, velocity={velocity}, direction={direction}");
    }
    
    void Update()
    {
        if (anode != null)
        {
            // Простая симуляция влияния напряжения
            // Положительное напряжение притягивает, отрицательное отталкивает
            Vector3 toAnode = (anode.position - transform.position).normalized;
            float force = voltage * 2f; // уменьшена сила воздействия напряжения
            rb.AddForce(toAnode * force);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Electron] OnTriggerEnter with {other.gameObject.name}, tag={other.tag}");
        if (other.CompareTag("Anode"))
        {
            // Электрон достиг анода — увеличиваем счётчик тока
            Debug.Log("[Electron] Collected by anode!");
            FindObjectOfType<Anode>()?.CollectElectron();
            Destroy(gameObject);
        }
    }
}