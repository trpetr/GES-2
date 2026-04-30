using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Настройки")]
    public float damage = 10f;
    public float attackRange = 2f;
    public float attackRate = 1f;
    public bool isAutomatic = true;

    protected float nextAttackTime;
    protected Health master; // Ссылка на того, кто держит оружие

    protected virtual void Awake()
    {
        // Находим Health на том же объекте или родителе, чтобы не стрелять в себя
        master = GetComponentInParent<Health>();
    }

    protected virtual void Update()
    {
        if (isAutomatic && attackRange > 0)
        {
            AutoDetection();
        }
    }

    private void AutoDetection()
    {
        // Ищем всех в радиусе
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (var hit in hits)
        {
            Health target = hit.GetComponent<Health>();

            // Если нашли Health и это не мы сами
            if (target != null && target.entityType == Health.EntityType.Player)
            {
                TryAttack(target);
                break; // Атакуем первую подходящую цель
            }
        }
    }

    public void TryAttack(Health target = null)
    {
        if (Time.time >= nextAttackTime)
        {
            ExecuteAttack(target);
            nextAttackTime = Time.time + (1f / attackRate);
        }
    }

    protected abstract void ExecuteAttack(Health target);
}