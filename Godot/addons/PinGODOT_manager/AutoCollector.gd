@tool
class_name AutoCollector
extends Node

# Runtime collector for selected state changes, method calls, signals, and interactions.
@export var collect_data: bool = true
@export var target_node: Node
@export_enum("Entity", "Activity", "Agent") var vertex_type: String = "Entity"
@export_range(0.05, 60.0, 0.05, "or_greater") var log_interval: float = 1.0
@export var monitored_flags: Dictionary = {}

var update_timer: Timer
var previous_state: Dictionary = {}
var interaction_helper: EntityInteractionHelper
var _method_connections: Array[Dictionary] = []


func _ready() -> void:
	if Engine.is_editor_hint():
		return
	if not is_instance_valid(target_node):
		target_node = get_parent()
	if not is_instance_valid(target_node):
		push_error("AutoCollector sem target_node valido")
		return
	if not collect_data:
		return

	interaction_helper = EntityInteractionHelper.new()
	add_child(interaction_helper)
	_auto_populate_monitored_flags(target_node)

	var selected_properties: Array = get_selected_properties()
	interaction_helper.initialize_monitored_properties(
		target_node,
		selected_properties,
		vertex_type
	)
	for property_name in selected_properties:
		previous_state[property_name] = target_node.get(property_name)

	_connect_interaction_signals(target_node)
	_connect_method_signals()
	_connect_configured_signals()

	# Properties are sampled; methods and signals are captured through connections.
	update_timer = Timer.new()
	update_timer.wait_time = max(log_interval, 0.05)
	update_timer.one_shot = false
	update_timer.autostart = true
	update_timer.timeout.connect(_check_state_changes)
	add_child(update_timer)
	update_timer.start()


func get_selected_properties() -> Array:
	var selected: Array = []
	for flag_name in monitored_flags.keys():
		if (
			monitored_flags.get(flag_name, false)
			and not is_method_flag(flag_name)
			and not is_signal_flag(flag_name)
		):
			selected.append(flag_name)
	return selected


func get_selected_methods() -> Array:
	var selected: Array = []
	for flag_name in monitored_flags.keys():
		if monitored_flags.get(flag_name, false) and is_method_flag(flag_name):
			selected.append(flag_name)
	return selected


func get_selected_signals() -> Array:
	var selected: Array = []
	for flag_name in monitored_flags.keys():
		if monitored_flags.get(flag_name, false) and is_signal_flag(flag_name):
			selected.append(str(flag_name).trim_prefix("signal::"))
	return selected


func _check_state_changes() -> void:
	if not is_instance_valid(target_node) or interaction_helper == null:
		return
	for property_name in get_selected_properties():
		var new_value = target_node.get(property_name)
		var old_value = previous_state.get(property_name, new_value)
		if old_value != new_value:
			interaction_helper.register_state_change(
				target_node,
				property_name,
				old_value,
				new_value,
				vertex_type
			)
			previous_state[property_name] = new_value


func _connect_interaction_signals(node: Node) -> void:
	if node is Area2D:
		_connect_once(node, "body_entered", Callable(self, "_on_body_entered").bind(node))
		_connect_once(node, "area_entered", Callable(self, "_on_area_entered").bind(node))
	elif node is CollisionObject2D:
		_connect_once(node, "input_event", Callable(self, "_on_input_event").bind(node))

	for child in node.get_children():
		if child is Node:
			_connect_interaction_signals(child)


func _connect_method_signals() -> void:
	# Instrumented methods emit generated signals used to record each invocation.
	for method_name in get_selected_methods():
		var signal_name: String = "%s_called" % str(method_name).lstrip("_")
		if not target_node.has_signal(signal_name):
			push_warning(
				"Sinal %s nao encontrado em %s. Instrumente o script pelo Dock." % [
					signal_name, target_node.name
				]
			)
			continue
		var callable: Callable = Callable(self, "_on_method_called").bind(
			str(method_name)
		)
		_connect_once(target_node, signal_name, callable)
		_method_connections.append({"signal": signal_name, "callable": callable})


func _connect_configured_signals() -> void:
	for signal_name in get_selected_signals():
		if not target_node.has_signal(signal_name):
			continue
		var argument_count: int = _signal_argument_count(str(signal_name))
		var callable: Callable = Callable(
			self,
			"_on_configured_signal_emitted"
		).bind(str(signal_name))
		# Signal arguments are intentionally ignored by the generic callback.
		if argument_count > 0:
			callable = callable.unbind(argument_count)
		_connect_once(target_node, signal_name, callable)


func _connect_once(node: Node, signal_name: String, callable: Callable) -> void:
	if node.has_signal(signal_name) and not node.is_connected(signal_name, callable):
		node.connect(signal_name, callable)


func _on_body_entered(body: Node, source_node: Node) -> void:
	var activity: Vertex = interaction_helper.register_generic_interaction(
		source_node,
		body,
		vertex_type if source_node == target_node else ""
	)
	_associate_activity_with_collector(activity, source_node)


func _on_area_entered(area: Area2D, source_node: Node) -> void:
	var activity: Vertex = interaction_helper.register_generic_interaction(
		source_node,
		area,
		vertex_type if source_node == target_node else ""
	)
	_associate_activity_with_collector(activity, source_node)


func _on_input_event(
	_viewport: Node,
	event: InputEvent,
	_shape_index: int,
	source_node: Node
) -> void:
	var pressed: bool = false
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton
		pressed = mouse_event.pressed
	elif event is InputEventScreenTouch:
		var touch_event: InputEventScreenTouch = event as InputEventScreenTouch
		pressed = touch_event.pressed
	if pressed:
		var activity: Vertex = interaction_helper.register_input_interaction(
			source_node,
			event,
			vertex_type if source_node == target_node else ""
		)
		_associate_activity_with_collector(activity, source_node)


func _associate_activity_with_collector(activity: Vertex, source_node: Node) -> void:
	if activity == null or source_node == target_node:
		return
	if not is_instance_valid(target_node) or interaction_helper == null:
		return
	var collector_vertex: Vertex = interaction_helper.find_or_create_vertex(
		target_node,
		activity.date,
		vertex_type
	)
	if collector_vertex != null:
		ProvenanceController.create_provenance_edge(activity, collector_vertex)


func _on_method_called(method_name: String) -> void:
	interaction_helper.register_method_call(target_node, method_name, vertex_type)


func _on_configured_signal_emitted(signal_name: String) -> void:
	interaction_helper.register_signal_emission(target_node, signal_name, vertex_type)


func set_monitored_flag(flag_name: String, value: bool) -> void:
	var new_flags: Dictionary = monitored_flags.duplicate()
	new_flags[flag_name] = value
	monitored_flags = new_flags


func _auto_populate_monitored_flags(target: Node) -> void:
	if not is_instance_valid(target):
		return
	# Rediscovery keeps existing selections while reflecting the current script API.
	var discovered: Dictionary = {}
	for property_info in target.get_property_list():
		var property_name: String = property_info.get("name", "")
		var type_id: int = property_info.get("type", TYPE_NIL)
		var usage: int = property_info.get("usage", 0)
		if property_name != "" and (usage & PROPERTY_USAGE_EDITOR) != 0:
			if type_id in [TYPE_BOOL, TYPE_INT, TYPE_FLOAT, TYPE_STRING, TYPE_VECTOR2, TYPE_VECTOR3]:
				discovered[property_name] = false

	var script := target.get_script()
	if script != null:
		var ignored := [
			"_init", "_ready", "_process", "_physics_process", "_input", "_unhandled_input"
		]
		for method_info in script.get_script_method_list():
			var method_name: String = method_info.get("name", "")
			if method_name == "" or ignored.has(method_name):
				continue
			if method_name.begins_with("get_") or method_name.begins_with("set_"):
				continue
			discovered[method_name] = false

		for signal_info in script.get_script_signal_list():
			var signal_name: String = signal_info.get("name", "")
			if signal_name != "" and not signal_name.ends_with("_called"):
				discovered["signal::" + signal_name] = false

	for key in monitored_flags.keys():
		if discovered.has(key):
			discovered[key] = monitored_flags[key]
	monitored_flags = discovered


func is_method_flag(flag_name: String) -> bool:
	if is_signal_flag(flag_name):
		return false
	if not is_instance_valid(target_node):
		return false
	var script := target_node.get_script()
	if script == null:
		return false
	for method_info in script.get_script_method_list():
		if method_info.get("name", "") == flag_name:
			return true
	return false


func is_signal_flag(flag_name: String) -> bool:
	return flag_name.begins_with("signal::")


func _signal_argument_count(signal_name: String) -> int:
	var script := target_node.get_script()
	if script == null:
		return 0
	for signal_info in script.get_script_signal_list():
		if signal_info.get("name", "") == signal_name:
			return signal_info.get("args", []).size()
	return 0
