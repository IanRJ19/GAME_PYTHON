extends Area2D
class_name MeleeHitbox

@export var damage: int = 20
@export var knockback_force: float = 260.0
@export var target_group: StringName = &""

var _owner: Node2D
var _already_hit: Dictionary = {}

func setup(owner: Node2D) -> void:
	_owner = owner

func reset_hits() -> void:
	_already_hit.clear()

func perform_attack() -> void:
	var outgoing_damage: int = damage
	var metadata: Dictionary = {"is_crit": false}
	if _owner != null and _owner.has_method("get_attack_damage"):
		outgoing_damage = int(_owner.call("get_attack_damage"))
	if _owner != null and _owner.has_method("get_attack_payload"):
		var payload: Variant = _owner.call("get_attack_payload")
		if payload is Dictionary:
			metadata = payload
			outgoing_damage = int(payload.get("damage", outgoing_damage))

	for area in get_overlapping_areas():
		if not area.has_method("receive_hit"):
			continue
		if target_group != &"":
			var target_body: Node = area.get_parent()
			if target_body == null or not target_body.is_in_group(target_group):
				continue

		var target_id := area.get_instance_id()
		if _already_hit.has(target_id):
			continue
		_already_hit[target_id] = true

		var direction := Vector2.RIGHT
		if _owner != null:
			direction = (area.global_position - _owner.global_position).normalized()
			if direction == Vector2.ZERO:
				direction = Vector2.RIGHT

		area.call("receive_hit", outgoing_damage, _owner, direction * knockback_force, metadata)
