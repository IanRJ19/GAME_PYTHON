using System;
using UnityEngine;

public class GameEvents : MonoBehaviour
{
    public static GameEvents Instance { get; private set; }

    public event Action<PlayerController> PlayerSpawned;
    public event Action<string> PlayerStateChanged;
    public event Action PlayerDied;
    public event Action<Vector2, int, bool> CombatHit;
    public event Action<int, int> ManaChanged;
    public event Action<string, float> SkillUsed;
    public event Action<bool> PauseToggled;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RaisePlayerSpawned(PlayerController player) => PlayerSpawned?.Invoke(player);
    public void RaisePlayerStateChanged(string state) => PlayerStateChanged?.Invoke(state);
    public void RaisePlayerDied() => PlayerDied?.Invoke();
    public void RaiseCombatHit(Vector2 worldPosition, int amount, bool isCrit) => CombatHit?.Invoke(worldPosition, amount, isCrit);
    public void RaiseManaChanged(int current, int max) => ManaChanged?.Invoke(current, max);
    public void RaiseSkillUsed(string skillName, float cooldown) => SkillUsed?.Invoke(skillName, cooldown);
    public void RaisePauseToggled(bool paused) => PauseToggled?.Invoke(paused);
}
