public struct ComboStep
{
    public float damageMult;
    public float offsetMult;
    public float knockbackMult;

    public ComboStep(float damageMult, float offsetMult, float knockbackMult)
    {
        this.damageMult = damageMult;
        this.offsetMult = offsetMult;
        this.knockbackMult = knockbackMult;
    }
}
