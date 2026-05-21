using UnityEngine;

public class DraggableClamp : MonoBehaviour
{
    // Камера нужна для перевода координат мыши из экрана в мир.
    private Camera mainCamera;

    // Флаг активного перетаскивания текущего зажима.
    private bool isDragging;

    void Start()
    {
        // Кэшируем ссылку на основную камеру сцены.
        mainCamera = Camera.main;
    }

    void OnMouseDown()
    {
        // Начинаем перетаскивание при нажатии на объект.
        isDragging = true;
    }

    void OnMouseUp()
    {
        // Завершаем перетаскивание при отпускании кнопки мыши.
        isDragging = false;
    }

    void Update()
    {
        if (isDragging)
        {
            // Двигаем зажим только по оси X, сохраняя текущую высоту (Y).
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePosition.x, transform.position.y, 0);
        }
    }
}
