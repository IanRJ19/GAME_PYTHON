using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invulnerabilityTime = 0.08f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    public event Action<int, int> HealthChanged;
    public event Action<int, GameObject> Damaged;
    public event Action<GameObject> Died;

    private float _invulnerabilityTimer;
    private float _dashInvulnerabilityTimer;

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    void Start()
    {
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void FixedUpdate()
    {
        _invulnerabilityTimer = Mathf.Max(_invulnerabilityTimer - Time.fixedDeltaTime, 0f);
        _dashInvulnerabilityTimer = Mathf.Max(_dashInvulnerabilityTimer - Time.fixedDeltaTime, 0f);
    }

    // Second, independent invulnerability window for dash i-frames — kept separate from
    // _invulnerabilityTimer (brief per-hit stun guard) so a hit landing mid-dash can't
    // overwrite the dash window with a shorter value and end it early.
    public void GrantInvulnerability(float duration)
    {
        _dashInvulnerabilityTimer = Mathf.Max(_dashInvulnerabilityTimer, duration);
    }

    public bool TakeDamage(int amount, GameObject source = null)
    {
        if (amount <= 0) return false;
        if (CurrentHealth <= 0) return false;
        if (_invulnerabilityTimer > 0f || _dashInvulnerabilityTimer > 0f) return false;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        _invulnerabilityTimer = invulnerabilityTime;
        Damaged?.Invoke(amount, source);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
        {
            Died?.Invoke(source);
        }
        return true;
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (CurrentHealth <= 0) return;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void RestoreFull()
    {
        CurrentHealth = maxHealth;
        _invulnerabilityTimer = 0f;
        _dashInvulnerabilityTimer = 0f;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetMaxHealth(int value, bool healToFull = false)
    {
        maxHealth = Mathf.Max(value, 1);
        CurrentHealth = healToFull ? maxHealth : Mathf.Min(CurrentHealth, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
