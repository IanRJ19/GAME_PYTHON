extends CanvasLayer

@onready var _health_bar: ProgressBar = $TopLeft/VBox/HealthBar
@onready var _health_label: Label = $TopLeft/VBox/HealthLabel
@onready var _level_label: Label = $TopLeft/VBox/LevelLabel
@onready var _mana_label: Label = $TopLeft/VBox/ManaLabel
@onready var _mana_bar: ProgressBar = $TopLeft/VBox/ManaBar
@onready var _xp_bar: ProgressBar = $TopLeft/VBox/XpBar
@onready var _xp_label: Label = $TopLeft/VBox/XpLabel
@onready var _stats_label: Label = $TopLeft/VBox/StatsLabel

var _tracked_health: HealthComponent
var _tracked_stats: Node

func _ready() -> void:
	GameEvents.player_spawned.connect(_on_player_spawned)
	GameEvents.mana_changed.connect(_on_mana_changed)
	if GameState.player != null:
		_bind_player(GameState.player)

func _on_player_spawned(player: Node) -> void:
	_bind_player(player)

func _bind_player(player: Node) -> void:
	if _tracked_health != null and _tracked_health.health_changed.is_connected(_on_health_changed):
		_tracked_health.health_changed.disconnect(_on_health_changed)
	if _tracked_stats != null:
		if _tracked_stats.has_signal("xp_changed") and _tracked_stats.is_connected("xp_changed", Callable(self, "_on_xp_changed")):
			_tracked_stats.disconnect("xp_changed", Callable(self, "_on_xp_changed"))
		if _tracked_stats.has_signal("level_changed") and _tracked_stats.is_connected("level_changed", Callable(self, "_on_level_changed")):
			_tracked_stats.disconnect("level_changed", Callable(self, "_on_level_changed"))
		if _tracked_stats.has_signal("stats_changed") and _tracked_stats.is_connected("stats_changed", Callable(self, "_on_stats_changed")):
			_tracked_stats.disconnect("stats_changed", Callable(self, "_on_stats_changed"))

	_tracked_health = player.get_node_or_null("Health") as HealthComponent
	_tracked_stats = player.get_node_or_null("Stats")

	if _tracked_health == null:
		return

	_tracked_health.health_changed.connect(_on_health_changed)
	_on_health_changed(_tracked_health.current_health, _tracked_health.max_health)
	if _tracked_stats != null:
		if _tracked_stats.has_signal("xp_changed"):
			_tracked_stats.connect("xp_changed", Callable(self, "_on_xp_changed"))
		if _tracked_stats.has_signal("level_changed"):
			_tracked_stats.connect("level_changed", Callable(self, "_on_level_changed"))
		if _tracked_stats.has_signal("stats_changed"):
			_tracked_stats.connect("stats_changed", Callable(self, "_on_stats_changed"))
		var current_xp: int = int(_tracked_stats.get("current_xp"))
		var level: int = int(_tracked_stats.get("level"))
		var xp_to_next: int = 100
		if _tracked_stats.has_method("xp_to_next_level"):
			xp_to_next = int(_tracked_stats.call("xp_to_next_level"))
		_on_xp_changed(current_xp, xp_to_next, level)
		_on_level_changed(level)
		_on_stats_changed()

func _on_health_changed(current: int, max_health: int) -> void:
	_health_bar.max_value = max_health
	_health_bar.value = current
	_health_label.text = "HP %d / %d" % [current, max_health]

func _on_xp_changed(current_xp: int, xp_to_next: int, _level: int) -> void:
	_xp_bar.max_value = xp_to_next
	_xp_bar.value = current_xp
	_xp_label.text = "XP %d / %d" % [current_xp, xp_to_next]

func _on_level_changed(level: int) -> void:
	var points := 0
	if _tracked_stats != null:
		points = int(_tracked_stats.get("unspent_points"))
	_level_label.text = "Lv. %d  SP: %d" % [level, points]

func _on_stats_changed() -> void:
	if _tracked_stats == null:
		return
	var atk: int = 0
	var defense: int = 0
	var level: int = int(_tracked_stats.get("level"))
	if _tracked_stats.has_method("get_attack_power"):
		atk = int(_tracked_stats.call("get_attack_power"))
	if _tracked_stats.has_method("get_defense_power"):
		defense = int(_tracked_stats.call("get_defense_power"))
	_stats_label.text = "ATK %d  DEF %d" % [atk, defense]
	_on_level_changed(level)

func _on_mana_changed(current: int, max_mana: int) -> void:
	_mana_bar.max_value = max_mana
	_mana_bar.value = current
	_mana_label.text = "MP %d / %d" % [current, max_mana]
