extends Node2D

@onready var _world: Node2D = $World

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	if _world == null:
		push_error("World scene failed to load in Main.")

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("pause"):
		GameState.toggle_pause()
		get_viewport().set_input_as_handled()
