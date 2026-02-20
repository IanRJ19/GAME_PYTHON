extends CharacterBody2D
class_name Player

enum MovementState {
	IDLE,
	RUN,
	AIR,
	ATTACK
}

@export var move_speed: float = 220.0
@export var acceleration: float = 1600.0
@export var friction: float = 1800.0
@export var jump_velocity: float = -420.0
@export var gravity_scale: float = 1.0
@export var max_fall_speed: float = 900.0
@export var coyote_time: float = 0.12
@export var jump_buffer_time: float = 0.12
@export var attack_cooldown: float = 0.25
@export var attack_offset: float = 28.0
@export var knockback_damping: float = 1100.0
@export var fall_limit_y: float = 1200.0
@export var skill_cast_lock: float = 0.1

var _base_gravity: float = ProjectSettings.get_setting("physics/2d/default_gravity")
var _coyote_timer: float = 0.0
var _jump_buffer_timer: float = 0.0
var _attack_cooldown_timer: float = 0.0
var _skill_lock_timer: float = 0.0
var _state: MovementState = MovementState.IDLE
var _facing: int = 1
var _is_attacking: bool = false
var _knockback_x: float = 0.0
var _spawn_position: Vector2

@onready var _attack_pivot: Node2D = $AttackPivot
@onready var _melee_hitbox: MeleeHitbox = $AttackPivot/MeleeHitbox
@onready var _health: HealthComponent = $Health
@onready var _stats: Node = $Stats
@onready var _sprite: Sprite2D = $Sprite
@onready var _sword_pivot: Node2D = $SwordPivot

var _run_anim_timer: float = 0.0
var _run_anim_frame: int = 0
var _sword_tween: Tween
var _sheet_columns: int = 6
var _frame_size: Vector2i = Vector2i(16, 16)

func _ready() -> void:
	_ensure_input_actions()
	add_to_group("player")
	_spawn_position = global_position
	_assign_player_texture()
	_set_sword_rest_pose()
	_melee_hitbox.setup(self)
	_health.died.connect(_on_died)
	if _stats != null and _stats.has_signal("stats_changed"):
		_stats.connect("stats_changed", Callable(self, "_sync_health_from_stats"))
	if _stats != null and _stats.has_signal("level_changed"):
		_stats.connect("level_changed", Callable(self, "_on_level_changed"))
	if _stats != null and _stats.has_signal("mana_changed"):
		_stats.connect("mana_changed", Callable(self, "_on_mana_changed"))
	_sync_health_from_stats()
	_emit_mana_state()
	GameState.register_player(self)
	GameEvents.player_spawned.emit(self)

func _physics_process(delta: float) -> void:
	_update_timers(delta)
	_handle_input_buffer()
	_apply_gravity(delta)
	_process_horizontal(delta)
	_process_jump()
	_process_attack()
	_process_skills()
	_apply_knockback_decay(delta)
	move_and_slide()
	_check_void_fall()
	_update_state()
	_update_sprite(delta)

func _update_timers(delta: float) -> void:
	if is_on_floor():
		_coyote_timer = coyote_time
	else:
		_coyote_timer = max(_coyote_timer - delta, 0.0)
	_jump_buffer_timer = max(_jump_buffer_timer - delta, 0.0)
	_attack_cooldown_timer = max(_attack_cooldown_timer - delta, 0.0)
	_skill_lock_timer = max(_skill_lock_timer - delta, 0.0)

func _handle_input_buffer() -> void:
	if Input.is_action_just_pressed("jump"):
		_jump_buffer_timer = jump_buffer_time

func _apply_gravity(delta: float) -> void:
	if not is_on_floor():
		velocity.y += _base_gravity * gravity_scale * delta
		velocity.y = min(velocity.y, max_fall_speed)

func _process_horizontal(delta: float) -> void:
	var axis := Input.get_axis("move_left", "move_right")
	var target_speed := axis * move_speed
	var rate := acceleration if absf(axis) > 0.0 else friction
	velocity.x = move_toward(velocity.x, target_speed, rate * delta)
	velocity.x += _knockback_x
	var previous_facing := _facing
	if axis > 0.0:
		_facing = 1
	elif axis < 0.0:
		_facing = -1
	if previous_facing != _facing:
		_set_sword_rest_pose()

func _process_jump() -> void:
	if _jump_buffer_timer <= 0.0:
		return
	if is_on_floor() or _coyote_timer > 0.0:
		velocity.y = jump_velocity
		_coyote_timer = 0.0
		_jump_buffer_timer = 0.0

func _process_attack() -> void:
	if _skill_lock_timer > 0.0:
		return
	if not Input.is_action_just_pressed("attack"):
		return
	if _attack_cooldown_timer > 0.0:
		return

	_attack_cooldown_timer = attack_cooldown
	_is_attacking = true
	_attack_pivot.position.x = attack_offset * _facing
	_melee_hitbox.reset_hits()
	_melee_hitbox.perform_attack()
	_play_sword_swing()
	_is_attacking = false

func _process_skills() -> void:
	if _skill_lock_timer > 0.0:
		return
	if Input.is_action_just_pressed("skill_1"):
		_try_cast_skill(0)
	elif Input.is_action_just_pressed("skill_2"):
		_try_cast_skill(1)
	elif Input.is_action_just_pressed("skill_3"):
		_try_cast_skill(2)

func _try_cast_skill(skill_index: int) -> void:
	if _stats == null:
		return
	if not _stats.has_method("get_skills_for_class"):
		return
	var skills: Array = _stats.call("get_skills_for_class")
	if skill_index < 0 or skill_index >= skills.size():
		return

	var skill: Dictionary = skills[skill_index]
	var skill_id := StringName(skill["id"])
	if not bool(_stats.call("can_use_skill", skill_id)):
		return
	if not bool(_stats.call("consume_skill", skill_id)):
		return

	_skill_lock_timer = skill_cast_lock
	_is_attacking = true
	_cast_skill_damage(skill)
	_play_sword_swing()
	GameEvents.skill_used.emit(str(skill["name"]), float(skill["cooldown"]))
	_is_attacking = false

func _cast_skill_damage(skill: Dictionary) -> void:
	if _stats == null:
		return
	var base_damage := int(_stats.call("compute_skill_damage", skill))
	var radius := float(skill.get("radius", 56.0))
	var knockback := float(skill.get("knockback", 320.0))
	var target_nodes := get_tree().get_nodes_in_group("enemy")
	for node in target_nodes:
		if not (node is Node2D):
			continue
		var enemy := node as Node2D
		if enemy.global_position.distance_to(global_position) > radius:
			continue
		if not enemy.has_node("Hurtbox"):
			continue
		var hurtbox := enemy.get_node("Hurtbox")
		if hurtbox == null or not hurtbox.has_method("receive_hit"):
			continue
		var payload := _build_damage_payload(base_damage)
		var dir := (enemy.global_position - global_position).normalized()
		if dir == Vector2.ZERO:
			dir = Vector2(float(_facing), 0.0)
		hurtbox.call("receive_hit", int(payload["damage"]), self, dir * knockback, payload)

func _update_state() -> void:
	var new_state: MovementState
	if _is_attacking:
		new_state = MovementState.ATTACK
	elif not is_on_floor():
		new_state = MovementState.AIR
	elif absf(velocity.x) > 5.0:
		new_state = MovementState.RUN
	else:
		new_state = MovementState.IDLE

	if new_state != _state:
		_state = new_state
		GameEvents.player_state_changed.emit(MovementState.keys()[_state])

func _ensure_input_actions() -> void:
	var bindings := {
		"move_left": [Key.KEY_A, Key.KEY_LEFT],
		"move_right": [Key.KEY_D, Key.KEY_RIGHT],
		"jump": [Key.KEY_SPACE, Key.KEY_W, Key.KEY_UP],
		"attack": [Key.KEY_J, Key.KEY_K, Key.KEY_F],
		"skill_1": [Key.KEY_Q, Key.KEY_1],
		"skill_2": [Key.KEY_E, Key.KEY_2],
		"skill_3": [Key.KEY_R, Key.KEY_3],
		"open_character": [Key.KEY_C],
		"open_inventory": [Key.KEY_I],
		"pause": [Key.KEY_ESCAPE]
	}

	for action_name in bindings.keys():
		if not InputMap.has_action(action_name):
			InputMap.add_action(action_name)
		for keycode in bindings[action_name]:
			if _action_has_key(action_name, keycode):
				continue
			var event := InputEventKey.new()
			event.physical_keycode = keycode
			InputMap.action_add_event(action_name, event)

func _action_has_key(action_name: StringName, keycode: Key) -> bool:
	for event in InputMap.action_get_events(action_name):
		if event is InputEventKey and event.physical_keycode == keycode:
			return true
	return false

func _on_died(_source: Node) -> void:
	global_position = _spawn_position
	velocity = Vector2.ZERO
	_knockback_x = 0.0
	_health.restore_full()

func _apply_knockback_decay(delta: float) -> void:
	_knockback_x = move_toward(_knockback_x, 0.0, knockback_damping * delta)

func apply_knockback(force: Vector2) -> void:
	_knockback_x += force.x
	velocity.y += force.y

func get_attack_damage() -> int:
	if _stats != null and _stats.has_method("get_attack_power"):
		return int(_stats.call("get_attack_power"))
	return 10

func get_attack_payload() -> Dictionary:
	return _build_damage_payload(get_attack_damage())

func mitigate_incoming_damage(raw_damage: int) -> int:
	var defense_power: int = 0
	if _stats != null and _stats.has_method("get_defense_power"):
		defense_power = int(_stats.call("get_defense_power"))
	var reduction := int(defense_power * 0.35)
	return max(raw_damage - reduction, 1)

func grant_xp(amount: int) -> void:
	if _stats != null and _stats.has_method("add_xp"):
		_stats.call("add_xp", amount)

func spend_stat_point(stat_name: StringName) -> bool:
	if _stats == null or not _stats.has_method("spend_stat_point"):
		return false
	return bool(_stats.call("spend_stat_point", stat_name, 1))

func get_runtime_profile() -> Dictionary:
	var profile: Dictionary = {
		"character_class": "Unknown",
		"level": 1,
		"strength": 0,
		"agility": 0,
		"vitality": 0,
		"energy": 0,
		"sp": 0,
		"mana": 0,
		"max_mana": 0,
		"skills": [],
		"inventory": []
	}
	if _stats == null:
		return profile
	profile["character_class"] = str(_stats.get("character_class"))
	profile["level"] = int(_stats.get("level"))
	profile["strength"] = int(_stats.get("strength"))
	profile["agility"] = int(_stats.get("agility"))
	profile["vitality"] = int(_stats.get("vitality"))
	profile["energy"] = int(_stats.get("energy"))
	profile["sp"] = int(_stats.get("unspent_points"))
	profile["mana"] = int(_stats.get("current_mana"))
	if _stats.has_method("get_max_mana"):
		profile["max_mana"] = int(_stats.call("get_max_mana"))
	if _stats.has_method("get_skills_for_class"):
		profile["skills"] = _stats.call("get_skills_for_class")
	if _stats.has_method("get_inventory_items"):
		profile["inventory"] = _stats.call("get_inventory_items")
	return profile

func _sync_health_from_stats() -> void:
	if _stats != null and _stats.has_method("get_max_health"):
		_health.set_max_health(int(_stats.call("get_max_health")), false)

func _on_level_changed(_level: int) -> void:
	if _stats != null and _stats.has_method("get_max_health"):
		_health.set_max_health(int(_stats.call("get_max_health")), true)

func _check_void_fall() -> void:
	if global_position.y > fall_limit_y:
		_on_died(self)

func _update_sprite(delta: float) -> void:
	if _sprite == null:
		return
	_sprite.flip_h = _facing < 0

	match _state:
		MovementState.IDLE:
			_set_sprite_frame(0)
		MovementState.AIR:
			_set_sprite_frame(2)
		MovementState.ATTACK:
			_set_sprite_frame(3)
		MovementState.RUN:
			_run_anim_timer += delta
			if _run_anim_timer >= 0.12:
				_run_anim_timer = 0.0
				_run_anim_frame = (_run_anim_frame + 1) % 3
			_set_sprite_frame(1 + _run_anim_frame)

func _assign_player_texture() -> void:
	var image := Image.load_from_file("res://assets/sprites/player/classic_hero_16x16.png")
	if image == null or image.is_empty():
		push_warning("Player texture missing: classic_hero_16x16.png")
		return
	_sprite.texture = ImageTexture.create_from_image(image)
	_sprite.region_enabled = true
	_set_sprite_frame(0)

func _set_sword_rest_pose() -> void:
	_sword_pivot.position = Vector2(10 * _facing, -2)
	_sword_pivot.scale.x = _facing
	_sword_pivot.rotation_degrees = -18.0

func _play_sword_swing() -> void:
	if _sword_tween != null and _sword_tween.is_running():
		_sword_tween.kill()
	_sword_tween = create_tween()
	_sword_tween.tween_property(_sword_pivot, "rotation_degrees", 36.0, 0.08)
	_sword_tween.tween_property(_sword_pivot, "rotation_degrees", -18.0, 0.1)

func _set_sprite_frame(frame_index: int) -> void:
	if _sprite == null:
		return
	var frame_x := frame_index % _sheet_columns
	var frame_y := int(frame_index / _sheet_columns)
	_sprite.region_rect = Rect2(frame_x * _frame_size.x, frame_y * _frame_size.y, _frame_size.x, _frame_size.y)

func _build_damage_payload(base_damage: int) -> Dictionary:
	var crit_chance := 0.1
	var crit_multiplier := 1.5
	if _stats != null and _stats.has_method("get_crit_chance"):
		crit_chance = float(_stats.call("get_crit_chance"))
	if _stats != null and _stats.has_method("get_crit_multiplier"):
		crit_multiplier = float(_stats.call("get_crit_multiplier"))
	var is_crit := randf() <= crit_chance
	var final_damage := base_damage
	if is_crit:
		final_damage = int(final_damage * crit_multiplier)
	return {"damage": final_damage, "is_crit": is_crit}

func _on_mana_changed(current: int, max_mana: int) -> void:
	GameEvents.mana_changed.emit(current, max_mana)

func _emit_mana_state() -> void:
	if _stats != null and _stats.has_method("get_max_mana"):
		var current := int(_stats.get("current_mana"))
		var max_mana := int(_stats.call("get_max_mana"))
		GameEvents.mana_changed.emit(current, max_mana)

