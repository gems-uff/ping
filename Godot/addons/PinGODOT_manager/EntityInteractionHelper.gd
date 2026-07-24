@tool
class_name EntityInteractionHelper
extends Node

# Converts Godot object events into PinGODOT vertices, attributes, and edges.

func get_node_position(node: Node) -> Vector3:
	if node is Node3D:
		return node.global_position
	if node is Node2D:
		return Vector3(node.global_position.x, node.global_position.y, 0.0)
	return Vector3.ZERO


func get_or_create_vertex_label(
	node: Node,
	timestamp: String,
	forced_type: String = ""
) -> String:
	var vertex := find_or_create_vertex(node, timestamp, forced_type)
	if vertex == null:
		return node.name
	for attr in vertex.attributes:
		if attr.name == "name":
			return attr.value
	return node.name


func initialize_monitored_properties(
	node: Node,
	property_names: Array,
	forced_type: String
) -> Vertex:
	var timestamp := _timestamp()
	var vertex := find_or_create_vertex(node, timestamp, forced_type)
	if vertex == null:
		return null
	for property_name in property_names:
		_update_attribute(vertex, str(property_name), str(node.get(property_name)))
	return vertex


func register_generic_interaction(
	source: Node,
	target: Node,
	source_type: String = "",
	target_type: String = ""
) -> Vertex:
	if not is_instance_valid(source) or not is_instance_valid(target):
		return null

	var timestamp := _timestamp()
	var source_vertex := find_or_create_vertex(source, timestamp, source_type)
	var target_vertex := find_or_create_vertex(target, timestamp, target_type)
	if source_vertex == null or target_vertex == null:
		return null

	var source_pos := get_node_position(source)
	var target_pos := get_node_position(target)
	var interaction_pos := (source_pos + target_pos) / 2.0
	var label := "%s_interacted_with_%s" % [source_vertex.label, target_vertex.label]

	var activity := ProvenanceController.add_vertex(
		timestamp,
		"Activity",
		label,
		[
			Attribute.new("type", "Interaction"),
			Attribute.new("source", source_vertex.label),
			Attribute.new("target", target_vertex.label),
			Attribute.new("prov:type", "prov:Activity"),
			Attribute.new("ObjectPosition_X", str(interaction_pos.x)),
			Attribute.new("ObjectPosition_Y", str(interaction_pos.y)),
			Attribute.new("ObjectPosition_Z", str(interaction_pos.z))
		]
	)
	if activity == null:
		return null

	# PinGODOT Base remains the single source of edge semantics and direction.
	ProvenanceController.create_provenance_edge(activity, source_vertex)
	if source_vertex.id != target_vertex.id:
		ProvenanceController.create_provenance_edge(activity, target_vertex)
	return activity


func register_input_interaction(
	node: Node,
	event: InputEvent,
	forced_type: String = ""
) -> Vertex:
	if not is_instance_valid(node):
		return null
	var timestamp := _timestamp()
	var node_vertex := find_or_create_vertex(node, timestamp, forced_type)
	if node_vertex == null:
		return null
	var pos := get_node_position(node)
	var activity := ProvenanceController.add_vertex(
		timestamp,
		"Activity",
		"%s_input" % node_vertex.label,
		[
			Attribute.new("event_type", event.get_class()),
			Attribute.new("prov:type", "prov:Activity"),
			Attribute.new("ObjectPosition_X", str(pos.x)),
			Attribute.new("ObjectPosition_Y", str(pos.y)),
			Attribute.new("ObjectPosition_Z", str(pos.z))
		]
	)
	if activity != null:
		ProvenanceController.create_provenance_edge(activity, node_vertex)
	return activity


func register_state_change(
	node: Node,
	property_name: String,
	old_value,
	new_value,
	forced_type: String = ""
) -> Vertex:
	if not is_instance_valid(node):
		return null

	var timestamp := _timestamp()
	var entity_vertex := find_or_create_vertex(node, timestamp, forced_type)
	if entity_vertex == null:
		return null
	var pos := get_node_position(node)
	var activity := ProvenanceController.add_vertex(
		timestamp,
		"Activity",
		"%s_%s_changed" % [entity_vertex.label, property_name],
		[
			Attribute.new("property", property_name),
			Attribute.new("prov:type", "prov:Activity"),
			Attribute.new("old_value", str(old_value)),
			Attribute.new("new_value", str(new_value)),
			Attribute.new("object", entity_vertex.label),
			Attribute.new("ObjectPosition_X", str(pos.x)),
			Attribute.new("ObjectPosition_Y", str(pos.y)),
			Attribute.new("ObjectPosition_Z", str(pos.z))
		]
	)
	if activity == null:
		return null

	_update_attribute(entity_vertex, property_name, str(new_value))
	ProvenanceController.create_provenance_edge(activity, entity_vertex)
	return activity


func register_method_call(
	node: Node,
	method_name: String,
	forced_type: String = ""
) -> Vertex:
	if not is_instance_valid(node):
		return null

	var timestamp := _timestamp()
	var object_vertex := find_or_create_vertex(node, timestamp, forced_type)
	if object_vertex == null:
		return null
	var pos := get_node_position(node)
	var activity := ProvenanceController.add_vertex(
		timestamp,
		"Activity",
		"%s_%s_invoked" % [object_vertex.label, method_name],
		[
			Attribute.new("method", method_name),
			Attribute.new("prov:type", "prov:Activity"),
			Attribute.new("ObjectPosition_X", str(pos.x)),
			Attribute.new("ObjectPosition_Y", str(pos.y)),
			Attribute.new("ObjectPosition_Z", str(pos.z))
		]
	)
	if activity != null:
		ProvenanceController.create_provenance_edge(activity, object_vertex)
	return activity


func register_signal_emission(
	node: Node,
	signal_name: String,
	forced_type: String = ""
) -> Vertex:
	if not is_instance_valid(node):
		return null
	var timestamp := _timestamp()
	var object_vertex := find_or_create_vertex(node, timestamp, forced_type)
	if object_vertex == null:
		return null
	var pos := get_node_position(node)
	var activity := ProvenanceController.add_vertex(
		timestamp,
		"Activity",
		"%s_%s_emitted" % [object_vertex.label, signal_name],
		[
			Attribute.new("signal", signal_name),
			Attribute.new("prov:type", "prov:Activity"),
			Attribute.new("ObjectPosition_X", str(pos.x)),
			Attribute.new("ObjectPosition_Y", str(pos.y)),
			Attribute.new("ObjectPosition_Z", str(pos.z))
		]
	)
	if activity != null:
		ProvenanceController.create_provenance_edge(activity, object_vertex)
	return activity


func find_or_create_vertex(
	node: Node,
	timestamp: String,
	forced_type: String = ""
) -> Vertex:
	if not is_instance_valid(node):
		return null
	# ObjectID maps each live Godot instance to one provenance vertex.
	var object_id := str(node.get_instance_id())
	for vertex in ProvenanceController.vertex_list:
		if vertex.attributes.any(
			func(attribute):
				return attribute.name == "ObjectID" and attribute.value == object_id
		):
			if forced_type != "" and vertex.type != forced_type:
				push_warning(
					"O objeto %s ja foi registrado como %s; a configuracao %s foi ignorada." % [
						node.name, vertex.type, forced_type
					]
				)
			return vertex

	var vertex_type := forced_type if forced_type != "" else _guess_vertex_type(node)
	if not vertex_type in ["Agent", "Activity", "Entity"]:
		push_error("Tipo de vertice invalido no AutoCollector: %s" % vertex_type)
		return null

	var node_name := node.name.strip_edges()
	if node_name == "" or node_name.begins_with("@"):
		var scene_path := node.get_scene_file_path()
		node_name = (
			scene_path.get_file().get_basename()
			if scene_path != ""
			else "%s_%s" % [node.get_class(), object_id]
		)

	var pos := get_node_position(node)
	return ProvenanceController.add_vertex(
		timestamp,
		vertex_type,
		node_name,
		[
			Attribute.new("type", node.get_class()),
			Attribute.new("name", node_name),
			Attribute.new("prov:type", "prov:" + vertex_type),
			Attribute.new("ObjectID", object_id),
			Attribute.new("ObjectPosition_X", str(pos.x)),
			Attribute.new("ObjectPosition_Y", str(pos.y)),
			Attribute.new("ObjectPosition_Z", str(pos.z))
		]
	)


# Backward-compatible alias for projects using the earlier private method.
func _find_or_create_vertex(
	node: Node,
	timestamp: String,
	forced_type: String = ""
) -> Vertex:
	return find_or_create_vertex(node, timestamp, forced_type)


func create_provenance_edge_with_logic(source: Vertex, target: Vertex) -> void:
	ProvenanceController.create_provenance_edge(source, target)


func _update_attribute(vertex: Vertex, name: String, value: String) -> void:
	for attribute in vertex.attributes:
		if attribute.name == name:
			attribute.value = value
			return
	vertex.attributes.append(Attribute.new(name, value))


func _guess_vertex_type(node: Node) -> String:
	var lower_name := node.name.to_lower()
	if lower_name.ends_with("_action") or node.get_class().to_lower().contains("event"):
		return "Activity"
	if node.has_signal("action_triggered") or node.has_signal("decision_made"):
		return "Agent"
	if node is CharacterBody2D or node is CharacterBody3D:
		return "Agent"
	return "Entity"


func _timestamp() -> String:
	return str(Time.get_ticks_msec() / 1000.0)
