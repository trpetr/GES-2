using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 5f; // Чтобы снаряды не летели вечно, если промахнулись

    [HideInInspector] public float damage;
    [HideInInspector] public Health master;

    private Vector2 _direction;

    // Метод инициализации снаряда
    public void Setup(float dmg, Vector2 dir, Health owner)
    {
        damage = dmg;
        _direction = dir;
        master = owner;

        // Поворачиваем снаряд "лицом" по направлению полета (визуально)
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Уничтожаем через время, чтобы не копились в памяти
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Двигаем снаряд в заданном направлении
        transform.position += (Vector3)_direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Health h = collision.GetComponent<Health>();

        // Проверяем: есть ли компонент Health и не является ли он владельцем оружия
        if (h != null && h != master && h.entityType == Health.EntityType.Player)
        {
            h.TakeDamage(damage);
            Destroy(gameObject);
        }
        // Опционально: уничтожать снаряд при ударе об стены (слой Ground)
        else if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Ground")) != 0)
        {
            Destroy(gameObject);
        }
    }
}