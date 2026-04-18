using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnerConfig", menuName = "Perraria/Enemy Spawner Config")]
public sealed class EnemySpawnerConfig : ScriptableObject
{
    public float SpawnInterval = 3f;
    public int MaxConcurrent = 8;
    public float MinSpawnDistance = 14f;
    public float MaxSpawnDistance = 22f;
    public float SkyRaycastHeight = 20f;
    public float RaycastDistance = 60f;
    public LayerMask GroundLayer;
    public List<EnemySpawnEntry> SpawnTable = new();
}
