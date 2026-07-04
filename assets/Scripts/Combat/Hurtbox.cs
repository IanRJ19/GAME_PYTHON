using UnityEngine;

// Attach to a trigger Collider2D. ownerBody is whatever GameObject "owns" this hurtbox
// (the body it protects) — it may optionally implement IKnockbackReceiver / IDamageMitigator.
public class Hurtbox : MonoBehaviour
{
    [SerializeField] private HealthComponent health;
    [SerializeField] private GameObject ownerBody;

    private IKnockbackReceiver _knockbackReceiver;
    private IDamageMitigator _damageMitigator;

    void Awake()
    {
        if (health == null)
        {
            health = GetComponentInParent<HealthComponent>();
        }
        if (ownerBody == null && transform.parent != null)
        {
            ownerBody = transform.parent.gameObject;
        }
        if (ownerBody != null)
        {
            _knockbackReceiver = ownerBody.GetComponent<IKnockbackReceiver>();
            _damageMitigator = ownerBody.GetComponent<IDamageMitigator>();
        }
    }

    public bool ReceiveHit(int damage, GameObject source, Vector2 knockback, bool isCrit = false)
    {
        if (health == null)
        {
            Debug.LogWarning("Hurtbox has no HealthComponent assigned.", this);
            return false;
        }

        int finalDamage = Mathf.Max(damage, 1);
        if (_damageMitigator != null)
        {
            finalDamage = Mathf.Max(_damageMitigator.MitigateIncomingDamage(finalDamage), 1);
        }

        bool applied = health.TakeDamage(finalDamage, source);
        if (applied)
        {
            _knockbackReceiver?.ApplyKnockback(knockback);
            GameEvents.Instance?.RaiseCombatHit(transform.position, finalDamage, isCrit);
        }
        return applied;
    }
}
