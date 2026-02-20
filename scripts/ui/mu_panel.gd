extends CanvasLayer

const RARITY_COLORS := {
	"normal": Color(0.82, 0.82, 0.82, 1.0),
	"magic": Color(0.48, 0.68, 1.0, 1.0),
	"rare": Color(0.61, 0.9, 0.58, 1.0),
	"excellent": Color(0.34, 1.0, 0.72, 1.0),
	"legendary": Color(1.0, 0.78, 0.35, 1.0)
}

@onready var _root_panel: PanelContainer = $RootPanel
@onready var _class_label: Label = $RootPanel/Body/Left/CharacterBox/VBox/ClassLabel
@onready var _level_label: Label = $RootPanel/Body/Left/CharacterBox/VBox/LevelLabel
@onready var _sp_label: Label = $RootPanel/Body/Left/CharacterBox/VBox/SPLabel
@onready var _str_label: Label = $RootPanel/Body/Left/CharacterBox/VBox/StrRow/Value
@onready var _agi_label: Label = $RootPanel/Body/Left/CharacterBox/VBox/AgiRow/Value
@onready var _vit_label: Label = $RootPanel/Body/Left/CharacterBox/VBox/VitRow/Value
@onready var _ene_label: Label = $RootPanel/Body/Left/CharacterBox/VBox/EneRow/Value
@onready var _skills_text: RichTextLabel = $RootPanel/Body/Left/SkillsBox/SkillsText
@onready var _inventory_grid: GridContainer = $RootPanel/Body/Right/InventoryBox/VBox/Grid
@onready var _tooltip_panel: PanelContainer = $Tooltip
@onready var _tooltip_title: Label = $Tooltip/VBox/Title
@onready var _tooltip_stats: Label = $Tooltip/VBox/Stats
@onready var _tooltip_desc: Label = $Tooltip/VBox/Desc

var _player: Node
var _inventory_buttons: Array[Button] = []

func _ready() -> void:
	_root_panel.visible = false
	_tooltip_panel.visible = false
	_create_inventory_buttons()
	GameEvents.player_spawned.connect(_on_player_spawned)
	if GameState.player != null:
		_bind_player(GameState.player)

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("open_character") or event.is_action_pressed("open_inventory"):
		_root_panel.visible = not _root_panel.visible
		_tooltip_panel.visible = false
		get_viewport().set_input_as_handled()
	if event.is_action_pressed("ui_cancel") and _root_panel.visible:
		_root_panel.visible = false
		_tooltip_panel.visible = false
		get_viewport().set_input_as_handled()

func _on_player_spawned(player: Node) -> void:
	_bind_player(player)

func _bind_player(player: Node) -> void:
	_player = player
	if _player.has_node("Stats"):
		var stats := _player.get_node("Stats")
		if stats.has_signal("stats_changed"):
			stats.connect("stats_changed", Callable(self, "_refresh"))
		if stats.has_signal("level_changed"):
			stats.connect("level_changed", Callable(self, "_refresh"))
		if stats.has_signal("class_changed"):
			stats.connect("class_changed", Callable(self, "_refresh"))
	_refresh()

func _refresh(_arg = null) -> void:
	if _player == null or not _player.has_method("get_runtime_profile"):
		return
	var profile: Dictionary = _player.call("get_runtime_profile")
	_class_label.text = "Class: %s" % str(profile.get("character_class", "Unknown"))
	_level_label.text = "Level: %d" % int(profile.get("level", 1))
	_sp_label.text = "SP: %d" % int(profile.get("sp", 0))
	_str_label.text = str(int(profile.get("strength", 0)))
	_agi_label.text = str(int(profile.get("agility", 0)))
	_vit_label.text = str(int(profile.get("vitality", 0)))
	_ene_label.text = str(int(profile.get("energy", 0)))
	_refresh_skills(profile.get("skills", []))
	_refresh_inventory(profile.get("inventory", []))

func _refresh_skills(skills: Array) -> void:
	var lines: Array[String] = []
	var idx := 1
	for skill_var in skills:
		var skill: Dictionary = skill_var
		lines.append("[color=#9FE6FF]%d.[/color] %s  MP:%d  CD:%.1fs" % [
			idx,
			str(skill.get("name", "Skill")),
			int(skill.get("mana_cost", 0.0)),
			float(skill.get("cooldown", 0.0))
		])
		idx += 1
	_skills_text.text = "\n".join(lines)

func _refresh_inventory(items: Array) -> void:
	for i in _inventory_buttons.size():
		var button := _inventory_buttons[i]
		if i >= items.size() or items[i].is_empty():
			button.text = ""
			button.tooltip_text = ""
			button.modulate = Color(1, 1, 1, 0.32)
			continue
		var item: Dictionary = items[i]
		button.text = str(item.get("name", "?")).left(3)
		var rarity := str(item.get("rarity", "normal")).to_lower()
		button.modulate = RARITY_COLORS.get(rarity, Color.WHITE)

func _create_inventory_buttons() -> void:
	for i in 20:
		var button := Button.new()
		button.custom_minimum_size = Vector2(52, 52)
		button.flat = true
		button.text = ""
		button.mouse_entered.connect(_on_slot_hovered.bind(i))
		button.mouse_exited.connect(_on_slot_left)
		_inventory_grid.add_child(button)
		_inventory_buttons.append(button)

func _on_slot_hovered(index: int) -> void:
	if _player == null or not _player.has_method("get_runtime_profile"):
		return
	var profile: Dictionary = _player.call("get_runtime_profile")
	var items: Array = profile.get("inventory", [])
	if index >= items.size() or items[index].is_empty():
		_tooltip_panel.visible = false
		return
	var item: Dictionary = items[index]
	var rarity := str(item.get("rarity", "normal")).to_lower()
	_tooltip_title.text = str(item.get("name", "Unknown Item"))
	_tooltip_title.modulate = RARITY_COLORS.get(rarity, Color.WHITE)
	_tooltip_stats.text = "Slot: %s   ATK +%d   DEF +%d" % [
		str(item.get("slot", "-")),
		int(item.get("atk", 0)),
		int(item.get("def", 0))
	]
	_tooltip_desc.text = str(item.get("desc", ""))
	_tooltip_panel.position = get_viewport().get_mouse_position() + Vector2(18, 16)
	_tooltip_panel.visible = true

func _on_slot_left() -> void:
	_tooltip_panel.visible = false

func _on_add_stat_pressed(stat_name: StringName) -> void:
	if _player == null or not _player.has_method("spend_stat_point"):
		return
	_player.call("spend_stat_point", stat_name)
	_refresh()

