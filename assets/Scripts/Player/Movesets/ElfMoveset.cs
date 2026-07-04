using UnityEngine;

// Elf: fast 3-hit combo, short window between swings, modest reach on the first two
// hits with a punchier, longer-reaching finisher — the "many quick hits, big finish"
// archetype, contrasting Knight's 2-step swing and Wizard's single deliberate strike.
public class ElfMoveset : IWeaponMoveset
{
    private static readonly ComboStep[] Steps =
    {
        new ComboStep(0.8f, 0.9f, 0.7f),
        new ComboStep(0.85f, 0.95f, 0.75f),
        new ComboStep(1.5f, 1.2f, 1.3f),
    };

    public float comboWindow = 0.4f;

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
