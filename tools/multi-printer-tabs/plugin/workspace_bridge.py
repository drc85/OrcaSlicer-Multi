# /// script
# requires-python = ">=3.12"
# dependencies = []
# [tool.orcaslicer.plugin]
# name = "Multi Printer Tabs Bridge"
# description = "Reports this workspace's printer and project name to the local Multi Printer Tabs window. No network or printer commands."
# author = "drc85"
# version = "0.1.0"
# ///
import json
import os
import time
import orca


class WorkspaceState(orca.script.ScriptPluginCapabilityBase):
    def get_name(self):
        return "Multi Printer Tabs State"

    def on_load(self):
        self._last = None
        self._publish()

    def execute(self):
        self._publish()
        return orca.ExecutionResult.success("Tab name updated")

    def on_lifecycle_event(self, event, ctx):
        # Host access must stay on Orca's UI thread. Some slicing events run
        # on workers, so observe only the project/preset events we need.
        watched = ("NewProject", "ProjectOpened", "ProjectAfterSave",
                   "ProjectClosed", "ProjectDirtyChanged", "PresetSelected",
                   "PresetSaved", "DeviceSelected", "ObjectAdded")
        if any(event == getattr(orca.LifecycleEvent, name, None) for name in watched):
            self._publish()

    def _publish(self):
        try:
            plater = orca.host.plater()
            preset = orca.host.preset_bundle().current_printer_preset()
            state = {"printer": preset.name,
                     "project": plater.project_path() or "",
                     "dirty": plater.is_project_dirty(), "pid": os.getpid()}
            if state == getattr(self, "_last", None):
                return
            folder = orca.host.plugin.storage()
            target = os.path.join(folder, "workspace-state.json")
            temporary = target + ".tmp"
            with open(temporary, "w", encoding="utf-8") as output:
                json.dump(dict(state, updated=time.time()), output, ensure_ascii=False)
            os.replace(temporary, target)
            self._last = state
        except Exception as error:
            # During on_load the plater may not exist yet. The next lifecycle
            # event retries. Never interfere with project operations.
            print("[multi-printer-tabs] state unavailable: " + type(error).__name__)


@orca.plugin
class WorkspaceBridge(orca.base):
    def register_capabilities(self):
        orca.register_capability(WorkspaceState)
