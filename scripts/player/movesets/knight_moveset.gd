extends RefCounted
class_name KnightMoveset

const STEPS: Array[Dictionary] = [
	{"damage_mult": 1.0, "offset_mult": 1.0, "knockback_mult": 1.0},
	{"damage_mult": 1.6, "offset_mult": 1.25, "knockback_mult": 1.4},
]

var combo_window: float = 0.6

var _combo_index: int = 0
var _combo_window_timer: float = 0.0
var _base_knockback_force: float = -1.0

func tick(delta: float) -> void:
	if _combo_window_timer <= 0.0:
		return
	_combo_window_timer = max(_combo_window_timer - delta, 0.0)
	if _combo_window_timer == 0.0:
		_combo_index = 0

func try_attack(player: Node, hitbox: MeleeHitbox) -> bool:
	var step: Dictionary = STEPS[_combo_index]
	var facing: Vector2 = player.get_facing_dir()
	if facing == Vector2.ZERO:
		facing = Vector2.RIGHT

	var pivot: Node2D = player.get_attack_pivot()
	pivot.position = facing * float(player.attack_offset) * float(step.get("offset_mult", 1.0))

	if _base_knockback_force < 0.0:
		_base_knockback_force = hitbox.knockback_force
	hitbox.knockback_force = _base_knockback_force * float(step.get("knockback_mult", 1.0))

	player.set_combo_damage_multiplier(float(step.get("damage_mult", 1.0)))
	hitbox.reset_hits()
	hitbox.perform_attack()
	player.set_combo_damage_multiplier(1.0)

	_combo_index = (_combo_index + 1) % STEPS.size()
	_combo_window_timer = combo_window
	return true
