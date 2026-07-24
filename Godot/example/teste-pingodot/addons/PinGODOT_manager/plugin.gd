@tool
extends EditorPlugin

# Registers the custom inspector and manager dock while the addon is enabled.
var inspector_plugin: EditorInspectorPlugin
var dock_panel: Control


func _enter_tree() -> void:
	inspector_plugin = preload(
		"res://addons/PinGODOT_manager/AutoCollectorInspector.gd"
	).new()
	add_inspector_plugin(inspector_plugin)

	dock_panel = preload(
		"res://addons/PinGODOT_manager/PinGManagerDock.gd"
	).new()
	dock_panel.name = "PinGODOT Manager"
	add_control_to_dock(DOCK_SLOT_RIGHT_UL, dock_panel)
	print("PinGODOT Manager carregado")


func _exit_tree() -> void:
	if inspector_plugin != null:
		remove_inspector_plugin(inspector_plugin)
	if dock_panel != null:
		remove_control_from_docks(dock_panel)
		dock_panel.queue_free()
	print("PinGODOT Manager descarregado")
