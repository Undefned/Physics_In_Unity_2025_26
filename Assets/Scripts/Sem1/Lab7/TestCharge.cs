using UnityEngine;

// Частица, которая движется под действием электрического поля
public class TestCharge : MonoBehaviour
{
    public float charge = 1f;      // Величина заряда частицы
    public float mass = 1f;        // Масса частицы
    public ElectricFieldSystem fieldSystem;

    private Vector3 currentVelocity;

    void FixedUpdate()
    {
        if (fieldSystem == null) return;

        // Получаем поле в текущей позиции
        Vector3 field = fieldSystem.GetTotalFieldAt(transform.position);

        // Сила, действующая на частицу: F = q * E
        Vector3 force = charge * field;

        // Ускорение по второму закону Ньютона: a = F / m
        Vector3 acceleration = force / mass;

        // Обновляем скорость и позицию
        currentVelocity += acceleration * Time.fixedDeltaTime;
        transform.position += currentVelocity * Time.fixedDeltaTime;
    }
}