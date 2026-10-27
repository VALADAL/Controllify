using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace Controllify;

static class Program
{
    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_POWER_THROTTLING_STATE { public uint Version; public uint ControlMask; public uint StateMask; }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetProcessInformation(IntPtr hProcess, int infoClass, ref PROCESS_POWER_THROTTLING_STATE info, int size);

    [DllImport("ntdll.dll")]
    static extern int NtResumeProcess(IntPtr processHandle);

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    // Ayudante: vigila a Controllify y, si alguien lo congela (suspende), lo reanuda.
    // Corre como un proceso aparte, así que no se congela junto con la app.
    static void RunGuardian(int pid, bool debug)
    {
        Process target;
        try { target = Process.GetProcessById(pid); } catch { return; }

        // Para que a este ayudante tampoco le bajen la velocidad en segundo plano
        try
        {
            var state = new PROCESS_POWER_THROTTLING_STATE { Version = 1, ControlMask = 1, StateMask = 0 };
            SetProcessInformation(Process.GetCurrentProcess().Handle, 4, ref state, Marshal.SizeOf(state));
        }
        catch { }

        IntPtr handle = OpenProcess(0x0800, false, pid);   // 0x0800 = permiso de suspender/reanudar
        if (handle == IntPtr.Zero) { return; }
        string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Controllify", "log.txt");
        int resumed = 0;

        while (!target.HasExited)
        {
            try
            {
                target.Refresh();
                bool suspended = false;
                foreach (ProcessThread t in target.Threads)
                {
                    if (t.ThreadState == System.Diagnostics.ThreadState.Wait && t.WaitReason == ThreadWaitReason.Suspended)
                    {
                        suspended = true;
                        break;
                    }
                }
                if (suspended)
                {
                    NtResumeProcess(handle);
                    resumed++;
                    if (debug)
                    {
                        try { File.AppendAllText(logFile, DateTime.Now.ToString("HH:mm:ss.fff") + "  GUARDIAN: Controllify estaba suspendido, lo reanude (" + resumed + ")" + Environment.NewLine); }
                        catch { }
                    }
                }
            }
            catch { }
            Thread.Sleep(250);
        }
    }

    [STAThread]
    static void Main()
    {
        // Modo ayudante: "Controllify.exe --guardian <pid>"
        var argv = Environment.GetCommandLineArgs();
        int gi = Array.IndexOf(argv, "--guardian");
        if (gi >= 0 && gi + 1 < argv.Length && int.TryParse(argv[gi + 1], out int guardPid))
        {
            RunGuardian(guardPid, argv.Contains("--debug"));
            return;
        }

        // Una sola copia: dos copias serían dos dispositivos de Spotify y se cortarían la música entre sí.
        using var mutex = new Mutex(true, "Controllify.SingleInstance", out bool first);
        if (!first) { return; }

        // Pide a Windows que no baje la velocidad de este proceso cuando no está al frente
        try
        {
            var me = Process.GetCurrentProcess();
            var state = new PROCESS_POWER_THROTTLING_STATE { Version = 1, ControlMask = 1, StateMask = 0 };
            SetProcessInformation(me.Handle, 4, ref state, Marshal.SizeOf(state));   // 4 = ProcessPowerThrottling
            me.PriorityClass = ProcessPriorityClass.High;
        }
        catch { }

        // Lanza el ayudante (a menos que pongas --no-guardian)
        if (!argv.Contains("--no-guardian"))
        {
            try
            {
                Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--guardian " + Environment.ProcessId + (argv.Contains("--debug") ? " --debug" : ""))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch { }
        }

        ApplicationConfiguration.Initialize();
        var args = argv;
        Application.Run(new MainForm(args.Contains("--debug"), args.Contains("--no-smtc"), args.Contains("--windowed")));
    }
}

class MainForm : Form
{
    const string AppUrl = "https://valadal.github.io/Controllify/";

    readonly WebView2 web = new();
    readonly bool debug;
    readonly bool noSmtc;   // --no-smtc: usa solo la sesión del navegador embebido
    readonly HttpClient http = new();
    SystemMediaTransportControls? smtc;
    InMemoryRandomAccessStream? thumbStream;   // se mantiene en memoria para que la carátula no desaparezca
    string lastTrack = "";
    bool? lastPaused;
    string lastDump = "";
    System.Threading.Timer? freeTimer;   // reloj que NO depende de la ventana (para comparar con el de la ventana)
    static readonly object logLock = new();

    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

    static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Controllify", "log.txt");

    void Log(string msg)
    {
        if (!debug) { return; }
        try
        {
            lock (logLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);
                File.AppendAllText(LogFile, DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg + Environment.NewLine);
            }
        }
        catch { }
    }

    public MainForm(bool debug, bool noSmtc, bool windowed)
    {
        this.debug = debug;
        this.noSmtc = noSmtc;
        Text = "Controllify";
        BackColor = Color.FromArgb(7, 9, 15);
        if (windowed)
        {
            // --windowed: ventana normal maximizada (con borde), para que el theme no la confunda con un juego a pantalla completa
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Maximized;
        }
        else
        {
            FormBorderStyle = FormBorderStyle.None;   // sin bordes, como un juego
            StartPosition = FormStartPosition.Manual;
            Bounds = Screen.PrimaryScreen!.Bounds;    // cubre toda la pantalla (Windows oculta la barra de tareas)
        }
        ShowInTaskbar = true;

        web.Dock = DockStyle.Fill;
        web.DefaultBackgroundColor = Color.FromArgb(7, 9, 15);
        Controls.Add(web);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (noSmtc) { Log("SMTC nativo desactivado (--no-smtc)"); } else { InitSmtc(); }

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Controllify", "WebView2");

        var options = new CoreWebView2EnvironmentOptions
        {
            // Audio sin pedir clic y que el navegador embebido no duerma la página.
            AdditionalBrowserArguments =
                "--autoplay-policy=no-user-gesture-required " +
                "--disable-renderer-backgrounding --disable-background-timer-throttling " +
                "--disable-backgrounding-occluded-windows " +
                "--disable-features=CalculateNativeWinOcclusion,IntensiveWakeUpThrottling,UseEcoQoSForBackgroundProcess"
        };

        var env = await CoreWebView2Environment.CreateAsync(null, dataDir, options);
        await web.EnsureCoreWebView2Async(env);

        var core = web.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.WebMessageReceived += OnWebMessage;
        core.Navigate(debug ? AppUrl + "?debug" : AppUrl);
        Log("WebView2 listo");

        if (debug)
        {
            // Cada segundo anota qué sesiones de medios ve Windows y cuál es la "actual"
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            var lastTick = DateTime.Now;
            timer.Tick += async (s2, e2) =>
            {
                // Si la ventana se queda "dormida", este reloj tarda más de 1 s en volver a sonar
                double gap = (DateTime.Now - lastTick).TotalSeconds;
                lastTick = DateTime.Now;
                if (gap > 3) { Log("LATIDO nativo tardó " + gap.ToString("0.0") + " s"); }
                await DumpSessionsAsync();
            };
            timer.Start();

            // Segundo reloj en otro hilo: si este también se atrasa, el proceso entero se está congelando.
            // También anota qué programa está al frente.
            var lastFree = DateTime.Now;
            string lastFg = "";
            freeTimer = new System.Threading.Timer(_ =>
            {
                double gap = (DateTime.Now - lastFree).TotalSeconds;
                lastFree = DateTime.Now;
                if (gap > 3) { Log("LATIDO hilo libre tardó " + gap.ToString("0.0") + " s"); }
                try
                {
                    GetWindowThreadProcessId(GetForegroundWindow(), out uint pid);
                    string name = Process.GetProcessById((int)pid).ProcessName;
                    if (name != lastFg) { lastFg = name; Log("PRIMER PLANO: " + name); }
                }
                catch { }
            }, null, 1000, 1000);
        }
    }

    // Solo para diagnóstico (--debug): lista todas las sesiones de medios de Windows
    async Task DumpSessionsAsync()
    {
        try
        {
            var mgr = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            string text = "SESIONES (actual=" + (mgr.GetCurrentSession()?.SourceAppUserModelId ?? "ninguna") + "):";
            foreach (var s in mgr.GetSessions())
            {
                var p = await s.TryGetMediaPropertiesAsync();
                string thumb = "sin";   // tamaño real de la carátula que ve Windows (para medir cuándo se actualiza)
                try { if (p.Thumbnail != null) { using var ts = await p.Thumbnail.OpenReadAsync(); thumb = ts.Size + "B"; } }
                catch { thumb = "error"; }
                var info = s.GetPlaybackInfo();
                var c = info.Controls;
                // botones: P=play p=pausa N=siguiente B=anterior
                text += " [" + s.SourceAppUserModelId + " | " + info.PlaybackStatus + " | " + p.Title +
                        " | tipo=" + info.PlaybackType + " | caratula=" + thumb +
                        " | botones=" + (c.IsPlayEnabled ? "P" : "-") + (c.IsPauseEnabled ? "p" : "-") +
                        (c.IsNextEnabled ? "N" : "-") + (c.IsPreviousEnabled ? "B" : "-") + "]";
            }
            if (text != lastDump) { lastDump = text; Log(text); }   // solo escribe cuando algo cambia
        }
        catch (Exception ex) { Log("error sesiones: " + ex.Message); }
    }

    // ---------- Windows media (SMTC) ----------
    void InitSmtc()
    {
        try
        {
            smtc = SystemMediaTransportControlsInterop.GetForWindow(Handle);
            smtc.IsEnabled = true;
            smtc.IsPlayEnabled = true;
            smtc.IsPauseEnabled = true;
            smtc.IsNextEnabled = true;
            smtc.IsPreviousEnabled = true;
            smtc.PlaybackStatus = MediaPlaybackStatus.Closed;
            Log("SMTC creado");

            smtc.ButtonPressed += (s, a) =>
            {
                string? cmd = a.Button switch
                {
                    SystemMediaTransportControlsButton.Play => "play",
                    SystemMediaTransportControlsButton.Pause => "pause",
                    SystemMediaTransportControlsButton.Next => "next",
                    SystemMediaTransportControlsButton.Previous => "previous",
                    _ => null
                };
                Log("SMTC boton: " + a.Button + " -> " + (cmd ?? "(ignorado)"));
                if (cmd != null) { SendToPage(cmd); }
            };

            smtc.PlaybackPositionChangeRequested += (s, a) =>
            {
                Log("SMTC seek: " + a.RequestedPlaybackPosition);
                SendToPage("seek:" + (long)a.RequestedPlaybackPosition.TotalMilliseconds);
            };
        }
        catch (Exception ex) { Log("SMTC error: " + ex); }
    }

    void SendToPage(string msg)
    {
        BeginInvoke(() =>
        {
            try { web.CoreWebView2?.PostWebMessageAsString(msg); Log("enviado a la pagina: " + msg); }
            catch (Exception ex) { Log("error enviando a la pagina: " + ex.Message); }
        });
    }

    void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.TryGetWebMessageAsString());
            var m = doc.RootElement;
            switch (m.GetProperty("type").GetString())
            {
                case "state": UpdateSmtc(m); break;
                case "log": Log("PAGINA " + m.GetProperty("msg").GetString()); break;
                case "quit": Close(); Application.Exit(); break;
            }
        }
        catch { }
    }

    void UpdateSmtc(JsonElement m)
    {
        if (smtc == null) { return; }

        bool paused = m.GetProperty("paused").GetBoolean();
        long pos = m.GetProperty("position").GetInt64();
        long dur = m.GetProperty("duration").GetInt64();
        string track = m.GetProperty("uri").GetString() ?? "";

        if (track != lastTrack)
        {
            lastTrack = track;
            var u = smtc.DisplayUpdater;
            u.Type = MediaPlaybackType.Music;
            u.MusicProperties.Title = m.GetProperty("title").GetString() ?? "";
            u.MusicProperties.Artist = m.GetProperty("artist").GetString() ?? "";
            u.MusicProperties.AlbumTitle = m.GetProperty("album").GetString() ?? "";
            u.Update();
            Log("cancion: " + u.MusicProperties.Title);
            _ = SetThumbnailAsync(m.GetProperty("art").GetString() ?? "", track);
        }

        smtc.PlaybackStatus = paused ? MediaPlaybackStatus.Paused : MediaPlaybackStatus.Playing;
        if (lastPaused != paused) { lastPaused = paused; Log("estado: " + (paused ? "pausa" : "reproduciendo")); }

        var tl = new SystemMediaTransportControlsTimelineProperties
        {
            StartTime = TimeSpan.Zero,
            MinSeekTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromMilliseconds(dur),
            MaxSeekTime = TimeSpan.FromMilliseconds(dur),
            Position = TimeSpan.FromMilliseconds(Math.Min(pos, dur))
        };
        smtc.UpdateTimelineProperties(tl);
    }

    // Descarga la carátula y la guarda en memoria: así Windows no depende de una URL que puede caducar.
    async Task SetThumbnailAsync(string url, string track)
    {
        if (smtc == null || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) { return; }
        try
        {
            byte[] bytes = await http.GetByteArrayAsync(uri);
            if (track != lastTrack) { return; }   // ya cambió la canción
            var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                writer.DetachStream();
            }
            stream.Seek(0);
            thumbStream?.Dispose();
            thumbStream = stream;
            var u = smtc.DisplayUpdater;
            u.Thumbnail = RandomAccessStreamReference.CreateFromStream(stream);
            u.Update();
            Log("caratula lista (" + bytes.Length + " bytes)");
        }
        catch (Exception ex) { Log("error con la caratula: " + ex.Message); }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        try
        {
            if (smtc != null)
            {
                smtc.PlaybackStatus = MediaPlaybackStatus.Closed;
                smtc.IsEnabled = false;
            }
        }
        catch { }
        base.OnFormClosed(e);
    }
}
