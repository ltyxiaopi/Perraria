using UnityEngine;

public struct ProjectileLaunchParams
{
    public int Damage;
    public float Knockback;
    public float Lifetime;
    public bool UseGravity;
    public Projectile.Owner Owner;
    public LayerMask TargetLayer;
    public LayerMask TerrainLayer;
    public bool StickOnTerrain;
    public ItemData PickupItemOnStick;
}
