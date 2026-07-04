// One implementation exists (KnightMoveset) — this interface only exists because C#
// requires it for PlayerController to call into per-class attack logic without a
// switch-on-type. Formalize further (shared base data, etc.) once Wizard/Elf movesets
// are added in Phase 1.
public interface IWeaponMoveset
{
    void Tick(float delta);
    bool TryAttack(PlayerController player, MeleeHitbox hitbox);
}
