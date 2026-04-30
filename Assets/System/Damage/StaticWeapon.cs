using UnityEngine;

public class StaticWeapon : WeaponBase
{
    public bool isDangerous = true;

    // Для статического оружия переопределяем Update, чтобы оно не искало цели сферой
    protected override void Update() { }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isDangerous) return;

        Health target = other.GetComponent<Health>();
        if (target != null) TryAttack(target);
    }

    protected override void ExecuteAttack(Health target)
    {
        if (target != null) target.TakeDamage(damage);
    }
}