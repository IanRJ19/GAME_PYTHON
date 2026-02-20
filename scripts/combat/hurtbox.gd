extends Area2D
class_name Hurtbox

@export var health_path: NodePath
@export var owner_body_path: NodePath

var _health: HealthComponent
var _owner_body: Node2D

func _ready() -> void:
	_health = get_node_or_null(health_path)
	if _health == null and has_node("../Health"):
		_health = get_node("../Health")

	_owner_body = get_node_or_null(owner_body_path)
	if _owner_body == null and get_parent() is Node2D:
		_owner_body = get_parent() as Node2D

func receive_hit(damage: int, source: Node, knockback: Vector2 = Vector2.ZERO, metadata: Dictionary = {}) -> bool:
	if _health == null:
		push_warning("Hurtbox has no HealthComponent assigned.")
		return false

	var final_damage: int = max(damage, 1)
	if _owner_body != null and _owner_body.has_method("mitigate_incoming_damage"):
		final_damage = int(_owner_body.call("mitigate_incoming_damage", final_damage))
	final_damage = max(final_damage, 1)

	var applied := _health.take_damage(final_damage, source)
	if applied and _owner_body != null and _owner_body.has_method("apply_knockback"):
		_owner_body.call("apply_knockback", knockback)
	if applied:
		var hit_position := global_position
		var is_crit := bool(metadata.get("is_crit", false))
		GameEvents.combat_hit.emit(hit_position, final_damage, is_crit)
	return applied
