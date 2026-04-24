namespace RecoilArena.Core
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(float amount, DamageContext context);
    }
}
