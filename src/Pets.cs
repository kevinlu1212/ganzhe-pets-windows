using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GanzhePets {
public class Clip {
    public int id { get; set; }
    public string label { get; set; }
    public string file { get; set; }
    public double duration { get; set; }
    public bool defaultOff { get; set; }
}
public class Preference {
    public double X = -1, Y = -1, Size = 150, Volume = 0.65;
    public bool Linked = true, AllClips = false, OnTop = true;
    public int FixedId = 0;
}
public class PetSpec {
    public string Key, Name;
    public int Count;
    public PetSpec(string key, string name, int count) { Key=key; Name=name; Count=count; }
}
public static class Program {
    public static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string DataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GanzhePets");
    public static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
    public static readonly List<PetWindow> Pets = new List<PetWindow>();
    public static readonly Random Random = new Random();
    public static Application App;
    public static int Generation;
    public static bool Testing;
    static Mutex mutex;
    static EventWaitHandle showEvent;
    public static string Report;
    public static readonly PetSpec[] Specs = {
        new PetSpec("ShaolanPet", "薄肌桌宠", 37),
        new PetSpec("SunPet", "太对了桌宠", 28),
        new PetSpec("FengGe-Desktop-Pet", "好事桌宠", 11)
    };
    [STAThread] public static int Main(string[] args) {
        Testing = args.Contains("--self-test") || args.Contains("--integration-test");
        Directory.CreateDirectory(DataRoot);
        int reportIndex = Array.IndexOf(args, "--report");
        Report = reportIndex >= 0 && reportIndex+1<args.Length ? args[reportIndex+1] : Path.Combine(DataRoot,"self-test.json");
        if (!Testing) {
            bool fresh;
            mutex = new Mutex(true, "Local\\GanzhePets.Windows.LocalPort", out fresh);
            showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\GanzhePets.Show");
            if (!fresh) { showEvent.Set(); showEvent.Dispose(); mutex.Dispose(); return 0; }
        }
        App = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        App.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e) {
            File.AppendAllText(Path.Combine(DataRoot,"error.log"), DateTime.Now.ToString("s")+" "+e.Exception+Environment.NewLine);
            e.Handled=true;
            if (!Testing) MessageBox.Show("桌宠遇到问题。详情已保存到："+Path.Combine(DataRoot,"error.log"), "三人桌宠");
            App.Shutdown(1);
        };
        App.Startup += delegate {
            try {
                foreach (PetSpec spec in Specs) Pets.Add(new PetWindow(spec, Array.IndexOf(Specs,spec)));
                if (args.Contains("--self-test")) { new AssetTest().Start(); return; }
                if (args.Contains("--integration-test")) { RunIntegrationTest(); return; }
                foreach (PetWindow pet in Pets) pet.Show();
                DispatcherTimer restore = new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(400) };
                restore.Tick += delegate { if (showEvent.WaitOne(0)) foreach (PetWindow pet in Pets) if (!pet.IsClosed) { pet.WindowState=WindowState.Normal; pet.Show(); pet.Activate(); } };
                restore.Start();
            } catch (Exception ex) {
                File.WriteAllText(Path.Combine(DataRoot,"error.log"),ex.ToString());
                if (!Testing) MessageBox.Show("桌宠启动失败："+ex.Message,"三人桌宠");
                App.Shutdown(1);
            }
        };
        int result=App.Run();
        if (showEvent!=null) showEvent.Dispose();
        if (mutex!=null) { mutex.ReleaseMutex(); mutex.Dispose(); }
        return result;
    }
    public static void Save(PetWindow pet) {
        if (Testing) return;
        try { File.WriteAllText(Path.Combine(DataRoot,pet.Spec.Key+".json"),Json.Serialize(pet.Settings)); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    public static PetWindow Find(string key) { return Pets.FirstOrDefault(p=>p.Spec.Key==key && !p.IsClosed); }
    public static void Trigger(PetWindow pet, Clip clip) {
        Generation++;
        foreach (PetWindow other in Pets) other.Stop();
        pet.Play(clip, pet.Spec.Key, 0, Generation);
    }
    public static void Respond(PetWindow from, string root, int hop, int generation) {
        if (generation!=Generation || hop>=3 || !from.Settings.Linked) return;
        PetWindow next = null;
        if (from.Spec.Key=="ShaolanPet" || from.Spec.Key=="FengGe-Desktop-Pet") next=Find("SunPet");
        else if (from.Spec.Key=="SunPet" && root=="ShaolanPet" && hop==1) next=Find("FengGe-Desktop-Pet");
        if (next==null || !next.Settings.Linked) return;
        Clip response = next.TakeNext();
        DispatcherTimer pause=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(140) };
        pause.Tick += delegate { pause.Stop(); if (generation==Generation && !next.IsClosed) next.Play(response,root,hop+1,generation); };
        pause.Start();
    }
    public static void ExitAll() { Generation++; foreach (PetWindow pet in Pets.ToArray()) if (!pet.IsClosed) pet.Close(); App.Shutdown(); }
    public static void Log(string text) { File.AppendAllText(Path.Combine(DataRoot,"playback.log"), DateTime.Now.ToString("s")+" "+text+Environment.NewLine); }
    public static void RunIntegrationTest() {
        List<string> steps=new List<string>();
        PetWindow.Played += delegate(PetWindow pet, Clip clip, int hop) { steps.Add(pet.Spec.Key+":"+hop); };
        PetWindow.MediaError += delegate(string error) { File.WriteAllText(Report,Json.Serialize(new { pass=false,error=error })); App.Shutdown(1); };
        foreach (PetWindow pet in Pets) pet.Settings.Volume=0;
        Trigger(Find("ShaolanPet"), Find("ShaolanPet").Clips.First(c=>!c.defaultOff));
        DispatcherTimer done=new DispatcherTimer { Interval=TimeSpan.FromSeconds(15) };
        done.Tick += delegate {
            done.Stop();
            string[] expected={"ShaolanPet:0","SunPet:1","FengGe-Desktop-Pet:2","SunPet:3"};
            bool pass=steps.SequenceEqual(expected);
            File.WriteAllText(Report,Json.Serialize(new { pass=pass,steps=steps,expected=expected }));
            foreach (PetWindow pet in Pets) pet.Stop();
            App.Shutdown(pass?0:1);
        };
        done.Start();
    }
}
public class PetWindow : Window {
    public readonly PetSpec Spec;
    public readonly List<Clip> Clips;
    public Preference Settings;
    public bool IsClosed;
    public static event Action<PetWindow,Clip,int> Played;
    public static event Action<string> MediaError;
    readonly string assetRoot;
    readonly MediaPlayer player=new MediaPlayer();
    readonly Queue<Clip> bag=new Queue<Clip>();
    readonly TextBlock caption=new TextBlock();
    readonly Border bubble=new Border();
    readonly Image face=new Image();
    readonly DispatcherTimer captionTimer=new DispatcherTimer();
    readonly DispatcherTimer safetyTimer=new DispatcherTimer();
    readonly RotateTransform rotation=new RotateTransform();
    Point pressPoint;
    double initialX,initialY;
    bool dragging, held, active;
    int lastId=-1, currentHop, currentGeneration;
    Clip currentClip;
    string currentRoot;
    public PetWindow(PetSpec spec,int index) {
        Spec=spec;
        assetRoot=Path.Combine(Program.Root,"assets",spec.Key);
        Clips=Program.Json.Deserialize<List<Clip>>(File.ReadAllText(Path.Combine(assetRoot,"clips.json")));
        if (Clips.Count!=spec.Count) throw new InvalidDataException(spec.Name+" 音频数量不正确");
        Settings=new Preference();
        if (!Program.Testing) try {
            string path=Path.Combine(Program.DataRoot,spec.Key+".json");
            if(File.Exists(path)) Settings=Program.Json.Deserialize<Preference>(File.ReadAllText(path)) ?? new Preference();
        } catch { Settings=new Preference(); }
        Settings.Size=Clamp(Settings.Size,90,300,150);
        Settings.Volume=Clamp(Settings.Volume,0,1,0.65);
        Title=spec.Name+" · Windows";
        WindowStyle=WindowStyle.None; AllowsTransparency=true; Background=Brushes.Transparent;
        ResizeMode=ResizeMode.NoResize; ShowInTaskbar=true; Topmost=Settings.OnTop;
        Width=Settings.Size+32; Height=Settings.Size+75;
        Rect area=SystemParameters.WorkArea;
        Left=Settings.X>=area.Left && Settings.X+Width<=area.Right ? Settings.X : area.Right-(SpecsWidth(index));
        Top=Settings.Y>=area.Top && Settings.Y+Height<=area.Bottom ? Settings.Y : area.Bottom-Height-20;
        Grid grid=new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height=new GridLength(60) });
        grid.RowDefinitions.Add(new RowDefinition { Height=new GridLength(1,GridUnitType.Star) });
        caption.TextAlignment=TextAlignment.Center; caption.TextWrapping=TextWrapping.Wrap;
        caption.Foreground=Brushes.White; caption.FontSize=12; caption.FontFamily=new FontFamily("Microsoft YaHei UI");
        bubble.Background=new SolidColorBrush(Color.FromArgb(220,30,32,40)); bubble.CornerRadius=new CornerRadius(10);
        bubble.Padding=new Thickness(9,6,9,6); bubble.Margin=new Thickness(0,0,0,5);
        bubble.VerticalAlignment=VerticalAlignment.Bottom; bubble.Child=caption; bubble.Visibility=Visibility.Hidden;
        grid.Children.Add(bubble);
        BitmapImage bitmap=new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption=BitmapCacheOption.OnLoad;
        bitmap.UriSource=new Uri(Path.Combine(assetRoot,"face.png")); bitmap.EndInit(); bitmap.Freeze();
        if (bitmap.PixelWidth<1) throw new InvalidDataException("头像无法读取");
        face.Source=bitmap; face.Stretch=Stretch.Uniform; face.Margin=new Thickness(10,0,10,4);
        face.RenderTransformOrigin=new Point(0.5,0.65); face.RenderTransform=rotation;
        Grid.SetRow(face,1); grid.Children.Add(face); Content=grid;
        ToolTip="点击说一句 · 拖动移动 · 右键设置";
        AutomationProperties.SetName(face,spec.Name+"，点击播放，拖动移动，右键设置");
        MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) {
            pressPoint=PointToScreen(e.GetPosition(this)); initialX=Left; initialY=Top; held=true; dragging=false;
            CaptureMouse(); e.Handled=true;
        };
        MouseMove += delegate(object sender, MouseEventArgs e) {
            if(!held || e.LeftButton!=MouseButtonState.Pressed) return;
            Point pt=PointToScreen(e.GetPosition(this));
            // Screen points use physical pixels; WPF window positions use device independent units.
            PresentationSource source=PresentationSource.FromVisual(this);
            Vector delta=pt-pressPoint;
            if(source!=null) delta=source.CompositionTarget.TransformFromDevice.Transform(delta);
            if(delta.Length>4) dragging=true;
            if(dragging) { Left=initialX+delta.X; Top=initialY+delta.Y; }
        };
        MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e) {
            if(!held) return; held=false; ReleaseMouseCapture();
            if(dragging) { KeepVisible(); SavePosition(); } else Program.Trigger(this,TakeNext());
            e.Handled=true;
        };
        LostMouseCapture += delegate { held=false; };
        KeyDown += delegate(object sender,KeyEventArgs e) { if(e.Key==Key.Space || e.Key==Key.Enter) Program.Trigger(this,TakeNext()); };
        ContextMenu=new ContextMenu();
        ContextMenu.Opened += delegate { BuildMenu(); };
        captionTimer.Tick += delegate { captionTimer.Stop(); bubble.Visibility=Visibility.Hidden; };
        safetyTimer.Tick += delegate { safetyTimer.Stop(); if(active) Fail("播放超时："+currentClip.file); };
        player.MediaOpened += delegate {
            if(!active) return;
            safetyTimer.Stop(); safetyTimer.Interval=TimeSpan.FromSeconds(Math.Max(5,currentClip.duration+4)); safetyTimer.Start();
            if(Played!=null) Played(this,currentClip,currentHop);
            Program.Log(Spec.Key+" clip="+currentClip.id+" hop="+currentHop+" root="+currentRoot);
        };
        player.MediaEnded += delegate {
            if(!active) return; active=false; safetyTimer.Stop(); Animate(false);
            player.Close(); Program.Respond(this,currentRoot,currentHop,currentGeneration);
        };
        player.MediaFailed += delegate(object sender,ExceptionEventArgs e) { Fail(e.ErrorException.Message); };
        Closing += delegate {
            SavePosition(); IsClosed=true; Stop();
            if(!Program.Testing && Program.Pets.All(p=>p==this || p.IsClosed)) Program.App.Shutdown();
        };
        Loaded += delegate { Program.Log(Spec.Key+" window loaded at "+Left+","+Top+" visible="+IsVisible); Say("点击说一句 · 右键设置",4); };
    }
    static double SpecsWidth(int index) { return (3-index)*195+20; }
    static double Clamp(double value,double min,double max,double fallback) { return Double.IsNaN(value)||Double.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value)); }
    void KeepVisible() {
        // Virtual desktop coordinates allow placing pets on secondary monitors.
        Left=Clamp(Left,SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth-Width,0);
        Top=Clamp(Top,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight-Height,0);
    }
    void SavePosition() { Settings.X=Left; Settings.Y=Top; Program.Save(this); }
    public Clip TakeNext() {
        if(Settings.FixedId>0) { Clip fixedClip=Clips.FirstOrDefault(c=>c.id==Settings.FixedId); if(fixedClip!=null) return fixedClip; }
        if(bag.Count==0) {
            List<Clip> pool=Clips.Where(c=>Settings.AllClips || !c.defaultOff).OrderBy(c=>Program.Random.Next()).ToList();
            if(pool.Count==0) pool=Clips.ToList();
            if(pool.Count>1 && pool[0].id==lastId) { Clip first=pool[0]; pool[0]=pool[1]; pool[1]=first; }
            foreach(Clip clip in pool) bag.Enqueue(clip);
        }
        Clip next=bag.Dequeue(); lastId=next.id; return next;
    }
    public void Play(Clip clip,string root,int hop,int generation) {
        Stop(); if(IsClosed) return;
        currentClip=clip; currentRoot=root; currentHop=hop; currentGeneration=generation; active=true;
        Say(clip.label,Math.Max(2.5,clip.duration+0.7)); Animate(true);
        safetyTimer.Interval=TimeSpan.FromSeconds(8); safetyTimer.Start();
        player.Volume=Settings.Volume;
        player.Open(new Uri(Path.Combine(assetRoot,"Clips",clip.file))); player.Play();
    }
    void Fail(string error) {
        Stop(); Program.Log("ERROR "+Spec.Key+" "+error); Say("音频播放失败，请查看说明",5);
        if(MediaError!=null) MediaError(Spec.Key+": "+error);
    }
    public void Stop() { active=false; safetyTimer.Stop(); player.Stop(); player.Close(); Animate(false); }
    void Animate(bool enabled) {
        if(!enabled) { rotation.BeginAnimation(RotateTransform.AngleProperty,null); rotation.Angle=0; return; }
        DoubleAnimation wobble=new DoubleAnimation(-3,3,TimeSpan.FromMilliseconds(160));
        wobble.AutoReverse=true; wobble.RepeatBehavior=RepeatBehavior.Forever;
        rotation.BeginAnimation(RotateTransform.AngleProperty,wobble);
    }
    void Say(string text,double seconds) {
        caption.Text=text; bubble.Visibility=Visibility.Visible; captionTimer.Stop();
        captionTimer.Interval=TimeSpan.FromSeconds(seconds); captionTimer.Start();
    }
    MenuItem Item(string text,Action action,bool check=false) {
        MenuItem menu=new MenuItem { Header=text,IsChecked=check };
        if(action!=null) menu.Click+=delegate { action(); };
        return menu;
    }
    void BuildMenu() {
        ContextMenu.Items.Clear();
        ContextMenu.Items.Add(new MenuItem { Header=Spec.Name+" · "+Clips.Count+" 句",IsEnabled=false });
        ContextMenu.Items.Add(Item("随机说一句",delegate { Program.Trigger(this,TakeNext()); }));
        MenuItem choose=Item("选择语录（点击试听）",null);
        foreach(Clip clip in Clips) { Clip selected=clip; choose.Items.Add(Item(clip.id+". "+clip.label,delegate { Program.Trigger(this,selected); })); }
        ContextMenu.Items.Add(choose);
        ContextMenu.Items.Add(Item("全部语录参与随机",delegate { Settings.AllClips=!Settings.AllClips; bag.Clear(); Program.Save(this); },Settings.AllClips));
        MenuItem fixedMenu=Item("固定播放一句",null);
        fixedMenu.Items.Add(Item("关闭固定，随机轮播",delegate { Settings.FixedId=0; Program.Save(this); },Settings.FixedId==0));
        foreach(Clip clip in Clips) { Clip selected=clip; fixedMenu.Items.Add(Item(clip.id+". "+clip.label,delegate { Settings.FixedId=selected.id; Program.Save(this); },Settings.FixedId==clip.id)); }
        ContextMenu.Items.Add(fixedMenu);
        ContextMenu.Items.Add(new Separator());
        ContextMenu.Items.Add(Item("互相接话",delegate { Settings.Linked=!Settings.Linked; Program.Generation++; Program.Save(this); },Settings.Linked));
        MenuItem volume=Item("音量",null);
        foreach(int n in new int[]{0,25,50,65,80,100}) { int value=n; volume.Items.Add(Item(n==0?"静音":n+"%",delegate { Settings.Volume=value/100.0; player.Volume=Settings.Volume; Program.Save(this); },Math.Abs(Settings.Volume-value/100.0)<0.01)); }
        ContextMenu.Items.Add(volume);
        MenuItem size=Item("大小",null);
        foreach(int n in new int[]{100,150,200,260}) { int value=n; size.Items.Add(Item(n+" 像素",delegate { Settings.Size=value; Width=value+32; Height=value+75; KeepVisible(); SavePosition(); },Math.Abs(Settings.Size-n)<1)); }
        ContextMenu.Items.Add(size);
        ContextMenu.Items.Add(Item("窗口置顶",delegate { Topmost=!Topmost; Settings.OnTop=Topmost; Program.Save(this); },Topmost));
        ContextMenu.Items.Add(Item("恢复三个桌宠的位置",delegate { foreach(PetWindow pet in Program.Pets) if(!pet.IsClosed) { Rect area=SystemParameters.WorkArea; pet.Left=area.Right-SpecsWidth(Program.Pets.IndexOf(pet)); pet.Top=area.Bottom-pet.Height-20; pet.SavePosition(); } }));
        ContextMenu.Items.Add(new Separator());
        ContextMenu.Items.Add(Item("使用说明",delegate { System.Diagnostics.Process.Start(Path.Combine(Program.Root,"使用说明.txt")); }));
        ContextMenu.Items.Add(Item("关闭这个桌宠",delegate { Close(); }));
        ContextMenu.Items.Add(Item("退出全部桌宠",Program.ExitAll));
    }
}
public class AssetTest {
    readonly Queue<Tuple<PetSpec,Clip>> pending=new Queue<Tuple<PetSpec,Clip>>();
    readonly List<object> results=new List<object>();
    readonly MediaPlayer player=new MediaPlayer();
    readonly DispatcherTimer timeout=new DispatcherTimer { Interval=TimeSpan.FromSeconds(10) };
    Tuple<PetSpec,Clip> current;
    bool failed, waiting, opened;
    public void Start() {
        foreach(PetWindow pet in Program.Pets) {
            BitmapSource bitmap=(BitmapSource)((Image)((Grid)pet.Content).Children[1]).Source;
            if(!pet.AllowsTransparency || bitmap.PixelWidth==0) throw new Exception("浮窗/头像验证失败");
            foreach(Clip clip in pet.Clips) {
                string path=Path.Combine(Program.Root,"assets",pet.Spec.Key,"Clips",clip.file);
                if(!File.Exists(path)) throw new FileNotFoundException(path);
                pending.Enqueue(Tuple.Create(pet.Spec,clip));
            }
            // The shuffle must exhaust every enabled clip before repeating.
            HashSet<int> seen=new HashSet<int>();
            int count=pet.Clips.Count(c=>!c.defaultOff);
            for(int i=0;i<count;i++) if(!seen.Add(pet.TakeNext().id)) throw new Exception("随机轮播提前重复");
        }
        // Some sub-second M4A clips report Duration.Automatic in WPF. Verify
        // actual decoding through MediaEnded instead of trusting metadata.
        player.MediaOpened += delegate { if(waiting) opened=true; };
        player.MediaEnded += delegate { if(waiting) Complete(opened,"decoded to end (silent playback)"); };
        player.MediaFailed += delegate(object sender,ExceptionEventArgs e) { if(waiting) Complete(false,e.ErrorException.Message); };
        timeout.Tick += delegate { if(waiting) Complete(false,"decode timeout"); };
        player.Volume=0; Next();
    }
    void Complete(bool ok,string detail) {
        waiting=false; timeout.Stop(); player.Stop(); player.Close();
        failed|=!ok;
        results.Add(new { pet=current.Item1.Key,clip=current.Item2.id,pass=ok,detail=detail });
        Program.App.Dispatcher.BeginInvoke(new Action(Next));
    }
    void Next() {
        if(pending.Count==0) {
            File.WriteAllText(Program.Report,Program.Json.Serialize(new { pass=!failed,images=3,audioFiles=results.Count,checks=results }));
            Program.App.Shutdown(failed?1:0); return;
        }
        current=pending.Dequeue(); waiting=true; opened=false; timeout.Start();
        player.Open(new Uri(Path.Combine(Program.Root,"assets",current.Item1.Key,"Clips",current.Item2.file)));
        player.Play();
    }
}
}
