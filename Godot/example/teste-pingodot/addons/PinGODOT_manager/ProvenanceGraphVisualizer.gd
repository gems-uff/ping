@tool
class_name ProvenanceGraphVisualizer
extends Control

# Interactive view of provenance data stored by PinGODOT Base.
# Grouping and temporal filtering affect presentation only.
const REPETITIVE_ACTIVITY_LABEL_THRESHOLD := 4
const GROUP_MIN_OCCURRENCES := 2

var zoom: float = 1.0
var pan_offset: Vector2 = Vector2.ZERO
var dragging: bool = false
var drag_start: Vector2 = Vector2.ZERO

# Visual IDs map aggregated elements back to their original provenance records.
var node_positions: Dictionary = {}
var _activity_label_counts: Dictionary = {}
var _display_vertices: Array[Vertex] = []
var _display_edges: Array[Edge] = []
var _display_vertex_by_id: Dictionary = {}
var _original_vertex_by_id: Dictionary = {}
var _original_edges_by_display_id: Dictionary = {}
var _group_members_by_id: Dictionary = {}
var _group_key_by_id: Dictionary = {}
var _member_to_display_id: Dictionary = {}
var _expanded_group_keys: Dictionary = {}
var _highlighted_node_ids: Dictionary = {}
var _highlighted_edge_ids: Dictionary = {}

var group_repeated_activities: bool = true
var time_cutoff: float = INF
var _time_min: float = 0.0
var _time_max: float = 0.0

var selected_node: String = ""
var hovered_node: String = ""

var _is_export_clone: bool = false

var tooltip_panel: PanelContainer
var tooltip_label: Label
var tooltip_visible: bool = false

var colors = {
	"Agent":    Color("#4A90E2"),
	"Activity": Color("#F5A623"),
	"Entity":   Color("#7ED321")
}

var edge_colors = {
	"WasAssociatedWith": Color("#4A90E2"),
	"WasGeneratedBy":    Color("#7ED321"),
	"Used":              Color("#F5A623"),
	"WasDerivedFrom":    Color("#9013FE"),
	"WasInformedBy":     Color("#FF6B6B"),
	"WasAttributedTo":   Color("#50E3C2"),
	"ActedOnBehalfOf":   Color("#D0021B"),
	"WasInfluencedBy":   Color("#B8E986")
}

func _ready():
	if _is_export_clone:
		_rebuild_display_graph()
		return

	mouse_filter = Control.MOUSE_FILTER_STOP
	custom_minimum_size = Vector2(1200, 800)

	_create_tooltip()

	await get_tree().process_frame
	_layout_graph()
	_fit_graph_to_view()


func _create_tooltip():
	if _is_export_clone:
		return

	tooltip_panel = PanelContainer.new()
	tooltip_panel.visible = false
	tooltip_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	tooltip_panel.z_index = 100
	tooltip_panel.custom_minimum_size = Vector2(280, 0)
	add_child(tooltip_panel)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.05, 0.08, 0.15, 0.97)
	style.border_color = Color(0.4, 0.5, 0.7, 0.6)
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_left = 8
	style.corner_radius_bottom_right = 8
	style.content_margin_left = 12
	style.content_margin_right = 12
	style.content_margin_top = 10
	style.content_margin_bottom = 10
	style.shadow_size = 4
	style.shadow_color = Color(0, 0, 0, 0.5)
	tooltip_panel.add_theme_stylebox_override("panel", style)

	tooltip_label = Label.new()
	tooltip_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	tooltip_label.custom_minimum_size = Vector2(250, 0)
	tooltip_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	tooltip_label.add_theme_font_size_override("font_size", 12)
	tooltip_label.add_theme_color_override("font_color", Color.WHITE)
	tooltip_panel.add_child(tooltip_label)


func _draw():
	var center = size / 2

	_draw_background(center)
	_draw_grid(center)
	_rebuild_selection_neighborhood()

	for edge in _display_edges:
		_draw_edge(edge, center)

	for vertex in _display_vertices:
		_draw_vertex(vertex, center)

	if not _is_export_clone:
		_draw_info()
	_draw_legend()


func _draw_background(_center: Vector2):
	draw_rect(Rect2(Vector2.ZERO, size), Color("#0F1419"))


func _draw_grid(_center: Vector2):
	var grid_size = 60 * zoom
	var grid_color = Color(0.15, 0.18, 0.25, 0.4)

	var start_x = int(-_center.x / grid_size) * grid_size
	var start_y = int(-_center.y / grid_size) * grid_size

	for x in range(int(start_x), int(size.x), int(grid_size)):
		draw_line(
			Vector2(x, 0) + pan_offset * zoom,
			Vector2(x, size.y) + pan_offset * zoom,
			grid_color, 0.5
		)

	for y in range(int(start_y), int(size.y), int(grid_size)):
		draw_line(
			Vector2(0, y) + pan_offset * zoom,
			Vector2(size.x, y) + pan_offset * zoom,
			grid_color, 0.5
		)


func _draw_edge(edge: Edge, center: Vector2):
	if not node_positions.has(edge.source_id) or not node_positions.has(edge.target_id):
		return
	if not _is_display_id_time_visible(edge.source_id):
		return
	if not _is_display_id_time_visible(edge.target_id):
		return
	if not _is_display_edge_time_visible(edge):
		return

	var from_pos: Vector2 = _world_to_screen(node_positions[edge.source_id], center)
	var to_pos: Vector2 = _world_to_screen(node_positions[edge.target_id], center)
	var is_highlighted: bool = (
		selected_node == "" or _highlighted_edge_ids.has(edge.id)
	)
	var opacity: float = 0.75 if is_highlighted else 0.08
	var thickness: float = 2.5 if is_highlighted else 1.0

	var color: Color = edge_colors.get(edge.relation, Color("#AAAAAA"))

	draw_line(
		from_pos + Vector2(2, 2) * zoom,
		to_pos + Vector2(2, 2) * zoom,
		Color(0, 0, 0, 0.3 if is_highlighted else 0.06),
		(thickness + 1.0) * zoom
	)

	color.a = opacity
	draw_line(from_pos, to_pos, color, thickness * zoom)

	var direction  = (to_pos - from_pos).normalized()
	var arrow_size = 12 * zoom
	var arrow_angle = PI / 6
	var arrow_pos   = to_pos - direction * 30 * zoom
	var arrow_left  = arrow_pos - direction.rotated(-arrow_angle) * arrow_size
	var arrow_right = arrow_pos - direction.rotated(arrow_angle)  * arrow_size
	if is_highlighted:
		draw_colored_polygon(
			PackedVector2Array([arrow_pos, arrow_left, arrow_right]),
			color
		)

	# Edge labels are shown only at a readable zoom to limit visual clutter.
	if not _is_export_clone and zoom >= 0.85 and is_highlighted:
		var label_pos: Vector2 = (from_pos + to_pos) / 2.0
		var font: Font = ThemeDB.fallback_font
		draw_string(
			font,
			label_pos,
			edge.relation,
			HORIZONTAL_ALIGNMENT_CENTER,
			-1,
			max(8, int(11 * zoom)),
			Color.WHITE
		)


func _draw_vertex(vertex: Vertex, center: Vector2):
	if not node_positions.has(vertex.id):
		return
	if not _is_vertex_time_visible(vertex):
		return

	var pos: Vector2 = _world_to_screen(node_positions[vertex.id], center)
	var radius: float = 28.0 * zoom
	var color: Color = colors.get(vertex.type, Color("#7ED321"))
	var is_selected: bool = vertex.id == selected_node
	var is_hovered: bool = vertex.id == hovered_node
	var is_highlighted: bool = (
		selected_node == "" or _highlighted_node_ids.has(vertex.id)
	)
	if not is_highlighted:
		color.a = 0.14

	draw_circle(pos + Vector2(2, 3) * zoom, radius + 4 * zoom, Color(0, 0, 0, 0.4))

	if is_selected:
		draw_circle(pos, radius + 12 * zoom, Color(1, 1, 0, 0.15))
		draw_arc(pos, radius + 10 * zoom, 0, TAU, 48, Color(1, 1, 0, 0.6), 2.0 * zoom)
	elif is_hovered:
		draw_circle(pos, radius + 8 * zoom, Color(1, 1, 1, 0.1))
		draw_arc(pos, radius + 6 * zoom, 0, TAU, 48, Color(1, 1, 1, 0.4), 1.5 * zoom)

	draw_circle(pos, radius, color)
	draw_arc(pos, radius, 0, TAU, 48, color.lightened(0.3), 2.5 * zoom)
	draw_circle(pos, radius * 0.3, color.darkened(0.4))

	if _should_draw_vertex_label(vertex, is_selected, is_hovered):
		var font: Font = ThemeDB.fallback_font
		var font_size: int = max(8, int(13 * zoom))
		var label_pos: Vector2 = Vector2(pos.x, pos.y - radius - 12 * zoom)
		var label_color: Color = Color.WHITE
		if not is_highlighted:
			label_color.a = 0.12
		draw_string(
			font,
			label_pos,
			_display_vertex_label(vertex),
			HORIZONTAL_ALIGNMENT_CENTER,
			-1,
			font_size,
			label_color
		)


func _should_draw_vertex_label(
	vertex: Vertex,
	is_selected: bool,
	is_hovered: bool
) -> bool:
	if selected_node != "" and not _highlighted_node_ids.has(vertex.id):
		return false
	if _group_members_by_id.has(vertex.id):
		return true
	if vertex.type != "Activity" or is_selected or is_hovered:
		return true
	var group_key: String = _activity_label_group_key(vertex.label)
	var is_repetitive: bool = (
		int(_activity_label_counts.get(group_key, 0))
		>= REPETITIVE_ACTIVITY_LABEL_THRESHOLD
	)
	if _is_export_clone:
		return not is_repetitive
	if zoom >= 0.75:
		return true
	return zoom >= 0.55 and not is_repetitive


func _rebuild_activity_label_counts() -> void:
	_activity_label_counts.clear()
	for vertex in ProvenanceController.vertex_list:
		if vertex.type != "Activity":
			continue
		var group_key: String = _activity_label_group_key(vertex.label)
		_activity_label_counts[group_key] = (
			int(_activity_label_counts.get(group_key, 0)) + 1
		)


func _activity_label_group_key(label: String) -> String:
	# Event suffixes are normalized so repeated observations share one key.
	for suffix in ["_changed", "_invoked", "_emitted", "_input"]:
		if label.ends_with(suffix):
			return label.trim_suffix(suffix)
	return label


func _rebuild_display_graph() -> void:
	# The display graph is rebuilt without modifying the imported source data.
	_display_vertices.clear()
	_display_edges.clear()
	_display_vertex_by_id.clear()
	_original_vertex_by_id.clear()
	_original_edges_by_display_id.clear()
	_group_members_by_id.clear()
	_group_key_by_id.clear()
	_member_to_display_id.clear()
	_rebuild_activity_label_counts()
	_rebuild_time_range()

	var activity_groups: Dictionary = {}
	for vertex in ProvenanceController.vertex_list:
		_original_vertex_by_id[vertex.id] = vertex
		if vertex.type != "Activity":
			_add_display_vertex(vertex)
			_member_to_display_id[vertex.id] = vertex.id
			continue
		var group_key: String = _activity_label_group_key(vertex.label)
		if not activity_groups.has(group_key):
			activity_groups[group_key] = []
		activity_groups[group_key].append(vertex)

	var sorted_group_keys: Array = activity_groups.keys()
	sorted_group_keys.sort()
	for group_key_variant in sorted_group_keys:
		var group_key: String = str(group_key_variant)
		var members: Array = activity_groups[group_key]
		members.sort_custom(
			func(a, b): return a.date.to_float() < b.date.to_float()
		)
		var should_group: bool = (
			group_repeated_activities
			and members.size() >= GROUP_MIN_OCCURRENCES
			and not _expanded_group_keys.has(group_key)
		)
		if should_group:
			var first_member: Vertex = members[0]
			var group_id: String = "__activity_group__" + group_key
			var group_vertex := Vertex.new(
				group_id,
				first_member.date,
				"Activity",
				group_key,
				[]
			)
			_add_display_vertex(group_vertex)
			_group_members_by_id[group_id] = members
			_group_key_by_id[group_id] = group_key
			for member in members:
				_member_to_display_id[member.id] = group_id
		else:
			for member in members:
				_add_display_vertex(member)
				_member_to_display_id[member.id] = member.id
				_group_key_by_id[member.id] = group_key

	var visual_edge_keys: Dictionary = {}
	var visual_edge_index: int = 0
	# Parallel visual edges are collapsed while their source edges are retained.
	for original_edge in ProvenanceController.edge_list:
		if not _member_to_display_id.has(original_edge.source_id):
			continue
		if not _member_to_display_id.has(original_edge.target_id):
			continue
		var display_source: String = _member_to_display_id[original_edge.source_id]
		var display_target: String = _member_to_display_id[original_edge.target_id]
		if display_source == display_target:
			continue
		var edge_key: String = "%s|%s|%s" % [
			display_source,
			original_edge.relation,
			display_target
		]
		if visual_edge_keys.has(edge_key):
			var existing_edge_id: String = visual_edge_keys[edge_key]
			_original_edges_by_display_id[existing_edge_id].append(original_edge)
			continue
		visual_edge_index += 1
		var display_edge := Edge.new(
			"__visual_edge_%d" % visual_edge_index,
			original_edge.label,
			original_edge.relation,
			original_edge.value,
			display_source,
			display_target
		)
		_display_edges.append(display_edge)
		visual_edge_keys[edge_key] = display_edge.id
		_original_edges_by_display_id[display_edge.id] = [original_edge]


func _add_display_vertex(vertex: Vertex) -> void:
	_display_vertices.append(vertex)
	_display_vertex_by_id[vertex.id] = vertex


func set_group_repeated_activities(enabled: bool) -> void:
	group_repeated_activities = enabled
	_expanded_group_keys.clear()
	selected_node = ""
	hovered_node = ""
	_hide_tooltip()
	_layout_graph()
	_fit_graph_to_view()


func _expand_activity_group(group_id: String) -> void:
	if not _group_key_by_id.has(group_id):
		return
	var group_key: String = _group_key_by_id[group_id]
	_expanded_group_keys[group_key] = true
	selected_node = ""
	hovered_node = ""
	_hide_tooltip()
	_layout_graph()
	_fit_graph_to_view()


func _display_vertex_label(vertex: Vertex) -> String:
	if not _group_members_by_id.has(vertex.id):
		return vertex.label
	var visible_count: int = _visible_group_members(vertex.id).size()
	return "%s ×%d" % [_group_key_by_id[vertex.id], visible_count]


func _rebuild_time_range() -> void:
	_time_min = 0.0
	_time_max = 0.0
	for vertex in ProvenanceController.vertex_list:
		if vertex.type == "Agent":
			continue
		_time_max = max(_time_max, vertex.date.to_float())
	if is_inf(time_cutoff) or time_cutoff > _time_max:
		time_cutoff = _time_max


func get_time_range() -> Vector2:
	_rebuild_time_range()
	return Vector2(_time_min, _time_max)


func set_time_cutoff(value: float) -> void:
	time_cutoff = clamp(value, _time_min, _time_max)
	if (
		selected_node != ""
		and not _is_display_id_time_visible(selected_node)
	):
		selected_node = ""
	hovered_node = ""
	_hide_tooltip()
	queue_redraw()


func _is_vertex_time_visible(vertex: Vertex) -> bool:
	# Agents remain visible as context while timestamped vertices follow the scrubber.
	if vertex.type == "Agent":
		return true
	if _group_members_by_id.has(vertex.id):
		return not _visible_group_members(vertex.id).is_empty()
	return vertex.date.to_float() <= time_cutoff + 0.0001


func _is_display_id_time_visible(display_id: String) -> bool:
	if not _display_vertex_by_id.has(display_id):
		return false
	return _is_vertex_time_visible(_display_vertex_by_id[display_id])


func _is_display_edge_time_visible(edge: Edge) -> bool:
	if not _original_edges_by_display_id.has(edge.id):
		return false
	for original_edge in _original_edges_by_display_id[edge.id]:
		if not _original_vertex_by_id.has(original_edge.source_id):
			continue
		if not _original_vertex_by_id.has(original_edge.target_id):
			continue
		var source_vertex: Vertex = _original_vertex_by_id[original_edge.source_id]
		var target_vertex: Vertex = _original_vertex_by_id[original_edge.target_id]
		if (
			_is_original_vertex_time_visible(source_vertex)
			and _is_original_vertex_time_visible(target_vertex)
		):
			return true
	return false


func _is_original_vertex_time_visible(vertex: Vertex) -> bool:
	return (
		vertex.type == "Agent"
		or vertex.date.to_float() <= time_cutoff + 0.0001
	)


func _visible_group_members(group_id: String) -> Array:
	var visible_members: Array = []
	if not _group_members_by_id.has(group_id):
		return visible_members
	for member in _group_members_by_id[group_id]:
		if member.date.to_float() <= time_cutoff + 0.0001:
			visible_members.append(member)
	return visible_members


func _rebuild_selection_neighborhood() -> void:
	# Selection highlights the vertex and its direct provenance neighborhood.
	_highlighted_node_ids.clear()
	_highlighted_edge_ids.clear()
	if selected_node == "" or not _is_display_id_time_visible(selected_node):
		return
	_highlighted_node_ids[selected_node] = true
	for edge in _display_edges:
		if not _is_display_edge_time_visible(edge):
			continue
		if edge.source_id == selected_node or edge.target_id == selected_node:
			_highlighted_node_ids[edge.source_id] = true
			_highlighted_node_ids[edge.target_id] = true
			_highlighted_edge_ids[edge.id] = true


func _draw_info():
	var visible_vertex_count: int = 0
	var visible_edge_count: int = 0
	for vertex in _display_vertices:
		if _is_vertex_time_visible(vertex):
			visible_vertex_count += 1
	for edge in _display_edges:
		if _is_display_edge_time_visible(edge):
			visible_edge_count += 1
	var info_text = "Visiveis: %d vertices / %d arestas  |  Tempo: %.2fs  |  Zoom: %.0f%%" % [
		visible_vertex_count,
		visible_edge_count,
		time_cutoff,
		zoom * 100.0
	]

	if selected_node != "":
		var vertex = _get_vertex_by_id(selected_node)
		if vertex:
			info_text += "  |  %s (%s)" % [
				_display_vertex_label(vertex),
				vertex.type
			]

	var font      = ThemeDB.fallback_font
	var text_size = font.get_string_size(info_text, HORIZONTAL_ALIGNMENT_RIGHT, -1, 13)
	var info_bg   = Rect2(Vector2(size.x - text_size.x - 30, 10), Vector2(text_size.x + 20, 30))
	draw_rect(info_bg, Color(0.05, 0.08, 0.15, 0.9), true)
	draw_rect(info_bg, Color(0.4, 0.5, 0.7, 0.4), false, 1.0)
	draw_string(font, Vector2(size.x - text_size.x - 20, 25), info_text, HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("#B8E986"))


func _draw_legend():
	# Relation types are omitted so the legend remains compact for large graphs.
	var legend_x: float = 20.0
	var legend_height: float = 105.0
	var bottom_margin: float = 0.0 if _is_export_clone else 65.0
	var legend_y: float = size.y - legend_height - bottom_margin + 20.0

	var legend_bg := Rect2(
		Vector2(legend_x - 10.0, legend_y - 10.0),
		Vector2(180.0, legend_height)
	)
	draw_rect(legend_bg, Color(0.05, 0.08, 0.15, 0.95), true)
	draw_rect(legend_bg, Color(0.4, 0.5, 0.7, 0.3), false, 1.0)

	var font := ThemeDB.fallback_font
	draw_string(
		font,
		Vector2(legend_x, legend_y),
		"TIPOS DE VERTICES",
		HORIZONTAL_ALIGNMENT_LEFT,
		-1,
		12,
		Color("#B8E986")
	)

	var vertex_types: Array[String] = ["Agent", "Activity", "Entity"]
	var y_offset: float = legend_y + 20.0
	for vertex_type in vertex_types:
		var vertex_color: Color = colors.get(vertex_type, Color.WHITE)
		var circle_position := Vector2(legend_x + 8.0, y_offset + 4.0)
		draw_circle(circle_position, 6.0, vertex_color)
		draw_arc(
			circle_position,
			6.0,
			0.0,
			TAU,
			24,
			vertex_color.lightened(0.3),
			1.0
		)
		draw_string(
			font,
			Vector2(circle_position.x + 15.0, y_offset + 8.0),
			vertex_type,
			HORIZONTAL_ALIGNMENT_LEFT,
			-1,
			11,
			Color.WHITE
		)
		y_offset += 22.0


func _gui_input(event):
	if _is_export_clone:
		return

	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				var clicked_node: String = _get_node_at_position(event.position)
				if clicked_node != "":
					if (
						clicked_node == selected_node
						and _group_members_by_id.has(clicked_node)
					):
						_expand_activity_group(clicked_node)
					else:
						selected_node = clicked_node
						_show_node_info(selected_node)
				else:
					selected_node = ""
					_hide_tooltip()
					dragging = true
					drag_start = event.position
			else:
				dragging = false
			queue_redraw()

		elif event.button_index == MOUSE_BUTTON_WHEEL_UP:
			zoom = min(zoom * 1.15, 3.0)
			queue_redraw()
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			zoom = max(zoom / 1.15, 0.05)
			queue_redraw()

	elif event is InputEventMouseMotion:
		if dragging:
			pan_offset += (event.position - drag_start) / zoom
			drag_start  = event.position
			queue_redraw()
		else:
			var prev_hovered = hovered_node
			hovered_node = _get_node_at_position(event.position)

			if prev_hovered != hovered_node:
				if hovered_node != "":
					_show_node_tooltip(hovered_node, event.position)
				else:
					_hide_tooltip()
				queue_redraw()
			elif hovered_node != "":
				_move_tooltip(event.position)


func _get_node_at_position(pos: Vector2) -> String:
	var center: Vector2 = size / 2.0
	var radius: float = 28.0 * zoom

	for vertex in _display_vertices:
		if not _is_vertex_time_visible(vertex):
			continue
		if not node_positions.has(vertex.id):
			continue
		var node_pos: Vector2 = _world_to_screen(
			node_positions[vertex.id],
			center
		)
		if pos.distance_to(node_pos) < radius + 5.0 * zoom:
			return vertex.id

	return ""


func _world_to_screen(world_pos: Vector2, center: Vector2) -> Vector2:
	return center + (world_pos + pan_offset) * zoom


func _get_vertex_by_id(id: String) -> Vertex:
	return _display_vertex_by_id.get(id, null)


func _layout_graph() -> void:
	# Agents occupy the center, entities the inner ring, and activities outer rings.
	node_positions.clear()
	_rebuild_display_graph()
	if _display_vertices.is_empty():
		return

	var agents: Array = []
	var entities: Array = []
	var activities: Array = []
	for vertex in _display_vertices:
		match vertex.type:
			"Agent":
				agents.append(vertex)
			"Entity":
				entities.append(vertex)
			_:
				activities.append(vertex)

	agents.sort_custom(func(a, b): return a.label < b.label)
	entities.sort_custom(func(a, b): return a.label < b.label)
	activities.sort_custom(func(a, b): return a.date.to_float() < b.date.to_float())

	_place_agents(agents)
	_place_ring(entities, 220.0, -PI / 2.0)
	_place_activity_rings(activities)
	_normalize_positions()


func _place_agents(agents: Array) -> void:
	if agents.is_empty():
		return
	if agents.size() == 1:
		node_positions[agents[0].id] = Vector2.ZERO
		return
	if agents.size() == 2:
		node_positions[agents[0].id] = Vector2(-110.0, 0.0)
		node_positions[agents[1].id] = Vector2(110.0, 0.0)
		return
	_place_ring(agents, 120.0, -PI / 2.0)


func _place_ring(vertices: Array, radius: float, start_angle: float) -> void:
	if vertices.is_empty():
		return
	for index in range(vertices.size()):
		var angle: float = start_angle + TAU * float(index) / float(vertices.size())
		node_positions[vertices[index].id] = Vector2(
			cos(angle) * radius,
			sin(angle) * radius * 0.68
		)


func _place_activity_rings(activities: Array) -> void:
	var remaining_index: int = 0
	var ring_index: int = 0
	var radius: float = 380.0
	var minimum_arc_spacing: float = 185.0
	while remaining_index < activities.size():
		var capacity: int = max(
			8,
			int(floor(TAU * radius / minimum_arc_spacing))
		)
		var count: int = min(capacity, activities.size() - remaining_index)
		var angle_offset: float = -PI / 2.0
		if ring_index % 2 == 1:
			angle_offset += PI / float(max(count, 1))
		for local_index in range(count):
			var activity: Vertex = activities[remaining_index + local_index]
			var angle: float = (
				angle_offset + TAU * float(local_index) / float(count)
			)
			node_positions[activity.id] = Vector2(
				cos(angle) * radius,
				sin(angle) * radius * 0.68
			)
		remaining_index += count
		ring_index += 1
		radius += 190.0


func _normalize_positions() -> void:
	if node_positions.is_empty():
		return
	var center_of_mass: Vector2 = Vector2.ZERO
	var valid_count: int = 0
	for raw_position in node_positions.values():
		var position: Vector2 = raw_position
		if position.is_finite():
			center_of_mass += position
			valid_count += 1
	if valid_count == 0:
		return
	center_of_mass /= float(valid_count)
	for vertex_id in node_positions.keys():
		var position: Vector2 = node_positions[vertex_id]
		node_positions[vertex_id] = position - center_of_mass


func _show_node_info(vertex_id: String):
	var vertex: Vertex = _get_vertex_by_id(vertex_id)
	if not vertex:
		return
	print("\n" + _format_vertex_info(vertex))


func _show_node_tooltip(vertex_id: String, mouse_pos: Vector2):
	if _is_export_clone or not tooltip_panel:
		return

	var vertex = _get_vertex_by_id(vertex_id)
	if not vertex:
		_hide_tooltip()
		return

	tooltip_label.text = _format_vertex_info(vertex)
	await get_tree().process_frame
	tooltip_panel.visible = true
	tooltip_visible = true
	_move_tooltip(mouse_pos)


func _move_tooltip(mouse_pos: Vector2):
	if not tooltip_visible or not tooltip_panel:
		return

	var offset := Vector2(16, 16)
	var pos    := mouse_pos + offset

	tooltip_panel.size = tooltip_panel.get_minimum_size()
	var max_x = size.x - tooltip_panel.size.x - 6
	var max_y = size.y - tooltip_panel.size.y - 6

	pos.x = clamp(pos.x, 6.0, max_x)
	pos.y = clamp(pos.y, 6.0, max_y)

	tooltip_panel.position = pos


func _hide_tooltip():
	if tooltip_panel:
		tooltip_panel.visible = false
	tooltip_visible = false


func _format_vertex_info(vertex: Vertex) -> String:
	if _group_members_by_id.has(vertex.id):
		return _format_activity_group_info(vertex)
	var lines = []
	lines.append("________________________")
	lines.append(_display_vertex_label(vertex))
	lines.append("________________________")
	lines.append("")
	lines.append("Tipo: " + vertex.type)
	lines.append("ID: "   + vertex.id)
	lines.append("Data: " + vertex.date)

	if vertex.attributes.size() > 0:
		lines.append("")
		lines.append("Atributos:")
		for attr in vertex.attributes:
			lines.append("  " + attr.name + ": " + str(attr.value))
	else:
		lines.append("")
		lines.append("(Sem atributos)")

	return "\n".join(lines)


func _format_activity_group_info(vertex: Vertex) -> String:
	var visible_members: Array = _visible_group_members(vertex.id)
	var all_members: Array = _group_members_by_id[vertex.id]
	var lines: Array[String] = []
	lines.append("________________________")
	lines.append(_display_vertex_label(vertex))
	lines.append("________________________")
	lines.append("")
	lines.append("Tipo: Activity agrupada")
	lines.append(
		"Quantidade visivel: %d de %d" % [
			visible_members.size(),
			all_members.size()
		]
	)
	if not visible_members.is_empty():
		var first_member: Vertex = visible_members[0]
		var last_member: Vertex = visible_members[visible_members.size() - 1]
		lines.append(
			"Intervalo: %s s ate %s s" % [
				first_member.date,
				last_member.date
			]
		)
		for field_name in ["property", "method", "signal", "event_type", "type"]:
			var field_value: String = _attribute_value(first_member, field_name)
			if field_value != "":
				lines.append("%s: %s" % [field_name.capitalize(), field_value])
		var initial_value: String = _attribute_value(first_member, "old_value")
		var final_value: String = _attribute_value(last_member, "new_value")
		if initial_value != "" or final_value != "":
			lines.append("Valor inicial: %s" % initial_value)
			lines.append("Valor final: %s" % final_value)
	lines.append("")
	lines.append("Clique novamente para expandir as ocorrencias.")
	return "\n".join(lines)


func _attribute_value(vertex: Vertex, attribute_name: String) -> String:
	for attribute in vertex.attributes:
		if attribute.name == attribute_name:
			return str(attribute.value)
	return ""


func refresh() -> void:
	selected_node = ""
	hovered_node = ""
	_hide_tooltip()
	_layout_graph()
	_fit_graph_to_view()


func fit_to_view() -> void:
	_fit_graph_to_view()


func _fit_graph_to_view() -> void:
	var bounds: Rect2 = _graph_bounds()
	if bounds.size == Vector2.ZERO:
		return

	var graph_center: Vector2 = bounds.position + bounds.size / 2.0
	var available: Vector2 = (size - Vector2(160.0, 160.0)).max(Vector2(100.0, 100.0))
	var fit_x: float = available.x / max(bounds.size.x + 80.0, 1.0)
	var fit_y: float = available.y / max(bounds.size.y + 80.0, 1.0)
	zoom = clamp(min(fit_x, fit_y), 0.05, 2.0)
	pan_offset = -graph_center
	queue_redraw()


func _graph_bounds() -> Rect2:
	if node_positions.is_empty():
		return Rect2()
	var min_pos: Vector2 = Vector2(INF, INF)
	var max_pos: Vector2 = Vector2(-INF, -INF)
	var found_valid: bool = false
	for vertex_id in node_positions.keys():
		if not _is_display_id_time_visible(str(vertex_id)):
			continue
		var position: Vector2 = node_positions[vertex_id]
		if not position.is_finite():
			continue
		found_valid = true
		min_pos.x = min(min_pos.x, position.x)
		min_pos.y = min(min_pos.y, position.y)
		max_pos.x = max(max_pos.x, position.x)
		max_pos.y = max(max_pos.y, position.y)
	return Rect2(min_pos, max_pos - min_pos) if found_valid else Rect2()


func export_graph_image(filepath: String = "user://provenance_graph.png") -> void:
	if node_positions.size() == 0:
		print("Nenhum no para exportar.")
		return

	var bounds: Rect2 = _graph_bounds()
	if bounds.size == Vector2.ZERO:
		push_error("O layout do grafo nao possui coordenadas validas.")
		return

	var padding: float = 90.0
	var export_size: Vector2 = Vector2(3840.0, 2160.0)
	var available: Vector2 = export_size - Vector2(padding, padding) * 2.0
	var export_zoom: float = min(
		available.x / max(bounds.size.x + 80.0, 1.0),
		available.y / max(bounds.size.y + 80.0, 1.0)
	)
	export_zoom = clamp(export_zoom, 0.05, 2.0)

	var sv := SubViewport.new()
	sv.size = Vector2i(int(export_size.x), int(export_size.y))
	sv.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	sv.render_target_clear_mode = SubViewport.CLEAR_MODE_ALWAYS
	sv.transparent_bg = false
	sv.disable_3d = true
	get_tree().root.add_child(sv)

	var clone := ProvenanceGraphVisualizer.new()
	# A detached clone excludes editor interaction state from the exported image.
	clone._is_export_clone = true
	clone.group_repeated_activities = group_repeated_activities
	clone._expanded_group_keys = _expanded_group_keys.duplicate(true)
	clone.time_cutoff = time_cutoff
	clone.node_positions = node_positions.duplicate(true)
	clone.zoom = export_zoom
	clone.pan_offset = -(bounds.position + bounds.size / 2.0)
	clone.selected_node = ""
	clone.hovered_node = ""

	sv.add_child(clone)

	clone.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	clone.custom_minimum_size = export_size
	clone.size = export_size
	clone.queue_redraw()

	for i in range(6):
		await get_tree().process_frame

	sv.render_target_update_mode = SubViewport.UPDATE_ONCE
	await get_tree().process_frame

	var img := sv.get_texture().get_image()

	if img == null or img.is_empty():
		print("ERRO: SubViewport nao renderizou.")
		sv.queue_free()
		return

	var err := img.save_png(filepath)
	if err == OK:
		print("Exportado: %s  (%dx%d px)" % [filepath, img.get_width(), img.get_height()])
	else:
		print("ERRO ao salvar: %d" % err)

	sv.queue_free()
