namespace RPGame.Core.Spells
{
    [SpellDisplayTag("AoE")]
    public interface IAoECapability
    {
        float Radius { get; }
    }

    [SpellDisplayTag("Duration")]
    public interface IDurationCapability
    {
        float Duration { get; }
    }

    [SpellDisplayTag("Control")]
    public interface IControlCapability
    {
        float ControlPower { get; }
    }

    [SpellDisplayTag("Orb")]
    public interface IOrbCapability
    {
        int OrbCount { get; }
    }

    [SpellDisplayTag("Projectile")]
    public interface IProjectileCapability
    {
    }

    public interface IWaveCapability : IAoECapability
    {
        float WaveAngle { get; }
        float WaveRange { get; }
    }

    public interface IZoneCapability : IAoECapability
    {
    }
}
