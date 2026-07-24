@tool
class_name ProvenanceExporter
extends Node

# JSON serializer and validated importer for PinGODOT provenance graphs.

func export_to_json(filepath: String = "user://provenance_data.json") -> bool:
	var data = {
		"metadata": {
			"format": "PinGODOT",
			"version": "1.0",
			"timestamp": Time.get_datetime_string_from_system(true),
			"vertex_count": ProvenanceController.vertex_list.size(),
			"edge_count": ProvenanceController.edge_list.size()
		},
		"vertices": [],
		"edges": []
	}

	for vertex in ProvenanceController.vertex_list:
		var attributes = []
		for attr in vertex.attributes:
			# Arrays preserve attribute order and allow repeated names.
			attributes.append({"name": attr.name, "value": attr.value})

		data.vertices.append({
			"id": vertex.id,
			"date": vertex.date,
			"type": vertex.type,
			"label": vertex.label,
			"attributes": attributes
		})

	for edge in ProvenanceController.edge_list:
		data.edges.append({
			"id": edge.id,
			"label": edge.label,
			"relation": edge.relation,
			"value": edge.value,
			"source": edge.source_id,
			"target": edge.target_id
		})

	var file = FileAccess.open(filepath, FileAccess.WRITE)
	if file == null:
		push_error("Falha ao exportar JSON para %s (erro %s)" % [filepath, FileAccess.get_open_error()])
		return false

	file.store_string(JSON.stringify(data, "\t"))
	file.close()
	print("Proveniencia exportada para JSON: ", filepath)
	return true


func import_from_json(filepath: String) -> bool:
	var file = FileAccess.open(filepath, FileAccess.READ)
	if file == null:
		push_error("Nao foi possivel abrir %s (erro %s)" % [filepath, FileAccess.get_open_error()])
		return false

	var json = JSON.new()
	var error = json.parse(file.get_as_text())
	file.close()
	if error != OK:
		push_error("JSON invalido em %s, linha %d: %s" % [filepath, json.get_error_line(), json.get_error_message()])
		return false

	var data = json.data
	if not data is Dictionary:
		push_error("A raiz do JSON deve ser um objeto")
		return false
	if not data.has("vertices") or not data.vertices is Array:
		push_error("O JSON precisa conter o array 'vertices'")
		return false
	if not data.has("edges") or not data.edges is Array:
		push_error("O JSON precisa conter o array 'edges'")
		return false

	# Validate the complete graph in memory before replacing the active data.
	var imported_vertices: Array = []
	var imported_edges: Array = []
	var vertex_ids: Dictionary = {}
	var edge_ids: Dictionary = {}
	var max_vertex_idx := 0
	var max_edge_idx := 0
	var last_imported_activity: Vertex = null

	for v_data in data.vertices:
		if not v_data is Dictionary:
			push_error("Cada vertice deve ser um objeto JSON")
			return false
		for required_key in ["id", "type"]:
			if not v_data.has(required_key) or not v_data[required_key] is String or v_data[required_key].is_empty():
				push_error("Vertice sem campo textual obrigatorio: %s" % required_key)
				return false

		var vertex_id: String = v_data.id
		var vertex_type: String = v_data.type
		if vertex_ids.has(vertex_id):
			push_error("ID de vertice duplicado: %s" % vertex_id)
			return false
		if not vertex_type in ["Agent", "Activity", "Entity"]:
			push_error("Tipo de vertice invalido: %s" % vertex_type)
			return false

		var attrs: Array = []
		if v_data.has("attributes"):
			if v_data.attributes is Array:
				for attr_data in v_data.attributes:
					if not attr_data is Dictionary or not attr_data.has("name") or not attr_data.has("value"):
						push_error("Atributo invalido no vertice %s" % vertex_id)
						return false
					attrs.append(Attribute.new(str(attr_data.name), str(attr_data.value)))
			elif v_data.attributes is Dictionary:
				# Accept the dictionary layout produced by earlier JSON exports.
				for key in v_data.attributes.keys():
					attrs.append(Attribute.new(str(key), str(v_data.attributes[key])))
			else:
				push_error("'attributes' deve ser um array ou objeto no vertice %s" % vertex_id)
				return false

		var vertex = Vertex.new(
			vertex_id,
			str(v_data.get("date", "")),
			vertex_type,
			str(v_data.get("label", "")),
			attrs
		)
		imported_vertices.append(vertex)
		vertex_ids[vertex_id] = true
		if vertex_type == "Activity":
			last_imported_activity = vertex
		max_vertex_idx = max(max_vertex_idx, _numeric_id_suffix(vertex_id, "vertex_"))

	for e_data in data.edges:
		if not e_data is Dictionary:
			push_error("Cada aresta deve ser um objeto JSON")
			return false
		for required_key in ["id", "relation", "source", "target"]:
			if not e_data.has(required_key) or not e_data[required_key] is String or e_data[required_key].is_empty():
				push_error("Aresta sem campo textual obrigatorio: %s" % required_key)
				return false

		var edge_id: String = e_data.id
		if edge_ids.has(edge_id):
			push_error("ID de aresta duplicado: %s" % edge_id)
			return false
		if not vertex_ids.has(e_data.source) or not vertex_ids.has(e_data.target):
			push_error("Aresta %s referencia um vertice inexistente" % edge_id)
			return false

		imported_edges.append(Edge.new(
			edge_id,
			str(e_data.get("label", "")),
			e_data.relation,
			str(e_data.get("value", "")),
			e_data.source,
			e_data.target
		))
		edge_ids[edge_id] = true
		max_edge_idx = max(max_edge_idx, _numeric_id_suffix(edge_id, "edge_"))

	# Continue ID generation above the highest imported numeric suffix.
	ProvenanceController.vertex_list = imported_vertices
	ProvenanceController.edge_list = imported_edges
	ProvenanceController.last_activity_global = last_imported_activity
	ProvenanceController._vertex_counter = max_vertex_idx
	ProvenanceController._edge_counter = max_edge_idx

	print("Proveniencia importada: ", imported_vertices.size(), " vertices e ", imported_edges.size(), " arestas")
	return true


func _numeric_id_suffix(id: String, prefix: String) -> int:
	if not id.begins_with(prefix):
		return 0
	var suffix = id.trim_prefix(prefix)
	return suffix.to_int() if suffix.is_valid_int() else 0


func show_notification(message: String) -> void:
	var dialog = AcceptDialog.new()
	dialog.title = "Proveniencia"
	dialog.dialog_text = message
	dialog.size = Vector2(400, 150)
	get_tree().get_root().add_child(dialog)
	dialog.popup_centered()
	dialog.confirmed.connect(func(): dialog.queue_free())


static func quick_export_json(filename: String = "provenance.json") -> bool:
	var exporter = ProvenanceExporter.new()
	var success = exporter.export_to_json("user://%s" % filename)
	exporter.free()
	return success
