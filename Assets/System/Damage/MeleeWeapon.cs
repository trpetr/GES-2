using UnityEngine;

public class MeleeWeapon : WeaponBase
{
    public LayerMask hitLayers;

    protected override void ExecuteAttack(Health target)
    {
        Vector2 direction = (target != null)
            ? (Vector2)(target.transform.position - transform.position).normalized
            : (Vector2)transform.right;

        // Добавляем hitLayers в параметры Raycast
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, attackRange, hitLayers);

        Debug.DrawRay(transform.position, direction * attackRange, Color.red, 0.1f);

        if (hit.collider != null)
        {
            Health h = hit.collider.GetComponent<Health>();
            if (h != null)
                h.TakeDamage(damage);
        }
    }
}