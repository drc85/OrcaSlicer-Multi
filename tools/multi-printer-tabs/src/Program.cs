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
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OrcaMultiPrinterTabs {
    static class Native {
        [DllImport("gdi32.dll")] public static extern IntPtr CreateRectRgn(int left,int top,int right,int bottom);
        [DllImport("gdi32.dll")] public static extern int CombineRgn(IntPtr dest,IntPtr first,IntPtr second,int mode);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
        [DllImport("user32.dll")] public static extern int SetWindowRgn(IntPtr hwnd,IntPtr region,bool redraw);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);

        public delegate bool EnumProc(IntPtr hwnd, IntPtr arg);
        [StructLayout(LayoutKind.Sequential)] public struct WindowRect { public int Left,Top,Right,Bottom; }
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out WindowRect rect);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
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
    static class PortableLayout {
        public static bool Configure(string root,Settings settings) {
            if(!File.Exists(Path.Combine(root,"portable-bundle.json")))return false;
            settings.OrcaPath=Path.Combine(root,"Orca","orca-slicer.exe");
            if(!File.Exists(settings.OrcaPath))throw new FileNotFoundException("The bundled Orca executable is missing. Extract the complete portable ZIP.",settings.OrcaPath);
            settings.TemplateData=Path.Combine(root,"data","t");
            settings.WorkspaceRoot=Path.Combine(root,"data","w");
            Directory.CreateDirectory(settings.TemplateData);
            string conf=Path.Combine(settings.TemplateData,"OrcaSlicer.conf");
            if(!File.Exists(conf))File.WriteAllText(conf,"{\"app\":{\"single_instance\":false,\"show_splash_screen\":false,\"language\":\"en_US\"}}",new UTF8Encoding(false));
            return true;
        }
    }
    class WebFavorite { public string Id {get;set;} public string Name {get;set;} public string Url {get;set;} }
    class Session { public List<string> Workspaces {get;set;} public string Active {get;set;} public int Layout {get;set;} }
    class Workspace {
        public string Id, Folder, StateFile, Printer="Orca", Project="New project";
        public Process Process; public IntPtr Window, OriginalStyle, OriginalExStyle;
        public Panel View; public Label Status; public Button Tab, CloseTab, InnerClose; public Panel WindowControls; public int RegionWidth,RegionHeight;
        public bool Starting=true, CloseRequested, Detached;
        public IntPtr ShowHook, MinHook, MoveHook;
        public Native.WinEvent Events;
        public DateTime LastTitle=DateTime.MinValue, CloseSent, ModalEnabledSince=DateTime.MinValue;
        public bool SawModal;
        public HashSet<IntPtr> Cloaked=new HashSet<IntPtr>();
        public WebView2 Browser;
        public DateTime Started=DateTime.UtcNow;
    }
    class Host : Form {
        readonly string root=AppDomain.CurrentDomain.BaseDirectory;
        readonly JavaScriptSerializer json=new JavaScriptSerializer();
        readonly List<Workspace> workspaces=new List<Workspace>();
        readonly FlowLayoutPanel tabs=new FlowLayoutPanel();
        readonly Panel content=new Panel();
        readonly Button add=new Button(), layoutButton=new Button(), webButton=new Button(), minimizeHost=new Button(), maximizeHost=new Button(), closeAll=new Button();
        readonly ContextMenuStrip layoutMenu=new ContextMenuStrip();
        readonly Timer pulse=new Timer();
        readonly ToolTip hints=new ToolTip();
        readonly Queue<Workspace> closeQueue=new Queue<Workspace>();
        Settings settings; Workspace active, closingWorkspace; bool shutdown, preparing;
        int layoutCount=1;
        bool exitWhenEmpty, portableBundle;
        readonly Color bg=Color.FromArgb(24,27,30), accent=Color.FromArgb(171,46,48);

        public Host() {
            Text="OrcaSlicer Multi Printer Tabs — Preview 0.17.0"; Width=1500; Height=950; MinimumSize=new Size(900,600);
            FormBorderStyle=FormBorderStyle.None; Padding=new Padding(8); MaximizedBounds=Screen.FromHandle(Handle).WorkingArea;
            BackColor=bg; ForeColor=Color.WhiteSmoke; Font=new Font("Segoe UI",10); StartPosition=FormStartPosition.CenterScreen;
            tabs.Dock=DockStyle.Fill; tabs.WrapContents=false; tabs.AutoScroll=true; tabs.BackColor=Color.FromArgb(53,27,30); tabs.Padding=new Padding(7,5,7,0);
            var top=new Panel { Dock=DockStyle.Top, Height=49, BackColor=tabs.BackColor };
            var actions=new FlowLayoutPanel { Dock=DockStyle.Right, Width=144, WrapContents=false, Padding=new Padding(0,6,0,0), BackColor=tabs.BackColor };
            var viewArea=new Panel {Dock=DockStyle.Left,Width=130,BackColor=tabs.BackColor};
            Style(add,"+",40); Style(layoutButton,"▦",55); Style(webButton,"🌐",55);Style(closeAll,"×",40);Style(minimizeHost,"−",40);Style(maximizeHost,"□",40);
            add.FlatAppearance.MouseOverBackColor=Color.FromArgb(35,175,90);
            add.FlatAppearance.MouseDownBackColor=Color.FromArgb(25,140,70);
            foreach(var button in new[]{webButton,layoutButton}) {
                button.FlatAppearance.MouseOverBackColor=Color.FromArgb(40,135,230);
                button.FlatAppearance.MouseDownBackColor=Color.FromArgb(30,105,190);
            }
            webButton.AccessibleName="Web Apps";hints.SetToolTip(webButton,"Web Apps");
            add.AccessibleName="New Orca tab"; layoutButton.AccessibleName="View: 1, 2, 4 or 8 tabs"; closeAll.AccessibleName="Close all tabs";
            hints.SetToolTip(layoutButton,"Switch view: show 1, 2, 4 or 8 tabs at once");
            foreach(int count in new[]{1,2,4,8}) {
                int choice=count;
                var item=new ToolStripMenuItem(count==1?"Single view":count+" tabs at once");
                item.Tag=count; item.Click+=delegate {layoutCount=choice;LayoutViews();SaveSession();};layoutMenu.Items.Add(item);
            }
            layoutButton.Location=new Point(6,6);webButton.Location=new Point(66,6);viewArea.Controls.Add(layoutButton);viewArea.Controls.Add(webButton);
            tabs.Controls.Add(add);actions.Controls.Add(minimizeHost);actions.Controls.Add(maximizeHost);actions.Controls.Add(closeAll);
            top.Controls.Add(tabs); top.Controls.Add(actions);top.Controls.Add(viewArea);
            content.Dock=DockStyle.Fill; content.BackColor=bg;
            content.Paint+=delegate(object sender,PaintEventArgs e){using(var brush=new SolidBrush(Color.FromArgb(255,58,76))){foreach(var w in workspaces.Where(x=>x.View.Visible&&x.Browser==null)){var box=w.View.Bounds;if(box.Left>0)e.Graphics.FillRectangle(brush,box.Left-8,box.Top,8,box.Height);if(box.Top>0)e.Graphics.FillRectangle(brush,Math.Max(0,box.Left-8),box.Top-8,box.Width+(box.Left>0?8:0),8);}}};
            Controls.Add(content); Controls.Add(top);
            tabs.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){Native.ReleaseCapture();Native.SendMessage(Handle,0x00A1,new IntPtr(2),IntPtr.Zero);}};
            tabs.DoubleClick+=delegate {ToggleMaximize();};
            content.Resize+=delegate {LayoutViews();};
            add.Click+=async delegate { shutdown=false;exitWhenEmpty=false;closeQueue.Clear();closingWorkspace=null;await AddWorkspace(null); };
            closeAll.FlatAppearance.MouseOverBackColor=Color.FromArgb(255,58,76);closeAll.FlatAppearance.MouseDownBackColor=Color.FromArgb(217,40,61);
            minimizeHost.AccessibleName="Minimize window";maximizeHost.AccessibleName="Maximize or restore window";
            minimizeHost.Click+=delegate{WindowState=FormWindowState.Minimized;};maximizeHost.Click+=delegate{ToggleMaximize();};
            closeAll.Click+=delegate {Close();};
            webButton.Click+=delegate {ShowWebApps();};
            layoutButton.Click+=delegate {ShowLayouts();};
            pulse.Interval=80; pulse.Tick+=delegate { Tick(); };
            Shown+=async delegate { await Initialize(); };
            FormClosing+=ClosingHost;
            Application.ApplicationExit+=delegate { foreach(var w in workspaces.ToArray()) Detach(w); };
        }
        List<WebFavorite> LoadFavorites() {
            string path=Path.Combine(root,"web-apps.json");
            if(File.Exists(path)) {
                try {return (json.Deserialize<List<WebFavorite>>(File.ReadAllText(path))??new List<WebFavorite>()).Where(f=>f!=null&&!String.IsNullOrWhiteSpace(f.Id)&&!String.IsNullOrWhiteSpace(f.Name)&&ValidUrl(f.Url)).ToList();}
                catch(Exception ex){MessageBox.Show(this,"Could not read Web Apps: "+ex.Message);return new List<WebFavorite>();}
            }
            return new List<WebFavorite>();
        }
        static bool ValidUrl(string value){Uri uri;return Uri.TryCreate(value,UriKind.Absolute,out uri)&&(uri.Scheme=="https"||uri.Scheme=="http");}
        Form Popup(string title,Size size) {
            var f=new Form{Text=title,ClientSize=size,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,BackColor=Color.FromArgb(48,49,57),ForeColor=Color.WhiteSmoke,Font=Font,Opacity=0.97};
            return f;
        }
        void ShowLayouts(){
            using(var f=Popup("Split View",new Size(460,250))){
                var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(14)};
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
                grid.RowStyles.Add(new RowStyle(SizeType.Percent,50));grid.RowStyles.Add(new RowStyle(SizeType.Percent,50));
                foreach(int count in new[]{1,2,4,8}){
                    int choice=count;var button=new Button{Dock=DockStyle.Fill,Margin=new Padding(7),FlatStyle=FlatStyle.Flat,BackColor=count==layoutCount?accent:Color.FromArgb(64,65,74),AccessibleName=count+" view layout"};
                    button.Paint+=delegate(object sender,PaintEventArgs e){int cols=choice==1?1:choice==2||choice==4?2:4,rows=choice<=2?1:2;var box=new Rectangle(22,15,button.Width-44,button.Height-39);using(var pen=new Pen(Color.WhiteSmoke,2)){e.Graphics.DrawRectangle(pen,box);for(int i=1;i<cols;i++)e.Graphics.DrawLine(pen,box.Left+box.Width*i/cols,box.Top,box.Left+box.Width*i/cols,box.Bottom);for(int i=1;i<rows;i++)e.Graphics.DrawLine(pen,box.Left,box.Top+box.Height*i/rows,box.Right,box.Top+box.Height*i/rows);}TextRenderer.DrawText(e.Graphics,choice+" view"+(choice==1?"":"s"),Font,new Rectangle(0,button.Height-22,button.Width,20),Color.WhiteSmoke,TextFormatFlags.HorizontalCenter);};
                    button.Click+=delegate{layoutCount=choice;LayoutViews();SaveSession();f.Close();};grid.Controls.Add(button);
                }f.Controls.Add(grid);f.ShowDialog(this);
            }
        }
        async void ShowWebApps(){
            WebFavorite selected=null;var favorites=LoadFavorites();
            using(var f=Popup("Web Apps",new Size(580,400))){
                var tiles=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),AutoScroll=true};
                var edit=new Button{Text="Edit list",Dock=DockStyle.Bottom,Height=44,FlatStyle=FlatStyle.Flat};
                Action refresh=delegate{tiles.Controls.Clear();if(favorites.Count==0)tiles.Controls.Add(new Label{Text="No Web Apps yet. Select Edit list to add your own links.",AutoSize=true,MaximumSize=new Size(510,0),Margin=new Padding(12)});foreach(var favorite in favorites){var item=favorite;var button=new Button{Text=item.Name,Size=new Size(245,100),Margin=new Padding(8),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(68,69,80),ForeColor=Color.WhiteSmoke,Font=new Font(Font.FontFamily,14)};button.Click+=delegate{selected=item;f.Close();};tiles.Controls.Add(button);}};
                edit.Click+=delegate{EditFavorites(f,favorites);refresh();};f.Controls.Add(tiles);f.Controls.Add(edit);refresh();f.ShowDialog(this);
            }
            if(selected!=null){shutdown=false;exitWhenEmpty=false;closeQueue.Clear();closingWorkspace=null;await OpenWebApp(selected.Id,selected.Name,selected.Url,"OrcaMultiWeb-"+String.Concat(selected.Id.Where(Char.IsLetterOrDigit)));}
        }
        void EditFavorites(Form owner,List<WebFavorite> favorites){
            var draft=favorites.Select(x=>new WebFavorite{Id=x.Id,Name=x.Name,Url=x.Url}).ToList();
            using(var f=Popup("Edit Web Apps",new Size(580,420))){
                var list=new ListBox{Left=18,Top=18,Width=544,Height=190,BackColor=bg,ForeColor=Color.WhiteSmoke,DisplayMember="Name"};
                var name=new TextBox{Left=90,Top=225,Width=472};var url=new TextBox{Left=90,Top=265,Width=472};
                f.Controls.Add(new Label{Text="Name",Left=18,Top=228});f.Controls.Add(new Label{Text="URL",Left=18,Top=268});f.Controls.Add(list);f.Controls.Add(name);f.Controls.Add(url);
                Action refresh=delegate{list.DataSource=null;list.DataSource=draft;list.DisplayMember="Name";};
                list.SelectedIndexChanged+=delegate{var item=list.SelectedItem as WebFavorite;if(item!=null){name.Text=item.Name;url.Text=item.Url;}};
                Action<bool> store=delegate(bool update){if(String.IsNullOrWhiteSpace(name.Text)||!ValidUrl(url.Text.Trim())){MessageBox.Show(f,"Enter a name and a valid http or https URL.");return;}var item=update?list.SelectedItem as WebFavorite:null;if(update&&item==null)return;if(item==null){item=new WebFavorite{Id="web-"+Guid.NewGuid().ToString("N")};draft.Add(item);}item.Name=name.Text.Trim();item.Url=url.Text.Trim();refresh();list.SelectedItem=item;};
                var addItem=new Button{Text="Add",Left=18,Top=318,Width=100};var updateItem=new Button{Text="Update",Left=128,Top=318,Width=100};var remove=new Button{Text="Remove",Left=238,Top=318,Width=100};var save=new Button{Text="Save",Left=352,Top=370,Width=100};var cancel=new Button{Text="Cancel",Left=462,Top=370,Width=100,DialogResult=DialogResult.Cancel};
                addItem.Click+=delegate{store(false);};updateItem.Click+=delegate{store(true);};remove.Click+=delegate{var item=list.SelectedItem as WebFavorite;if(item!=null){draft.Remove(item);refresh();}};
                save.Click+=delegate{try{string path=Path.Combine(root,"web-apps.json");File.WriteAllText(path+".tmp",json.Serialize(draft),new UTF8Encoding(false));if(File.Exists(path))File.Replace(path+".tmp",path,null);else File.Move(path+".tmp",path);favorites.Clear();favorites.AddRange(draft);f.Close();}catch(Exception ex){MessageBox.Show(f,"Could not save Web Apps: "+ex.Message);}};
                f.Controls.AddRange(new Control[]{addItem,updateItem,remove,save,cancel});f.CancelButton=cancel;refresh();f.ShowDialog(owner);
            }
        }
        async Task OpenWebApp(string id,string title,string url,string profileFolder) {
            var existing=workspaces.Find(candidate=>candidate.Id==id);
            if(existing!=null){Select(existing);return;}
            var w=new Workspace {Id=id,Printer=title,Project="Web",Starting=false};
            w.View=new Panel {BackColor=bg};
            w.Tab=new Button();Style(w.Tab,title,305);w.Tab.TextAlign=ContentAlignment.MiddleLeft;
            w.Tab.Click+=delegate{Select(w);};
            tabs.Controls.Add(w.Tab);tabs.Controls.SetChildIndex(add,tabs.Controls.Count-1);
            var bar=new Panel {Dock=DockStyle.Top,Height=34,BackColor=Color.FromArgb(34,38,43)};
            var name=new Label {Text=title+" · "+new Uri(url).Host,Dock=DockStyle.Fill,ForeColor=Color.WhiteSmoke,Padding=new Padding(12,7,0,0)};
            var reload=new Button {Text="↻",Dock=DockStyle.Right,Width=40};
            var close=new Button {Text="×",Dock=DockStyle.Right,Width=40};
            close.AccessibleName="Close "+title;
            reload.Click+=delegate{if(w.Browser!=null&&w.Browser.CoreWebView2!=null)w.Browser.Reload();};
            close.Click+=delegate{Remove(w);};
            bar.Controls.Add(name);bar.Controls.Add(reload);bar.Controls.Add(close);
            w.Browser=new WebView2 {Dock=DockStyle.Fill,DefaultBackgroundColor=bg};
            w.View.Controls.Add(w.Browser);w.View.Controls.Add(bar);
            content.Controls.Add(w.View);workspaces.Add(w);Select(w);
            try {
                var env=await CoreWebView2Environment.CreateAsync(null,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OrcaMulti","WebApps",profileFolder));
                if(w.View.IsDisposed)return;
                await w.Browser.EnsureCoreWebView2Async(env);
                w.Browser.CoreWebView2.NewWindowRequested+=delegate(object sender,CoreWebView2NewWindowRequestedEventArgs e){e.Handled=true;w.Browser.CoreWebView2.Navigate(e.Uri);};
                w.Browser.Source=new Uri(url);
            }catch(Exception ex){Log(title+": "+ex.Message);MessageBox.Show(this,"Could not open the web view: "+ex.Message,title);}
        }
        protected override void WndProc(ref Message m) {
            if(m.Msg==0x0084 && WindowState!=FormWindowState.Maximized) {
                var point=PointToClient(new Point(unchecked((short)(m.LParam.ToInt64()&65535)),unchecked((short)((m.LParam.ToInt64()>>16)&65535))));
                bool left=point.X<Padding.Left,right=point.X>=ClientSize.Width-Padding.Right,top=point.Y<Padding.Top,bottom=point.Y>=ClientSize.Height-Padding.Bottom;
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
                portableBundle=PortableLayout.Configure(root,settings);
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
                if(!File.Exists(Path.Combine(settings.TemplateData,"OrcaSlicer.conf")))throw new InvalidOperationException("The template must contain OrcaSlicer.conf.");
                string template=Path.GetFullPath(settings.TemplateData).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                string workspaceRoot=Path.GetFullPath(settings.WorkspaceRoot).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                if(workspaceRoot.StartsWith(template,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Workspace folders must not be inside the template settings folder.");
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
        void Prepare(Workspace w,string templateSource) {
            bool existing=File.Exists(Path.Combine(w.Folder,".workspace-ready"));
            if(!existing) CopyDirectory(templateSource,w.Folder);
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
                string templateSource=portableBundle && active!=null && active.Browser==null && File.Exists(Path.Combine(active.Folder,"OrcaSlicer.conf"))?active.Folder:settings.TemplateData;
                if(id==null) {
                    do {id=Guid.NewGuid().ToString("N").Substring(0,8);}
                    while(Directory.Exists(Path.Combine(settings.WorkspaceRoot,id)));
                }
                w=new Workspace {Id=id,Folder=Path.Combine(settings.WorkspaceRoot,id)};
                w.View=new Panel {BackColor=bg};
                w.Status=new Label {Dock=DockStyle.Fill,Text="Copying settings for this workspace ...",TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.Silver};
                w.View.Controls.Add(w.Status); content.Controls.Add(w.View);
                w.Tab=new Button(); Style(w.Tab,"Orca startet …",305); w.Tab.TextAlign=ContentAlignment.MiddleLeft;
                w.Tab.Click+=delegate {Select(w);};
                workspaces.Add(w);tabs.Controls.Add(w.Tab);tabs.Controls.SetChildIndex(add,tabs.Controls.Count-1);Select(w);
                await Task.Run(delegate {Prepare(w,templateSource);});
                w.Status.Text="Orca startet …";
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
                    if(evt==0x800B && obj==0 && hwnd==observed.Window) BeginInvoke((Action)delegate {if(!IsDisposed)KeepChildAnchored(observed);});
                    if(evt==0x0016 && hwnd==observed.Window) BeginInvoke((Action)delegate {Native.ShowWindow(observed.Window,9);WindowState=FormWindowState.Minimized;});
                };
                w.ShowHook=Native.SetWinEventHook(0x8000,0x8002,IntPtr.Zero,w.Events,(uint)w.Process.Id,0,0);
                w.MinHook=Native.SetWinEventHook(0x0016,0x0016,IntPtr.Zero,w.Events,(uint)w.Process.Id,0,0);
                w.MoveHook=Native.SetWinEventHook(0x800B,0x800B,IntPtr.Zero,w.Events,(uint)w.Process.Id,0,0);
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
        void KeepChildAnchored(Workspace w) {
            if(w.Window==IntPtr.Zero||w.Detached||WindowState==FormWindowState.Minimized||!w.View.Visible)return;
            Native.WindowRect box;if(!Native.GetWindowRect(w.Window,out box))return;
            var expected=w.View.PointToScreen(Point.Empty);
            int dx=box.Left-expected.X,dy=box.Top-expected.Y;
            if(dx==0&&dy==0&&box.Right-box.Left==w.View.ClientSize.Width&&box.Bottom-box.Top==w.View.ClientSize.Height)return;
            // Orca's custom title bar can move a reparented HWND directly. Keep
            // the child at (0,0), forwarding that drag delta to the outer host.
            if((dx!=0||dy!=0)&&(Native.GetAsyncKeyState(1)&0x8000)!=0&&WindowState==FormWindowState.Normal)
                Location=new Point(Location.X+dx,Location.Y+dy);
            ResizeChild(w);
        }
        void EnsureTabClose(Workspace w) {
            if(w.CloseTab!=null)return;
            w.Tab.Padding=new Padding(0,0,28,0);
            w.CloseTab=new Button{Text="×",Width=26,Dock=DockStyle.Right,FlatStyle=FlatStyle.Flat,TabStop=false,BackColor=Color.FromArgb(70,36,39),ForeColor=Color.WhiteSmoke,AccessibleName="Close tab"};
            w.CloseTab.FlatAppearance.BorderSize=0;w.CloseTab.Click+=delegate{RequestClose(w);};w.Tab.Controls.Add(w.CloseTab);
        }
        void UpdateInnerControls(Workspace w) {
            int width=w.View.ClientSize.Width,height=w.View.ClientSize.Height;
            if(width<=0||height<=0)return;
            float scale=Native.GetDpiForWindow(w.Window)/96f;if(scale<=0)scale=1;
            int strip=(int)Math.Ceiling(100*scale),bar=(int)Math.Ceiling(30*scale);
            if(w.WindowControls==null){
                w.WindowControls=new Panel{BackColor=Color.FromArgb(36,44,47)};
                w.InnerClose=new Button{Text="×",Dock=DockStyle.Right,Width=(int)(32*scale),FlatStyle=FlatStyle.Flat,ForeColor=Color.WhiteSmoke,AccessibleName="Close Orca tab"};
                w.InnerClose.FlatAppearance.BorderSize=0;w.InnerClose.Click+=delegate{RequestClose(w);};w.WindowControls.Controls.Add(w.InnerClose);w.View.Controls.Add(w.WindowControls);
            }
            w.WindowControls.Bounds=new Rectangle(Math.Max(0,width-strip),0,Math.Min(width,strip),bar);w.WindowControls.BringToFront();
            if(w.RegionWidth==width&&w.RegionHeight==height)return;
            var full=Native.CreateRectRgn(0,0,width,height);var cut=Native.CreateRectRgn(Math.Max(0,width-strip),0,width,bar);
            Native.CombineRgn(full,full,cut,4);Native.DeleteObject(cut);
            if(Native.SetWindowRgn(w.Window,full,true)==0)Native.DeleteObject(full);
            w.RegionWidth=width;w.RegionHeight=height;
        }
        void ResizeChild(Workspace w) {
            if(w.Window!=IntPtr.Zero && !w.Detached) Native.SetWindowPos(w.Window,IntPtr.Zero,0,0,w.View.ClientSize.Width,w.View.ClientSize.Height,0x0014|0x0020);
            if(w.Window!=IntPtr.Zero&&!w.Detached)UpdateInnerControls(w);
        }
        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            int edge=WindowState==FormWindowState.Maximized?0:8;
            if(Padding.Left!=edge)Padding=new Padding(edge);
            if(content!=null)LayoutViews();
        }
        void LayoutViews() {
            if(content==null||workspaces==null||layoutButton==null)return;
            var orcaViews=workspaces.Where(item=>item.Browser==null).ToList();
            bool webMode=active!=null && active.Browser!=null;
            int index=active==null?0:Math.Max(0,orcaViews.IndexOf(active));
            int start=(index/layoutCount)*layoutCount;
            int cols=layoutCount==1?1:layoutCount==2?2:layoutCount==4?2:4;
            int rows=layoutCount<=2?1:2;
            layoutButton.Text="▦ "+layoutCount;
            foreach(ToolStripMenuItem item in layoutMenu.Items)item.Checked=(int)item.Tag==layoutCount;
            for(int i=0;i<workspaces.Count;i++) {
                var w=workspaces[i];EnsureTabClose(w);int orcaIndex=orcaViews.IndexOf(w);
                bool visible=w.Browser!=null?w==active:!webMode && orcaIndex>=start && orcaIndex<start+layoutCount;
                w.Tab.BackColor=visible?accent:Color.FromArgb(70,36,39);
                w.Tab.ForeColor=Color.White;
                w.Tab.FlatAppearance.BorderSize=w==active?2:0;
                w.Tab.FlatAppearance.BorderColor=Color.FromArgb(255,126,140);
                if(w==active)w.Tab.BackColor=Color.FromArgb(217,40,61);
                w.CloseTab.BackColor=w.Tab.BackColor;
                var closeColor=w.Tab.BackColor;
                w.CloseTab.FlatAppearance.MouseOverBackColor=Color.FromArgb(Math.Min(255,closeColor.R+35),Math.Min(255,closeColor.G+35),Math.Min(255,closeColor.B+35));
                w.CloseTab.FlatAppearance.MouseDownBackColor=Color.FromArgb(Math.Min(255,closeColor.R+50),Math.Min(255,closeColor.G+50),Math.Min(255,closeColor.B+50));
                if(visible) {
                    if(w.Browser!=null){w.View.Bounds=content.ClientRectangle;w.View.Visible=true;continue;}
                    int cell=orcaIndex-start,col=cell%cols,row=cell/cols;
                    int x=col*content.ClientSize.Width/cols,y=row*content.ClientSize.Height/rows;
                    int width=(col+1)*content.ClientSize.Width/cols-x,height=(row+1)*content.ClientSize.Height/rows-y;
                    int gapX=col>0?8:0,gapY=row>0?8:0;
                    w.View.Bounds=new Rectangle(x+gapX,y+gapY,Math.Max(1,width-gapX),Math.Max(1,height-gapY));
                }
                w.View.Visible=visible;ResizeChild(w);
                if(w.Window!=IntPtr.Zero)Native.ShowWindow(w.Window,visible?5:0);
            }
            content.Invalidate();
        }
        void Select(Workspace w) {
            active=w;
            LayoutViews();if(w.Window!=IntPtr.Zero)Native.Focus(w.Window);else if(w.Browser!=null)w.Browser.Focus();SaveSession();
            if(w.Browser!=null)Text=w.Printer+" — OrcaSlicer Multi Printer Tabs";
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
                        KeepChildAnchored(w);
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
            Native.SetWindowRgn(w.Window,IntPtr.Zero,true);Native.SetParent(w.Window,IntPtr.Zero);Native.SetStyle(w.Window,-16,w.OriginalStyle);Native.SetStyle(w.Window,-20,w.OriginalExStyle);
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
            if(w.MoveHook!=IntPtr.Zero){Native.UnhookWinEvent(w.MoveHook);w.MoveHook=IntPtr.Zero;}
            foreach(var handle in w.Cloaked){if(Native.IsWindow(handle)){int cloak=0;Native.DwmSetWindowAttribute(handle,13,ref cloak,4);}}
            w.Cloaked.Clear();
        }
        void SaveSession() {
            try {File.WriteAllText(Path.Combine(root,"session.json"),json.Serialize(new Session {Workspaces=workspaces.Where(w=>w.Browser==null).Select(w=>w.Id).ToList(),Active=active==null?null:active.Id,Layout=layoutCount}),new UTF8Encoding(false));}catch(Exception ex){Log(ex.Message);}
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
            Native.SetCurrentProcessExplicitAppUserModelID("OrcaSlicer.MultiPrinterTabs");
            bool created;
            using(var mutex=new System.Threading.Mutex(true,"Local\\OrcaSlicerMultiPrinterTabs",out created)) {
            if(!created){MessageBox.Show("The tab application is already running. Use + to open another tab.","Orca Slicer Multi Printer Tabs");return;}
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Host());
            }
        }
    }
}
