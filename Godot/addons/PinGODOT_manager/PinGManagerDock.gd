@tool
class_name PinGManagerDock
extends Control

const BASE_EXPORTER_PATH := "res://PinGODOT/ProvenanceExporter.gd"
const VISUALIZER_PATH := "res://addons/PinGODOT_manager/ProvenanceGraphVisualizer.gd"
const SIGNALS_BEGIN := "# PINGODOT_GENERATED_SIGNALS_BEGIN"
const SIGNALS_END := "# PINGODOT_GENERATED_SIGNALS_END"
const EMIT_MARKER := "# PINGODOT_GENERATED_EMIT:"

# Generated markers make source instrumentation reversible and idempotent.
var collector_list: ItemList
var _function_regex := RegEx.new()
var _signal_regex := RegEx.new()


func _ready() -> void:
	name = "PinGODOT Manager"
	_function_regex.compile("^(\\s*)func\\s+(\\w+)\\s*\\(")
	_signal_regex.compile("^\\s*signal\\s+(\\w+)")
	_build_interface()
	_refresh_list()


func _build_interface() -> void:
	var container := VBoxContainer.new()
	container.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(container)

	var title := Label.new()
	title.text = "Objetos com AutoCollector"
	title.add_theme_font_size_override("font_size", 14)
	container.add_child(title)

	var hint := Label.new()
	hint.text = "Selecione os coletores que deseja instrumentar."
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	container.add_child(hint)

	collector_list = ItemList.new()
	collector_list.select_mode = ItemList.SELECT_MULTI
	collector_list.custom_minimum_size = Vector2(0, 150)
	collector_list.size_flags_vertical = Control.SIZE_EXPAND_FILL
	container.add_child(collector_list)

	_add_button(container, "Atualizar lista", _refresh_list)
	_add_button(container, "Instrumentar metodos selecionados", _instrument_selected)
	_add_button(container, "Desinstrumentar scripts selecionados", _deinstrument_selected)
	container.add_child(HSeparator.new())
	_add_button(container, "Importar JSON", _open_import_dialog)
	_add_button(container, "Exportar JSON", _open_export_dialog)
	_add_button(container, "Visualizar grafo importado", _visualize_graph)
	_add_button(container, "Estatisticas", _show_stats)


func _add_button(container: VBoxContainer, text: String, action: Callable) -> void:
	var button := Button.new()
	button.text = text
	button.pressed.connect(action)
	container.add_child(button)


func _refresh_list() -> void:
	collector_list.clear()
	var root := get_tree().edited_scene_root
	if root == null:
		return
	for collector in _find_collectors(root):
		var target_name: String = (
			str(collector.target_node.name)
			if is_instance_valid(collector.target_node)
			else "sem target"
		)
		var index := collector_list.add_item(
			"%s -> %s [%s]" % [collector.name, target_name, collector.vertex_type]
		)
		collector_list.set_item_metadata(index, root.get_path_to(collector))


func _find_collectors(node: Node) -> Array:
	var result: Array = []
	if node is AutoCollector:
		result.append(node)
	for child in node.get_children():
		if child is Node:
			result.append_array(_find_collectors(child))
	return result


func _selected_collectors() -> Array:
	var root := get_tree().edited_scene_root
	var result: Array = []
	if root == null:
		return result
	for index in collector_list.get_selected_items():
		var path: NodePath = collector_list.get_item_metadata(index)
		var collector := root.get_node_or_null(path)
		if collector is AutoCollector:
			result.append(collector)
	return result


func _instrument_selected() -> void:
	var collectors := _selected_collectors()
	if collectors.is_empty():
		_show_message("Instrumentacao", "Selecione pelo menos um AutoCollector.")
		return

	# The current selection defines which scripts may be rewritten.
	var scripts: Dictionary = {}
	for collector in collectors:
		if not is_instance_valid(collector.target_node):
			continue
		var script = collector.target_node.get_script()
		if not script is GDScript:
			continue
		if not scripts.has(script.resource_path):
			scripts[script.resource_path] = {"script": script, "methods": {}}

	# Merge methods from collectors sharing a script to preserve every selection.
	var root := get_tree().edited_scene_root
	for collector in _find_collectors(root):
		if not is_instance_valid(collector.target_node):
			continue
		var script = collector.target_node.get_script()
		if script is GDScript and scripts.has(script.resource_path):
			for method_name in collector.get_selected_methods():
				scripts[script.resource_path].methods[method_name] = true

	if scripts.is_empty():
		_show_message("Instrumentacao", "Nenhum metodo selecionado em um script GDScript.")
		return

	var messages: Array[String] = []
	var success_count := 0
	for entry in scripts.values():
		var result := _instrument_script(entry.script, entry.methods.keys())
		messages.append(result.message)
		if result.success:
			success_count += 1

	_show_message(
		"Instrumentacao",
		"%d de %d scripts instrumentados.\n\n%s" % [
			success_count, scripts.size(), "\n".join(messages)
		]
	)


func _deinstrument_selected() -> void:
	var collectors := _selected_collectors()
	if collectors.is_empty():
		_show_message("Desinstrumentacao", "Selecione pelo menos um AutoCollector.")
		return

	var scripts: Dictionary = {}
	for collector in collectors:
		if is_instance_valid(collector.target_node):
			var script = collector.target_node.get_script()
			if script is GDScript:
				scripts[script.resource_path] = script

	var messages: Array[String] = []
	var success_count := 0
	for script in scripts.values():
		var result := _deinstrument_script(script)
		messages.append(result.message)
		if result.success:
			success_count += 1

	_show_message(
		"Desinstrumentacao",
		"%d de %d scripts processados.\n\n%s" % [
			success_count, scripts.size(), "\n".join(messages)
		]
	)


func _instrument_script(script: GDScript, method_names: Array) -> Dictionary:
	var path := script.resource_path
	if path == "" or method_names.is_empty():
		return _result(false, "%s: nenhum metodo selecionado." % path)

	var original := _read_text(path)
	if original == "":
		return _result(false, "%s: nao foi possivel ler o script." % path)

	# Rebuild from a clean source to prevent duplicate generated blocks.
	var clean := _remove_generated_instrumentation(original)
	var lines := Array(clean.split("\n"))
	var generated: Array = []
	var inserted_methods: Array[String] = []
	var pending_method := ""
	var function_indent := ""

	for line_variant in lines:
		var line := str(line_variant)
		generated.append(line)

		if pending_method != "":
			if line.strip_edges().ends_with(":"):
				_append_method_emit(generated, function_indent, pending_method)
				inserted_methods.append(pending_method)
				pending_method = ""
				function_indent = ""
			continue

		var match := _function_regex.search(line)
		if match == null:
			continue
		var method_name := match.get_string(2)
		if not method_names.has(method_name):
			continue
		function_indent = match.get_string(1)
		if line.strip_edges().ends_with(":"):
			_append_method_emit(generated, function_indent, method_name)
			inserted_methods.append(method_name)
		else:
			pending_method = method_name

	if inserted_methods.is_empty():
		return _result(false, "%s: os metodos selecionados nao foram encontrados." % path)

	_insert_generated_signals(generated, clean, inserted_methods)
	var updated := "\n".join(generated)
	# Invalid generated code is rejected before the project script is replaced.
	var validation := _validate_gdscript(updated)
	if validation != OK:
		return _result(false, "%s: a instrumentacao gerou GDScript invalido (erro %d)." % [path, validation])

	if not _write_with_backup(path, original, updated):
		return _result(false, "%s: falha ao salvar; o original foi preservado." % path)

	script.source_code = updated
	script.reload()
	return _result(true, "%s: %d metodos instrumentados." % [path, inserted_methods.size()])


func _deinstrument_script(script: GDScript) -> Dictionary:
	var path := script.resource_path
	var original := _read_text(path)
	if original == "":
		return _result(false, "%s: nao foi possivel ler o script." % path)
	var updated := _remove_generated_instrumentation(original)
	if updated == original:
		return _result(true, "%s: nenhuma instrumentacao do PinGODOT encontrada." % path)

	var validation := _validate_gdscript(updated)
	if validation != OK:
		return _result(false, "%s: remocao cancelada por erro de validacao %d." % [path, validation])
	if not _write_with_backup(path, original, updated):
		return _result(false, "%s: falha ao salvar; o original foi preservado." % path)

	script.source_code = updated
	script.reload()
	return _result(true, "%s: instrumentacao removida." % path)


func _append_method_emit(lines: Array, indent: String, method_name: String) -> void:
	var body_indent := indent + "\t"
	lines.append("%s%s %s" % [body_indent, EMIT_MARKER, method_name])
	lines.append(
		"%semit_signal(\"%s_called\")" % [body_indent, method_name.lstrip("_")]
	)


func _insert_generated_signals(
	lines: Array,
	clean_source: String,
	method_names: Array[String]
) -> void:
	var existing: Dictionary = {}
	for line in clean_source.split("\n"):
		var match := _signal_regex.search(line)
		if match != null:
			existing[match.get_string(1)] = true

	var declarations: Array[String] = []
	for method_name in method_names:
		var signal_name := "%s_called" % method_name.lstrip("_")
		if not existing.has(signal_name):
			declarations.append("signal %s" % signal_name)
	if declarations.is_empty():
		return

	var insertion_index := 0
	for index in range(lines.size()):
		if str(lines[index]).strip_edges().begins_with("extends "):
			insertion_index = index + 1
			break

	var block: Array[String] = ["", SIGNALS_BEGIN]
	block.append_array(declarations)
	block.append(SIGNALS_END)
	for index in range(block.size() - 1, -1, -1):
		lines.insert(insertion_index, block[index])


func _remove_generated_instrumentation(source: String) -> String:
	var output: Array[String] = []
	var inside_signal_block := false
	var skip_next_emit := false

	for line_variant in source.split("\n"):
		var line := str(line_variant)
		var trimmed := line.strip_edges()
		if trimmed == SIGNALS_BEGIN:
			inside_signal_block = true
			continue
		if inside_signal_block:
			if trimmed == SIGNALS_END:
				inside_signal_block = false
			continue
		if trimmed.begins_with(EMIT_MARKER):
			skip_next_emit = true
			continue
		if skip_next_emit:
			skip_next_emit = false
			if trimmed.begins_with("emit_signal("):
				continue
		output.append(line)

	return "\n".join(output)


func _validate_gdscript(source: String) -> Error:
	var validator := GDScript.new()
	validator.source_code = source
	return validator.reload()


func _write_with_backup(path: String, original: String, updated: String) -> bool:
	# Every source rewrite is preceded by a timestamped backup.
	var timestamp := Time.get_datetime_string_from_system().replace(":", "-")
	var backup_path := "%s.pingodot-%s.bak" % [path, timestamp]
	var backup := FileAccess.open(backup_path, FileAccess.WRITE)
	if backup == null:
		push_error("Nao foi possivel criar backup: %s" % backup_path)
		return false
	backup.store_string(original)
	backup.close()

	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(updated)
	file.close()
	return true


func _read_text(path: String) -> String:
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return ""
	var content := file.get_as_text()
	file.close()
	return content


func _result(success: bool, message: String) -> Dictionary:
	return {"success": success, "message": message}


func _open_import_dialog() -> void:
	var dialog := FileDialog.new()
	dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	dialog.access = FileDialog.ACCESS_USERDATA
	dialog.add_filter("*.json", "JSON")
	dialog.title = "Importar proveniencia JSON"
	dialog.file_selected.connect(_import_json.bind(dialog))
	add_child(dialog)
	dialog.popup_centered(Vector2i(700, 450))


func _import_json(path: String, dialog: FileDialog) -> void:
	var exporter = _new_exporter()
	if exporter == null:
		_show_message("Erro", "Instale o Base em res://PinGODOT.")
	else:
		var success: bool = exporter.import_from_json(path)
		exporter.free()
		_show_message(
			"Importacao",
			"Proveniencia importada com sucesso. Agora o grafo pode ser visualizado."
			if success
			else "Falha ao importar o JSON. Consulte o painel de erros."
		)
	dialog.queue_free()


func _open_export_dialog() -> void:
	if ProvenanceController.vertex_list.is_empty():
		_show_message("Exportacao", "Nao ha proveniencia carregada para exportar.")
		return
	var dialog := FileDialog.new()
	dialog.file_mode = FileDialog.FILE_MODE_SAVE_FILE
	dialog.access = FileDialog.ACCESS_USERDATA
	dialog.add_filter("*.json", "JSON")
	var timestamp := Time.get_datetime_string_from_system().replace(":", "-")
	dialog.current_file = "provenance_data_%s.json" % timestamp
	dialog.title = "Exportar proveniencia JSON"
	dialog.file_selected.connect(_export_json.bind(dialog))
	add_child(dialog)
	dialog.popup_centered(Vector2i(700, 450))


func _export_json(path: String, dialog: FileDialog) -> void:
	var final_path: String = (
		path if path.to_lower().ends_with(".json") else path + ".json"
	)
	var exporter = _new_exporter()
	if exporter == null:
		_show_message("Erro", "Instale o Base em res://PinGODOT.")
	else:
		var success: bool = exporter.export_to_json(final_path)
		exporter.free()
		_show_message(
			"Exportacao",
			"JSON exportado para %s" % final_path
			if success
			else "Falha ao exportar o JSON. Consulte o painel de erros."
		)
	dialog.queue_free()


func _new_exporter():
	# JSON serialization is delegated to Base to keep one data contract.
	var exporter_script = load(BASE_EXPORTER_PATH)
	return exporter_script.new() if exporter_script != null else null


func _visualize_graph() -> void:
	if ProvenanceController.vertex_list.is_empty():
		_show_message(
			"Visualizacao",
			"Nao ha dados no processo do editor. Exporte o JSON durante o jogo e importe-o pelo Dock."
		)
		return
	var visualizer_script = load(VISUALIZER_PATH)
	if visualizer_script == null:
		_show_message("Erro", "ProvenanceGraphVisualizer.gd nao encontrado.")
		return

	var window := Window.new()
	window.title = "Grafo de proveniencia"
	window.size = Vector2i(1200, 800)
	window.transient = true
	window.exclusive = false

	var visualizer = visualizer_script.new()
	window.add_child(visualizer)
	visualizer.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

	var toolbar := HBoxContainer.new()
	toolbar.position = Vector2(10, 10)
	var refresh := Button.new()
	refresh.text = "Recalcular layout"
	refresh.pressed.connect(visualizer.refresh)
	toolbar.add_child(refresh)
	var fit := Button.new()
	fit.text = "Enquadrar"
	fit.pressed.connect(visualizer.fit_to_view)
	toolbar.add_child(fit)
	var export_image := Button.new()
	export_image.text = "Exportar imagem"
	export_image.pressed.connect(_export_graph_image.bind(visualizer))
	toolbar.add_child(export_image)
	var group_repeated := CheckButton.new()
	group_repeated.text = "Agrupar repetidos"
	group_repeated.button_pressed = true
	group_repeated.tooltip_text = (
		"Representa Activities equivalentes como um unico no visual."
	)
	group_repeated.toggled.connect(
		visualizer.set_group_repeated_activities
	)
	toolbar.add_child(group_repeated)
	var close := Button.new()
	close.text = "Fechar"
	close.pressed.connect(window.queue_free)
	toolbar.add_child(close)
	window.add_child(toolbar)

	var timeline_panel := PanelContainer.new()
	timeline_panel.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	timeline_panel.offset_left = 10.0
	timeline_panel.offset_right = -10.0
	timeline_panel.offset_top = -55.0
	timeline_panel.offset_bottom = -10.0

	var timeline := HBoxContainer.new()
	timeline.add_theme_constant_override("separation", 10)
	timeline_panel.add_child(timeline)

	var timeline_title := Label.new()
	timeline_title.text = "Linha do tempo"
	timeline.add_child(timeline_title)

	var time_range: Vector2 = visualizer.get_time_range()
	var timeline_slider := HSlider.new()
	timeline_slider.min_value = time_range.x
	timeline_slider.max_value = max(time_range.y, time_range.x + 0.01)
	timeline_slider.step = max(time_range.y / 500.0, 0.01)
	timeline_slider.value = time_range.y
	timeline_slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	timeline_slider.custom_minimum_size = Vector2(500, 0)
	timeline.add_child(timeline_slider)

	var timeline_value := Label.new()
	timeline_value.custom_minimum_size = Vector2(150, 0)
	timeline_value.text = "%.2f / %.2f s" % [time_range.y, time_range.y]
	timeline.add_child(timeline_value)

	var show_all_time := Button.new()
	show_all_time.text = "Tempo completo"
	timeline.add_child(show_all_time)

	timeline_slider.value_changed.connect(
		_on_timeline_changed.bind(
			visualizer,
			timeline_value,
			time_range.y
		)
	)
	show_all_time.pressed.connect(
		_show_full_timeline.bind(timeline_slider, time_range.y)
	)
	window.add_child(timeline_panel)

	add_child(window)
	window.close_requested.connect(window.queue_free)
	window.popup_centered()


func _on_timeline_changed(
	value: float,
	visualizer: Control,
	value_label: Label,
	maximum: float
) -> void:
	visualizer.set_time_cutoff(value)
	value_label.text = "%.2f / %.2f s" % [value, maximum]


func _show_full_timeline(slider: HSlider, maximum: float) -> void:
	slider.value = maximum


func _export_graph_image(visualizer: Control) -> void:
	var timestamp := Time.get_datetime_string_from_system().replace(":", "-")
	visualizer.export_graph_image("user://provenance_graph_%s.png" % timestamp)


func _show_stats() -> void:
	var type_counts: Dictionary = {}
	var relation_counts: Dictionary = {}
	for vertex in ProvenanceController.vertex_list:
		type_counts[vertex.type] = type_counts.get(vertex.type, 0) + 1
	for edge in ProvenanceController.edge_list:
		relation_counts[edge.relation] = relation_counts.get(edge.relation, 0) + 1

	var message := "Vertices: %d\nArestas: %d\nComponentes: %d\n\nTIPOS\n" % [
		ProvenanceController.vertex_list.size(),
		ProvenanceController.edge_list.size(),
		_count_connected_components()
	]
	for type_name in type_counts.keys():
		message += "  %s: %d\n" % [type_name, type_counts[type_name]]
	message += "\nRELACOES\n"
	for relation in relation_counts.keys():
		message += "  %s: %d\n" % [relation, relation_counts[relation]]
	_show_message("Estatisticas de proveniencia", message)


func _count_connected_components() -> int:
	if ProvenanceController.vertex_list.is_empty():
		return 0
	# Edge direction is irrelevant for this structural connectivity diagnostic.
	var adjacency: Dictionary = {}
	for vertex in ProvenanceController.vertex_list:
		adjacency[vertex.id] = []
	for edge in ProvenanceController.edge_list:
		if adjacency.has(edge.source_id) and adjacency.has(edge.target_id):
			adjacency[edge.source_id].append(edge.target_id)
			adjacency[edge.target_id].append(edge.source_id)

	var visited: Dictionary = {}
	var component_count: int = 0
	for vertex_id in adjacency.keys():
		if visited.has(vertex_id):
			continue
		component_count += 1
		var stack: Array = [vertex_id]
		visited[vertex_id] = true
		while not stack.is_empty():
			var current_id: String = str(stack.pop_back())
			for neighbor_id in adjacency[current_id]:
				if not visited.has(neighbor_id):
					visited[neighbor_id] = true
					stack.append(neighbor_id)
	return component_count


func _show_message(title: String, text: String) -> void:
	var dialog := AcceptDialog.new()
	dialog.title = title
	dialog.dialog_text = text
	dialog.exclusive = false
	add_child(dialog)
	dialog.confirmed.connect(dialog.queue_free)
	dialog.popup_centered()
