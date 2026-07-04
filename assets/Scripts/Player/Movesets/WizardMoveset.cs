using UnityEngine;

// Wizard: no combo chain, a single deliberate strike with longer reach and a softer
// knockback (a magical push rather than a physical stagger) — the "slow, hits hard,
// long range" archetype. Basic-attack differentiation only; the real spell-like AoE
// bursts (Q/E/R) already exist via StatsComponent's skill data, independent of this.
public class WizardMoveset : IWeaponMoveset
{
    private static readonly ComboStep[] Steps =
    {
        new ComboStep(1.3f, 1.6f, 0.6f),
    };

    public float comboWindow = 0.35f;

    private int _comboIndex;
    private float _comboWindowTimer;
    private float _baseKnockbackForce = -1f;

    public void Tick(float delta)
    {
        if (_comboWindowTimer <= 0f) return;
        _comboWindowTimer = Mathf.Max(_comboWindowTimer - delta, 0f);
        if (_comboWindowTimer == 0f)
        {
            _comboIndex = 0;
        }
    }

    public bool TryAttack(PlayerController player, MeleeHitbox hitbox)
    {
        ComboStep step = Steps[_comboIndex];
        Vector2 facing = player.GetFacingDir();
        if (facing == Vector2.zero) facing = Vector2.right;

        Transform pivot = player.GetAttackPivot();
        pivot.localPosition = facing * player.attackOffset * step.offsetMult;
        pivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);

        if (_baseKnockbackForce < 0f)
        {
            _baseKnockbackForce = hitbox.knockbackForce;
        }
        hitbox.knockbackForce = _baseKnockbackForce * step.knockbackMult;

        player.SetComboDamageMultiplier(step.damageMult);
        hitbox.ResetHits();
        hitbox.PerformAttack();
        player.SetComboDamageMultiplier(1f);

        _comboIndex = (_comboIndex + 1) % Steps.Length;
        _comboWindowTimer = comboWindow;
        return true;
    }
}
