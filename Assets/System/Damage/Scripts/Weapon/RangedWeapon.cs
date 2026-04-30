using UnityEngine;

public class RangedWeapon : WeaponBase
{
    public GameObject projectilePrefab;
    public Transform firePoint;

    protected override void ExecuteAttack(Health target)
    {
        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile2D projectile = projObj.GetComponent<Projectile2D>();

        if (projectile != null)
        {
            Vector2 direction;
            if (target != null)
                direction = (target.transform.position - firePoint.position).normalized;
            else
                direction = firePoint.right;

            projectile.Setup(damage, direction, master);
        }
    }
}