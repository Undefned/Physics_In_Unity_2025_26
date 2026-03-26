using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    public PushCube pushCubeSystem; // Привязать систему куба!

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cube"))
        {
            Debug.Log("Триггер финиша сработал!");
            // Принудительно устанавливаем достижение финиша
            pushCubeSystem.OnFinishReached();
        }
    }
}