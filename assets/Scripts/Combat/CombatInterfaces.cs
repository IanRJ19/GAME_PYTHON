public struct AttackPayload
{
    public int damage;
    public bool isCrit;

    public AttackPayload(int damage, bool isCrit)
    {
        this.damage = damage;
        this.isCrit = isCrit;
    }
}

public interface IAttacker
{
    int GetAttackDamage();
    AttackPayload GetAttackPayload();
}

public interface IKnockbackReceiver
{
    void ApplyKnockback(UnityEngine.Vector2 force);
}

public interface IDamageMitigator
{
    int MitigateIncomingDamage(int rawDamage);
}
