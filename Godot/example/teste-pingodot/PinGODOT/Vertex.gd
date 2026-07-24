class_name Vertex
extends RefCounted

# In-memory representation of a PROV Agent, Activity, or Entity.
var id: String
var date: String
var type: String
var label: String
var attributes: Array = []

func _init(_id: String = "", _date: String = "", _type: String = "", _label: String = "", _attrs: Array = []):
	id = _id
	date = _date
	type = _type
	label = _label
	# Clone attributes to prevent vertices from sharing mutable metadata.
	for a in _attrs:
		attributes.append(a.clone())

func clone() -> Vertex:
	return Vertex.new(id, date, type, label, attributes)
