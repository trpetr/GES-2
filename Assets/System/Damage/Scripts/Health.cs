using UnityEngine;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    public enum EntityType { Player, NPC }
    public EntityType entityType;
    public float maxHealth = 100f;
    public float currentHealth;

    void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float damage, Vector2 sourcePosition)
    {
        currentHealth -= damage;

        // Если это игрок, вызываем отбрасывание
        if (entityType == EntityType.Player)
        {
            var player = GetComponent<PlayerPlatformer>();
            if (player != null) player.ApplyKnockback(sourcePosition);
        }

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (entityType == EntityType.Player)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        else
            Destroy(gameObject);
    }
}