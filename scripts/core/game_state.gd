extends Node

var player: Player
var world: Node2D
var is_paused: bool = false

func register_player(value: Player) -> void:
	player = value

func register_world(value: Node2D) -> void:
	world = value
	GameEvents.world_registered.emit(world)

func toggle_pause() -> void:
	is_paused = not is_paused
	get_tree().paused = is_paused
	GameEvents.pause_toggled.emit(is_paused)
