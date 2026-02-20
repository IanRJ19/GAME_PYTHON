extends CharacterBody2D
class_name DummyEnemy

@export var gravity_scale: float = 1.0
@export var max_fall_speed: float = 900.0
@export var knockback_damping: float = 900.0
@export var contact_damage: int = 16
@export var contact_knockback: float = 240.0
@export var touch_damage_cooldown: float = 0.5
@export var xp_reward: int = 28

var _base_gravity: float = ProjectSettings.get_setting("physics/2d/default_gravity")
var _knockback_x: float = 0.0
var _touch_cooldowns: Dictionary = {}

@onready var _health: HealthComponent = $Health
@onready var _visual: CanvasItem = $Sprite if has_node("Sprite") else $Visual
@onready var _health_bar: ProgressBar = $HealthBar
@onready var _touch_area: Area2D = $TouchDamage

func _ready() -> void:
	add_to_group("enemy")
	_assign_enemy_texture()
	_health.died.connect(_on_died)
	_health.damaged.connect(_on_damaged)
	_health.health_changed.connect(_on_health_changed)
	_on_health_changed(_health.current_health, _health.max_health)

func _physics_process(delta: float) -> void:
	if not is_on_floor():
		velocity.y += _base_gravity * gravity_scale * delta
		velocity.y = min(velocity.y, max_fall_speed)

	_knockback_x = move_toward(_knockback_x, 0.0, knockback_damping * delta)
	velocity.x = _knockback_x
	move_and_slide()
	_process_touch_damage(delta)

func apply_knockback(force: Vector2) -> void:
	_knockback_x += force.x
	velocity.y += force.y

func _on_damaged(_amount: int, _source: Node) -> void:
	_visual.modulate = Color(1.0, 0.55, 0.55, 1.0)
	var tween := create_tween()
	tween.tween_property(_visual, "modulate", Color(1.0, 1.0, 1.0, 1.0), 0.18)

func _on_health_changed(current: int, max_health: int) -> void:
	_health_bar.max_value = max_health
	_health_bar.value = current
	_health_bar.visible = current < max_health

func _on_died(source: Node) -> void:
	if source != null and source.has_method("grant_xp"):
		source.call("grant_xp", xp_reward)
	queue_free()

func _process_touch_damage(delta: float) -> void:
	for key in _touch_cooldowns.keys():
		_touch_cooldowns[key] = max(_touch_cooldowns[key] - delta, 0.0)
		if _touch_cooldowns[key] == 0.0:
			_touch_cooldowns.erase(key)

	for area in _touch_area.get_overlapping_areas():
		if not area.has_method("receive_hit"):
			continue
		var target_body: Node = area.get_parent()
		if target_body == null or not target_body.is_in_group("player"):
			continue
		var target_id := area.get_instance_id()
		if _touch_cooldowns.has(target_id):
			continue

		var direction := (area.global_position - global_position).normalized()
		if direction == Vector2.ZERO:
			direction = Vector2.RIGHT
		area.call("receive_hit", contact_damage, self, direction * contact_knockback, {"is_crit": false})
		_touch_cooldowns[target_id] = touch_damage_cooldown

func _assign_enemy_texture() -> void:
	if not has_node("Sprite"):
		return
	var sprite := $Sprite as Sprite2D
	var image := Image.load_from_file("res://assets/sprites/enemy/skeleton/skeleton/idle/right/idle_right0000.png")
	if image == null or image.is_empty():
		push_warning("Enemy texture missing: idle_right0000.png")
		return
	sprite.texture = ImageTexture.create_from_image(image)
