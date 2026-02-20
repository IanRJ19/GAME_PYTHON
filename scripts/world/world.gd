extends Node2D

func _ready() -> void:
	GameState.register_world(self)
