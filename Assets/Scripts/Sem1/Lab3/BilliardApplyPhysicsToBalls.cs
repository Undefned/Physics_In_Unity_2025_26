using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BilliardApplyPhysicsToBalls : MonoBehaviour
{
    [Header("Physics Settings")]
    public PhysicsMaterial ballPhysicsMaterial; // Физический материал для упругости
    
    [Header("Ball Settings")]
    public string ballTag = "BilliardBall"; // Тег для поиска шаров
    public string cueBallTag = "CueBall"; // Тег для поиска шаров
    public float ballMass = 1.0f;
    public float ballDrag = 0.1f;
    public float ballAngularDrag = 0.05f;
    
    [Header("Auto Setup")]
    public bool setupOnStart = true;
    public bool includeInactiveBalls = false;

    // [Header("Balls")]
    // public int countOfCommonBalls = 0;

    private List<GameObject> allBalls = new List<GameObject>();
    private GameObject cueBall = new GameObject();
    

    void Start()
    {
        if (setupOnStart)
        {
            ApplyPhysicsToAllBalls();
        }
    }

    // Основной метод применения физики ко всем шарам
    [ContextMenu("Apply Physics To All Balls")]
    public void ApplyPhysicsToAllBalls()
    {
        FindAllBalls();
        ApplyRigidBodyToAllBalls();
        ApplyPhysicsMaterialToAllBalls();
        
        Debug.Log($"Successfully applied physics to {allBalls.Count} balls");
    }

    // Найти все шары на сцене
    private void FindAllBalls()
    {
        allBalls.Clear();

        // Поиск по тегу
        GameObject[] taggedBalls = GameObject.FindGameObjectsWithTag(ballTag);
        // countOfCommonBalls = taggedBalls.Length;

        cueBall = GameObject.FindGameObjectWithTag(cueBallTag);

        allBalls.AddRange(taggedBalls);
        allBalls.Add(cueBall);
        
        Debug.Log($"Found {allBalls.Count} balls on scene");
    }

    // Применить Rigidbody ко всем шарам
    private void ApplyRigidBodyToAllBalls()
    {
        foreach (GameObject ball in allBalls)
        {
            if (ball == null) continue;
            
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = ball.AddComponent<Rigidbody>();
                Debug.Log($"Added Rigidbody to: {ball.name}");
            }

            // Настройка параметров Rigidbody
            rb.mass = ballMass;
            rb.linearDamping = ballDrag;
            rb.angularDamping = ballAngularDrag;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // Для битка другие настройки
            if (ball.name.Contains(cueBallTag))
            {
                // Особые настройки для битка
                rb.mass = ballMass * 1.2f; // Чуть тяжелее
            }
        }
    }

    // Применить физический материал ко всем шарам
    private void ApplyPhysicsMaterialToAllBalls()
    {
        // Создаем физический материал по умолчанию, если не назначен
        if (ballPhysicsMaterial == null)
        {
            ballPhysicsMaterial = CreateDefaultPhysicsMaterial();
            Debug.Log("Created default physics material");
        }

        foreach (GameObject ball in allBalls)
        {
            if (ball == null) continue;
            
            // Получаем или добавляем коллайдер
            SphereCollider collider = ball.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = ball.AddComponent<SphereCollider>();
                Debug.Log($"Added SphereCollider to: {ball.name}");
            }

            // Применяем физический материал
            collider.material = ballPhysicsMaterial;
        }
    }

    // Создание физического материала по умолчанию
    private PhysicsMaterial CreateDefaultPhysicsMaterial()
    {
        PhysicsMaterial material = new PhysicsMaterial();
        
        // НАСТРОЙКА ФИЗИЧЕСКОГО МАТЕРИАЛА ДЛЯ УПРУГОСТИ:
        material.bounciness = 0.8f;        // Коэффициент упругости (0-1)
        material.dynamicFriction = 0.1f;   // Трение при движении
        material.staticFriction = 0.1f;    // Трение покоя
        material.bounceCombine = PhysicsMaterialCombine.Maximum;
        material.frictionCombine = PhysicsMaterialCombine.Minimum;
        
        return material;
    }

    // Метод для обновления физики без поиска шаров
    [ContextMenu("Update Physics Settings Only")]
    public void UpdatePhysicsSettings()
    {
        if (allBalls.Count == 0)
        {
            FindAllBalls();
        }
        
        ApplyRigidBodyToAllBalls();
        ApplyPhysicsMaterialToAllBalls();
        
        Debug.Log($"Updated physics settings for {allBalls.Count} balls");
    }
}