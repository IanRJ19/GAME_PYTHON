extends Node

signal player_spawned(player: Node)
signal player_state_changed(state_name: String)
signal player_died
signal combat_hit(world_position: Vector2, amount: int, is_crit: bool)
signal mana_changed(current: int, max_mana: int)
signal skill_used(skill_name: String, cooldown: float)
signal world_registered(world: Node)
signal scene_changed(scene_path: String)
signal pause_toggled(paused: bool)
