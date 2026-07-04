using System;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterClass
{
    Knight,
    Wizard,
    Elf
}

[Serializable]
public class SkillDef
{
    public string id;
    public string displayName;
    public float manaCost;
    public float cooldown;
    public float baseDamage;
    public float scaleStr;
    public float scaleAgi;
    public float scaleEne;
    public float radius;
    public float knockback;

    public SkillDef(string id, string displayName, float manaCost, float cooldown, float baseDamage,
        float scaleStr, float scaleAgi, float scaleEne, float radius, float knockback)
    {
        this.id = id;
        this.displayName = displayName;
        this.manaCost = manaCost;
        this.cooldown = cooldown;
        this.baseDamage = baseDamage;
        this.scaleStr = scaleStr;
        this.scaleAgi = scaleAgi;
        this.scaleEne = scaleEne;
        this.radius = radius;
        this.knockback = knockback;
    }
}

[Serializable]
public class InventoryItem
{
    public string itemName;
    public string rarity;
    public string slot;
    public int atk;
    public int def;
    public string description;

    public bool IsEmpty => string.IsNullOrEmpty(itemName);
}

public class StatsComponent : MonoBehaviour
{
    public event Action<int, int, int> XpChanged;   // currentXp, xpToNext, level
    public event Action<int> LevelChanged;
    public event Action StatsChanged;
    public event Action<int, int> ManaChanged;       // currentMana, maxMana
    public event Action<CharacterClass> ClassChanged;

    [Header("Class")]
    public CharacterClass characterClass = CharacterClass.Knight;

    [Header("Progression")]
    public int level = 1;
    public int currentXp = 0;
    public int unspentPoints = 5;
    public int pointsPerLevel = 5;

    [Header("Base stats")]
    public int strength = 12;
    public int agility = 10;
    public int vitality = 12;
    public int energy = 9;

    [Header("Derived base values")]
    public int baseMaxHealth = 90;
    public int baseMaxMana = 45;
    public float manaRegenPerSecond = 6f;

    public float CurrentMana { get; private set; }

    private readonly Dictionary<string, float> _skillCooldowns = new Dictionary<string, float>();
    public List<InventoryItem> InventoryItems { get; private set; } = new List<InventoryItem>();

    void Awake()
    {
        SeedInventory();
        CurrentMana = GetMaxMana();
    }

    void Start()
    {
        EmitAll();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        CurrentMana = Mathf.Min(CurrentMana + manaRegenPerSecond * dt, GetMaxMana());
        ManaChanged?.Invoke((int)CurrentMana, GetMaxMana());

        List<string> keys = new List<string>(_skillCooldowns.Keys);
        foreach (string id in keys)
        {
            _skillCooldowns[id] = Mathf.Max(_skillCooldowns[id] - dt, 0f);
        }
    }

    public int GetAttackPower()
    {
        float classBonus = characterClass switch
        {
            CharacterClass.Knight => strength * 0.4f,
            CharacterClass.Wizard => energy * 0.5f,
            CharacterClass.Elf => agility * 0.45f,
            _ => 0f
        };
        return (int)(8 + level * 2.4f + strength * 1.5f + agility * 0.45f + classBonus);
    }

    public int GetDefensePower()
    {
        return (int)(vitality * 1.25f + agility * 0.55f + level * 0.8f);
    }

    public int GetMaxHealth()
    {
        return (int)(baseMaxHealth + vitality * 13 + level * 7);
    }

    public int GetMaxMana()
    {
        float classMultiplier = characterClass switch
        {
            CharacterClass.Knight => 0.85f,
            CharacterClass.Wizard => 1.35f,
            CharacterClass.Elf => 1.1f,
            _ => 1f
        };
        return (int)((baseMaxMana + energy * 12 + level * 5) * classMultiplier);
    }

    public float GetCritChance()
    {
        return Mathf.Clamp(0.05f + agility * 0.004f, 0.05f, 0.42f);
    }

    public float GetCritMultiplier()
    {
        return 1.55f + energy * 0.01f;
    }

    public int XpToNextLevel()
    {
        return (int)(45 + Mathf.Pow(level, 1.5f) * 30f);
    }

    public void AddXp(int amount)
    {
        if (amount <= 0) return;

        currentXp += amount;
        int nextXp = XpToNextLevel();
        bool leveledUp = false;

        while (currentXp >= nextXp)
        {
            currentXp -= nextXp;
            level += 1;
            unspentPoints += pointsPerLevel;
            CurrentMana = GetMaxMana();
            nextXp = XpToNextLevel();
            leveledUp = true;
            LevelChanged?.Invoke(level);
        }

        XpChanged?.Invoke(currentXp, nextXp, level);
        if (leveledUp)
        {
            StatsChanged?.Invoke();
            ManaChanged?.Invoke((int)CurrentMana, GetMaxMana());
        }
    }

    public bool SpendStatPoint(string statName, int amount = 1)
    {
        if (amount <= 0) return false;
        if (amount > unspentPoints) return false;

        switch (statName)
        {
            case "strength": strength += amount; break;
            case "agility": agility += amount; break;
            case "vitality": vitality += amount; break;
            case "energy": energy += amount; break;
            default: return false;
        }

        unspentPoints -= amount;
        CurrentMana = Mathf.Min(CurrentMana, GetMaxMana());
        StatsChanged?.Invoke();
        ManaChanged?.Invoke((int)CurrentMana, GetMaxMana());
        return true;
    }

    public void ChangeClass(CharacterClass newClass)
    {
        if (characterClass == newClass) return;
        characterClass = newClass;
        StatsChanged?.Invoke();
        ClassChanged?.Invoke(characterClass);
        ManaChanged?.Invoke((int)CurrentMana, GetMaxMana());
    }

    public bool CanUseSkill(string skillId)
    {
        SkillDef skill = GetSkillDefinition(skillId);
        if (skill == null) return false;
        if (_skillCooldowns.TryGetValue(skillId, out float remaining) && remaining > 0f) return false;
        return CurrentMana >= skill.manaCost;
    }

    public bool ConsumeSkill(string skillId)
    {
        SkillDef skill = GetSkillDefinition(skillId);
        if (skill == null) return false;
        if (!CanUseSkill(skillId)) return false;

        CurrentMana -= skill.manaCost;
        _skillCooldowns[skillId] = skill.cooldown;
        ManaChanged?.Invoke((int)CurrentMana, GetMaxMana());
        return true;
    }

    public float GetSkillCooldownRemaining(string skillId)
    {
        return _skillCooldowns.TryGetValue(skillId, out float remaining) ? remaining : 0f;
    }

    public SkillDef GetSkillDefinition(string skillId)
    {
        foreach (SkillDef skill in GetSkillsForClass())
        {
            if (skill.id == skillId) return skill;
        }
        return null;
    }

    public List<SkillDef> GetSkillsForClass()
    {
        switch (characterClass)
        {
            case CharacterClass.Knight:
                return new List<SkillDef>
                {
                    new SkillDef("power_slash", "Power Slash", 12f, 1.4f, 24f, 2.0f, 0.4f, 0f, 46f, 310f),
                    new SkillDef("guard_break", "Guard Break", 20f, 3.0f, 40f, 2.4f, 0.2f, 0f, 58f, 450f),
                    new SkillDef("earth_splitter", "Earth Splitter", 28f, 5.5f, 58f, 3.0f, 0.35f, 0f, 78f, 520f),
                };
            case CharacterClass.Wizard:
                return new List<SkillDef>
                {
                    new SkillDef("magic_missile", "Magic Missile", 14f, 1.0f, 22f, 0f, 0f, 2.3f, 55f, 260f),
                    new SkillDef("frost_ring", "Frost Ring", 24f, 3.8f, 37f, 0f, 0f, 2.8f, 92f, 180f),
                    new SkillDef("meteor_burst", "Meteor Burst", 36f, 6.5f, 64f, 0f, 0f, 3.3f, 110f, 300f),
                };
            default: // Elf
                return new List<SkillDef>
                {
                    new SkillDef("piercing_shot", "Piercing Shot", 12f, 1.2f, 23f, 0.6f, 2.2f, 0f, 62f, 270f),
                    new SkillDef("wind_step", "Wind Step", 18f, 2.8f, 34f, 0f, 2.8f, 0.6f, 72f, 360f),
                    new SkillDef("star_fall", "Star Fall", 30f, 5.8f, 56f, 0f, 3.0f, 1.0f, 102f, 420f),
                };
        }
    }

    public int ComputeSkillDamage(SkillDef skill)
    {
        float value = skill.baseDamage;
        value += skill.scaleStr * strength;
        value += skill.scaleAgi * agility;
        value += skill.scaleEne * energy;
        value += level * 1.1f;
        return (int)value;
    }

    void SeedInventory()
    {
        InventoryItems = new List<InventoryItem>
        {
            new InventoryItem { itemName = "Bronze Sword", rarity = "normal", slot = "Weapon", atk = 12, def = 0, description = "A plain sword. Reliable for early hunting." },
            new InventoryItem { itemName = "Leather Armor", rarity = "normal", slot = "Armor", atk = 0, def = 10, description = "Light armor used by new adventurers." },
            new InventoryItem { itemName = "Excellent Ring", rarity = "excellent", slot = "Ring", atk = 4, def = 4, description = "Adds excellent balance to attack and defense." },
            new InventoryItem { itemName = "Soul Potion", rarity = "magic", slot = "Consumable", atk = 0, def = 0, description = "Restores mana over time." },
            new InventoryItem { itemName = "Guardian Boots", rarity = "rare", slot = "Boots", atk = 0, def = 8, description = "Heavy boots crafted for dungeon pushes." },
        };
        while (InventoryItems.Count < 20)
        {
            InventoryItems.Add(new InventoryItem());
        }
    }

    void EmitAll()
    {
        XpChanged?.Invoke(currentXp, XpToNextLevel(), level);
        LevelChanged?.Invoke(level);
        StatsChanged?.Invoke();
        ManaChanged?.Invoke((int)CurrentMana, GetMaxMana());
        ClassChanged?.Invoke(characterClass);
    }
}
