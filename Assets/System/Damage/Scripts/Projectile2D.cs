using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 5f;

    [Header("Настройки столкновений")]
    public LayerMask hitLayers; // Укажите слои врагов и окружения (Ground)

    [HideInInspector] public float damage;
    [HideInInspector] public Health master;

    private Vector2 _direction;

    public void Setup(float dmg, Vector2 dir, Health owner)
    {
        damage = dmg;
        _direction = dir;
        master = owner;

        // Визуальный поворот снаряда
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += (Vector3)_direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Проверяем, входит ли объект в разрешенные слои (битовая маска)
        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            Health h = collision.GetComponent<Health>();

            // 2. Если попали в объект с Health (и это не владелец)
            if (h != null && h != master)
            {
                h.TakeDamage(damage, transform.position);
                Destroy(gameObject);
            }
            // 3. Если попали в объект без Health, но это слой из маски (например, стена)
            else if (h == null)
            {
                Destroy(gameObject);
            }
        }
    }
}