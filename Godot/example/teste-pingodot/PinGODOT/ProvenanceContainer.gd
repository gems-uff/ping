class_name ProvenanceContainer
extends RefCounted

# Detached provenance collection used for lookup and traversal.
var vertex_list: Array = []
var edge_list: Array = []

func _init(_vertices: Array = [], _edges: Array = []):
	if _vertices.size():
		vertex_list = _vertices.duplicate(true)
	if _edges.size():
		edge_list = _edges.duplicate(true)

func get_vertex_by_id(vid: String) -> Vertex:
	for v in vertex_list:
		if v.id == vid:
			return v
	return null

func get_edges_to(vid: String) -> Array:
	var out = []
	for e in edge_list:
		if e.target_id == vid:
			out.append(e)
	return out

func get_edges_from(vid: String) -> Array:
	var out = []
	for e in edge_list:
		if e.source_id == vid:
			out.append(e)
	return out
