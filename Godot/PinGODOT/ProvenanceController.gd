@tool
extends Node
# Register this script as the "ProvenanceController" Autoload.

var vertex_list: Array = []
var edge_list: Array = []
var last_activity_global: Vertex = null

# Disabled by default to avoid interpreting temporal order as causality.
var auto_chain_activities: bool = false

# Independent counters keep generated IDs unique after JSON imports.
var _vertex_counter: int = 0
var _edge_counter: int = 0

func _ready():
	print("ProvenanceController inicializado")

func _new_vertex_id() -> String:
	_vertex_counter += 1
	return "vertex_%d" % _vertex_counter

func _new_edge_id() -> String:
	_edge_counter += 1
	return "edge_%d" % _edge_counter

func add_vertex(date: String, type: String, label: String, attrs: Array, target = null) -> Vertex:
	if not type in ["Agent", "Activity", "Entity"]:
		push_error("Tipo de vertice invalido: %s" % type)
		return null

	var v = Vertex.new(_new_vertex_id(), date, type, label, attrs)
	vertex_list.append(v)

	# Optional global chaining represents consecutive activities as WasInformedBy.
	if v.type == "Activity" and auto_chain_activities:
		if last_activity_global != null:
			var temporal_edge = Edge.new(
				_new_edge_id(),
				"Auto",
				"WasInformedBy",
				"",
				v.id,
				last_activity_global.id
			)
			edge_list.append(temporal_edge)
		last_activity_global = v

	if target:
		create_provenance_edge(v, target)
	return v

func create_provenance_edge(src: Vertex, tgt: Vertex) -> void:
	if src == null or tgt == null:
		push_error("Nao e possivel criar uma aresta com vertices nulos")
		return
	if src.id == tgt.id:
		push_error("Arestas de proveniencia autorreferentes nao sao permitidas")
		return

	var rel = "WasAssociatedWith"

	if src.type == "Activity":
		if tgt.type == "Activity":   rel = "WasInformedBy"
		elif tgt.type == "Agent":    rel = "WasAssociatedWith"
		elif tgt.type == "Entity":   rel = "Used"
	elif src.type == "Agent":
		if tgt.type == "Activity":
			# PROV association is normalized to Activity -> Agent.
			var tmp = src
			src = tgt
			tgt = tmp
			rel = "WasAssociatedWith"
		elif tgt.type == "Agent":    rel = "ActedOnBehalfOf"
		elif tgt.type == "Entity":
			# PROV attribution is normalized to Entity -> Agent.
			var tmp = src
			src = tgt
			tgt = tmp
			rel = "WasAttributedTo"
	elif src.type == "Entity":
		if tgt.type == "Activity":   rel = "WasGeneratedBy"
		elif tgt.type == "Agent":    rel = "WasAttributedTo"
		elif tgt.type == "Entity":   rel = "WasDerivedFrom"

	# Preserve one edge for each source, target, and relation tuple.
	for e in edge_list:
		if e.source_id == src.id and e.target_id == tgt.id and e.relation == rel:
			return

	var e = Edge.new(_new_edge_id(), "Neutral", rel, "", src.id, tgt.id)
	edge_list.append(e)

func create_influence_edge(target_id: String, source_id: String, name: String, value: String) -> String:
	var e = Edge.new(_new_edge_id(), name, "WasInfluencedBy", value, source_id, target_id)
	edge_list.append(e)
	return e.id

func update_influence_edge(edge_id: String, source_id: String, target_id: String, name: String, value: String) -> bool:
	for i in edge_list.size():
		if edge_list[i].id == edge_id:
			edge_list[i] = Edge.new(edge_id, name, "WasInfluencedBy", value, source_id, target_id)
			return true
	push_error("Aresta de influencia nao encontrada: %s" % edge_id)
	return false

func clear() -> void:
	vertex_list.clear()
	edge_list.clear()
	last_activity_global = null
	_vertex_counter = 0
	_edge_counter = 0
