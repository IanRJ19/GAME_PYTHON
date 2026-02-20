extends Node

var current_scene_path: String = ""

func go_to(scene_path: String) -> void:
	var error := get_tree().change_scene_to_file(scene_path)
	if error != OK:
		push_error("Failed to load scene: %s" % scene_path)
		return
	current_scene_path = scene_path
	GameEvents.scene_changed.emit(scene_path)
