using UnityEngine;
using UnityEngine.UI;

// Minimal HUD for the Phase 0 vertical slice: HP/MP/XP bars + level label.
// Assign the Slider/Text references in the Inspector once the Canvas exists in the scene.
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private Text healthLabel;
    [SerializeField] private Slider manaBar;
    [SerializeField] private Text manaLabel;
    [SerializeField] private Slider xpBar;
    [SerializeField] private Text xpLabel;
    [SerializeField] private Text levelLabel;

    private HealthComponent _trackedHealth;
    private StatsComponent _trackedStats;

    void OnEnable()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.PlayerSpawned += OnPlayerSpawned;
        }
        if (GameState.Instance != null && GameState.Instance.Player != null)
        {
            BindPlayer(GameState.Instance.Player);
        }
    }

    void OnDisable()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.PlayerSpawned -= OnPlayerSpawned;
        }
        Unbind();
    }

    void OnPlayerSpawned(PlayerController player)
    {
        BindPlayer(player);
    }

    void BindPlayer(PlayerController player)
    {
        Unbind();

        _trackedHealth = player.Health;
        _trackedStats = player.Stats;

        if (_trackedHealth != null)
        {
            _trackedHealth.HealthChanged += OnHealthChanged;
            OnHealthChanged(_trackedHealth.CurrentHealth, _trackedHealth.MaxHealth);
        }

        if (_trackedStats != null)
        {
            _trackedStats.XpChanged += OnXpChanged;
            _trackedStats.LevelChanged += OnLevelChanged;
            _trackedStats.ManaChanged += OnManaChanged;
            OnXpChanged(_trackedStats.currentXp, _trackedStats.XpToNextLevel(), _trackedStats.level);
            OnLevelChanged(_trackedStats.level);
            OnManaChanged((int)_trackedStats.CurrentMana, _trackedStats.GetMaxMana());
        }
    }

    void Unbind()
    {
        if (_trackedHealth != null)
        {
            _trackedHealth.HealthChanged -= OnHealthChanged;
        }
        if (_trackedStats != null)
        {
            _trackedStats.XpChanged -= OnXpChanged;
            _trackedStats.LevelChanged -= OnLevelChanged;
            _trackedStats.ManaChanged -= OnManaChanged;
        }
    }

    void OnHealthChanged(int current, int max)
    {
        if (healthBar != null) { healthBar.maxValue = max; healthBar.value = current; }
        if (healthLabel != null) healthLabel.text = $"HP {current} / {max}";
    }

    void OnManaChanged(int current, int max)
    {
        if (manaBar != null) { manaBar.maxValue = max; manaBar.value = current; }
        if (manaLabel != null) manaLabel.text = $"MP {current} / {max}";
    }

    void OnXpChanged(int currentXp, int xpToNext, int level)
    {
        if (xpBar != null) { xpBar.maxValue = xpToNext; xpBar.value = currentXp; }
        if (xpLabel != null) xpLabel.text = $"XP {currentXp} / {xpToNext}";
    }

    void OnLevelChanged(int level)
    {
        int points = _trackedStats != null ? _trackedStats.unspentPoints : 0;
        if (levelLabel != null) levelLabel.text = $"Lv. {level}  SP: {points}";
    }
}
