extends Node
# Register this script as the "InfluenceController" Autoload.

# Consumable influences are tracked separately because their quantity changes.
var inf_list: Array = []
var consumable_list: Array = []

func _ready():
	print("InfluenceController inicializado")

func create_influence(tag: String, id: String, src_vertex_id: String, name: String, value: String, consumable: bool, quantity: int, missable_id: String = "", duration_seconds: float = -1.0) -> InfluenceEdge:
	if src_vertex_id.is_empty():
		push_error("A influencia precisa de um vertice de origem")
		return null
	if consumable and quantity <= 0:
		push_error("A quantidade de uma influencia consumivel deve ser positiva")
		return null

	# A negative expiration time represents an influence with no time limit.
	var expiration_time = -1.0
	if duration_seconds >= 0.0:
		expiration_time = Time.get_ticks_msec() / 1000.0 + duration_seconds

	var inf = InfluenceEdge.new(tag, id, src_vertex_id, name, value, consumable, quantity, missable_id, expiration_time)
	if consumable:
		consumable_list.append(inf)
	else:
		inf_list.append(inf)
	return inf

func clean_influences():
	inf_list.clear()
	consumable_list.clear()

func remove_influence_by_tag(tag: String):
	inf_list = inf_list.filter(func(i): return i.tag != tag)
	consumable_list = consumable_list.filter(func(i): return i.tag != tag)

func remove_influence_by_id(id: String):
	inf_list = inf_list.filter(func(i): return i.id != id)
	consumable_list = consumable_list.filter(func(i): return i.id != id)

func was_influenced_by_tag(tag: String, target_id: String):
	_check_influence(tag, target_id, true)

func was_influenced_by_id(id: String, target_id: String):
	_check_influence(id, target_id, false)

func _check_influence(type_: String, target_id: String, is_tag: bool):
	# Expired and depleted influences are removed after iteration.
	for list_idx in range(2):
		var list = inf_list if list_idx == 0 else consumable_list
		var to_remove = []

		for i in list:
				var key = i.tag if is_tag else i.id
				if key == type_:
					var now = Time.get_ticks_msec() / 1000.0
					if i.expiration_time < 0 or now < i.expiration_time:
						if i.missable_id != "":
							ProvenanceController.update_influence_edge(i.missable_id, i.source, target_id, i.inf_type, i.inf_value)
						else:
							ProvenanceController.create_influence_edge(target_id, i.source, i.inf_type, i.inf_value)
						if i.consumable:
							i.quantity -= 1
							if i.quantity <= 0:
								to_remove.append(i)
					else:
						to_remove.append(i)

		for r in to_remove:
			if list_idx == 0:
				inf_list.erase(r)
			else:
				consumable_list.erase(r)
