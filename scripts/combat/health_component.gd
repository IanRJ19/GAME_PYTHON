extends Node
class_name HealthComponent

signal health_changed(current: int, max_health: int)
signal damaged(amount: int, source: Node)
signal died(source: Node)

@export var max_health: int = 100
@export var invulnerability_time: float = 0.08

var current_health: int
var _invulnerability_timer: float = 0.0

func _ready() -> void:
	current_health = max_health
	health_changed.emit(current_health, max_health)

func _physics_process(delta: float) -> void:
	_invulnerability_timer = max(_invulnerability_timer - delta, 0.0)

func take_damage(amount: int, source: Node = null) -> bool:
	if amount <= 0:
		return false
	if current_health <= 0:
		return false
	if _invulnerability_timer > 0.0:
		return false

	current_health = max(current_health - amount, 0)
	_invulnerability_timer = invulnerability_time
	damaged.emit(amount, source)
	health_changed.emit(current_health, max_health)

	if current_health == 0:
		died.emit(source)
	return true

func heal(amount: int) -> void:
	if amount <= 0:
		return
	if current_health <= 0:
		return
	current_health = min(current_health + amount, max_health)
	health_changed.emit(current_health, max_health)

func restore_full() -> void:
	current_health = max_health
	_invulnerability_timer = 0.0
	health_changed.emit(current_health, max_health)

func set_max_health(value: int, heal_to_full: bool = false) -> void:
	max_health = max(value, 1)
	if heal_to_full:
		current_health = max_health
	else:
		current_health = min(current_health, max_health)
	health_changed.emit(current_health, max_health)
