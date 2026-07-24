@tool
extends EditorInspectorPlugin

# Custom inspector for selecting the target members monitored by AutoCollector.
var checkboxes: Dictionary = {}


func _can_handle(object: Object) -> bool:
	return object is AutoCollector


func _parse_begin(object: Object) -> void:
	checkboxes.clear()
	var collector := object as AutoCollector
	var container := VBoxContainer.new()

	var title := Label.new()
	title.text = "Monitoramento do PinGODOT"
	title.add_theme_font_size_override("font_size", 14)
	title.add_theme_color_override("font_color", Color.DEEP_SKY_BLUE)
	container.add_child(title)

	# Runtime falls back to the parent node; mirroring it here keeps the editor ready.
	_ensure_default_target(collector)

	if not is_instance_valid(collector.target_node):
		var warning := Label.new()
		warning.text = "Adicione o AutoCollector como filho do objeto monitorado."
		warning.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		warning.add_theme_color_override("font_color", Color.ORANGE)
		container.add_child(warning)
		_add_toolbar(container, collector)
		add_custom_control(container)
		return

	var target_info := Label.new()
	target_info.text = "Alvo: %s" % collector.target_node.name
	target_info.tooltip_text = "No monitorado pelo AutoCollector"
	target_info.add_theme_color_override("font_color", Color.LIGHT_GREEN)
	container.add_child(target_info)

	if collector.monitored_flags.is_empty():
		collector._auto_populate_monitored_flags(collector.target_node)

	var filter_input := LineEdit.new()
	filter_input.placeholder_text = "Filtrar propriedades e metodos..."
	filter_input.text_changed.connect(_update_visibility)
	container.add_child(filter_input)
	_add_toolbar(container, collector)

	var properties: Array = []
	var methods: Array = []
	var signals: Array = []
	for flag_name in collector.monitored_flags.keys():
		if collector.is_signal_flag(flag_name):
			signals.append(flag_name)
		elif collector.is_method_flag(flag_name):
			methods.append(flag_name)
		else:
			properties.append(flag_name)
	properties.sort()
	methods.sort()
	signals.sort()

	_add_section(container, collector, "Propriedades", properties, "property")
	_add_section(container, collector, "Metodos", methods, "method")
	_add_section(container, collector, "Sinais", signals, "signal")
	add_custom_control(container)


func _add_toolbar(container: VBoxContainer, collector: AutoCollector) -> void:
	var toolbar := HBoxContainer.new()

	var check_all := Button.new()
	check_all.text = "Marcar tudo"
	check_all.pressed.connect(_toggle_all.bind(collector, true))
	toolbar.add_child(check_all)

	var uncheck_all := Button.new()
	uncheck_all.text = "Desmarcar tudo"
	uncheck_all.pressed.connect(_toggle_all.bind(collector, false))
	toolbar.add_child(uncheck_all)

	var refresh := Button.new()
	refresh.text = "Atualizar"
	refresh.pressed.connect(_refresh.bind(collector))
	toolbar.add_child(refresh)
	container.add_child(toolbar)


func _add_section(
	container: VBoxContainer,
	collector: AutoCollector,
	title: String,
	flag_names: Array,
	kind: String
) -> void:
	if flag_names.is_empty():
		return
	var heading := Label.new()
	heading.text = title
	heading.add_theme_color_override("font_color", Color.ORANGE)
	container.add_child(heading)

	for flag_name in flag_names:
		var check := CheckBox.new()
		var suffix := ""
		if kind == "property":
			var value = collector.target_node.get(flag_name)
			suffix = " = %s" % str(value)
		var display_name := str(flag_name).trim_prefix("signal::")
		check.text = "%s%s" % [display_name, suffix]
		match kind:
			"method":
				check.tooltip_text = "Metodo instrumentado por sinal gerado pelo Manager"
			"signal":
				check.tooltip_text = "Sinal declarado pelo script e monitorado em runtime"
			_:
				check.tooltip_text = "Propriedade verificada no intervalo configurado"
		check.button_pressed = collector.monitored_flags.get(flag_name, false)
		check.toggled.connect(_toggle_flag.bind(collector, str(flag_name)))
		container.add_child(check)
		checkboxes[flag_name] = check


func _toggle_flag(
	pressed: bool,
	collector: AutoCollector,
	flag_name: String
) -> void:
	collector.set_monitored_flag(flag_name, pressed)
	_mark_scene_changed()


func _toggle_all(collector: AutoCollector, value: bool) -> void:
	for flag_name in collector.monitored_flags.keys():
		collector.set_monitored_flag(flag_name, value)
		if checkboxes.has(flag_name):
			checkboxes[flag_name].set_pressed_no_signal(value)
	_mark_scene_changed()


func _refresh(collector: AutoCollector) -> void:
	_ensure_default_target(collector)
	if is_instance_valid(collector.target_node):
		collector._auto_populate_monitored_flags(collector.target_node)
		collector.notify_property_list_changed()
		_mark_scene_changed()


func _ensure_default_target(collector: AutoCollector) -> void:
	if is_instance_valid(collector.target_node):
		return
	var parent := collector.get_parent()
	if not is_instance_valid(parent):
		return
	collector.target_node = parent
	collector._auto_populate_monitored_flags(parent)
	collector.notify_property_list_changed()
	_mark_scene_changed()


func _update_visibility(text: String) -> void:
	var normalized := text.strip_edges().to_lower()
	for flag_name in checkboxes.keys():
		checkboxes[flag_name].visible = str(flag_name).to_lower().contains(normalized)


func _mark_scene_changed() -> void:
	EditorInterface.mark_scene_as_unsaved()
