using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy3D : MonoBehaviour
{
    private NavMeshAgent agent;
    private PlayerFP targetPlayer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Ищем игрока по компоненту PlayerFP
        targetPlayer = Object.FindFirstObjectByType<PlayerFP>();
    }

    void Update()
    {
        // Проверка: есть ли игрок и стоит ли враг на навигационной сетке
        if (targetPlayer != null && agent.isOnNavMesh)
        {
            // Просто обновляем точку назначения на текущую позицию игрока
            agent.SetDestination(targetPlayer.transform.position);
        }
    }
}