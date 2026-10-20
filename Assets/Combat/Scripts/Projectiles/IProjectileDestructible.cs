namespace RPGame.Combat.Projectiles
{
    public enum ProjectileTeam
    {
        Player,
        Enemy
    }

    public interface IProjectileDestructible
    {
        ProjectileTeam Team { get; }
        void DestroyProjectile();
    }
}
