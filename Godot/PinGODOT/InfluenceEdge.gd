class_name InfluenceEdge
extends RefCounted

# Pending influence metadata resolved when a target event is observed.
var tag: String
var id: String
var source: String
var inf_type: String
var inf_value: String
var consumable: bool = false
var quantity: int = 1
var missable_id: String = ""
var expiration_time: float = -1.0

func _init(_tag: String = "", _id: String = "", _src: String = "", _type: String = "", _val: String = "", _cons: bool = false, _qty: int = 1, _missable: String = "", _exp: float = -1.0):
	tag = _tag
	id = _id
	source = _src
	inf_type = _type
	inf_value = _val
	consumable = _cons
	quantity = _qty
	missable_id = _missable
	expiration_time = _exp
