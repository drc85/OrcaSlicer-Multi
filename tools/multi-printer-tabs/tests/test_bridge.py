"""Offline behavior tests; never import Orca or access real printer settings."""
import importlib.util
import json
import pathlib
import sys
import tempfile
import types
import unittest

class BridgeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.printer = "Snapmaker U1 PAXX"
        self.project = "C:/Projects/Halter.3mf"
        self.dirty = False
        self.ready = True
        events = ("NewProject", "ProjectOpened", "ProjectAfterSave", "ProjectClosed",
                  "ProjectDirtyChanged", "PresetSelected", "PresetSaved", "DeviceSelected", "ObjectAdded")
        class Capability: pass
        def plater():
            if not self.ready: raise RuntimeError("not ready")
            return types.SimpleNamespace(project_path=lambda:self.project, is_project_dirty=lambda:self.dirty)
        host = types.SimpleNamespace(plater=plater,
            preset_bundle=lambda:types.SimpleNamespace(current_printer_preset=lambda:types.SimpleNamespace(name=self.printer)),
            plugin=types.SimpleNamespace(storage=lambda:self.temp.name))
        self.old = sys.modules.get("orca")
        sys.modules["orca"] = types.SimpleNamespace(script=types.SimpleNamespace(ScriptPluginCapabilityBase=Capability),
            base=object, plugin=lambda cls:cls, host=host,
            LifecycleEvent=types.SimpleNamespace(**{name:name for name in events}),
            ExecutionResult=types.SimpleNamespace(success=lambda message:message))
        path=pathlib.Path(__file__).parents[1]/"plugin"/"workspace_bridge.py"
        spec=importlib.util.spec_from_file_location("test_workspace_bridge",path)
        self.module=importlib.util.module_from_spec(spec); spec.loader.exec_module(self.module)
        self.bridge=self.module.WorkspaceState()

    def tearDown(self):
        if self.old is None: sys.modules.pop("orca",None)
        else: sys.modules["orca"]=self.old
        self.temp.cleanup()

    def read(self):
        return json.loads((pathlib.Path(self.temp.name)/"workspace-state.json").read_text(encoding="utf-8"))

    def test_independent_printer_and_project_changes(self):
        self.bridge.on_load()
        self.assertEqual(self.read()["printer"],"Snapmaker U1 PAXX")
        self.printer="Bambu Lab H3C 0.4 nozzle"
        self.project="C:/Projects/Gehäuse.3mf"; self.dirty=True
        self.bridge.on_lifecycle_event("PresetSelected",None)
        self.assertEqual(self.read()["printer"],self.printer)
        self.assertEqual(self.read()["project"],self.project)
        self.assertTrue(self.read()["dirty"])

    def test_startup_retry_and_new_project(self):
        self.ready=False; self.bridge.on_load()
        self.assertFalse((pathlib.Path(self.temp.name)/"workspace-state.json").exists())
        self.ready=True; self.project=""
        self.bridge.on_lifecycle_event("NewProject",None)
        self.assertEqual(self.read()["project"],"")

    def test_slicing_worker_events_do_not_access_gui(self):
        self.bridge.on_load()
        before=self.read(); self.printer="Should not read during slicing"
        self.bridge.on_lifecycle_event("SliceStarted",None)
        self.assertEqual(self.read(),before)

if __name__=="__main__": unittest.main()
