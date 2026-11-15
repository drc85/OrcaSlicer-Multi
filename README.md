# OrcaSlicer Multi Printer Tabs

Use several complete OrcaSlicer workspaces in one window. Keep a Bambu printer, a Snapmaker U1 and other supported printers open at the same time, with a separate project and Device view in each tab.

**Windows x64 preview 0.16.0.** This is an independent companion application. It hosts separate OrcaSlicer processes in one window; it does not change Orca's C++ application or add a native tab feature to an upstream build. Building the inherited OrcaSlicer source alone does not build this companion.

## Download and install

**[Download Windows installer](https://github.com/drc85/OrcaSlicer-Multi/releases/download/multi-tabs-v0.16.0/OrcaMultiPrinterTabs-0.16.0-Setup.exe)** · **[Download portable ZIP](https://github.com/drc85/OrcaSlicer-Multi/releases/download/multi-tabs-v0.16.0/OrcaMultiPrinterTabs-0.16.0-win-x64.zip)**

Open [Releases](https://github.com/drc85/OrcaSlicer-Multi/releases) and download either:

- `OrcaMultiPrinterTabs-0.16.0-Setup.exe`: per-user installer with Start menu and optional desktop shortcut. No administrator rights required.
- `OrcaMultiPrinterTabs-0.16.0-win-x64.zip`: portable package. Extract the entire ZIP into a writable folder and run `OrcaSlicerMultiPrinterTabs.exe`. Keep its `plugin` folder and `.exe.config` beside it.

These downloads contain the companion and its own small tab-name bridge only. They do not contain an OrcaSlicer executable, third-party plugins, accounts, printer settings or projects. The binaries are unsigned; Windows may display its usual publisher warning. SHA-256 checksums are included with the release.

## Requirements / Nightly

- Windows 10/11 x64, .NET Framework 4.8 and the Microsoft Edge WebView2 Evergreen Runtime (normally present on Windows 11).
- A separately installed OrcaSlicer development/Nightly build with the Python plugin API used by `plugin/workspace_bridge.py`.
- Local testing used **OrcaSlicer 2.6.0-dev, H3C PR #17273, commit `1babccb8`**, including H3C and Snapmaker U1 workspace names. Newer Nightly compatibility is not guaranteed.

Get Orca from the [official OrcaSlicer releases](https://github.com/OrcaSlicer/OrcaSlicer/releases). This fork starts from upstream `main`, the development source used for Nightly work. The companion runs the `orca-slicer.exe` you choose: it does not download, replace or update Orca. H3C support comes from that Orca build, not from this window host.

## OrcaSlicer must be installed separately

The installer installs **only the Multi Printer Tabs companion**. Install or extract a compatible OrcaSlicer development/Nightly build separately, then select its `orca-slicer.exe` on first launch. A compatible portable Orca installation also works; it does not need a Windows installer. Keep that Orca executable and its complete application folder available after setup.

## Allow multiple Orca instances

Multiple instances must be allowed so each tab can run its own Orca workspace. In Orca's preferences, enable **Allow multiple instances**, or disable **Single instance / Only one instance** if your build uses that wording. Apply the setting before using several tabs and restart Orca if requested.

The companion also sets `app.single_instance = false` automatically in each workspace copy. It does not change the original template's setting. If a new tab opens in an existing Orca window instead, check this preference in the affected workspace and confirm that you selected a compatible Orca build.

## First start

1. Close Orca before copying its configuration, so the files are consistent. Keep a backup of your projects.
2. Start the companion and select your `orca-slicer.exe`.
3. Select your Orca data folder **containing `OrcaSlicer.conf`**. A normal Windows installation usually uses `%APPDATA%\OrcaSlicer`. A portable installation can use another folder.
4. The first tab receives a local copy of that folder. Each new tab receives its own copy of the same template. The original folder is not edited.
5. Select a printer and open a project using the embedded Orca's normal controls.

The release is clean. On your own computer, the configuration you select can include your own profiles, logins and plugins; those are copied locally into workspace folders. Nothing in this companion uploads those copies. Orca and any plugins you install retain their own network behavior.

Default workspace storage is `Documents\OrcaMulti\OT\<8-character-id>`. Short paths matter: some Python/plugin packages fail under deeply nested paths. `settings.json`, `session.json` and `host.log` are stored beside the companion. You can edit `WorkspaceRoot` in `settings.json` while the application is closed; do not place it inside the template folder. Moving existing workspaces requires moving their folders too.

## Controls

All companion labels, tooltips, startup/status messages and bridge notifications are in English. Embedded Orca and Windows system dialogs follow their own language settings.

- The fixed Split View button opens a visual grid picker for **1, 2, 4 or 8** workspaces. The fixed globe button beside it opens **Web Apps**. Both buttons highlight blue on hover.
- Workspace tabs show **selected printer · current project**. The active tab is highlighted. `*` follows Orca's unsaved-project indicator.
- **+**, directly after the last tab, highlights green on hover and creates another Orca workspace. Open a different project or select another printer independently in each one.
- The top strip is dark red; bright red dividers appear only between split views. Drag the dark outer edges or corners to resize the window.
- Each tab has its own **×**. Minimizing and maximizing are controlled by the shared buttons at the top right; the inner Orca minimize/maximize controls are covered in embedded views. The shared **×** highlights red on hover.
- The outer **X** at the far right closes all workspaces and then exits. Orca asks about unsaved work itself; there is no second companion confirmation. Cancel in Orca keeps the session running. **+** also cancels a pending shutdown.
- Drag unused space on the top strip to move the window; double-click it to maximize/restore.

Tabs keep independent configuration folders and locks. Changes are not continuously synchronized between tabs or back into the original Orca installation. Background tabs keep running and can maintain printer connections. The host itself sends no print, movement or heating commands.

## Web Apps: add, open and remove links

The public release starts with an **empty favorites list**. No personal links or browser profiles are included.

1. Click the **globe** button beside Split View. A centered translucent Web Apps picker opens.
2. Click **Edit list** at the bottom.
3. Enter a **Name** and a complete **http://** or **https://** URL, then click **Add**.
4. Click **Save**. Your favorite now appears as a tile in Web Apps.
5. Click its tile to open the site in its own top-level tab. Opening it again selects the existing tab.

To change an entry, select it in **Edit list**, edit its name or URL, click **Update**, then **Save**. To delete one, select it, click **Remove**, then **Save**. **Cancel** discards unsaved list changes. Close a website with its tab's **×**. Closing a web tab does not sign you out.

Favorites are stored locally in `web-apps.json` beside the companion. WebView2 stores cookies and sign-in sessions under `%LOCALAPPDATA%\\OrcaMulti\\WebApps`. Do not publish either location: saved URLs can themselves contain private tokens. Website tabs are not restored automatically on startup. Some websites may restrict embedded browsers, downloads, popups or sign-in; compatibility with every site is not guaranteed.

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

The companion source is on the **[multi-printer-tabs branch](https://github.com/drc85/OrcaSlicer-Multi/tree/multi-printer-tabs)**, under `tools/multi-printer-tabs`. This `main` branch presents the project guide and retains the upstream development source. Switch to the companion branch before using the build commands below.

From `tools/multi-printer-tabs` in PowerShell:

```powershell
.\build.ps1
python .\tests\test_bridge.py
.\test-clone.ps1
.\package.ps1
```

The build uses the .NET Framework compiler included with Windows; no .NET SDK is needed. `restore-webview.ps1` downloads the pinned Microsoft WebView2 SDK from NuGet. Build from the full repository checkout so the original Orca icon is available. `package.ps1` builds the portable ZIP and checksums. To also build the installer, install [Inno Setup 6](https://jrsoftware.org/isinfo.php) and pass `-InnoCompiler 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'`.

The release workflow builds only this companion and publishes a prerelease from its exact source commit. It does not rebuild the full Orca C++ application. Offline bridge tests use a fake Orca API. The clone regression test checks required plugin assets survive and application logs are omitted; neither test reads real printer data. The common local window-host behavior was manually exercised; complete end-to-end printing tests remain outstanding.

## Privacy and removal

Only explicitly selected source/build/documentation files are published. Do not share `web-apps.json`, WebView2 profiles, `settings.json`, `session.json`, `host.log`, workspace folders or Orca data folders: they may contain local paths, login tokens, printer addresses and personal settings. Do not upload screenshots containing credentials when reporting an issue.

The companion's own bridge writes printer name, project path, dirty state and process ID into local plugin storage. It has no network access or printer-control commands. It is project source, not a copy of a user's installed third-party plugin.

Uninstall the installed companion through Windows Settings, or delete the extracted portable files after closing it. Workspace data under `Documents\OrcaMulti\OT` is deliberately retained; remove it yourself only when you no longer need its settings/projects. Your original Orca installation remains separate.

The companion source is provided under AGPL-3.0, matching this repository's [license](LICENSE.txt). OrcaSlicer and third-party plugins retain their respective licenses. This project has no official affiliation with OrcaSlicer, Bambu Lab or Snapmaker. AI-assisted development was used.


## Upstream documentation

The original OrcaSlicer README is available separately in [README.upstream.md](README.upstream.md). This repository's home page describes the Multi Printer Tabs companion.
