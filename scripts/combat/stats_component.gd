extends Node
class_name StatsComponent

signal xp_changed(current_xp: int, xp_to_next: int, level: int)
signal level_changed(level: int)
signal stats_changed
signal mana_changed(current_mana: int, max_mana: int)
signal class_changed(value: String)

const CLASS_KNIGHT := "Knight"
const CLASS_WIZARD := "Wizard"
const CLASS_ELF := "Elf"

@export_enum("Knight", "Wizard", "Elf") var character_class: String = CLASS_KNIGHT
@export var level: int = 1
@export var current_xp: int = 0
@export var unspent_points: int = 5

@export var strength: int = 12
@export var agility: int = 10
@export var vitality: int = 12
@export var energy: int = 9

@export var points_per_level: int = 5
@export var base_max_health: int = 90
@export var base_max_mana: int = 45
@export var mana_regen_per_second: float = 6.0

var current_mana: float = 0.0
var skill_cooldowns: Dictionary = {}
var inventory_items: Array[Dictionary] = []

func _ready() -> void:
	_seed_inventory()
	current_mana = float(get_max_mana())
	_emit_all()

func _physics_process(delta: float) -> void:
	current_mana = min(current_mana + mana_regen_per_second * delta, float(get_max_mana()))
	mana_changed.emit(int(current_mana), get_max_mana())

	for skill_id in skill_cooldowns.keys():
		var value: float = float(skill_cooldowns[skill_id])
		value = max(value - delta, 0.0)
		skill_cooldowns[skill_id] = value

func get_attack_power() -> int:
	var class_bonus := 0.0
	match character_class:
		CLASS_KNIGHT:
			class_bonus = strength * 0.4
		CLASS_WIZARD:
			class_bonus = energy * 0.5
		CLASS_ELF:
			class_bonus = agility * 0.45
	return int(8 + level * 2.4 + strength * 1.5 + agility * 0.45 + class_bonus)

func get_defense_power() -> int:
	return int(vitality * 1.25 + agility * 0.55 + level * 0.8)

func get_max_health() -> int:
	return int(base_max_health + vitality * 13 + level * 7)

func get_max_mana() -> int:
	var class_multiplier := 1.0
	match character_class:
		CLASS_KNIGHT:
			class_multiplier = 0.85
		CLASS_WIZARD:
			class_multiplier = 1.35
		CLASS_ELF:
			class_multiplier = 1.1
	return int((base_max_mana + energy * 12 + level * 5) * class_multiplier)

func get_crit_chance() -> float:
	return clampf(0.05 + agility * 0.004, 0.05, 0.42)

func get_crit_multiplier() -> float:
	return 1.55 + energy * 0.01

func xp_to_next_level() -> int:
	return int(45 + pow(level, 1.5) * 30.0)

func add_xp(amount: int) -> void:
	if amount <= 0:
		return

	current_xp += amount
	var next_xp: int = xp_to_next_level()
	var leveled_up: bool = false

	while current_xp >= next_xp:
		current_xp -= next_xp
		level += 1
		unspent_points += points_per_level
		current_mana = float(get_max_mana())
		next_xp = xp_to_next_level()
		leveled_up = true
		level_changed.emit(level)

	xp_changed.emit(current_xp, next_xp, level)
	if leveled_up:
		stats_changed.emit()
		mana_changed.emit(int(current_mana), get_max_mana())

func spend_stat_point(stat_name: StringName, amount: int = 1) -> bool:
	if amount <= 0:
		return false
	if amount > unspent_points:
		return false

	match stat_name:
		&"strength":
			strength += amount
		&"agility":
			agility += amount
		&"vitality":
			vitality += amount
		&"energy":
			energy += amount
		_:
			return false

	unspent_points -= amount
	current_mana = min(current_mana, float(get_max_mana()))
	stats_changed.emit()
	mana_changed.emit(int(current_mana), get_max_mana())
	return true

func change_class(new_class_name: String) -> void:
	if character_class == new_class_name:
		return
	if not [CLASS_KNIGHT, CLASS_WIZARD, CLASS_ELF].has(new_class_name):
		return
	character_class = new_class_name
	stats_changed.emit()
	class_changed.emit(character_class)
	mana_changed.emit(int(current_mana), get_max_mana())

func can_use_skill(skill_id: StringName) -> bool:
	var skill := get_skill_definition(skill_id)
	if skill.is_empty():
		return false
	if float(skill_cooldowns.get(skill_id, 0.0)) > 0.0:
		return false
	return current_mana >= float(skill["mana_cost"])

func consume_skill(skill_id: StringName) -> bool:
	var skill := get_skill_definition(skill_id)
	if skill.is_empty():
		return false
	if not can_use_skill(skill_id):
		return false

	current_mana -= float(skill["mana_cost"])
	skill_cooldowns[skill_id] = float(skill["cooldown"])
	mana_changed.emit(int(current_mana), get_max_mana())
	return true

func get_skill_cooldown_remaining(skill_id: StringName) -> float:
	return float(skill_cooldowns.get(skill_id, 0.0))

func get_skill_definition(skill_id: StringName) -> Dictionary:
	var skills := get_skills_for_class()
	for skill in skills:
		if str(skill["id"]) == str(skill_id):
			return skill
	return {}

func get_skills_for_class() -> Array[Dictionary]:
	match character_class:
		CLASS_KNIGHT:
			return [
				{"id": "power_slash", "name": "Power Slash", "mana_cost": 12.0, "cooldown": 1.4, "base_damage": 24.0, "scale_str": 2.0, "scale_agi": 0.4, "radius": 46.0, "knockback": 310.0},
				{"id": "guard_break", "name": "Guard Break", "mana_cost": 20.0, "cooldown": 3.0, "base_damage": 40.0, "scale_str": 2.4, "scale_agi": 0.2, "radius": 58.0, "knockback": 450.0},
				{"id": "earth_splitter", "name": "Earth Splitter", "mana_cost": 28.0, "cooldown": 5.5, "base_damage": 58.0, "scale_str": 3.0, "scale_agi": 0.35, "radius": 78.0, "knockback": 520.0}
			]
		CLASS_WIZARD:
			return [
				{"id": "magic_missile", "name": "Magic Missile", "mana_cost": 14.0, "cooldown": 1.0, "base_damage": 22.0, "scale_ene": 2.3, "radius": 55.0, "knockback": 260.0},
				{"id": "frost_ring", "name": "Frost Ring", "mana_cost": 24.0, "cooldown": 3.8, "base_damage": 37.0, "scale_ene": 2.8, "radius": 92.0, "knockback": 180.0},
				{"id": "meteor_burst", "name": "Meteor Burst", "mana_cost": 36.0, "cooldown": 6.5, "base_damage": 64.0, "scale_ene": 3.3, "radius": 110.0, "knockback": 300.0}
			]
		_:
			return [
				{"id": "piercing_shot", "name": "Piercing Shot", "mana_cost": 12.0, "cooldown": 1.2, "base_damage": 23.0, "scale_agi": 2.2, "scale_str": 0.6, "radius": 62.0, "knockback": 270.0},
				{"id": "wind_step", "name": "Wind Step", "mana_cost": 18.0, "cooldown": 2.8, "base_damage": 34.0, "scale_agi": 2.8, "scale_ene": 0.6, "radius": 72.0, "knockback": 360.0},
				{"id": "star_fall", "name": "Star Fall", "mana_cost": 30.0, "cooldown": 5.8, "base_damage": 56.0, "scale_agi": 3.0, "scale_ene": 1.0, "radius": 102.0, "knockback": 420.0}
			]

func compute_skill_damage(skill: Dictionary) -> int:
	var value := float(skill.get("base_damage", 20.0))
	value += float(skill.get("scale_str", 0.0)) * strength
	value += float(skill.get("scale_agi", 0.0)) * agility
	value += float(skill.get("scale_ene", 0.0)) * energy
	value += level * 1.1
	return int(value)

func get_inventory_items() -> Array[Dictionary]:
	return inventory_items

func get_item_tooltip(index: int) -> Dictionary:
	if index < 0 or index >= inventory_items.size():
		return {}
	return inventory_items[index]

func _seed_inventory() -> void:
	inventory_items = [
		{"name": "Bronze Sword", "rarity": "normal", "slot": "Weapon", "atk": 12, "def": 0, "desc": "A plain sword. Reliable for early hunting."},
		{"name": "Leather Armor", "rarity": "normal", "slot": "Armor", "atk": 0, "def": 10, "desc": "Light armor used by new adventurers."},
		{"name": "Excellent Ring", "rarity": "excellent", "slot": "Ring", "atk": 4, "def": 4, "desc": "Adds excellent balance to attack and defense."},
		{"name": "Soul Potion", "rarity": "magic", "slot": "Consumable", "atk": 0, "def": 0, "desc": "Restores mana over time."},
		{"name": "Guardian Boots", "rarity": "rare", "slot": "Boots", "atk": 0, "def": 8, "desc": "Heavy boots crafted for dungeon pushes."}
	]
	while inventory_items.size() < 20:
		inventory_items.append({})

func _emit_all() -> void:
	xp_changed.emit(current_xp, xp_to_next_level(), level)
	level_changed.emit(level)
	stats_changed.emit()
	mana_changed.emit(int(current_mana), get_max_mana())
	class_changed.emit(character_class)


