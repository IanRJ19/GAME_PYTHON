using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class DummyEnemy : MonoBehaviour, IKnockbackReceiver
{
    public int contactDamage = 16;
    public float contactKnockback = 240f;
    public float touchDamageCooldown = 0.5f;
    public int xpReward = 28;
    public float knockbackDamping = 900f;
    public float touchRadius = 0.4f;

    [SerializeField] private HealthComponent health;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Transform touchOrigin;
    [SerializeField] private LayerMask playerMask;

    private Rigidbody2D _rb;
    private Vector2 _knockbackVelocity;
    private readonly Dictionary<int, float> _touchCooldowns = new Dictionary<int, float>();

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    void Start()
    {
        health.Died += OnDied;
        health.Damaged += OnDamaged;
        health.HealthChanged += OnHealthChanged;
        OnHealthChanged(health.CurrentHealth, health.MaxHealth);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        _knockbackVelocity = Vector2.MoveTowards(_knockbackVelocity, Vector2.zero, knockbackDamping * dt);
        _rb.linearVelocity = _knockbackVelocity;
        ProcessTouchDamage(dt);
    }

    public void ApplyKnockback(Vector2 force)
    {
        _knockbackVelocity += force;
    }

    void OnDamaged(int amount, GameObject source)
    {
        if (visual == null) return;
        visual.color = new Color(1f, 0.55f, 0.55f, 1f);
        // Simple flash-back; a proper tween/coroutine can replace this once DOTween or
        // similar is added to the project.
        CancelInvoke(nameof(ResetVisualColor));
        Invoke(nameof(ResetVisualColor), 0.18f);
    }

    void ResetVisualColor()
    {
        if (visual != null) visual.color = Color.white;
    }

    void OnHealthChanged(int current, int max)
    {
        if (healthBar == null) return;
        healthBar.maxValue = max;
        healthBar.value = current;
        healthBar.gameObject.SetActive(current < max);
    }

    void OnDied(GameObject source)
    {
        if (source != null)
        {
            PlayerController killer = source.GetComponent<PlayerController>();
            killer?.GrantXp(xpReward);
        }
        Destroy(gameObject);
    }

    void ProcessTouchDamage(float delta)
    {
        List<int> keys = new List<int>(_touchCooldowns.Keys);
        foreach (int key in keys)
        {
            float remaining = Mathf.Max(_touchCooldowns[key] - delta, 0f);
            if (remaining <= 0f) _touchCooldowns.Remove(key);
            else _touchCooldowns[key] = remaining;
        }

        Vector2 origin = touchOrigin != null ? (Vector2)touchOrigin.position : (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, touchRadius, playerMask);
        foreach (Collider2D col in hits)
        {
            Hurtbox hurtbox = col.GetComponent<Hurtbox>();
            if (hurtbox == null) continue;

            int id = col.GetInstanceID();
            if (_touchCooldowns.ContainsKey(id)) continue;

            Vector2 direction = ((Vector2)col.transform.position - origin).normalized;
            if (direction == Vector2.zero) direction = Vector2.right;

            hurtbox.ReceiveHit(contactDamage, gameObject, direction * contactKnockback, false);
            _touchCooldowns[id] = touchDamageCooldown;
        }
    }
}
