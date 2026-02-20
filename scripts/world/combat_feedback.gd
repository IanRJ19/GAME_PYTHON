extends Node2D

@onready var _sound_player: AudioStreamPlayer2D = $HitSound

func _ready() -> void:
	GameEvents.combat_hit.connect(_on_combat_hit)
	var stream := AudioStreamWAV.load_from_file("res://assets/audio/hit.wav")
	if stream != null:
		_sound_player.stream = stream

func _on_combat_hit(world_position: Vector2, amount: int, is_crit: bool) -> void:
	_spawn_damage_number(world_position, amount, is_crit)
	_spawn_hit_particles(world_position, is_crit)
	_play_hit_sound(world_position, is_crit)

func _spawn_damage_number(world_position: Vector2, amount: int, is_crit: bool) -> void:
	var label := Label.new()
	label.position = world_position + Vector2(-12, -28)
	label.text = "%d" % amount
	label.z_index = 120
	label.modulate = Color(1.0, 0.95, 0.42, 1.0) if is_crit else Color(1.0, 1.0, 1.0, 1.0)
	label.scale = Vector2.ONE * (1.3 if is_crit else 1.0)
	add_child(label)

	var tween := create_tween()
	tween.tween_property(label, "position:y", label.position.y - 36.0, 0.45)
	tween.parallel().tween_property(label, "modulate:a", 0.0, 0.45)
	tween.tween_callback(label.queue_free)

func _spawn_hit_particles(world_position: Vector2, is_crit: bool) -> void:
	var particles := CPUParticles2D.new()
	particles.position = world_position
	particles.one_shot = true
	particles.explosiveness = 0.92
	particles.amount = 14 if is_crit else 9
	particles.lifetime = 0.22
	particles.spread = 120.0
	particles.initial_velocity_min = 36.0
	particles.initial_velocity_max = 115.0
	particles.scale_amount_min = 1.4 if is_crit else 0.8
	particles.scale_amount_max = 2.1 if is_crit else 1.4
	particles.color = Color(1.0, 0.85, 0.3, 1.0) if is_crit else Color(0.9, 0.95, 1.0, 1.0)
	add_child(particles)
	particles.emitting = true

	var timer := get_tree().create_timer(0.6)
	timer.timeout.connect(particles.queue_free)

func _play_hit_sound(world_position: Vector2, is_crit: bool) -> void:
	if _sound_player.stream == null:
		return
	_sound_player.global_position = world_position
	_sound_player.pitch_scale = 1.18 if is_crit else 0.96
	_sound_player.play()
