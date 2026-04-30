using UnityEngine;
using UnityEngine.Timeline;

public class MeleeWeapon : WeaponBase
{
    [Header("Режимы ближнего боя")]
    public bool useAreaAttack = false;    // Режим 1: Атака по области
    public bool forceHorizontal = false;  // Режим 2: Только горизонтально

    public LayerMask hitLayers;

    protected override void ExecuteAttack(Health target)
    {
        if (useAreaAttack)
        {
            PerformAreaAttack();
        }
        else
        {
            PerformRaycastAttack(target);
        }
    }

    private void PerformAreaAttack()
    {
        // Кидаем сферический каст (OverlapCircle)
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, attackRange, hitLayers);

        foreach (var col in targets)
        {
            Health h = col.GetComponent<Health>();
            if (h != null && h != master)
            {
                h.TakeDamage(damage, transform.position);
            }
        }
        // Дебаг сферы (рисуем круг в Гизмосах или через Debug)
    }

    private void PerformRaycastAttack(Health target)
    {
        Vector2 direction;

        if (target != null && !forceHorizontal)
        {
            // Свободное прицеливание в цель
            direction = (target.transform.position - transform.position).normalized;
        }
        else
        {
            // Только по горизонтали (вправо или влево)
            direction = transform.right;
            if (forceHorizontal) direction.y = 0;
            direction.Normalize();
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, attackRange, hitLayers);
        Debug.DrawRay(transform.position, direction * attackRange, Color.red, 0.1f);

        if (hit.collider != null)
        {
            Health h = hit.collider.GetComponent<Health>();
            if (h != null && h != master)
            {
                h.TakeDamage(damage, transform.position);
            }
        }
    }

    // Отрисовка радиуса атаки в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}