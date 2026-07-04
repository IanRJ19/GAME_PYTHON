using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IAttacker, IKnockbackReceiver, IDamageMitigator
{
    private enum MovementState { Idle, Run, Attack }

    [Header("Movement")]
    public float moveSpeed = 4.4f;
    public float acceleration = 32f;
    public float friction = 36f;

    [Header("Dash")]
    public float dashSpeed = 12.4f;
    public float dashDuration = 0.16f;
    public float dashCooldown = 0.6f;
    public float dashInvulnerabilityTime = 0.2f;

    [Header("Combat")]
    public float attackCooldown = 0.25f;
    public float attackOffset = 0.56f;
    public float knockbackDamping = 22f;
    public float skillCastLock = 0.1f;

    [Header("Refs")]
    [SerializeField] private Transform attackPivot;
    [SerializeField] private MeleeHitbox meleeHitbox;
    [SerializeField] private HealthComponent health;
    [SerializeField] private StatsComponent stats;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("8-directional idle sprites (N, NE, E, SE, S, SW, W, NW)")]
    [SerializeField] private Sprite[] idleSprites = new Sprite[8];

    // angle-step (0=E,1=NE,2=N,...,7=SE, 45deg increments CCW) -> idleSprites array index (N,NE,E,SE,S,SW,W,NW)
    private static readonly int[] StepToSpriteIndex = { 2, 1, 0, 7, 6, 5, 4, 3 };

    private Rigidbody2D _rb;
    private Vector2 _facingDir = Vector2.down;
    private Vector2 _knockbackVelocity;
    private float _attackCooldownTimer;
    private float _skillLockTimer;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private bool _isDashing;
    private bool _isAttacking;
    private float _comboDamageMultiplier = 1f;
    private MovementState _state = MovementState.Idle;
    private IWeaponMoveset _moveset;
    private Vector3 _spawnPosition;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _moveset = CreateMoveset();
        _spawnPosition = transform.position;
    }

    IWeaponMoveset CreateMoveset()
    {
        CharacterClass characterClass = stats != null ? stats.characterClass : CharacterClass.Knight;
        return characterClass switch
        {
            CharacterClass.Wizard => new WizardMoveset(),
            CharacterClass.Elf => new ElfMoveset(),
            _ => new KnightMoveset(),
        };
    }

    void Start()
    {
        if (meleeHitbox != null) meleeHitbox.Setup(gameObject);
        if (health != null) health.Died += OnDied;
        UpdateFacingSprite();
        GameState.Instance?.RegisterPlayer(this);
        GameEvents.Instance?.RaisePlayerSpawned(this);
    }

    void Update()
    {
        HandleDashInput();
        HandleAttackInput();
        HandleSkillInput();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        TickTimers(dt);
        _moveset.Tick(dt);

        if (!_isDashing)
        {
            ProcessMovement(dt);
            ApplyKnockbackDecay(dt);
        }

        UpdateState();
        UpdateFacingSprite();
    }

    // ---- Movement ----

    void ProcessMovement(float delta)
    {
        Vector2 inputDir = ReadInputDirection();
        Vector2 targetVelocity = inputDir * moveSpeed;
        float rate = inputDir.sqrMagnitude > 0f ? acceleration : friction;
        _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, targetVelocity, rate * delta);
        _rb.linearVelocity += _knockbackVelocity;

        if (inputDir != Vector2.zero)
        {
            _facingDir = inputDir.normalized;
        }
    }

    Vector2 ReadInputDirection()
    {
        float x = 0f;
        float y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        Vector2 dir = new Vector2(x, y);
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    void TickTimers(float delta)
    {
        _attackCooldownTimer = Mathf.Max(_attackCooldownTimer - delta, 0f);
        _skillLockTimer = Mathf.Max(_skillLockTimer - delta, 0f);
        _dashCooldownTimer = Mathf.Max(_dashCooldownTimer - delta, 0f);

        if (_dashTimer > 0f)
        {
            _dashTimer = Mathf.Max(_dashTimer - delta, 0f);
            if (_dashTimer <= 0f) _isDashing = false;
        }
    }

    // ---- Dash ----

    void HandleDashInput()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (_isDashing || _dashCooldownTimer > 0f) return;

        Vector2 inputDir = ReadInputDirection();
        Vector2 dashDir = inputDir != Vector2.zero ? inputDir.normalized : _facingDir;
        StartDash(dashDir);
    }

    void StartDash(Vector2 direction)
    {
        _isDashing = true;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;
        _facingDir = direction;
        _rb.linearVelocity = direction * dashSpeed;
        health?.GrantInvulnerability(dashInvulnerabilityTime);
    }

    // ---- Attack / combo ----

    void HandleAttackInput()
    {
        if (_isDashing) return;
        if (_skillLockTimer > 0f) return;
        if (!Input.GetKeyDown(KeyCode.J) && !Input.GetKeyDown(KeyCode.K) && !Input.GetKeyDown(KeyCode.F)) return;
        if (_attackCooldownTimer > 0f) return;

        _attackCooldownTimer = attackCooldown;
        _isAttacking = true;
        _moveset.TryAttack(this, meleeHitbox);
        _isAttacking = false;
    }

    // ---- Skills (Q/E/R) ----

    void HandleSkillInput()
    {
        if (_isDashing) return;
        if (_skillLockTimer > 0f) return;
        if (Input.GetKeyDown(KeyCode.Q)) TryCastSkill(0);
        else if (Input.GetKeyDown(KeyCode.E)) TryCastSkill(1);
        else if (Input.GetKeyDown(KeyCode.R)) TryCastSkill(2);
    }

    void TryCastSkill(int skillIndex)
    {
        if (stats == null) return;
        var skills = stats.GetSkillsForClass();
        if (skillIndex < 0 || skillIndex >= skills.Count) return;

        SkillDef skill = skills[skillIndex];
        if (!stats.CanUseSkill(skill.id)) return;
        if (!stats.ConsumeSkill(skill.id)) return;

        _skillLockTimer = skillCastLock;
        _isAttacking = true;
        CastSkillDamage(skill);
        _isAttacking = false;
        GameEvents.Instance?.RaiseSkillUsed(skill.displayName, skill.cooldown);
    }

    void CastSkillDamage(SkillDef skill)
    {
        if (stats == null) return;
        int baseDamage = stats.ComputeSkillDamage(skill);
        float radius = skill.radius > 0f ? skill.radius : 56f;
        float knockback = skill.knockback > 0f ? skill.knockback : 320f;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (Vector2.Distance(enemy.transform.position, transform.position) > radius) continue;

            Hurtbox hurtbox = enemy.GetComponentInChildren<Hurtbox>();
            if (hurtbox == null) continue;

            Vector2 dir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            if (dir == Vector2.zero) dir = _facingDir;

            hurtbox.ReceiveHit(baseDamage, gameObject, dir * knockback, false);
        }
    }

    // ---- State / visuals ----

    void UpdateState()
    {
        MovementState newState;
        if (_isAttacking) newState = MovementState.Attack;
        else if (_rb.linearVelocity.magnitude > 0.1f) newState = MovementState.Run;
        else newState = MovementState.Idle;

        if (newState != _state)
        {
            _state = newState;
            GameEvents.Instance?.RaisePlayerStateChanged(_state.ToString());
        }
    }

    void UpdateFacingSprite()
    {
        if (spriteRenderer == null || idleSprites == null || idleSprites.Length < 8) return;
        float angleDeg = Mathf.Atan2(_facingDir.y, _facingDir.x) * Mathf.Rad2Deg;
        angleDeg = (angleDeg + 360f) % 360f;
        int step = Mathf.RoundToInt(angleDeg / 45f) % 8;
        int spriteIndex = StepToSpriteIndex[step];
        Sprite sprite = idleSprites[spriteIndex];
        if (sprite != null) spriteRenderer.sprite = sprite;
    }

    void OnDied(GameObject source)
    {
        transform.position = _spawnPosition;
        _rb.linearVelocity = Vector2.zero;
        _knockbackVelocity = Vector2.zero;
        _isDashing = false;
        _dashTimer = 0f;
        health.RestoreFull();
    }

    void ApplyKnockbackDecay(float delta)
    {
        _knockbackVelocity = Vector2.MoveTowards(_knockbackVelocity, Vector2.zero, knockbackDamping * delta);
    }

    // ---- Public API used by movesets / hitboxes / hurtboxes ----

    public Vector2 GetFacingDir() => _facingDir;

    public Transform GetAttackPivot() => attackPivot;

    public void SetComboDamageMultiplier(float value) => _comboDamageMultiplier = value;

    public void ApplyKnockback(Vector2 force)
    {
        _knockbackVelocity += force;
    }

    public int GetAttackDamage()
    {
        int baseDamage = stats != null ? stats.GetAttackPower() : 10;
        return Mathf.RoundToInt(baseDamage * _comboDamageMultiplier);
    }

    public AttackPayload GetAttackPayload()
    {
        int baseDamage = GetAttackDamage();
        float critChance = stats != null ? stats.GetCritChance() : 0.1f;
        float critMultiplier = stats != null ? stats.GetCritMultiplier() : 1.5f;
        bool isCrit = Random.value <= critChance;
        int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * critMultiplier) : baseDamage;
        return new AttackPayload(finalDamage, isCrit);
    }

    public int MitigateIncomingDamage(int rawDamage)
    {
        int defensePower = stats != null ? stats.GetDefensePower() : 0;
        int reduction = (int)(defensePower * 0.35f);
        return Mathf.Max(rawDamage - reduction, 1);
    }

    public void GrantXp(int amount) => stats?.AddXp(amount);

    public bool SpendStatPoint(string statName) => stats != null && stats.SpendStatPoint(statName, 1);

    public StatsComponent Stats => stats;
    public HealthComponent Health => health;
}
