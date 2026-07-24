class_name Attribute
extends RefCounted

# Name-value metadata attached to provenance vertices.
var name: String = ""
var value: String = ""

func _init(_name: String = "", _value: String = ""):
	name = _name
	value = _value

func clone() -> Attribute:
	return Attribute.new(name, value)

func get_formatted() -> String:
	return "%s: %s" % [name, value]
