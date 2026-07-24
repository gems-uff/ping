extends Node2D

# PINGODOT_GENERATED_SIGNALS_BEGIN
signal collect_coin_called
signal take_damage_called
signal collect_powerup_called
signal win_game_called
signal lose_game_called
signal restart_game_called
# PINGODOT_GENERATED_SIGNALS_END



signal coin_collected(total_score: int)
signal player_damaged(remaining_lives: int)
signal powerup_collected(duration: float)
signal game_won(final_score: int)
signal game_lost(reason: String)
signal game_restarted

const PROVENANCE_FILE := "user://manager_test_collection.json"
const PLAY_AREA := Rect2(35, 130, 830, 410)
const PLAYER_RADIUS := 20.0
const COIN_RADIUS := 13.0
const OBSTACLE_RADIUS := 20.0
const POWERUP_RADIUS := 15.0
const NORMAL_SPEED := 250.0
const BOOST_SPEED := 390.0
const COINS_TO_WIN := 7
const INITIAL_TIME := 45.0
const INITIAL_LIVES := 3

# Estas propriedades aparecem no Inspector do AutoCollector e mudam durante a
# partida, permitindo testar o monitoramento automatico de estado.
@export var score: int = 0
@export var lives: int = INITIAL_LIVES
@export var time_remaining: float = INITIAL_TIME
@export var boost_active: bool = false
@export var game_finished: bool = false
@export var player_position: Vector2 = Vector2(120, 330)

@onready var player: CharacterBody2D = $Player
@onready var coin: Area2D = $Coin
@onready var obstacle_a: Area2D = $ObstacleA
@onready var obstacle_b: Area2D = $ObstacleB
@onready var powerup: Area2D = $PowerUp

var random := RandomNumberGenerator.new()
var boost_time_remaining := 0.0
var damage_cooldown := 0.0
var export_accumulator := 0.0
var powerup_available := true

var score_label: Label
var lives_label: Label
var time_label: Label
var status_label: Label
var message_label: Label


func _ready() -> void:
	random.randomize()
	coin.body_entered.connect(_on_coin_body_entered)
	obstacle_a.body_entered.connect(_on_obstacle_body_entered.bind(obstacle_a))
	obstacle_b.body_entered.connect(_on_obstacle_body_entered.bind(obstacle_b))
	powerup.body_entered.connect(_on_powerup_body_entered)
	_build_interface()
	_update_interface()
	_export_collection()
	queue_redraw()


func _process(delta: float) -> void:
	export_accumulator += delta
	if export_accumulator >= 1.0:
		export_accumulator = 0.0
		_export_collection()

	if damage_cooldown > 0.0:
		damage_cooldown = max(0.0, damage_cooldown - delta)

	if boost_active:
		boost_time_remaining = max(0.0, boost_time_remaining - delta)
		if boost_time_remaining <= 0.0:
			boost_active = false
			message_label.text = "O impulso terminou."

	if game_finished:
		_update_interface()
		queue_redraw()
		return

	time_remaining = max(0.0, time_remaining - delta)
	if time_remaining <= 0.0:
		lose_game("time_out")
		return

	var direction := Vector2.ZERO
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		direction.x -= 1.0
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		direction.x += 1.0
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		direction.y -= 1.0
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		direction.y += 1.0

	var current_speed := BOOST_SPEED if boost_active else NORMAL_SPEED
	player.velocity = direction.normalized() * current_speed
	player.move_and_slide()
	player.position.x = clamp(
		player.position.x,
		PLAY_AREA.position.x + PLAYER_RADIUS,
		PLAY_AREA.end.x - PLAYER_RADIUS
	)
	player.position.y = clamp(
		player.position.y,
		PLAY_AREA.position.y + PLAYER_RADIUS,
		PLAY_AREA.end.y - PLAYER_RADIUS
	)
	player_position = player.position
	_update_interface()
	queue_redraw()


func _unhandled_key_input(event: InputEvent) -> void:
	if (
		event is InputEventKey
		and event.pressed
		and not event.echo
		and event.keycode == KEY_R
	):
		restart_game()


# Metodo intencionalmente publico para ser descoberto e instrumentado pelo
# Manager. O sinal abaixo testa tambem a descoberta de sinais com argumentos.
func collect_coin() -> void:
	# PINGODOT_GENERATED_EMIT: collect_coin
	emit_signal("collect_coin_called")
	if game_finished:
		return
	score += 1
	coin_collected.emit(score)
	message_label.text = "Moeda %d coletada!" % score

	if score >= COINS_TO_WIN:
		coin.position = Vector2(-200, -200)
		win_game()
	else:
		_place_randomly(coin, 150.0)
		if score % 2 == 0 and not powerup_available:
			powerup_available = true
			_place_randomly(powerup, 180.0)

	_export_collection()
	_update_interface()
	queue_redraw()


func take_damage(obstacle: Area2D) -> void:
	# PINGODOT_GENERATED_EMIT: take_damage
	emit_signal("take_damage_called")
	if game_finished or damage_cooldown > 0.0:
		return
	damage_cooldown = 1.0
	lives -= 1
	player_damaged.emit(lives)
	_place_randomly(obstacle, 190.0)

	if lives <= 0:
		lose_game("no_lives")
	else:
		message_label.text = "Voce perdeu uma vida!"

	_export_collection()
	_update_interface()
	queue_redraw()


func collect_powerup() -> void:
	# PINGODOT_GENERATED_EMIT: collect_powerup
	emit_signal("collect_powerup_called")
	if game_finished or not powerup_available:
		return
	powerup_available = false
	boost_active = true
	boost_time_remaining = 5.0
	time_remaining = min(INITIAL_TIME, time_remaining + 5.0)
	powerup.position = Vector2(-200, -200)
	powerup_collected.emit(boost_time_remaining)
	message_label.text = "Impulso ativado: velocidade e +5 segundos!"
	_export_collection()
	_update_interface()
	queue_redraw()


func win_game() -> void:
	# PINGODOT_GENERATED_EMIT: win_game
	emit_signal("win_game_called")
	if game_finished:
		return
	game_finished = true
	game_won.emit(score)
	message_label.text = "Voce venceu! Pressione R para reiniciar."
	_export_collection()


func lose_game(reason: String) -> void:
	# PINGODOT_GENERATED_EMIT: lose_game
	emit_signal("lose_game_called")
	if game_finished:
		return
	game_finished = true
	game_lost.emit(reason)
	message_label.text = "Fim de jogo! Pressione R para tentar novamente."
	_export_collection()


func restart_game() -> void:
	# PINGODOT_GENERATED_EMIT: restart_game
	emit_signal("restart_game_called")
	score = 0
	lives = INITIAL_LIVES
	time_remaining = INITIAL_TIME
	boost_active = false
	boost_time_remaining = 0.0
	damage_cooldown = 0.0
	game_finished = false
	powerup_available = true
	player.position = Vector2(120, 330)
	player.velocity = Vector2.ZERO
	player_position = player.position
	coin.position = Vector2(700, 320)
	obstacle_a.position = Vector2(430, 220)
	obstacle_b.position = Vector2(520, 430)
	powerup.position = Vector2(750, 470)
	game_restarted.emit()
	message_label.text = "Nova partida iniciada. Colete sete moedas."
	_export_collection()
	_update_interface()
	queue_redraw()


func _on_coin_body_entered(body: Node2D) -> void:
	if body == player:
		collect_coin()


func _on_obstacle_body_entered(body: Node2D, obstacle: Area2D) -> void:
	if body == player:
		take_damage(obstacle)


func _on_powerup_body_entered(body: Node2D) -> void:
	if body == player:
		collect_powerup()


func _place_randomly(area: Area2D, minimum_player_distance: float) -> void:
	var candidate := area.position
	for attempt in range(30):
		candidate = Vector2(
			random.randf_range(PLAY_AREA.position.x + 30.0, PLAY_AREA.end.x - 30.0),
			random.randf_range(PLAY_AREA.position.y + 30.0, PLAY_AREA.end.y - 30.0)
		)
		if candidate.distance_to(player.position) >= minimum_player_distance:
			break
	area.position = candidate


func _export_collection() -> void:
	var exporter := ProvenanceExporter.new()
	var success := exporter.export_to_json(PROVENANCE_FILE)
	exporter.free()
	if not success:
		push_error("Nao foi possivel exportar a coleta do Manager")


func _build_interface() -> void:
	var title := Label.new()
	title.position = Vector2(35, 18)
	title.text = "CACA-MOEDAS: TESTE DO MANAGER"
	title.add_theme_font_size_override("font_size", 26)
	title.add_theme_color_override("font_color", Color("78dce8"))
	add_child(title)

	score_label = Label.new()
	score_label.position = Vector2(35, 62)
	score_label.add_theme_font_size_override("font_size", 18)
	add_child(score_label)

	lives_label = Label.new()
	lives_label.position = Vector2(210, 62)
	lives_label.add_theme_font_size_override("font_size", 18)
	add_child(lives_label)

	time_label = Label.new()
	time_label.position = Vector2(350, 62)
	time_label.add_theme_font_size_override("font_size", 18)
	add_child(time_label)

	status_label = Label.new()
	status_label.position = Vector2(570, 62)
	status_label.size = Vector2(295, 28)
	status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	status_label.add_theme_font_size_override("font_size", 16)
	add_child(status_label)

	message_label = Label.new()
	message_label.position = Vector2(35, 96)
	message_label.size = Vector2(830, 25)
	message_label.text = "Colete sete moedas, evite os obstaculos e pegue o impulso."
	message_label.add_theme_color_override("font_color", Color("c5cee0"))
	add_child(message_label)

	var controls := Label.new()
	controls.position = Vector2(35, 557)
	controls.text = "Mover: WASD ou setas     Reiniciar: R     JSON: manager_test_collection.json"
	controls.add_theme_color_override("font_color", Color("8b9bb4"))
	add_child(controls)


func _update_interface() -> void:
	score_label.text = "Moedas: %d/%d" % [score, COINS_TO_WIN]
	lives_label.text = "Vidas: %d" % lives
	time_label.text = "Tempo: %.1f s" % time_remaining
	status_label.text = (
		"IMPULSO: %.1f s" % boost_time_remaining
		if boost_active
		else "Velocidade normal"
	)
	status_label.add_theme_color_override(
		"font_color",
		Color("75f0a0") if boost_active else Color("8b9bb4")
	)


func _draw() -> void:
	draw_rect(PLAY_AREA, Color("111a2e"), true)
	draw_rect(PLAY_AREA, Color("33466b"), false, 3.0)

	for x in range(60, 866, 40):
		draw_line(
			Vector2(x, PLAY_AREA.position.y),
			Vector2(x, PLAY_AREA.end.y),
			Color(0.2, 0.28, 0.42, 0.22),
			1.0
		)
	for y in range(150, 541, 40):
		draw_line(
			Vector2(PLAY_AREA.position.x, y),
			Vector2(PLAY_AREA.end.x, y),
			Color(0.2, 0.28, 0.42, 0.22),
			1.0
		)

	# Jogador.
	if boost_active:
		draw_circle(player.position, PLAYER_RADIUS + 8.0, Color(0.3, 1.0, 0.7, 0.22))
	draw_circle(player.position, PLAYER_RADIUS, Color("4da6ff"))
	draw_circle(player.position, PLAYER_RADIUS, Color("a8d8ff"), false, 3.0)
	draw_polygon(
		PackedVector2Array([
			player.position + Vector2(8, 0),
			player.position + Vector2(-5, -6),
			player.position + Vector2(-5, 6)
		]),
		PackedColorArray([Color.WHITE])
	)

	# Moeda.
	if not game_finished:
		draw_circle(coin.position, COIN_RADIUS, Color("ffc857"))
		draw_circle(coin.position, COIN_RADIUS, Color("fff1a8"), false, 3.0)
		draw_circle(coin.position, 4.0, Color("d58b22"))

	# Obstaculos.
	var obstacles: Array[Area2D] = [obstacle_a, obstacle_b]
	for obstacle in obstacles:
		draw_circle(obstacle.position, OBSTACLE_RADIUS, Color("e55765"))
		draw_circle(obstacle.position, OBSTACLE_RADIUS, Color("ff9aa5"), false, 3.0)
		draw_line(
			obstacle.position + Vector2(-7, -7),
			obstacle.position + Vector2(7, 7),
			Color.WHITE,
			3.0
		)
		draw_line(
			obstacle.position + Vector2(7, -7),
			obstacle.position + Vector2(-7, 7),
			Color.WHITE,
			3.0
		)

	# Power-up.
	if powerup_available and not game_finished:
		draw_circle(powerup.position, POWERUP_RADIUS, Color("55d98a"))
		draw_circle(powerup.position, POWERUP_RADIUS, Color("b8ffd1"), false, 3.0)
		draw_line(
			powerup.position + Vector2(-7, 0),
			powerup.position + Vector2(7, 0),
			Color.WHITE,
			3.0
		)
		draw_line(
			powerup.position + Vector2(0, -7),
			powerup.position + Vector2(0, 7),
			Color.WHITE,
			3.0
		)

	# Barra de tempo.
	var time_ratio: float = clamp(time_remaining / INITIAL_TIME, 0.0, 1.0)
	var bar_rect := Rect2(600, 100, 265, 10)
	draw_rect(bar_rect, Color("28334d"), true)
	draw_rect(
		Rect2(bar_rect.position, Vector2(bar_rect.size.x * time_ratio, bar_rect.size.y)),
		Color("75f0a0") if time_ratio > 0.3 else Color("e55765"),
		true
	)
