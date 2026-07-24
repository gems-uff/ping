extends Node

# Lightweight API for explicit provenance collection from a scene node.
var use_custom_coords: bool = false
var custom_position: Vector3 = Vector3.ZERO
var current_vertex: Vertex = null

func _add_position_attributes(attrs: Array) -> void:
	if use_custom_coords:
		attrs.append(Attribute.new("ObjectPosition_X", str(custom_position.x)))
		attrs.append(Attribute.new("ObjectPosition_Y", str(custom_position.y)))
		attrs.append(Attribute.new("ObjectPosition_Z", str(custom_position.z)))
	elif get_parent() != null:
		if get_parent() is Node2D:
			var pos = get_parent().global_position
			attrs.append(Attribute.new("ObjectPosition_X", str(pos.x)))
			attrs.append(Attribute.new("ObjectPosition_Y", str(pos.y)))
			attrs.append(Attribute.new("ObjectPosition_Z", "0"))
		elif get_parent() is Node3D:
			var pos = get_parent().global_position
			attrs.append(Attribute.new("ObjectPosition_X", str(pos.x)))
			attrs.append(Attribute.new("ObjectPosition_Y", str(pos.y)))
			attrs.append(Attribute.new("ObjectPosition_Z", str(pos.z)))

func new_agent(label: String) -> Vertex:
	var attrs = []
	attrs.append(Attribute.new("type", "Agent"))
	_add_position_attributes(attrs)
	var timestamp = str(Time.get_ticks_msec() / 1000.0)
	var vertex = ProvenanceController.add_vertex(timestamp, "Agent", label, attrs)
	current_vertex = vertex
	return vertex

func new_activity(label: String) -> Vertex:
	var attrs = []
	attrs.append(Attribute.new("type", "Activity"))
	_add_position_attributes(attrs)
	var timestamp = str(Time.get_ticks_msec() / 1000.0)
	var vertex = ProvenanceController.add_vertex(timestamp, "Activity", label, attrs)
	# Only events from this collector are chained; unrelated objects stay independent.
	if current_vertex:
		ProvenanceController.create_provenance_edge(vertex, current_vertex)
	current_vertex = vertex
	return vertex

func new_entity(label: String) -> Vertex:
	var attrs = []
	attrs.append(Attribute.new("type", "Entity"))
	_add_position_attributes(attrs)
	var timestamp = str(Time.get_ticks_msec() / 1000.0)
	var vertex: Vertex
	if current_vertex:
		vertex = ProvenanceController.add_vertex(timestamp, "Entity", label, attrs, current_vertex)
	else:
		vertex = ProvenanceController.add_vertex(timestamp, "Entity", label, attrs)
	current_vertex = vertex
	return vertex
