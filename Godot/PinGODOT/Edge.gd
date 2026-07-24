class_name Edge
extends RefCounted

# Directed PROV relation between two vertex identifiers.
var id: String
var label: String
var relation: String
var value: String
var source_id: String
var target_id: String

func _init(_id: String = "", _label: String = "", _relation: String = "", _value: String = "", _src: String = "", _tgt: String = ""):
	id = _id
	label = _label
	relation = _relation
	value = _value
	source_id = _src
	target_id = _tgt
