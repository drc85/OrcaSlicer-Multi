# OrcaSlicer Multi Printer Tabs

Use several complete OrcaSlicer workspaces in one window. Keep a Bambu printer, a Snapmaker U1 and other supported printers open at the same time, with a separate project and Device view in each tab.

**Windows x64 preview 0.9.** This is an independent companion application. It hosts separate OrcaSlicer processes in one window; it does not change Orca's C++ application or add a native tab feature to an upstream build. Building the inherited OrcaSlicer source alone does not build this companion.

## Download and install

Open [Releases](https://github.com/drc85/OrcaSlicer-Multi/releases) and download either:

- `OrcaMultiPrinterTabs-0.9.0-Setup.exe`: per-user installer with Start menu and optional desktop shortcut. No administrator rights required.
- `OrcaMultiPrinterTabs-0.9.0-win-x64.zip`: portable package. Extract the entire ZIP into a writable folder and run `OrcaSlicerMultiPrinterTabs.exe`. Keep its `plugin` folder and `.exe.config` beside it.

These downloads contain the companion and its own small tab-name bridge only. They do not contain an OrcaSlicer executable, third-party plugins, accounts, printer settings or projects. The binaries are unsigned; Windows may display its usual publisher warning. SHA-256 checksums are included with the release.

## Requirements / Nightly

- Windows 10/11 x64 and .NET Framework 4.8.
- A separately installed OrcaSlicer development/Nightly build with the Python plugin API used by `plugin/workspace_bridge.py`.
- Local testing used **OrcaSlicer 2.6.0-dev, H3C PR #17273, commit `1babccb8`**, including H3C and Snapmaker U1 workspace names. Newer Nightly compatibility is not guaranteed.

Get Orca from the [official OrcaSlicer releases](https://github.com/OrcaSlicer/OrcaSlicer/releases). This fork starts from upstream `main`, the development source used for Nightly work. The companion runs the `orca-slicer.exe` you choose: it does not download, replace or update Orca. H3C support comes from that Orca build, not from this window host.

## First start

1. Close Orca before copying its configuration, so the files are consistent. Keep a backup of your projects.
2. Start the companion and select your `orca-slicer.exe`.
3. Select your Orca data folder **containing `OrcaSlicer.conf`**. A normal Windows installation usually uses `%APPDATA%\OrcaSlicer`. A portable installation can use another folder.
4. The first tab receives a local copy of that folder. Each new tab receives its own copy of the same template. The original folder is not edited.
5. Select a printer and open a project using the embedded Orca's normal controls.

The release is clean. On your own computer, the configuration you select can include your own profiles, logins and plugins; those are copied locally into workspace folders. Nothing in this companion uploads those copies. Orca and any plugins you install retain their own network behavior.

Default workspace storage is `Documents\OrcaMulti\OT\<8-character-id>`. Short paths matter: some Python/plugin packages fail under deeply nested paths. `settings.json`, `session.json` and `host.log` are stored beside the companion. You can edit `WorkspaceRoot` in `settings.json` while the application is closed; do not place it inside the template folder. Moving existing workspaces requires moving their folders too.

## Controls

- The view selector at the far left shows **1, 2, 4 or 8** full workspaces. Layouts are single view, two side by side, 2×2, and 4×2.
- Workspace tabs show **selected printer · current project**. The active tab is highlighted. `*` follows Orca's unsaved-project indicator.
- **+**, directly after the last tab, creates another workspace. Open a different project or select another printer independently in each one.
- The top strip is dark red; bright red dividers separate the split views.
- Orca's own **X** closes that workspace. Its minimize/maximize buttons control the shared outer window.
- The outer **X** at the far right closes all workspaces and then exits. Orca asks about unsaved work itself; there is no second companion confirmation. Cancel in Orca keeps the session running. **+** also cancels a pending shutdown.
- Drag unused space on the top strip to move the window; double-click it to maximize/restore.

Tabs keep independent configuration folders and locks. Changes are not continuously synchronized between tabs or back into the original Orca installation. Background tabs keep running and can maintain printer connections. The host itself sends no print, movement or heating commands.

## Bambu printers: networking and filament management

For the Bambu workflow used with this setup, install these separately in your compatible Orca build before making new workspace copies:

- [Open Bamboo Networking Plugin — Orca Cloud](https://cloud.orcaslicer.com/p/51a2b22084da), with [networking library source](https://github.com/persano/open-bambu-networking-plugin). This provides the Open Bamboo integration used for Bambu connectivity here. Follow its installation and library-update instructions, then restart Orca and sign in locally if required.
- [Bambu Filament Manager — Orca Cloud](https://cloud.orcaslicer.com/app/plugins/plugin-hub/3c5fccb6-d04f-4ab6-9b99-5b3eb7ddacb7), for the associated filament/AMS workflow. Follow its own setup instructions. It is an additional filament-management tool, not a universal requirement for sending a print.

The networking plugin's name is **Open Bamboo**, which is different from Bambu Lab's separate **Bambu Connect** application. Neither third-party plugin nor any credentials are bundled in this repository or release. Their support, firmware compatibility and login requirements are controlled by their maintainers.

## Plugin and CAD compatibility

Workspace copying preserves installed Python packages, texture files and extracted wheel contents, including nested resource folders named `cache`. Only top-level application logs/cache/OTA/backup folders, Python bytecode caches and lock/log files are skipped. This prevents texture packages such as FilamentHub from losing their required assets during a copy.

CAD availability depends on the selected Orca build and its configuration. The companion does not install CAD components or force the experimental setting. If Orca requests a restart after a preference change, close and reopen that workspace normally. Changing the template does not change already created workspace copies.

## Limits and troubleshooting

- Each tab is a complete process with its own memory/GPU use. Start with two; eight can be demanding. Smaller grid cells may be cramped.
- Startup splash is disabled in workspace copies, and window hooks/cloaking reduce the separate-window flash. A brief flash can still occur because Orca creates its window before the companion embeds it.
- If a tab waits for startup, check the taskbar for Orca's own login, plugin, error or permission dialogs. The host does not automatically answer them.
- Printer-name updates need the included bridge and compatible plugin APIs. The project name also follows Orca's native title. If the name is stale, check the bridge is enabled in that workspace.
- Session data remembers workspace identities and layout, not complete in-memory project recovery. Save projects normally. There is no guaranteed recovery after a crash.
- Full printer commands, cameras, all modal dialogs, mixed-DPI/multi-monitor use and eight simultaneous printers have not been comprehensively tested. This is an experimental Windows-only companion, not an official OrcaSlicer feature.
- The wrapper does not support tab reordering, continuous profile sharing or automatic project reopening. A host crash can expose the separate Orca windows again.

## Build and tests

From `tools/multi-printer-tabs` in PowerShell:

```powershell
.\build.ps1
python .\tests\test_bridge.py
.\test-clone.ps1
.\package.ps1
```

The build uses the .NET Framework compiler included with Windows; no .NET SDK is needed. `package.ps1` builds the portable ZIP and checksums. To also build the installer, install [Inno Setup 6](https://jrsoftware.org/isinfo.php) and pass `-InnoCompiler 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'`.

The release workflow builds only this companion and publishes a prerelease from its exact source commit. It does not rebuild the full Orca C++ application. Offline bridge tests use a fake Orca API. The clone regression test checks required plugin assets survive and application logs are omitted; neither test reads real printer data. The common local window-host behavior was manually exercised; complete end-to-end printing tests remain outstanding.

## Privacy and removal

Only explicitly selected source/build/documentation files are published. Do not share `settings.json`, `session.json`, `host.log`, workspace folders or Orca data folders: they may contain local paths, login tokens, printer addresses and personal settings. Do not upload screenshots containing credentials when reporting an issue.

The companion's own bridge writes printer name, project path, dirty state and process ID into local plugin storage. It has no network access or printer-control commands. It is project source, not a copy of a user's installed third-party plugin.

Uninstall the installed companion through Windows Settings, or delete the extracted portable files after closing it. Workspace data under `Documents\OrcaMulti\OT` is deliberately retained; remove it yourself only when you no longer need its settings/projects. Your original Orca installation remains separate.

The companion source is provided under AGPL-3.0, matching this repository's [license](../../LICENSE.txt). OrcaSlicer and third-party plugins retain their respective licenses. This project has no official affiliation with OrcaSlicer, Bambu Lab or Snapmaker. AI-assisted development was used.
