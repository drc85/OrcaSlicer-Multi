using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace OrcaMultiPrinterTabs {
    static class Native {
        public delegate bool EnumProc(IntPtr hwnd, IntPtr arg);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr arg);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hwnd, uint cmd);
        [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] public static extern IntPtr GetStyle(IntPtr hwnd, int index);
        [DllImport("user32.dll", EntryPoint="SetWindowLongPtrW")] public static extern IntPtr SetStyle(IntPtr hwnd, int index, IntPtr value);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int cmd);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd, StringBuilder text, int count);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
        [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hwnd,uint msg,IntPtr w,IntPtr l);
        public delegate void WinEvent(IntPtr hook,uint evt,IntPtr hwnd,int objectId,int childId,uint thread,uint time);
        [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr module,WinEvent callback,uint process,uint thread,uint flags);
        [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr hwnd);
        [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
        [DllImport("shell32.dll",CharSet=CharSet.Unicode)] public static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
        public static string Title(IntPtr h) { var b=new StringBuilder(2048); GetWindowText(h,b,b.Capacity); return b.ToString(); }
        public static string Class(IntPtr h) { var b=new StringBuilder(128); GetClassName(h,b,b.Capacity); return b.ToString(); }
        public static IntPtr FindMain(int processId) {
            IntPtr result=IntPtr.Zero;
            EnumWindows(delegate(IntPtr h,IntPtr a) {
                uint pid; GetWindowThreadProcessId(h,out pid);
                if(pid==(uint)processId && GetWindow(h,4)==IntPtr.Zero && Title(h).EndsWith(" - OrcaSlicer")) {
                    result=h; return false;
                }
                return true;
            },IntPtr.Zero);
            return result;
        }
        public static void Focus(IntPtr h) {
            uint pid; uint thread=GetWindowThreadProcessId(h,out pid), current=GetCurrentThreadId();
            bool attached=thread!=current && AttachThreadInput(current,thread,true);
            try { SetFocus(h); } finally { if(attached) AttachThreadInput(current,thread,false); }
        }
    }

    class Settings { public string OrcaPath {get;set;} public string TemplateData {get;set;} public string WorkspaceRoot {get;set;} }
    class Session { public List<string> Workspaces {get;set;} public string Active {get;set;} public int Layout {get;set;} }
    class Workspace {
        public string Id, Folder, StateFile, Printer="Orca", Project="New project";
        public Process Process; public IntPtr Window, OriginalStyle, OriginalExStyle;
        public Panel View; public Label Status; public Button Tab;
        public bool Starting=true, CloseRequested, Detached;
        public IntPtr ShowHook, MinHook;
        public Native.WinEvent Events;
        public DateTime LastTitle=DateTime.MinValue, CloseSent, ModalEnabledSince=DateTime.MinValue;
        public bool SawModal;
        public HashSet<IntPtr> Cloaked=new HashSet<IntPtr>();
        public DateTime Started=DateTime.UtcNow;
    }
    class Host : Form {
        readonly string root=AppDomain.CurrentDomain.BaseDirectory;
        readonly JavaScriptSerializer json=new JavaScriptSerializer();
        readonly List<Workspace> workspaces=new List<Workspace>();
        readonly FlowLayoutPanel tabs=new FlowLayoutPanel();
        readonly Panel content=new Panel();
        readonly Button add=new Button(), layoutButton=new Button(), closeAll=new Button();
        readonly ContextMenuStrip layoutMenu=new ContextMenuStrip();
        readonly Timer pulse=new Timer();
        readonly ToolTip hints=new ToolTip();
        readonly Queue<Workspace> closeQueue=new Queue<Workspace>();
        Settings settings; Workspace active, closingWorkspace; bool shutdown, preparing;
        int layoutCount=1;
        bool exitWhenEmpty;
        readonly Color bg=Color.FromArgb(24,27,30), accent=Color.FromArgb(171,46,48);

        public Host() {
            Text="Orca Slicer Multi Printer Tabs — Preview 0.9.1"; Width=1500; Height=950; MinimumSize=new Size(900,600);
            FormBorderStyle=FormBorderStyle.None; Padding=new Padding(2); MaximizedBounds=Screen.FromHandle(Handle).WorkingArea;
            BackColor=bg; ForeColor=Color.WhiteSmoke; Font=new Font("Segoe UI",10); StartPosition=FormStartPosition.CenterScreen;
            tabs.Dock=DockStyle.Fill; tabs.WrapContents=false; tabs.AutoScroll=true; tabs.BackColor=Color.FromArgb(53,27,30); tabs.Padding=new Padding(7,5,7,0);
            var top=new Panel { Dock=DockStyle.Top, Height=49, BackColor=tabs.BackColor };
            var actions=new FlowLayoutPanel { Dock=DockStyle.Right, Width=48, Padding=new Padding(0,6,0,0), BackColor=tabs.BackColor };
            var viewArea=new Panel {Dock=DockStyle.Left,Width=77,BackColor=tabs.BackColor};
            Style(add,"+",40); Style(layoutButton,"▦ 1",65); Style(closeAll,"×",40);
            add.AccessibleName="New Orca tab"; layoutButton.AccessibleName="View: 1, 2, 4 or 8 tabs"; closeAll.AccessibleName="Close all Orca tabs";
            hints.SetToolTip(layoutButton,"Switch view: show 1, 2, 4 or 8 tabs at once");
            foreach(int count in new[]{1,2,4,8}) {
                int choice=count;
                var item=new ToolStripMenuItem(count==1?"Single view":count+" tabs at once");
                item.Tag=count; item.Click+=delegate {layoutCount=choice;LayoutViews();SaveSession();};layoutMenu.Items.Add(item);
            }
            layoutButton.Location=new Point(6,6);viewArea.Controls.Add(layoutButton);
            tabs.Controls.Add(add);actions.Controls.Add(closeAll);
            top.Controls.Add(tabs); top.Controls.Add(actions);top.Controls.Add(viewArea);
            content.Dock=DockStyle.Fill; content.BackColor=bg;
            content.Paint+=delegate(object sender,PaintEventArgs e) {
                using(var pen=new Pen(Color.FromArgb(255,58,76),8)) {
                    foreach(var item in workspaces.Where(item=>item.View.Visible)) {
                        var box=item.View.Bounds;box.Inflate(4,4);e.Graphics.DrawRectangle(pen,box);
                    }
                }
            };
            Controls.Add(content); Controls.Add(top);
            tabs.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){Native.ReleaseCapture();Native.SendMessage(Handle,0x00A1,new IntPtr(2),IntPtr.Zero);}};
            tabs.DoubleClick+=delegate {ToggleMaximize();};
            content.Resize+=delegate {LayoutViews();};
            add.Click+=async delegate { shutdown=false;exitWhenEmpty=false;closeQueue.Clear();closingWorkspace=null;await AddWorkspace(null); };
            closeAll.Click+=delegate {Close();};
            layoutButton.Click+=delegate {layoutMenu.Show(layoutButton,new Point(0,layoutButton.Height));};
            pulse.Interval=80; pulse.Tick+=delegate { Tick(); };
            Shown+=async delegate { await Initialize(); };
            FormClosing+=ClosingHost;
            Application.ApplicationExit+=delegate { foreach(var w in workspaces.ToArray()) Detach(w); };
        }
        protected override void WndProc(ref Message m) {
            if(m.Msg==0x0084 && WindowState!=FormWindowState.Maximized) {
                var point=PointToClient(new Point(unchecked((short)(m.LParam.ToInt64()&65535)),unchecked((short)((m.LParam.ToInt64()>>16)&65535))));
                bool left=point.X<5,right=point.X>=ClientSize.Width-5,top=point.Y<5,bottom=point.Y>=ClientSize.Height-5;
                int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;
                if(hit!=0){m.Result=new IntPtr(hit);return;}
            }
            base.WndProc(ref m);
        }
        void ToggleMaximize() {
            MaximizedBounds=Screen.FromHandle(Handle).WorkingArea;
            WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;
        }
        void Style(Button b,string text,int width) {
            b.Text=text; b.Width=width; b.Height=34; b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderSize=0;
            b.BackColor=Color.FromArgb(70,36,39); b.ForeColor=Color.White; b.Margin=new Padding(2,0,2,0); b.Cursor=Cursors.Hand;
        }
        void Log(string text) { try { File.AppendAllText(Path.Combine(root,"host.log"),DateTime.Now.ToString("s")+" "+text+Environment.NewLine); } catch {} }
        Dictionary<string,object> ReadObject(string file) {
            string text=File.ReadAllText(file,Encoding.UTF8);
            text=String.Join("\n",text.Split('\n').Where(line=>!line.TrimStart().StartsWith("#")));
            return json.Deserialize<Dictionary<string,object>>(text);
        }
        async Task Initialize() {
            try {
                string file=Path.Combine(root,"settings.json");
                if(File.Exists(file)) settings=json.Deserialize<Settings>(File.ReadAllText(file));
                if(settings==null) settings=new Settings();
                if(String.IsNullOrEmpty(settings.WorkspaceRoot)) settings.WorkspaceRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"OrcaMulti","OT");
                if(!File.Exists(settings.OrcaPath)) {
                    using(var pick=new OpenFileDialog {Title="Select your orca-slicer.exe",Filter="OrcaSlicer|orca-slicer.exe"}) {
                        if(pick.ShowDialog()!=DialogResult.OK) {Close();return;} settings.OrcaPath=pick.FileName;
                    }
                }
                if(!Directory.Exists(settings.TemplateData)) {
                    using(var pick=new FolderBrowserDialog {Description="Select the Orca data folder to copy as a workspace template"}) {
                        if(pick.ShowDialog()!=DialogResult.OK) {Close();return;} settings.TemplateData=pick.SelectedPath;
                    }
                }
                if(!File.Exists(Path.Combine(settings.TemplateData,"OrcaSlicer.conf")))throw new InvalidOperationException("The template folder must contain OrcaSlicer.conf.");
                string template=Path.GetFullPath(settings.TemplateData).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                string workspaceRoot=Path.GetFullPath(settings.WorkspaceRoot).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                if(workspaceRoot.StartsWith(template,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Workspace folders must be outside the template settings folder.");
                try {Icon=Icon.ExtractAssociatedIcon(settings.OrcaPath);}catch{}
                File.WriteAllText(file,json.Serialize(settings),new UTF8Encoding(false));
                Directory.CreateDirectory(settings.WorkspaceRoot); pulse.Start();
                var sessionFile=Path.Combine(root,"session.json");
                Session session=!Environment.GetCommandLineArgs().Contains("--fresh") && File.Exists(sessionFile)?json.Deserialize<Session>(File.ReadAllText(sessionFile)):null;
                if(session!=null && session.Workspaces!=null && session.Workspaces.Count>0) {
                    if(new[]{1,2,4,8}.Contains(session.Layout))layoutCount=session.Layout;
                    foreach(string id in session.Workspaces.Take(12)) if(IsId(id)) await AddWorkspace(id);
                    var last=workspaces.Find(w=>w.Id==session.Active); if(last!=null) Select(last);
                } else await AddWorkspace(null);
            } catch(Exception ex) { Log(ex.ToString()); MessageBox.Show(this,ex.Message,"Startup failed"); }
        }
        static bool IsId(string id) {
            if(String.IsNullOrEmpty(id))return false;
            Guid g; if(Guid.TryParseExact(id,"N",out g))return true;
            return id.Length==8 && id.All(c=>Uri.IsHexDigit(c));
        }
        void CopyDirectory(string source,string target,bool topLevel=true) {
            Directory.CreateDirectory(target);
            foreach(string file in Directory.GetFiles(source)) {
                string name=Path.GetFileName(file);
                if(name.EndsWith(".lock",StringComparison.OrdinalIgnoreCase)||name.EndsWith(".log",StringComparison.OrdinalIgnoreCase)) continue;
                File.Copy(file,Path.Combine(target,name),true);
            }
            foreach(string dir in Directory.GetDirectories(source)) {
                string name=Path.GetFileName(dir);
                // Only top-level application caches are disposable. Plugin folders
                // named python/textures and installed Python packages are required.
                if(name=="__pycache__" || name=="pycache")continue;
                if(topLevel && (new[]{"log","cache","ota"}.Contains(name)||name.StartsWith("user_backup"))) continue;
                if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0) continue;
                CopyDirectory(dir,Path.Combine(target,name),false);
            }
        }
        void Prepare(Workspace w) {
            bool existing=File.Exists(Path.Combine(w.Folder,".workspace-ready"));
            if(!existing) CopyDirectory(settings.TemplateData,w.Folder);
            string conf=Path.Combine(w.Folder,"OrcaSlicer.conf");
            var cfg=ReadObject(conf);
            var app=cfg["app"] as Dictionary<string,object>;
            if(app!=null) { app["single_instance"]=false; app["show_splash_screen"]=false; }
            File.WriteAllText(conf,json.Serialize(cfg),new UTF8Encoding(false));
            string plugin=Path.Combine(w.Folder,"orca_plugins","multi_printer_tabs_bridge"); Directory.CreateDirectory(plugin);
            File.Copy(Path.Combine(root,"plugin","workspace_bridge.py"),Path.Combine(plugin,"workspace_bridge.py"),true);
            string bridgeState=Path.Combine(plugin,".install_state.json");
            if(!File.Exists(bridgeState)) File.WriteAllText(bridgeState,"{\"enabled\":true,\"installed_from\":\"local\",\"installed_version\":\"0.1.0\",\"plugin_name\":\"Multi Printer Tabs Bridge\",\"capabilities\":[{\"Multi Printer Tabs State\":true}]}" ,new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(w.Folder,".workspace-ready"),"1");
            try {
                var presets=cfg["presets"] as Dictionary<string,object>;
                if(presets!=null && presets.ContainsKey("machine")) w.Printer=Convert.ToString(presets["machine"]);
            } catch {}
        }
        async Task AddWorkspace(string id) {
            if(preparing || shutdown || settings==null) return;
            preparing=true; add.Enabled=false;
            Workspace w=null;
            try {
                if(id==null) {
                    do {id=Guid.NewGuid().ToString("N").Substring(0,8);}
                    while(Directory.Exists(Path.Combine(settings.WorkspaceRoot,id)));
                }
                w=new Workspace {Id=id,Folder=Path.Combine(settings.WorkspaceRoot,id)};
                w.View=new Panel {BackColor=bg};
                w.Status=new Label {Dock=DockStyle.Fill,Text="Copying your settings for this tab …",TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.Silver};
                w.View.Controls.Add(w.Status); content.Controls.Add(w.View);
                w.Tab=new Button(); Style(w.Tab,"Starting Orca …",305); w.Tab.TextAlign=ContentAlignment.MiddleLeft;
                w.Tab.Click+=delegate {Select(w);};
                workspaces.Add(w);tabs.Controls.Add(w.Tab);tabs.Controls.SetChildIndex(add,tabs.Controls.Count-1);Select(w);
                await Task.Run(delegate {Prepare(w);});
                w.Status.Text="Starting Orca …";
                var info=new ProcessStartInfo(settings.OrcaPath,"--datadir \""+w.Folder+"\"") {WorkingDirectory=Path.GetDirectoryName(settings.OrcaPath),UseShellExecute=false,WindowStyle=ProcessWindowStyle.Hidden};
                // No shared datadir: every process owns its configuration and locks.
                w.Process=Process.Start(info); w.Started=DateTime.UtcNow;
                Workspace observed=w;
                w.Events=delegate(IntPtr hook,uint evt,IntPtr hwnd,int obj,int child,uint thread,uint time) {
                    if(IsDisposed||observed.Detached)return;
                    if((evt==0x8000||evt==0x8002) && obj==0 && observed.Window==IntPtr.Zero && Native.GetWindow(hwnd,4)==IntPtr.Zero && Native.GetParent(hwnd)==IntPtr.Zero && Native.Class(hwnd).StartsWith("wxWindow")) {
                        int cloak=1;Native.DwmSetWindowAttribute(hwnd,13,ref cloak,4);
                        observed.Cloaked.Add(hwnd);
                        var ex=Native.GetStyle(hwnd,-20).ToInt64();
                        Native.SetStyle(hwnd,-20,new IntPtr((ex & ~0x40000L)|0x80L));
                        Native.ShowWindow(hwnd,0);
                        if(Native.Title(hwnd).EndsWith(" - OrcaSlicer"))BeginInvoke((Action)delegate {if(observed.Window==IntPtr.Zero && !IsDisposed)Embed(observed,hwnd);});
                    }
                    if(evt==0x0016 && hwnd==observed.Window) BeginInvoke((Action)delegate {Native.ShowWindow(observed.Window,9);WindowState=FormWindowState.Minimized;});
                };
                w.ShowHook=Native.SetWinEventHook(0x8000,0x8002,IntPtr.Zero,w.Events,(uint)w.Process.Id,0,0);
                w.MinHook=Native.SetWinEventHook(0x0016,0x0016,IntPtr.Zero,w.Events,(uint)w.Process.Id,0,0);
                Log("Started workspace "+w.Id+" PID "+w.Process.Id); SaveSession();
            } catch(Exception ex) {
                Log(ex.ToString()); if(w!=null) {w.Starting=false;w.Status.Text="Startup failed: "+ex.Message;}
                MessageBox.Show(this,ex.Message,"Could not start Orca");
            } finally {preparing=false;add.Enabled=true;}
        }
        void Embed(Workspace w,IntPtr handle) {
            if(w.Window!=IntPtr.Zero||!Native.IsWindow(handle))return;
            w.Window=handle; w.OriginalStyle=Native.GetStyle(handle,-16);w.OriginalExStyle=Native.GetStyle(handle,-20);
            long style=w.OriginalStyle.ToInt64();
            Native.ShowWindow(handle,0);
            Native.SetStyle(handle,-16,new IntPtr((style & ~0x80CF0000L)|0x40000000L));
            Native.SetStyle(handle,-20,new IntPtr(w.OriginalExStyle.ToInt64() & ~0x00040000L));
            Native.SetParent(handle,w.View.Handle);
            if(Native.GetWindow(handle,4)!=IntPtr.Zero) { /* dialogs retain Orca ownership */ }
            w.Starting=false;w.Status.Visible=false; LayoutViews();
            int uncloak=0;Native.DwmSetWindowAttribute(handle,13,ref uncloak,4);w.Cloaked.Remove(handle);
            Log("Embedded workspace "+w.Id); if(w==active) Native.Focus(w.Window);
        }
        void ResizeChild(Workspace w) {
            if(w.Window!=IntPtr.Zero && !w.Detached) Native.SetWindowPos(w.Window,IntPtr.Zero,0,0,w.View.ClientSize.Width,w.View.ClientSize.Height,0x0014|0x0020);
        }
        protected override void OnResize(EventArgs e) {base.OnResize(e);if(content!=null)LayoutViews();}
        void LayoutViews() {
            if(content==null||workspaces==null||layoutButton==null)return;
            int index=active==null?0:Math.Max(0,workspaces.IndexOf(active));
            int start=(index/layoutCount)*layoutCount;
            int cols=layoutCount==1?1:layoutCount==2?2:layoutCount==4?2:4;
            int rows=layoutCount<=2?1:2;
            layoutButton.Text="▦ "+layoutCount;
            foreach(ToolStripMenuItem item in layoutMenu.Items)item.Checked=(int)item.Tag==layoutCount;
            for(int i=0;i<workspaces.Count;i++) {
                var w=workspaces[i];bool visible=i>=start && i<start+layoutCount;
                w.Tab.BackColor=w==active?accent:visible?Color.FromArgb(103,49,53):Color.FromArgb(70,36,39);
                w.Tab.ForeColor=Color.White;
                w.Tab.FlatAppearance.BorderSize=w==active?2:0;
                w.Tab.FlatAppearance.BorderColor=Color.FromArgb(255,126,140);
                if(w==active)w.Tab.BackColor=Color.FromArgb(217,40,61);
                if(visible) {
                    int cell=i-start,col=cell%cols,row=cell/cols;
                    int x=col*content.ClientSize.Width/cols,y=row*content.ClientSize.Height/rows;
                    int width=(col+1)*content.ClientSize.Width/cols-x,height=(row+1)*content.ClientSize.Height/rows-y;
                    w.View.Bounds=new Rectangle(x+4,y+4,Math.Max(1,width-8),Math.Max(1,height-8));
                }
                w.View.Visible=visible;ResizeChild(w);
                if(w.Window!=IntPtr.Zero)Native.ShowWindow(w.Window,visible?5:0);
            }
            content.Invalidate();
        }
        void Select(Workspace w) {
            active=w;
            LayoutViews();if(w.Window!=IntPtr.Zero)Native.Focus(w.Window);SaveSession();
        }
        void Tick() {
            foreach(var w in workspaces.ToArray()) {
                try {
                    if(w.Process==null)continue;
                    if(w.Process.HasExited) {Log("Exited workspace "+w.Id);Remove(w);continue;}
                    if(w.Window==IntPtr.Zero && w.Starting) {
                        IntPtr handle=Native.FindMain(w.Process.Id);if(handle!=IntPtr.Zero)Embed(w,handle);
                        else if((DateTime.UtcNow-w.Started).TotalSeconds>90) w.Status.Text="Orca is taking longer to start. Check the taskbar for Orca dialogs.";
                    }
                    if(w.Window!=IntPtr.Zero) {
                        if(Native.IsIconic(w.Window)){Native.ShowWindow(w.Window,9);WindowState=FormWindowState.Minimized;}
                        else if(Native.IsZoomed(w.Window)){Native.ShowWindow(w.Window,9);ToggleMaximize();LayoutViews();}
                        if((DateTime.UtcNow-w.LastTitle).TotalMilliseconds>=650){UpdateTitle(w);w.LastTitle=DateTime.UtcNow;}
                    }
                } catch(Exception ex) {Log("Tick: "+ex.Message);}
            }
            if(exitWhenEmpty && workspaces.Count==0){pulse.Stop();Close();return;}
            if(shutdown) {
                if(closingWorkspace!=null && !workspaces.Contains(closingWorkspace))closingWorkspace=null;
                if(closingWorkspace!=null && Native.IsWindow(closingWorkspace.Window)) {
                    if(!Native.IsWindowEnabled(closingWorkspace.Window)){closingWorkspace.SawModal=true;closingWorkspace.ModalEnabledSince=DateTime.MinValue;}
                    else if(closingWorkspace.SawModal) {
                        if(closingWorkspace.ModalEnabledSince==DateTime.MinValue)closingWorkspace.ModalEnabledSince=DateTime.UtcNow;
                        else if((DateTime.UtcNow-closingWorkspace.ModalEnabledSince).TotalSeconds>5) {shutdown=false;exitWhenEmpty=false;closeQueue.Clear();closingWorkspace=null;}
                    }
                }
                if(shutdown && closingWorkspace==null && closeQueue.Count>0) {
                    var next=closeQueue.Dequeue();if(workspaces.Contains(next)){closingWorkspace=next;RequestClose(next);}
                }
                if(workspaces.Count==0){pulse.Stop();Close();}
            }
        }
        void UpdateTitle(Workspace w) {
            bool dirty=false;
            string title=Native.Title(w.Window);
            if(title.EndsWith(" - OrcaSlicer")) w.Project=title.Substring(0,title.Length-13).TrimStart('*').Trim();
            if(String.IsNullOrWhiteSpace(w.Project)||w.Project=="Untitled")w.Project="New project";
            dirty=title.StartsWith("*");
            if(w.StateFile==null || !File.Exists(w.StateFile)) {
                string folder=Path.Combine(w.Folder,"orca_plugins","plugin_data","multi_printer_tabs_bridge");
                if(Directory.Exists(folder)) w.StateFile=Directory.GetFiles(folder,"workspace-state.json",SearchOption.AllDirectories).FirstOrDefault();
                if(w.StateFile==null) {
                    string data=Path.Combine(w.Folder,"orca_plugins","plugin_data");
                    if(Directory.Exists(data)) w.StateFile=Directory.GetFiles(data,"workspace-state.json",SearchOption.AllDirectories).FirstOrDefault();
                }
            }
            if(w.StateFile!=null) {
                try {
                    var state=ReadObject(w.StateFile);
                    if(Convert.ToInt32(state["pid"])==w.Process.Id) {
                        w.Printer=Convert.ToString(state["printer"]);
                        // Orca's current native title stays authoritative for project naming,
                        // including unsaved edits and loading a new project.
                    }
                } catch(IOException) {} catch(ArgumentException) {}
            }
            string label=w.Printer+" · "+w.Project+(dirty?" *":"");
            if(w.Tab.Text!=label) {w.Tab.Text=label;hints.SetToolTip(w.Tab,label);}
            if(w==active) Text=label+" — Orca Slicer Multi Printer Tabs";
        }
        void RequestClose(Workspace w) {
            if(w==null)return;
            if(w.Window==IntPtr.Zero) {if(w.Process==null||w.Process.HasExited)Remove(w);return;}
            Select(w);w.CloseRequested=true;w.CloseSent=DateTime.UtcNow;w.SawModal=false;
            Native.PostMessage(w.Window,0x0010,IntPtr.Zero,IntPtr.Zero);
            // Orca owns the save prompt. Never kill the process or discard a project.
        }
        void Detach(Workspace w) {
            ReleaseHooks(w);
            if(w.Detached||w.Window==IntPtr.Zero||!Native.IsWindow(w.Window))return;
            Native.SetParent(w.Window,IntPtr.Zero);Native.SetStyle(w.Window,-16,w.OriginalStyle);Native.SetStyle(w.Window,-20,w.OriginalExStyle);
            Native.SetWindowPos(w.Window,IntPtr.Zero,70,70,1280,850,0x0034);Native.ShowWindow(w.Window,5);w.Detached=true;
        }
        void Remove(Workspace w) {
            ReleaseHooks(w);
            bool wasActive=w==active;workspaces.Remove(w);tabs.Controls.Remove(w.Tab);content.Controls.Remove(w.View);
            w.Tab.Dispose();w.View.Dispose();if(wasActive){active=null;if(workspaces.Count>0)Select(workspaces[workspaces.Count-1]);}SaveSession();
        }
        void ReleaseHooks(Workspace w) {
            if(w.ShowHook!=IntPtr.Zero){Native.UnhookWinEvent(w.ShowHook);w.ShowHook=IntPtr.Zero;}
            if(w.MinHook!=IntPtr.Zero){Native.UnhookWinEvent(w.MinHook);w.MinHook=IntPtr.Zero;}
            foreach(var handle in w.Cloaked){if(Native.IsWindow(handle)){int cloak=0;Native.DwmSetWindowAttribute(handle,13,ref cloak,4);}}
            w.Cloaked.Clear();
        }
        void SaveSession() {
            try {File.WriteAllText(Path.Combine(root,"session.json"),json.Serialize(new Session {Workspaces=workspaces.Select(w=>w.Id).ToList(),Active=active==null?null:active.Id,Layout=layoutCount}),new UTF8Encoding(false));}catch(Exception ex){Log(ex.Message);}
        }
        void ClosingHost(object sender,FormClosingEventArgs e) {
            if(workspaces.Count==0)return;
            e.Cancel=true;
            // Orca owns the save/discard/cancel prompts. Do not add a second confirmation.
            if(shutdown)return;
            exitWhenEmpty=true;shutdown=true;closingWorkspace=null;closeQueue.Clear();foreach(var w in workspaces.ToArray())closeQueue.Enqueue(w);
            // Cancel in an Orca save dialog leaves the wrapper running. The + button
            // cancels a pending shutdown so the user can continue work.
        }
    }
    static class Program {
        [STAThread] static void Main() {
            System.Threading.Thread.CurrentThread.CurrentUICulture=System.Globalization.CultureInfo.GetCultureInfo("en-US");
            Native.SetCurrentProcessExplicitAppUserModelID("OrcaSlicer.MultiPrinterTabs");
            bool created;
            using(var mutex=new System.Threading.Mutex(true,"Local\\OrcaSlicerMultiPrinterTabsPreview",out created)) {
            if(!created){MessageBox.Show("The tab window is already running. Open another tab with +.","Orca Slicer Multi Printer Tabs");return;}
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Host());
            }
        }
    }
}
