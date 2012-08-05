#region Copyright (C) 2005-2012 Team MediaPortal

// Copyright (C) 2005-2012 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

#region usings

using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using MediaPortal;
using MediaPortal.Common.Utils;
using MediaPortal.Configuration;
using MediaPortal.Database;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.InputDevices;
using MediaPortal.IR;
using MediaPortal.Player;
using MediaPortal.Profile;
using MediaPortal.RedEyeIR;
using MediaPortal.Ripper;
using MediaPortal.SerialIR;
using MediaPortal.Util;
using MediaPortal.Services;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.Win32;
using Action = MediaPortal.GUI.Library.Action;
using Timer = System.Timers.Timer;

#endregion

namespace MediaPortal
{
  // ReSharper disable InconsistentNaming
  public enum EXECUTION_STATE : uint

  public enum EXECUTION_STATE : uint
    ES_SYSTEM_REQUIRED  = 0x00000001,
    ES_SYSTEM_REQUIRED = 0x00000001,
    ES_CONTINUOUS       = 0x80000000
    ES_CONTINUOUS = 0x80000000
}
// ReSharper restore InconsistentNaming
}

public class MediaPortalApp : D3DApp, IRender
{
  #region vars

#if AUTOUPDATE
  private ApplicationUpdateManager _updater = null;
  private Thread _updaterThread = null;
  private const int UPDATERTHREAD_JOIN_TIMEOUT = 3 * 1000;
  private int _lastMousePositionX;
  private int _lastMousePositionY;
  private bool _playingState;
  private bool _showStats;
  private bool _showStatsPrevious;
  private readonly Rectangle[] _region;
  private int _xpos;
  private int _frameCount;
  private SerialUIR _serialuirdevice;
  private USBUIRT _usbuirtdevice;
  private WinLirc _winlircdevice;
  private RedEye _redeyedevice;
  private readonly bool _useScreenSaver;
  private readonly bool _useIdleblankScreen;
  private static bool _isWinScreenSaverInUse;
  private readonly int _timeScreenSaver;
  private bool _restoreTopMost;
  private bool _startWithBasicHome;
  private bool _useOnlyOneHome;
  private bool _suspended;
  private bool _runAutomaticResume;
  private bool _ignoreContextMenuAction;
  private DateTime _lastContextMenuAction;
  private bool _onResumeRunning;
  private bool _onResumeAutomaticRunning;
  protected string DateFormat;
  protected bool UseLongDateFormat;
  private readonly bool _showLastActiveModule;
  private readonly int _suspendGracePeriodSec;
  private static bool _mpCrashed;
  private static int _startupDelay;
  private static bool _waitForTvServer;
  private static bool _waitForTvServer = false;
  private static DateTime _lastOnresume = DateTime.Now;
  private static string _alternateConfig = string.Empty;
  private static string _safePluginsList;
#if AUTOUPDATE
  string m_strNewVersion = "";
  bool m_bNewVersionAvailable = false;
  bool m_bCancelVersion = false;
  private MouseEventArgs _lastMouseClickEvent;
  private Timer _mouseClickTimer;
  private bool _mouseClickFired;
  // ReSharper disable InconsistentNaming
  private const int WM_SYSCOMMAND             = 0x0112;
  private const int WM_POWERBROADCAST         = 0x0218;
  private const int WM_ENDSESSION             = 0x0016;
  private const int WM_DEVICECHANGE           = 0x0219;
  private const int WM_QUERYENDSESSION        = 0x0011;
  private const int WM_NULL                   = 0x0000;
  private const int WM_ACTIVATE               = 0x0006; // http://msdn.microsoft.com/en-us/library/windows/desktop/ms646274(v=vs.85).aspx
  private const int WM_ACTIVATEAPP            = 0x001C; // http://msdn.microsoft.com/en-us/library/windows/desktop/ms632614(v=vs.85).aspx
  private const int PBT_APMQUERYSUSPEND       = 0x0000;
  private const int PBT_APMQUERYSTANDBY       = 0x0001;
  private const int PBT_APMQUERYSTANDBY = 0x0001;
  private const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
  private const int PBT_APMSUSPEND            = 0x0004;
  private const int PBT_APMSTANDBY            = 0x0005;
  private const int PBT_APMRESUMECRITICAL     = 0x0006;
  private const int PBT_APMRESUMESUSPEND      = 0x0007;
  private const int PBT_APMRESUMESTANDBY      = 0x0008;
  private const int PBT_APMRESUMEAUTOMATIC    = 0x0012;
  private const int SC_SCREENSAVE             = 0xF140;
  private const int SC_MONITORPOWER           = 0xF170;
  private const int SPI_SETSCREENSAVEACTIVE   = 17;
  private const int SPI_GETSCREENSAVEACTIVE   = 16;
  private const int SPIF_SENDWININICHANGE     = 0x0002;
  private const int GPU_HUNG                  = -2005530508;
  private const int D3DERR_INVALIDCALL        = -2005530516;
  private const int GPU_REMOVED               = -2005530512;
  // ReSharper restore InconsistentNaming
  private const string Mutex = "{E0151CBA-7F81-41df-9849-F5298A779EB3}";
  private bool _supportsFiltering;
  private bool _supportsAlphaBlend;
  private int _anisotropy;
  private DateTime _updateTimer = DateTime.MinValue;
  private static SplashScreen _splashScreen;
  private static SplashScreen splashScreen;
#if !DEBUG
  private static bool _avoidVersionChecking;
#endif
  private string _outdatedSkinName;
  private int _lastMessage = WM_NULL;
  private string _OutdatedSkinName = null;

  #endregion

  [DllImport("user32")]
  private static extern bool SystemParametersInfo(int uAction, int uParam, ref bool lpvParam, int fuWinIni);

  [DllImport("user32")]
  private static extern bool SystemParametersInfo(int uAction, int uParam, int lpvParam, int fuWinIni);

  [DllImport("Kernel32.DLL")]
  private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE state);
  private static RestartOptions _restartOptions = RestartOptions.Reboot;
  private static bool _useRestartOptions;
  private static bool useRestartOptions = false;

  private static extern bool PathIsNetworkPath(string path);
  private static extern bool PathIsNetworkPath(string Path);

  #region main()
  //NProf doesn't work if the [STAThread] attribute is set
  //NProf doesnt work if the [STAThread] attribute is set
  //but is needed when you want to play music or video
  [STAThread]
  public static void Main(string[] args)
  {
    Thread.CurrentThread.Name = "MPMain";
    if (args.Length > 0)
      foreach (var arg in args)
      foreach (string arg in args)
      {
        if (arg == "/fullscreen")
          FullscreenOverride = true;
          _fullscreenOverride = true;
        }
        if (arg == "/windowed")
          WindowedOverride = true;
          _windowedOverride = true;
        }
        if (arg.StartsWith("/fullscreen="))
          var argValue = arg.Remove(0, 12); // remove /?= from the argument  
          FullscreenOverride |= argValue != "no";
          WindowedOverride |= argValue.Equals("no");
          _windowedOverride |= argValue.Equals("no");
        }
        if (arg == "/crashtest")
        {
          _mpCrashed = true;
        }
        if (arg.StartsWith("/screen="))
        {
          var screenarg = arg.Remove(0, 8); // remove /?= from the argument          
          if (!int.TryParse(screenarg, out ScreenNumberOverride))
          if (!int.TryParse(screenarg, out _screenNumberOverride))
            ScreenNumberOverride = -1;
            _screenNumberOverride = -1;
          }
        }
        if (arg.StartsWith("/skin="))
          var skinOverrideArg = arg.Remove(0, 6); // remove /?= from the argument
          StrSkinOverride = skinOverrideArg;
          _strSkinOverride = skinOverrideArg;
        }
        if (arg.StartsWith("/config="))
        {
          _alternateConfig = arg.Remove(0, 8); // remove /?= from the argument
          if (!Path.IsPathRooted(_alternateConfig))
          {
            _alternateConfig = Config.GetFile(Config.Dir.Config, _alternateConfig);
          }
        }
        if (arg.StartsWith("/safelist="))
        {
          _safePluginsList = arg.Remove(0, 10); // remove /?= from the argument
        }

#if !DEBUG
        _avoidVersionChecking = false;
        if (arg.ToLowerInvariant() == "/avoidversioncheck")
        {
          _avoidVersionChecking = true;
          Log.Warn("Version check is disabled by command line switch \"/avoidVersionCheck\"");
        }
#endif
      }
    }

    if (string.IsNullOrEmpty(_alternateConfig))
    {
      Log.BackupLogFiles();
    }
    else
    {
      if (File.Exists(_alternateConfig))
      {
        try
        {
          MPSettings.ConfigPathName = _alternateConfig;
          Log.BackupLogFiles();
          Log.Info("Using alternate configuration file: {0}", MPSettings.ConfigPathName);
        }
        catch (Exception ex)
        {
          Log.BackupLogFiles();
          Log.Error("Failed to change to alternate configuration file:");
          Log.Error(ex);
        }
      }
      else
      {
        Log.BackupLogFiles();
        Log.Info("Alternative configuration file was specified but the file was not found: '{0}'", _alternateConfig);
        Log.Info("Using default configuration file instead.");
      }
    }

    if (!Config.DirsFileUpdateDetected)
      //check if Mediaportal has been configured
      var fi = new FileInfo(MPSettings.ConfigPathName);
      FileInfo fi = new FileInfo(MPSettings.ConfigPathName);
      if (!File.Exists(MPSettings.ConfigPathName) || (fi.Length < 10000))
      {
        //no, then start configuration.exe in wizard form
        Log.Info("MediaPortal.xml not found. Launching configuration tool and exiting...");
        try
        {
          Process.Start(Config.GetFile(Config.Dir.Base, "configuration.exe"), @"/wizard");
        }
        catch {} // no exception logging needed, since MP is now closed
        return;
      }
      bool autoHideTaskbar;
#if !DEBUG
      bool watchdogEnabled;
      bool restartOnError;
      int restartDelay;
#endif
      int restartDelay = 10;
        var treadPriority = xmlreader.GetValueAsString("general", "ThreadPriority", "Normal");
        switch (treadPriority)
        {
          case "AboveNormal":
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
            break;
          case "High":
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            break;
          case "BelowNormal":
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            break;
          Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
          Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        autoHideTaskbar = xmlreader.GetValueAsBool("general", "hidetaskbar", false);
        _waitForTvServer = xmlreader.GetValueAsBool("general", "wait for tvserver", false);
#if !DEBUG
        watchdogEnabled = xmlreader.GetValueAsBool("general", "watchdogEnabled", true);
        restartOnError = xmlreader.GetValueAsBool("general", "restartOnError", false);
        restartDelay = xmlreader.GetValueAsInt("general", "restart delay", 10);
#endif
        restartOnError = xmlreader.GetValueAsBool("general", "restartOnError", false);
        restartDelay = xmlreader.GetValueAsInt("general", "restart delay", 10);        

        GUIGraphicsContext._useScreenSelector |= xmlreader.GetValueAsBool("screenselector", "usescreenselector", false);
      }
#if !DEBUG
        // BAV: fixing mantis 1216: Watcher process uses a wrong folder for integrity file
        using (var sw = new StreamWriter(Config.GetFile(Config.Dir.Config, "mediaportal.running"), false))
      {
        //StreamWriter sw = new StreamWriter(Application.StartupPath + "\\mediaportal.running", false);
        // BAV: fixing mantis bug 1216: Watcher process uses a wrong folder for integrity file
        using (StreamWriter sw = new StreamWriter(Config.GetFile(Config.Dir.Config, "mediaportal.running"), false))
        {
        var cmdargs = "-watchdog";
          sw.Close();
        }
          cmdargs += " -restartMP " + restartDelay;
        string cmdargs = "-watchdog";
        var mpWatchDog = new Process
                           {
                             StartInfo =
                               {
                                 ErrorDialog = true,
                                 UseShellExecute = true,
                                 WorkingDirectory = Application.StartupPath,
                                 FileName = "WatchDog.exe",
                                 Arguments = cmdargs
                               }
                           };
        mpWatchDog.StartInfo.UseShellExecute = true;
        mpWatchDog.StartInfo.WorkingDirectory = Application.StartupPath;
        mpWatchDog.StartInfo.FileName = "WatchDog.exe";
      var versionInfo = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);
        mpWatchDog.Start();
      }
#endif
      //Log MediaPortal version build and operating system level
      FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);

      Log.Info("Main: MediaPortal v" + versionInfo.FileVersion + " is starting up on " +
               OSInfo.OSInfo.GetOSDisplayVersion());
#if DEBUG
      Log.Info("Debug build: " + Application.ProductVersion);
#else
      Log.Info("Build: " + Application.ProductVersion);
#endif
      var lastSuccessTime = "NEVER !!!";
      UIntPtr res;
      OSPrerequisites.OSPrerequisites.OsCheck(false);
      var options = Convert.ToInt32(Reg.RegistryRights.ReadKey);
      //Log last install of WindowsUpdate patches
      string LastSuccessTime = "NEVER !!!";
      UIntPtr res = UIntPtr.Zero;

      var rKey = new UIntPtr(Convert.ToUInt32(Reg.RegistryRoot.HKLM));
      int lastError;
      var retval = Reg.RegOpenKeyEx(rKey,
        options = options | Convert.ToInt32(Reg.RegWow64Options.KEY_WOW64_64KEY);
      }
      UIntPtr rKey = new UIntPtr(Convert.ToUInt32(Reg.RegistryRoot.HKLM));
      int lastError = 0;
      int retval = Reg.RegOpenKeyEx(rKey,
                                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\Results\\Install",
        var sKey = new System.Text.StringBuilder((int)lKey);
      if (retval == 0)
      {
        uint tKey;
          lastSuccessTime = sKey.ToString();
        System.Text.StringBuilder sKey = new System.Text.StringBuilder((int)lKey);
        retval = Reg.RegQueryValueEx(res, "LastSuccessTime", 0, out tKey, sKey, ref lKey);
        if (retval == 0)
        {
          LastSuccessTime = sKey.ToString();
        }
        else
        {
          lastError = Marshal.GetLastWin32Error();
          Log.Debug("RegQueryValueEx retval=<{0}>, lastError=<{1}>", retval, lastError);
        }
      }
      Log.Info("Main: Last install from WindowsUpdate is dated {0}", lastSuccessTime);
      {
        lastError = Marshal.GetLastWin32Error();
        Log.Debug("RegOpenKeyEx retval=<{0}>, lastError=<{1}>", retval, lastError);
      }
      Log.Info("Main: Last install from WindowsUpdate is dated {0}", LastSuccessTime);

      //Disable "ghosting" for WindowsVista and up
      if (OSInfo.OSInfo.VistaOrLater())
      {
        Log.Debug("Disabling process window ghosting");
        NativeMethods.DisableProcessWindowsGhosting();
      }

      //Start MediaPortal
      var mpFi = new FileInfo(Assembly.GetExecutingAssembly().Location);
      foreach (Config.Dir option in Enum.GetValues(typeof (Config.Dir)))
      using (var processLock = new ProcessLock(Mutex))
        Log.Info("{0} - {1}", option, Config.GetFolder(option));
      }
      FileInfo mpFi = new FileInfo(Assembly.GetExecutingAssembly().Location);
      Log.Info("Main: Assembly creation time: {0} (UTC)", mpFi.LastWriteTimeUtc.ToUniversalTime());
      using (ProcessLock processLock = new ProcessLock(mpMutex))
      {
        if (processLock.AlreadyExists)
        {
          Log.Warn("Main: MediaPortal is already running");
        var applicationPath = Application.ExecutablePath;
        }
        Application.EnableVisualStyles();
        if (!string.IsNullOrEmpty(applicationPath))
        {
          Directory.SetCurrentDirectory(applicationPath);
          Log.Info("Main: Set current directory to: {0}", applicationPath);
        }
        //Localization strings for new splash screen and for MediaPortal itself
        applicationPath = Path.GetDirectoryName(applicationPath);
        Directory.SetCurrentDirectory(applicationPath);
        // Initialize the skin and theme prior to beginning the splash screen thread. This provides for the splash screen to be used in a theme.
        string strSkin;
        //Localization strings for new splashscreen and for MediaPortal itself
        LoadLanguageString();

        // Initialize the skin and theme prior to beginning the splash screen thread.  This provides for the splash screen to be used in a theme.
            strSkin = StrSkinOverride.Length > 0 ? StrSkinOverride : xmlreader.GetValueAsString("skin", "name", "Default");
        try
        {
          using (Settings xmlreader = new MPSettings())
          {
            strSkin = _strSkinOverride.Length > 0 ? _strSkinOverride : xmlreader.GetValueAsString("skin", "name", "Default");
          }
        }
        catch (Exception)
        {
          strSkin = "Default";
        }
        var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SKIN_CHANGED, 0, 0, 0, 0, 0, null);
        GUIGraphicsContext.Skin = strSkin;
        SkinSettings.Load();

        // Send a message that the skin has changed.
//#if !DEBUG
        var version = ConfigurationManager.AppSettings["version"];
        _splashScreen = new SplashScreen {Version = version};
        _splashScreen.Run();
//#endif
        splashScreen = new SplashScreen();
        splashScreen.Version = version;
        splashScreen.Run();
        //clientInfo=null;
          ServiceController ctrl;
        Application.DoEvents();
        if (_waitForTvServer)
        {
          ServiceController ctrl = null;
          try
          {
            ctrl = new ServiceController("TVService");
            string name = ctrl.ServiceName;
          }
          catch (Exception)
          {
            ctrl = null;
            if (_splashScreen != null)
          }
              _splashScreen.SetInformation(GUILocalizeStrings.Get(60)); // Waiting for startup of TV service...
          {
            Log.Debug("Main: TV service found. Checking status...");
            if (splashScreen != null)
            {
              splashScreen.SetInformation(GUILocalizeStrings.Get(60)); // Waiting for startup of TV service...
            }
            if (ctrl.Status == ServiceControllerStatus.StartPending || ctrl.Status == ServiceControllerStatus.Stopped)
            {
              if (ctrl.Status == ServiceControllerStatus.StartPending)
              {
                Log.Info("Main: TV service start is pending. Waiting...");
              }
              if (ctrl.Status == ServiceControllerStatus.Stopped)
              {
                Log.Info("Main: TV service is stopped, so we try start it...");
                try
                {
                  ctrl.Start();
                }
                catch (Exception)
                {
                  Log.Info("TvService seems to be already starting up.");
                }
              }
              try
              {
                ctrl.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 45));
              }
              catch (Exception) {}
              if (ctrl.Status == ServiceControllerStatus.Running)
              {
                Log.Info("Main: The TV service has started successfully.");
              }
              else
              {
                Log.Info("Main: Startup of the TV service failed - current status: {0}", ctrl.Status.ToString());
              }
            }
            Log.Info("Main: TV service is in status {0} - proceeding...", ctrl.Status.ToString());
            ctrl.Close();
          }
          for (var i = _startupDelay; i > 0; i--)
        Application.DoEvents();
            if (_splashScreen != null)
        {
              _splashScreen.SetInformation(String.Format(GUILocalizeStrings.Get(61), i));
          {
            if (splashScreen != null)
            {
              splashScreen.SetInformation(String.Format(GUILocalizeStrings.Get(61), i.ToString()));
              // Waiting {0} second(s) before startup...
            }
            Application.DoEvents();
            Thread.Sleep(1000);
        }
        Log.Debug("Main: Checking prerequisites");
        try
            var strLine = "Please install a newer DirectX 9.0c redist!\r\n";
          // CHECK if DirectX 9.0c if installed
          Log.Debug("Main: Verifying DirectX 9");
          if (!DirectXCheck.IsInstalled())
            if (_splashScreen != null)
            string strLine = "Please install a newer DirectX 9.0c redist!\r\n";
              _splashScreen.Stop();
              _splashScreen = null;
#if !DEBUG
            if (splashScreen != null)
            {
              splashScreen.Stop();
              splashScreen = null;
            }
#endif
            MessageBox.Show(strLine, "MediaPortal", MessageBoxButtons.OK, MessageBoxIcon.Error);
          const string mediaPlayerVersion = "11";
          }
          Application.DoEvents();

          if (FilterChecker.CheckFileVersion(Environment.SystemDirectory + "\\wmp.dll", mediaPlayerVersion + ".0.0000.0000",
          string WMP_Main_Ver = "11";
          Log.Debug("Main: Verifying Windows Media Player");

          Version aParamVersion;
          if (FilterChecker.CheckFileVersion(Environment.SystemDirectory + "\\wmp.dll", WMP_Main_Ver + ".0.0000.0000",
                                             out aParamVersion))
          {
            if (_splashScreen != null)
          }
              _splashScreen.Stop();
              _splashScreen = null;
#if !DEBUG
            if (splashScreen != null)
            var strLine = "Please install Windows Media Player " + mediaPlayerVersion + "\r\n";
               strLine += "MediaPortal cannot run without Windows Media Player " + mediaPlayerVersion;
              splashScreen = null;
#endif
            string strLine = "Please install Windows Media Player " + WMP_Main_Ver + "\r\n";
            strLine = strLine + "MediaPortal cannot run without Windows Media Player " + WMP_Main_Ver;
          // Check TvPlugin version
          var mpExe = Assembly.GetExecutingAssembly().Location;
          var tvPlugin = Config.GetFolder(Config.Dir.Plugins) + "\\Windows\\TvPlugin.dll";

#if !DEBUG
            var tvPluginVersion = FileVersionInfo.GetVersionInfo(tvPlugin).ProductVersion;
            var mpVersion = FileVersionInfo.GetVersionInfo(mpExe).ProductVersion;
            if (mpVersion != tvPluginVersion)
          if (File.Exists(tvPlugin) && !_avoidVersionChecking)
              var strLine = "TvPlugin and MediaPortal don't have the same version.\r\n";
                 strLine += "Please update the older component to the same version as the newer one.\r\n";
                 strLine += "MediaPortal Version: " + mpVersion + "\r\n";
                 strLine += "TvPlugin    Version: " + tvPluginVersion;
              if (_splashScreen != null)
              string strLine = "TvPlugin and MediaPortal don't have the same version.\r\n";
                _splashScreen.Stop();
                _splashScreen = null;
              strLine += "TvPlugin    Version: " + tvPluginVersion;
              if (splashScreen != null)
              {
                splashScreen.Stop();
                splashScreen = null;
              }
              MessageBox.Show(strLine, "MediaPortal", MessageBoxButtons.OK, MessageBoxIcon.Error);
              Log.Info(strLine);
              return;
        }
        catch (Exception) {}
        //following crashes on some pc's, dunno why
        //Log.Info("  Stop any known recording processes");
        //Utils.KillExternalTVProcesses();
        if (_splashScreen != null)
        try
          _splashScreen.SetInformation(GUILocalizeStrings.Get(62)); // Initializing DirectX...
#endif
        Application.DoEvents();
        var app = new MediaPortalApp();
        {
          splashScreen.SetInformation(GUILocalizeStrings.Get(62)); // Initializing DirectX...
        }

        MediaPortalApp app = new MediaPortalApp();
        Log.Debug("Main: Initializing DirectX");
          if (_splashScreen != null)
        {
            _splashScreen.SetInformation(GUILocalizeStrings.Get(63)); // Initializing input devices...
          Application.AddMessageFilter(filter);
          // Initialize Input Devices
          if (splashScreen != null)
          {
            splashScreen.SetInformation(GUILocalizeStrings.Get(63)); // Initializing input devices...
          }
          InputDevices.Init();
          try
          {
            //app.PreRun();
            Log.Info("Main: Running");
            GUIGraphicsContext.BlankScreen = false;
            Application.Run(app);
            app.Focus();
            Debug.WriteLine("after Application.Run");
          }
            //#if !DEBUG
          catch (Exception ex)
          {
            Log.Error(ex);
            Log.Error("MediaPortal stopped due to an exception {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace);
            _mpCrashed = true;
          }
            //#endif
          finally
          {
            Application.RemoveMessageFilter(filter);
          }
          app.OnExit();
        }
#if !DEBUG
        }
        catch (Exception ex)
        {
          Log.Error(ex);
        if (_splashScreen != null)
          _mpCrashed = true;
          _splashScreen.Stop();
          _splashScreen = null;
#if !DEBUG
        if (splashScreen != null)
        {
          splashScreen.Stop();
          splashScreen = null;
        }
          // only re-show the task bar if MP is the one that has hidden it.
          HideTaskBar(false);
        if (_useRestartOptions)
        {
          Log.Info("Main: Exiting Windows - {0}", _restartOptions);
          Win32API.EnableStartBar(true);
          Win32API.ShowStartBar(true);
        }
        if (useRestartOptions)
          WindowsController.ExitWindows(_restartOptions, false);
          Log.Info("Main: Exiting Windows - {0}", restartOptions);
          if (File.Exists(Config.GetFile(Config.Dir.Config, "mediaportal.running")))
          {
            File.Delete(Config.GetFile(Config.Dir.Config, "mediaportal.running"));
          }
          WindowsController.ExitWindows(restartOptions, false);
        }
        else
        {
          if (!_mpCrashed)
          {
            if (File.Exists(Config.GetFile(Config.Dir.Config, "mediaportal.running")))
            {
              File.Delete(Config.GetFile(Config.Dir.Config, "mediaportal.running"));
            }
      var msg = "The file MediaPortalDirs.xml has been changed by a recent update in the MediaPortal application directory.\n\n";
      msg     += "You have to open the file ";
      msg     += Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Team MediaPortal\MediaPortalDirs.xml";
      msg     += " with an editor, update it with all changes and SAVE it at least once to start up MediaPortal successfully after this update.\n\n";
      msg     += "If you are not using windows user profiles for MediaPortal's configuration management, ";
      msg     += "just delete the whole directory mentioned above and reconfigure MediaPortal.";
      const string msg2 = "\n\n\nDo you want to open your local file now?";
      msg += Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Team MediaPortal\MediaPortalDirs.xml";
      msg +=
      if (_splashScreen != null)
      msg += "If you are not using windows user profiles for MediaPortal's configuration management, ";
        _splashScreen.Stop();
        _splashScreen = null;
      Log.Error(msg);
#if !DEBUG
      var result = MessageBox.Show(msg + msg2, "MediaPortal - Update Conflict", MessageBoxButtons.YesNo,
      {
        splashScreen.Stop();
        splashScreen = null;
      }
#endif
      DialogResult result = MessageBox.Show(msg + msg2, "MediaPortal - Update Conflict", MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Stop);
      try
      {
        if (result == DialogResult.Yes)
        {
          Process.Start("notepad.exe",
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                        @"\Team MediaPortal\MediaPortalDirs.xml");
        }
      }
      catch (Exception)
      {
        MessageBox.Show(
          "Error opening file " + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
          @"\Team MediaPortal\MediaPortalDirs.xml using notepad.exe", "Error", MessageBoxButtons.OK,
          MessageBoxIcon.Error);
  private static UnhandledExceptionLogger _logger;

  /// <remark>This method is only used in release builds.</remark>
  }

    _logger = new UnhandledExceptionLogger();
    var current = AppDomain.CurrentDomain;
    current.UnhandledException += _logger.LogCrash;
  /// <remark>This method is only used in release builds.
  private static void AddExceptionHandler()
  {
    logger = new UnhandledExceptionLogger();
    AppDomain current = AppDomain.CurrentDomain;
    current.UnhandledException += new UnhandledExceptionEventHandler(logger.LogCrash);
  }
#endif

  #endregion

  #region remote callbacks

  private void OnRemoteCommand(object command)
  {
    GUIGraphicsContext.OnAction(new Action((Action.ActionType)command, 0, 0));
  }
  {
    _region = new Rectangle[1];
    _xpos = 50;
    _suspendGracePeriodSec = 5;
    UseLongDateFormat = false;
    DateFormat = string.Empty;
    _lastContextMenuAction = DateTime.MaxValue;
    _timeScreenSaver = 300;
    _useScreenSaver = true;
    var clientSizeX = 720;
    var clientSizeY = 576;
    var screenNumber = 0;

  public MediaPortalApp()
  {
    int clientSizeX = 720;
      _useScreenSaver = xmlreader.GetValueAsBool("general", "IdleTimer", true);
      _timeScreenSaver = xmlreader.GetValueAsInt("general", "IdleTimeValue", 300);
      _useIdleblankScreen = xmlreader.GetValueAsBool("general", "IdleBlanking", false);
    using (Settings xmlreader = new MPSettings())
    {
      _showLastActiveModule = xmlreader.GetValueAsBool("general", "showlastactivemodule", false);
      useIdleblankScreen = xmlreader.GetValueAsBool("general", "IdleBlanking", false);
      clientSizeX = xmlreader.GetValueAsInt("general", "sizex", clientSizeX);
      clientSizeY = xmlreader.GetValueAsInt("general", "sizey", clientSizeY);
      showLastActiveModule = xmlreader.GetValueAsBool("general", "showlastactivemodule", false);
      if (ScreenNumberOverride >= 0)
      lastActiveModuleFullscreen = xmlreader.GetValueAsBool("general", "lastactivemodulefullscreen", false);
        screenNumber = ScreenNumberOverride;
    }
    if (GUIGraphicsContext._useScreenSelector)
    {
      if (_screenNumberOverride >= 0)
      {
        screenNumber = _screenNumberOverride;
      }
      if (screenNumber < 0 || screenNumber >= Screen.AllScreens.Length)
      {
        screenNumber = 0;
      }
      Log.Info("currentScreenNr:" + screenNumber);
    MinimumSize = GUIGraphicsContext.currentScreen.Bounds.Width > clientSizeX ? new Size(clientSizeX + 8, clientSizeY + 27) : new Size(720, 576);
      MinimumSize = new Size(clientSizeX + 8, clientSizeY + 27);
    }
    else
    {
      MinimumSize = new Size(720, 576);
    }
    Text = "MediaPortal";
    GUIGraphicsContext.form = this;
        AutoHideMouse = xmlreader.GetValueAsBool("general", "autohidemouse", true);
    GUIGraphicsContext.RenderGUI = this;
        GUIGraphicsContext.AllowRememberLastFocusedItem = xmlreader.GetValueAsBool("gui","allowRememberLastFocusedItem", false);
        GUIGraphicsContext.DBLClickAsRightClick = xmlreader.GetValueAsBool("general", "dblclickasrightclick", false);
        MinimizeOnStartup = xmlreader.GetValueAsBool("general", "minimizeonstartup", false);
        MinimizeOnGuiExit = xmlreader.GetValueAsBool("general", "minimizeonexit", false);
        GUIGraphicsContext.AllowRememberLastFocusedItem = xmlreader.GetValueAsBool("gui",
                                                                                   "allowRememberLastFocusedItem", false);
        GUIGraphicsContext.DBLClickAsRightClick =
          xmlreader.GetValueAsBool("general", "dblclickasrightclick", false);
        _minimizeOnStartup = xmlreader.GetValueAsBool("general", "minimizeonstartup", false);
        _minimizeOnGuiExit = xmlreader.GetValueAsBool("general", "minimizeonexit", false);
      }
    }
    catch (Exception)
    {
    Activated += MediaPortalAppActivated;
    Deactivate += MediaPortalAppDeactivate;
    SetStyle(ControlStyles.Opaque, true);
    SetStyle(ControlStyles.UserPaint, true);
    SetStyle(ControlStyles.AllPaintingInWmPaint, true);
 }

  public override sealed Size MinimumSize
  {
    get
    {
      return base.MinimumSize;
    }
    set
    {
      base.MinimumSize = value;
    }
  }

  private static void MediaPortalAppDeactivate(object sender, EventArgs e)
    DoStartupJobs();
    //    startThread.Priority = ThreadPriority.BelowNormal;
    //    startThread.Start();
  }
  private static void MediaPortalAppActivated(object sender, EventArgs e)
  private static void MediaPortalApp_Deactivate(object sender, EventArgs e)
  {
    GUIGraphicsContext.HasFocus = false;
  }

  private static void MediaPortalApp_Activated(object sender, EventArgs e)
  {
    GUIGraphicsContext.HasFocus = true;
  }

  #endregion

  #region RenderStats() method

  private void RenderStats()
  {
        if (_showStats != _showStatsPrevious)
    {
      UpdateStats();

      if (GUIGraphicsContext.IsEvr && g_Player.HasVideo)
            VMR9Util.g_vmr9.EnableEVRStatsDrawing(_showStats);
        if (m_bShowStats != m_bShowStatsPrevious)
        {
          // notify EVR presenter only when the setting changes
          if (VMR9Util.g_vmr9 != null)
          {
            VMR9Util.g_vmr9.EnableEVRStatsDrawing(m_bShowStats);
        // EVR presenter will draw the stats internally
        _showStatsPrevious = _showStats;
          {
      }
      _showStatsPrevious = false;

      if (_showStats)
        }
      {
        var font = GUIFontManager.GetFont(0);
      }

      if (m_bShowStats)
          // '\n' doesn't work with the DirectX9 Ex device, so the string is split into two
          font.DrawText(80, 40, 0xffffffff, FrameStatsLine1, GUIControl.Alignment.ALIGN_LEFT, -1);
          font.DrawText(80, 55, 0xffffffff, FrameStatsLine2, GUIControl.Alignment.ALIGN_LEFT, -1);
          _region[0].X = _xpos;
          _region[0].Y = 0;
          _region[0].Width = 4;
          _region[0].Height = GUIGraphicsContext.Height;
          GUIGraphicsContext.DX9Device.Clear(ClearFlags.Target, Color.FromArgb(255, 255, 255, 255), 1.0f, 0, _region);
          font.DrawText(80, 55, 0xffffffff, frameStatsLine2, GUIControl.Alignment.ALIGN_LEFT, -1);
          region[0].X = m_ixpos;
          region[0].Y = 0;
          _frameCount++;
          if (_frameCount >= (int)fStep)
          GUIGraphicsContext.DX9Device.Clear(ClearFlags.Target, Color.FromArgb(255, 255, 255, 255), 1.0f, 0, region);
            _frameCount = 0;
            _xpos += 12;
            if (_xpos > GUIGraphicsContext.Width - 50)
          m_iFrameCount++;
              _xpos = 50;
          {
            m_iFrameCount = 0;
            m_ixpos += 12;
            if (m_ixpos > GUIGraphicsContext.Width - 50)
            {
              m_ixpos = 50;
            }
          }
        }
      }
    }
    catch 
    {
      // Intentionally left blank - if stats rendering fails it is not a critical issue
    }
  }

    try
    {
      switch (msg.Msg)

        case WM_POWERBROADCAST:
          Log.Info("Main: WM_POWERBROADCAST: {0}", msg.WParam.ToInt32());
          switch (msg.WParam.ToInt32())
          {
    {
      if (msg.Msg == WM_POWERBROADCAST)
      {
            case PBT_APMQUERYSUSPEND:
              Log.Info("Main: Windows is requesting hibernate mode - UI bit: {0}", msg.LParam.ToInt32());
              break;
            //The PBT_APMQUERYSUSPEND message is sent to request permission to suspend the computer.
            //An application that grants permission should carry out preparations for the suspension before returning.
            //Return TRUE to grant the request to suspend. To deny the request, return BROADCAST_QUERY_DENY.
          case PBT_APMQUERYSUSPEND:
            case PBT_APMQUERYSTANDBY:
              // Stop all media before suspending or hibernating
              Log.Info("Main: Windows is requesting standby mode - UI bit: {0}", msg.LParam.ToInt32());
              break;
            //An application that grants permission should carry out preparations for the suspension before returning.
            //Return TRUE to grant the request to suspend. To deny the request, return BROADCAST_QUERY_DENY.
          case PBT_APMQUERYSTANDBY:
            case PBT_APMQUERYSUSPENDFAILED:
              Log.Info("Main: Windows is denied to go to suspended mode");
              // dero: IT IS NOT SAFE to rely on this message being sent! Sometimes it is not sent even if we
              // processed PBT_AMQUERYSUSPEND/PBT_APMQUERYSTANDBY
              // I observed this using TVService.PowerScheduler
              break;
          case PBT_APMQUERYSUSPENDFAILED:
            Log.Info("Main: Windows is denied to go to suspended mode");
            // dero: IT IS NOT SAFE to rely on this message being sent! Sometimes it is not sent even if we
            case PBT_APMQUERYSTANDBYFAILED:
              Log.Info("Main: Windows is denied to go to standby mode");
              // dero: IT IS NOT SAFE to rely on this message being sent! Sometimes it is not sent even if we
              // processed PBT_AMQUERYSUSPEND/PBT_APMQUERYSTANDBY
              // I observed this using TVService.PowerScheduler
              break;
          case PBT_APMQUERYSTANDBYFAILED:
            case PBT_APMSTANDBY:
              Log.Info("Main: Windows is going to standby");
              OnSuspend();
              break;
            break;
            case PBT_APMSUSPEND:
              Log.Info("Main: Windows is suspending");
              OnSuspend();
              break;
            break;

          case PBT_APMSUSPEND:
            Log.Info("Main: Windows is suspending");
            case PBT_APMRESUMECRITICAL:
              Log.Info("Main: Windows has resumed from critical hibernate mode");
              OnResume();
              break;
            //this event can indicate that some or all applications did not receive a PBT_APMSUSPEND event. 
            //For example, this event can be broadcast after a critical suspension caused by a failing battery.
            case PBT_APMRESUMESUSPEND:
              Log.Info("Main: Windows has resumed from hibernate mode");
              OnResume();
              break;

            //The PBT_APMRESUMESUSPEND event is broadcast as a notification that the system has resumed operation after being suspended.
            case PBT_APMRESUMESTANDBY:
              Log.Info("Main: Windows has resumed from standby mode");
              OnResume();
              break;

            //The PBT_APMRESUMESTANDBY event is broadcast as a notification that the system has resumed operation after being standby.
          case PBT_APMRESUMESTANDBY:
            case PBT_APMRESUMEAUTOMATIC:
              Log.Info("Main: Windows has resumed from standby or hibernate mode to handle a requested event");
              OnResumeAutomatic();

              using (Settings xmlreader = new MPSettings())
              {
                var useS3Hack = xmlreader.GetValueAsBool("debug", "useS3Hack", false);
                if (useS3Hack)
                {
                  Log.Info("Main: useS3Hack enabled, calling OnResume() on automatic resume");
                  OnResume();
                }
              }

              break;
          }
          _lastMessage = msg.Msg;
          break;
        case WM_QUERYENDSESSION:
          Log.Info("Main: Windows is requesting shutdown mode");
          base.WndProc(ref msg);
          Log.Info("Main: shutdown mode granted");
          ShuttingDown = true;
          msg.Result = (IntPtr)1; // tell windows we are ready to shutdown
          _lastMessage = msg.Msg;
          break;
        // http://mantis.team-mediaportal.com/view.php?id=1073
        case WM_ENDSESSION:
          base.WndProc(ref msg);
          Log.Info("Main: shutdown mode executed");
          msg.Result = IntPtr.Zero; // tell windows it's OK to shutdown        
          _mouseClickTimer.Stop();
          _mouseClickTimer.Dispose();
          Application.ExitThread();
          Application.Exit();
          _lastMessage = msg.Msg;
          break;
        case WM_DEVICECHANGE:
          if (RemovableDriveHelper.HandleDeviceChangedMessage(msg))
          {
            return;
          }
          _lastMessage = msg.Msg;
          break;
        case WM_ACTIVATEAPP:
          if (Ready && _lastMessage == WM_ACTIVATE)
          {
            var activate = (((int) msg.WParam != 0));
            if (activate)
            {
              RestoreFromTray();
            }
            else
            {
              MinimizeToTray();
            }
          }
          _lastMessage = msg.Msg;
          break;
        default:
          _lastMessage = msg.Msg;
          break;
      
      // don't continue if activating or deactivating app
      if (_lastMessage == WM_ACTIVATEAPP)
      {
          g_Player.WndProc(ref msg);
          base.WndProc(ref msg);
          return;
      
      }
      else if (msg.Msg == WM_DEVICECHANGE)
      {
        if (RemovableDriveHelper.HandleDeviceChangedMessage(msg))
      }

      Action action;
      char key;
      Keys keyCode;
      if (InputDevices.WndProc(ref msg, out action, out key, out keyCode))
        }
        if (msg.Result.ToInt32() != 1)
        return;
          msg.Result = new IntPtr(0);
        }
        if (action != null && action.wID != Action.ActionType.ACTION_INVALID)
        {
          Log.Info("Main: Incoming action: {0}", action.wID);
          if (ActionTranslator.GetActionDetail(GUIWindowManager.ActiveWindowEx, action))
      else
            if (action.SoundFileName.Length > 0 && !g_Player.Playing)
          if (msg.Result.ToInt32() != 1)
              Utils.PlaySound(action.SoundFileName, false, true);
          {
            {
          GUIGraphicsContext.OnAction(action);
          GUIGraphicsContext.ResetLastActivity();
        }
        if (keyCode != Keys.A)
        {
          Log.Info("Main: Incoming Keycode: {0}", keyCode.ToString());
          var ke = new KeyEventArgs(keyCode);
          KeyDownEvent(ke);
          }
          if (key != 0)
        if (key != 0)
        {
          Log.Info("Main: Incoming Key: {0}", key);
          var e = new KeyPressEventArgs(key);
          KeyPressEvent(e);
          return;
        }
        return;
      }

            KeyPressEventArgs e = new KeyPressEventArgs(key);
            keypressed(e);
        // windows wants to activate the screen saver
          }
          return;
        }
      }

      // plugins menu clicked?
      if (msg.Msg == WM_SYSCOMMAND && (msg.WParam.ToInt32() == SC_SCREENSAVE || msg.WParam.ToInt32() == SC_MONITORPOWER))
      {
        // windows wants to activate the screensaver
        if ((GUIGraphicsContext.IsFullScreenVideo && !g_Player.Paused) ||
            GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_SLIDESHOW)
        {
          //disable it when we're watching tv/movies/...
          msg.Result = new IntPtr(0);
          return;
        }
      }
      //if (msg.Msg==WM_KEYDOWN) Debug.WriteLine("msg keydown");
  private static readonly object SyncObj = new object();
      base.WndProc(ref msg);
    }
  // since local DBs have no problems.
    {
      Log.Error(ex);
    var dbPath = FolderSettings.DatabaseName;
    var isRemotePath = (string.IsNullOrEmpty(dbPath) || PathIsNetworkPath(dbPath));

  private static object syncObj = new object();

  // we only reopen the DB connections if the DB path is remote.      
  // since local db's have no problems.
  private void ReOpenDBs()
  {
    string dbPath = FolderSettings.DatabaseName;
    bool isRemotePath = (string.IsNullOrEmpty(dbPath) || PathIsNetworkPath(dbPath));
    if (isRemotePath)
    {
      Log.Info("Main: reopen FolderDatabase3 sqllite database.");
      FolderSettings.ReOpen();
    }
    dbPath = MediaPortal.Picture.Database.PictureDatabase.DatabaseName;
    isRemotePath = (string.IsNullOrEmpty(dbPath) || PathIsNetworkPath(dbPath));
    if (isRemotePath)
    {
      Log.Info("Main: reopen PictureDatabase sqllite database.");
      MediaPortal.Picture.Database.PictureDatabase.ReOpen();
    }
    dbPath = MediaPortal.Video.Database.VideoDatabase.DatabaseName;
    isRemotePath = (string.IsNullOrEmpty(dbPath) || PathIsNetworkPath(dbPath));
    if (isRemotePath)
    {
      Log.Info("Main: reopen VideoDatabaseV5.db3 sqllite database.");
      MediaPortal.Video.Database.VideoDatabase.ReOpen();
    }

    dbPath = MediaPortal.Music.Database.MusicDatabase.Instance.DatabaseName;
  // since local DBs have no problems.
  private static void DisposeDBs()
    {
    var dbPath = FolderSettings.DatabaseName;
    var isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
    }
  }

  // we only dispose the DB connections if the DB path is remote.      
  // since local db's have no problems.
  private void DisposeDBs()
  {
    string dbPath = FolderSettings.DatabaseName;
    bool isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
    if (isRemotePath)
    {
      Log.Info("Main: disposing FolderDatabase3 sqllite database.");
      FolderSettings.Dispose();
    }

    dbPath = MediaPortal.Picture.Database.PictureDatabase.DatabaseName;
    isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
    if (isRemotePath)
    {
      Log.Info("Main: disposing PictureDatabase sqllite database.");
      MediaPortal.Picture.Database.PictureDatabase.Dispose();
    }

    dbPath = MediaPortal.Video.Database.VideoDatabase.DatabaseName;
    isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
    if (isRemotePath)
    {
      Log.Info("Main: disposing VideoDatabaseV5.db3 sqllite database.");
      MediaPortal.Video.Database.VideoDatabase.Dispose();
    }

  private static bool Currentmodulefullscreen()
    isRemotePath = (!string.IsNullOrEmpty(dbPath) && PathIsNetworkPath(dbPath));
    var currentmodulefullscreen = (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_TVFULLSCREEN ||
                                   GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_MUSIC ||
                                   GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO ||
                                   GUIWindowManager.ActiveWindow ==
                                   (int)GUIWindow.Window.WINDOW_FULLSCREEN_TELETEXT);
  }

  private bool Currentmodulefullscreen()
  //called when windows hibernates or goes into standby mode
  private void OnSuspend()
                                    GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_MUSIC ||
    lock (SyncObj)
                                    GUIWindowManager.ActiveWindow ==
                                    (int)GUIWindow.Window.WINDOW_FULLSCREEN_TELETEXT);
    return currentmodulefullscreen;
  }

      _ignoreContextMenuAction = true;
  private void OnSuspend(ref Message msg)
  {
    lock (syncObj)
    {
      if (_suspended)
      {
      }
      ignoreContextMenuAction = true;
      _suspended = true;
      GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.SUSPENDING; // this will close all open dialogs      

      Log.Info("Main: Stopping playback");
      if (GUIGraphicsContext.IsPlaying)
      {
        bool wasFullscreen = Currentmodulefullscreen();
        g_Player.Stop();
        //wait for player to stop before proceeding                
      if (GUIGraphicsContext.DX9Device.PresentationParameters.Windowed == false && !Windowed)
        {
          Thread.Sleep(100);
        }
      }

      SaveLastActiveModule();

      //switch to windowed mode
      if (GUIGraphicsContext.DX9Device.PresentationParameters.Windowed == false && !windowed)
      {
        Log.Info("Main: Switching to windowed mode");
      // since local DBs have no problems.
      }
      //stop playback
      _suspended = true;
      _runAutomaticResume = true;
      InputDevices.Stop();
      Log.Info("Main: Stopping AutoPlay");
      AutoPlay.StopListening();
  private static readonly object SyncResume = new object();
  private static readonly object SyncResumeAutomatic = new object();
      DisposeDBs();
        {
          break;
    if (_onResumeAutomaticRunning)
        ts = now - DateTime.Now;
      }
    }
    return connected;
  }
    lock (SyncResumeAutomatic)
  private void OnResumeAutomatic()
  {
    if (_onResumeAutomaticRunning == true)
    {
      Log.Info("Main: OnResumeAutomatic - already running -> return without further action");
      return;
    }
    Log.Debug("Main: OnResumeAutomatic - set lock for syncronous inits");
    lock (syncResumeAutomatic)
    {
      if (!_runAutomaticResume)
      {
        Log.Info("Main: OnResumeAutomatic - OnResume called but !_suspended");
        return;
      }

      _onResumeAutomaticRunning = false;
      var waitOnResume = xmlreader.GetValueAsBool("general", "delay resume", false)
      Log.Info("Main: OnResumeAutomatic - Done");
    }
  }

  private void OnResume()
  {
    using (Settings xmlreader = new MPSettings())
    {
      int waitOnResume = xmlreader.GetValueAsBool("general", "delay resume", false)
                           ? xmlreader.GetValueAsInt("general", "delay", 0)
    _ignoreContextMenuAction = true;
    if (_onResumeRunning)
      {
        Log.Info("MP waiting on resume {0} secs", waitOnResume);
        Thread.Sleep(waitOnResume * 1000);
      }
    }
    lock (SyncResume)
    GUIGraphicsContext.ResetLastActivity(); // avoid ScreenSaver after standby
    ignoreContextMenuAction = true;
    if (_onResumeRunning == true)
    {
      Log.Info("Main: OnResume - already running -> return without further action");
      return;
    }
    Log.Debug("Main: OnResume - set lock for syncronous inits");
    lock (syncResume)
    {
      if (!_suspended)
      {
        Log.Info("Main: OnResume - OnResume called but !_suspended");
        return;
      }

      _onResumeRunning = true;

      var oldState = EXECUTION_STATE.ES_CONTINUOUS;

      // Systems without DirectX9 Ex have lost graphics device in suspend/hibernate cycle
      if (!GUIGraphicsContext.IsDirectX9ExUsed())
      {
        Log.Info("Main: OnResume - set GUIGraphicsContext.State.LOST");
        GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.LOST;
      }
          const EXECUTION_STATE state = EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED;
      bool turnMonitorOn;
      using (Settings xmlreader = new MPSettings())
      {        
        turnMonitorOn = xmlreader.GetValueAsBool("general", "turnmonitoronafterresume", false);
        if (turnMonitorOn)
        {
          Log.Info("Main: OnResume - Trying to wake up the monitor / tv");
          EXECUTION_STATE state = EXECUTION_STATE.ES_CONTINUOUS |
                                  EXECUTION_STATE.ES_DISPLAY_REQUIRED;
          oldState = SetThreadExecutionState(state);
        }
        if (xmlreader.GetValueAsBool("general", "restartonresume", false))
        {
          Log.Info("Main: OnResume - prepare for restart!");
          Utils.RestartMePo();
        }
      }
      if (_startWithBasicHome && File.Exists(GUIGraphicsContext.GetThemedSkinFile(@"\basichome.xml")))
      {
      RecoverDevice();
        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_SECOND_HOME);
      }
      else
      {
        Log.Info("Main: OnResume - Switch to home screen");
        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_HOME);
      }
      Log.Info("Main: OnResume - calling recover device");
      if (turnMonitorOn)
      {
        SetThreadExecutionState(oldState);
      }
      Log.Info("Main: OnResume - init InputDevices");
      InputDevices.Init();
      _suspended = false;
      Log.Debug("Main: OnResume - show last active module?");
      bool result = base.ShowLastActiveModule();

        GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.RUNNING;
      _ignoreContextMenuAction = false;

      Log.Debug("Main: OnResume - autoplay start listening");
      AutoPlay.StartListening();

      Log.Info("Main: OnResume - initializing volume handler");
      MediaPortal.Player.VolumeHandler vh = MediaPortal.Player.VolumeHandler.Instance;

  // Trap the OnShown event so we can hide the window if the minimize on startup option is set
      ignoreContextMenuAction = false;
      _lastOnresume = DateTime.Now;
    if (MinimizeOnStartup && FirstTimeWindowDisplayed)
    }
      FirstTimeWindowDisplayed = false;

  #endregion

  // Trap the OnShown event so we can hide the window if the mimimize on startup option is set
  protected override void OnShown(EventArgs e)
  {
    if (_minimizeOnStartup && _firstTimeWindowDisplayed)
    {
      _firstTimeWindowDisplayed = false;
      DoMinimizeOnStartup();
    }
  public void MediaPortalProcess()
  }

  #region process

  /// <summary>
  /// Process() gets called when a dialog is presented.
  /// It contains the message loop 
  /// </summary>
  public void MPProcess()
  {
    if (_suspended)
    {
      return;
    } //we are suspended/hibernated
    try
    {
      g_Player.Process();
      HandleMessage();
      FrameMove();
      FullRender();
      if (GUIGraphicsContext.Vmr9Active)
      {
        Thread.Sleep(50);
      }
    }
    catch (Exception ex)
    {
      Log.Error(ex);
    }
  }

  #endregion

  #region RenderFrame()

  public void RenderFrame(float timePassed)
  {
    if (_suspended)
    {
      return;
    } //we are suspended/hibernated
    try
    {
      CreateStateBlock();
      GUILayerManager.Render(timePassed);
      RenderStats();
    }
    catch (Exception ex)
    {
      Log.Error(ex);
      Log.Error("RenderFrame exception {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace);
    }
  }

  #endregion

  #region Onstartup() / OnExit()
    MouseTimeOutTimer = DateTime.Now;
  /// <summary>
  /// OnStartup() gets called just before the application starts
  /// </summary>
    _mouseClickTimer = new Timer(SystemInformation.DoubleClickTime) {AutoReset = false, Enabled = false};
    _mouseClickTimer.Elapsed += MouseClickTimerElapsed;
    _mouseClickTimer.SynchronizingObject = this;
    _mouseTimeOutTimer = DateTime.Now;
    UpdateSplashScreenMessage(GUILocalizeStrings.Get(64)); // Starting plugins...
      DateFormat = xmlreader.GetValueAsString("home", "dateformat", "<Day> <Month> <DD>");
    PluginManager.Start();
    tMouseClickTimer = new Timer(SystemInformation.DoubleClickTime);
    tMouseClickTimer.AutoReset = false;
    tMouseClickTimer.Enabled = false;
    tMouseClickTimer.Elapsed += new ElapsedEventHandler(tMouseClickTimer_Elapsed);
    tMouseClickTimer.SynchronizingObject = this;
    using (Settings xmlreader = new MPSettings())
    {
      _dateFormat = xmlreader.GetValueAsString("home", "dateformat", "<Day> <Month> <DD>");
    }
    // Asynchronously pre-initialize the music engine if we're using the BassMusicPlayer
    if (BassMusicPlayer.IsDefaultMusicPlayer)
    {
      BassMusicPlayer.CreatePlayerAsync();
    }
    try
    {
      GUIPropertyManager.SetProperty("#date", GetDate());
      GUIPropertyManager.SetProperty("#time", GetTime());
      if (_splashScreen != null)
      GUIPropertyManager.SetProperty("#SDOW", GetShortDayOfWeek()); // Sun
        _splashScreen.Stop();
      GUIPropertyManager.SetProperty("#Month", GetMonth()); // 01
        while (!_splashScreen.isStopped())
      GUIPropertyManager.SetProperty("#MOY", GetMonthOfYear()); // January
      GUIPropertyManager.SetProperty("#SY", GetShortYear()); // 80
      GUIPropertyManager.SetProperty("#Year", GetYear()); // 1980
        _splashScreen = null;
      if (splashScreen != null)
      {
      if (_useScreenSaver)
        Activate();
        SystemParametersInfo(SPI_GETSCREENSAVEACTIVE, 0, ref _isWinScreenSaverInUse, 0);
        if (_isWinScreenSaverInUse)
          Thread.Sleep(100);
        }
        splashScreen = null;
      }
      // disable screen saver when MP running and internal selected
      if (useScreenSaver)
      {
        SystemParametersInfo(SPI_GETSCREENSAVEACTIVE, 0, ref isWinScreenSaverInUse, 0);
        if (isWinScreenSaverInUse)
        {
          SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, 0, 0, SPIF_SENDWININICHANGE);
        }
    if (_outdatedSkinName != null || PluginManager.IncompatiblePluginAssemblies.Count > 0 || PluginManager.IncompatiblePlugins.Count > 0)

      GlobalServiceProvider.Add<IVideoThumbBlacklist>(new MediaPortal.Video.Database.VideoThumbBlacklistDBImpl());
      Utils.CheckThumbExtractorVersion();
    }
    catch (Exception ex)
    }
    if (_OutdatedSkinName != null || PluginManager.IncompatiblePluginAssemblies.Count > 0 || PluginManager.IncompatiblePlugins.Count > 0)
    {
      GUIWindowManager.SendThreadCallback(ShowStartupWarningDialogs, 0, 0, null);
    }
    Log.Debug("Main: Autoplay start listening");
    AutoPlay.StartListening();
    Log.Info("Main: Initializing volume handler");
      var dlg = (GUIDialogIncompatiblePlugins)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_INCOMPATIBLE_PLUGINS);
  }

  private int ShowStartupWarningDialogs(int param1, int param2, object data)
    if (_outdatedSkinName != null)
    // If skin is outdated it may not have a skin file for this dialog but user may choose to use it anyway
      var dlg = (GUIDialogOldSkin)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OLD_SKIN);
      dlg.UserSkin = _outdatedSkinName;
    {
      GUIDialogIncompatiblePlugins dlg = (GUIDialogIncompatiblePlugins)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_INCOMPATIBLE_PLUGINS);
      dlg.DoModal(GUIWindowManager.ActiveWindow);
    }

    if (_OutdatedSkinName != null)
    {
      GUIDialogOldSkin dlg = (GUIDialogOldSkin)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OLD_SKIN);
      dlg.UserSkin = _OutdatedSkinName;
      dlg.DoModal(GUIWindowManager.ActiveWindow);
    }

    return 0;
  }

  /// <summary>
  /// Load string_xx.xml based on config
  /// </summary>
  private static void LoadLanguageString()
  {
    string mylang;
    try
    {
      using (Settings xmlreader = new MPSettings())
      {
        mylang = xmlreader.GetValueAsString("gui", "language", "English");
      }
    }
    catch
    {
      Log.Warn("Load language file failed, fallback to \"English\"");
      mylang = "English";
    }
    Log.Info("Loading selected language: " + mylang);
    try
    {
      GUILocalizeStrings.Load(mylang);
    }
    catch (Exception ex)
    {
      MessageBox.Show(
        String.Format("Failed to load your language! Aborting startup...\n\n{0}\nstack:{1}", ex.Message, ex.StackTrace),
        "Critical error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
      Application.Exit();
    Log.Debug("Main: SaveLastActiveModule - enabled {0}", _showLastActiveModule);
    var currentmodulefullscreen = Currentmodulefullscreen();
    var currentmodulefullscreenstate = GUIPropertyManager.GetProperty("#currentmodulefullscreenstate");
    var currentmoduleid = GUIPropertyManager.GetProperty("#currentmoduleid");
    if (_showLastActiveModule)
  /// </summary>
  private void SaveLastActiveModule()
  {
            currentmoduleid = Convert.ToString( (int) GUIWindow.Window.WINDOW_TV);
            break;

          case (int) WINDOW_FULLSCREEN_VIDEO:
            currentmoduleid = Convert.ToString( (int) GUIWindow.Window.WINDOW_TV);
            break;
        }        
        */

        if (currentmodulefullscreen)
        {
          currentmoduleid = Convert.ToString(GUIWindowManager.GetPreviousActiveWindow());
        }


        if (!currentmodulefullscreen && currentmodulefullscreenstate == "True")
        {
          currentmodulefullscreen = true;
        }
        if (currentmoduleid.Length == 0)
        {
          currentmoduleid = "0";
        }

        string section;
        switch (GUIWindowManager.ActiveWindow)
        {
          case (int)GUIWindow.Window.WINDOW_PICTURES:
            {
              section = "pictures";
              break;
            }
          case (int)GUIWindow.Window.WINDOW_MUSIC:
            {
              section = "music";
              break;
            }
          case (int)GUIWindow.Window.WINDOW_VIDEOS:
            {
              section = "movies";
        var rememberLastFolder = xmlreader.GetValueAsBool(section, "rememberlastfolder", false);
        var lastFolder = xmlreader.GetValueAsString(section, "lastfolder", "");
          default:
        var vDir = new VirtualDirectory();
        vDir.LoadSettings(section);
        int pincode;
        var lastFolderPinProtected = vDir.IsProtectedShare(lastFolder, out pincode);
        }

        bool rememberLastFolder = xmlreader.GetValueAsBool(section, "rememberlastfolder", false);
        string lastFolder = xmlreader.GetValueAsString(section, "lastfolder", "");

        VirtualDirectory VDir = new VirtualDirectory();
        VDir.LoadSettings(section);
        int pincode = 0;
        bool lastFolderPinProtected = VDir.IsProtectedShare(lastFolder, out pincode);
        if (rememberLastFolder && lastFolderPinProtected)
        {
          lastFolder = "root";
          xmlreader.SetValue(section, "lastfolder", lastFolder);
          Log.Debug("Main: reverting to root folder, pin protected folder was open, SaveLastFolder {0}", lastFolder);
        }

        xmlreader.SetValue("general", "lastactivemodule", currentmoduleid);
        xmlreader.SetValueAsBool("general", "lastactivemodulefullscreen", currentmodulefullscreen);
        Log.Debug("Main: SaveLastActiveModule - module {0}", currentmoduleid);
        Log.Debug("Main: SaveLastActiveModule - fullscreen {0}", currentmodulefullscreen);
      }
    }
  }

    if (_usbuirtdevice != null)
  /// OnExit() Gets called just b4 application stops
      _usbuirtdevice.Close();
  protected override void OnExit()
    if (_serialuirdevice != null)
    SaveLastActiveModule();
      _serialuirdevice.Close();
    Log.Info("Main: Exiting");
    if (_redeyedevice != null)
    if (usbuirtdevice != null)
      _redeyedevice.Close();
      usbuirtdevice.Close();
    }
    if (serialuirdevice != null)
    {
      serialuirdevice.Close();
    }
    if (redeyedevice != null)
    {
      redeyedevice.Close();
    }
#if AUTOUPDATE
    StopUpdater();
    if (_mouseClickTimer != null)
    GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
      _mouseClickTimer.Stop();
      _mouseClickTimer.Dispose();
      _mouseClickTimer = null;
    // this gives the windows the chance to do some cleanup
    InputDevices.Stop();
    AutoPlay.StopListening();
    PluginManager.Stop();
    if (tMouseClickTimer != null)
    {
      tMouseClickTimer.Stop();
      tMouseClickTimer.Dispose();
      tMouseClickTimer = null;
    if (_isWinScreenSaverInUse)
    GUIWaitCursor.Dispose();
    GUIFontManager.ReleaseUnmanagedResources();
    GUIFontManager.Dispose();
    GUITextureManager.Dispose();
    GUIWindowManager.Clear();
    GUILocalizeStrings.Dispose();
    TexturePacker.Cleanup();
    VolumeHandler.Dispose();
    if (isWinScreenSaverInUse)
    {
      SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, 1, 0, SPIF_SENDWININICHANGE);
    }
  }

  /// <summary>
  /// The device has been created.  Resources that are not lost on
  /// Reset() can be created here -- resources in Pool.Managed,
  /// Pool.Scratch, or Pool.SystemMemory.  Image surfaces created via
  /// CreateImageSurface are never lost and can be created here.  Vertex
  /// shaders and pixel shaders can also be created here as they are not
  /// lost on Reset().
  /// </summary>
  protected override void InitializeDeviceObjects()
  {
    GUIWindowManager.Clear();
    GUIWaitCursor.Dispose();
    GUITextureManager.Dispose();
    UpdateSplashScreenMessage(GUILocalizeStrings.Get(65)); // Loading keymap.xml...
    ActionTranslator.Load();
    GUIGraphicsContext.ActiveForm = Handle;
    UpdateSplashScreenMessage(GUILocalizeStrings.Get(67)); // Caching graphics...
    try
    {
    catch (Exception exs)
    {
      MessageBox.Show(String.Format("Failed to load your skin! Aborting startup...\n\n{0}", exs.Message),
                      "Critical error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
      Close();
    }

    //System.Diagnostics.Debugger.Launch();

    Utils.FileExistsInCache(Config.GetSubFolder(Config.Dir.Skin, "") + "dummy.png");
    Utils.FileExistsInCache(Config.GetSubFolder(Config.Dir.Thumbs, "") + "dummy.png");
    Utils.FileExistsInCache(Thumbs.Videos + "\\dummy.png");
    Utils.FileExistsInCache(Thumbs.MusicFolder + "\\dummy.png");

    GUIGraphicsContext.Load();
    UpdateSplashScreenMessage(GUILocalizeStrings.Get(68)); // Loading fonts...
    GUIFontManager.LoadFonts(GUIGraphicsContext.GetThemedSkinFile(@"\fonts.xml"));
    UpdateSplashScreenMessage(String.Format(GUILocalizeStrings.Get(69), GUIGraphicsContext.SkinName + " - " + GUIThemeManager.CurrentTheme)); // Loading skin ({0})...
    GUIFontManager.InitializeDeviceObjects();
    GUIWindowManager.Initialize();
    UpdateSplashScreenMessage(GUILocalizeStrings.Get(70)); // Loading window plugins...
    if (!string.IsNullOrEmpty(_safePluginsList))
    {
      UseLongDateFormat = xmlreader.GetValueAsBool("home", "LongTimeFormat", false);
    }
    PluginManager.LoadWindowPlugins();
      var autosize = xmlreader.GetValueAsBool("gui", "autosize", true);
      if (autosize && !GUIGraphicsContext.Fullscreen)
      {
        Size = GUIGraphicsContext.currentScreen.Bounds.Width > GUIGraphicsContext.SkinSize.Width 
          ? new Size(GUIGraphicsContext.SkinSize.Width + 8, GUIGraphicsContext.SkinSize.Height + 54)
          : new Size(GUIGraphicsContext.SkinSize.Width, GUIGraphicsContext.SkinSize.Height);
          //int nettoHeightOffset = GUIGraphicsContext.currentScreen.Bounds.Height - GUIGraphicsContext.currentScreen.WorkingArea.Heigh t;
          //int nettoWidthOffset = GUIGraphicsContext.currentScreen.Bounds.Width - GUIGraphicsContext.currentScreen.WorkingArea.Width ;
          //Size = new Size(GUIGraphicsContext.SkinSize.Width + nettoWidthOffset, GUIGraphicsContext.SkinSize.Height + nettoHeightOffset);
        }
      }
        else
        {
          Size = new Size(GUIGraphicsContext.SkinSize.Width, GUIGraphicsContext.SkinSize.Height);
        }
        if (GUIGraphicsContext.IsDirectX9ExUsed())
        {
          SwitchFullScreenOrWindowed(true);
          OnDeviceReset(null, null);
        }
      }
      else
      {
        GUIGraphicsContext.Load();
        GUIWindowManager.OnResize();
      }
    }

    Log.Info("Main: Initializing windowmanager");
    GUIWindowManager.PreInit();
    GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.RUNNING;
    Log.Info("Main: Activating windowmanager");
    if ((_startWithBasicHome) && (File.Exists(GUIGraphicsContext.GetThemedSkinFile(@"\basichome.xml"))))
    {
      GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_SECOND_HOME);
    }
    }
    if (GUIGraphicsContext.DX9Device != null)
    {
      _anisotropy = GUIGraphicsContext.DX9Device.DeviceCaps.MaxAnisotropy;
      _supportsFiltering = Manager.CheckDeviceFormat(
        GUIGraphicsContext.DX9Device.DeviceCaps.AdapterOrdinal,
        GUIGraphicsContext.DX9Device.DeviceCaps.DeviceType,
        GUIGraphicsContext.DX9Device.DisplayMode.Format,
        Usage.RenderTarget | Usage.QueryFilter, ResourceType.Textures,
        Format.A8R8G8B8);
      _supportsAlphaBlend = Manager.CheckDeviceFormat(GUIGraphicsContext.DX9Device.DeviceCaps.AdapterOrdinal,
                                                      GUIGraphicsContext.DX9Device.DeviceCaps.DeviceType,
                                                      GUIGraphicsContext.DX9Device.DisplayMode.Format,
                                                      Usage.RenderTarget | Usage.QueryPostPixelShaderBlending,
                                                      ResourceType.Surface,
                                                      Format.A8R8G8B8);
    }

    // ReSharper disable ObjectCreationAsStatement
    new GUILayerRenderer();
    // ReSharper restore ObjectCreationAsStatement
      GUIGraphicsContext.DX9Device.DisplayMode.Format,
      Usage.RenderTarget | Usage.QueryFilter, ResourceType.Textures,
      Format.A8R8G8B8);
    bSupportsAlphaBlend = Manager.CheckDeviceFormat(GUIGraphicsContext.DX9Device.DeviceCaps.AdapterOrdinal,
  /// Updates the splash screen to display the given string. 
  /// This method checks whether the splash screen exists.
                                                    Usage.RenderTarget | Usage.QueryPostPixelShaderBlending,
                                                    ResourceType.Surface,
                                                    Format.A8R8G8B8);

    new GUILayerRenderer();
    WorkingSet.Minimize();
      if (_splashScreen != null)

        _splashScreen.SetInformation(aSplashLine);
  /// Updates the splashscreen to display the given string. 
  /// This method checks whether the splashscreen exists.
  /// </summary>
  /// <param name="aSplashLine"></param>
  private void UpdateSplashScreenMessage(string aSplashLine)
  {
    try
    {
      if (splashScreen != null)
      {
        splashScreen.SetInformation(aSplashLine);
      }
    }
    catch (Exception ex)
    {
      Log.Error("Main: Could not update splashscreen - {0}", ex.Message);
    }
  }

  protected override void OnDeviceLost(object sender, EventArgs e)
  {
    Log.Warn("Main: ***** OnDeviceLost *****");
    GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.LOST;
    base.OnDeviceLost(sender, e);
  }

  /// <summary>
  /// The device exists, but may have just been Reset().  Resources in
  /// Pool.Managed and any other device state that persists during
  /// rendering should be set here.  Render states, matrices, textures,
  /// etc., that don't change during rendering can be set once here to
  /// avoid redundant state setting during Render() or FrameMove().
  /// </summary>
  protected override void OnDeviceReset(Object sender, EventArgs e)
        var activeWin = GUIWindowManager.ActiveWindow;
    // Only perform the device reset if we're not shutting down MediaPortal.
    if (GUIGraphicsContext.CurrentState != GUIGraphicsContext.State.STOPPING)
    {
      Log.Info("Main: OnDeviceReset called");

      // Only perform the device reset if we're not shutting down MediaPortal.
      if (GUIGraphicsContext.CurrentState != GUIGraphicsContext.State.STOPPING)
      {
        Log.Info("Main: Resetting DX9 device");

        int activeWin = GUIWindowManager.ActiveWindow;
        if (activeWin == 0 && !GUIWindowManager.HasPreviousWindow())
        // Device lost must be prioritized over this one!
          if (_startWithBasicHome && File.Exists(GUIGraphicsContext.GetThemedSkinFile(@"\basichome.xml")))
          {
            activeWin = (int)GUIWindow.Window.WINDOW_SECOND_HOME;
          }
        }

        if (GUIGraphicsContext.DX9ExRealDeviceLost)
        {
          activeWin = (int)GUIWindow.Window.WINDOW_HOME;
        }
          // Device lost must be priorized over this one!
        else if (Currentmodulefullscreen())
        {
          activeWin = GUIWindowManager.GetPreviousActiveWindow();
          GUIWindowManager.ShowPreviousWindow();
        }

        GUIWindowManager.UnRoute();
        // avoid that there is an active Window when GUIWindowManager.ActivateWindow(activeWin); is called
        Log.Info("Main: UnRoute - done");

        GUITextureManager.Dispose();
        GUIFontManager.Dispose();

        GUIGraphicsContext.DX9Device.EvictManagedResources();
        GUIWaitCursor.Dispose();
        GUIGraphicsContext.Load();
        GUIFontManager.LoadFonts(GUIGraphicsContext.GetThemedSkinFile(@"\fonts.xml"));
        GUIFontManager.InitializeDeviceObjects();

        if (GUIGraphicsContext.DX9Device != null)
        {
          GUIWindowManager.OnResize();
          GUIWindowManager.PreInit();
          GUIWindowManager.ActivateWindow(activeWin);
          GUIWindowManager.OnDeviceRestored();
        }
        // Must set the FVF after reset
        GUIFontManager.SetDevice();

        GUIGraphicsContext.DX9ExRealDeviceLost = false;
        Log.Info("Main: Resetting DX9 device done");
      }
    }
  }
      ResizeOngoing = false;
      if (OurClientSize != ClientSize)

  /// <summary>
  /// Handle OnResizeEnd
  /// </summary>
  protected override void OnResizeEnd(EventArgs e)
  {
    Log.Info("Main: OnResizeEnd");
    if (GUIGraphicsContext.IsDirectX9ExUsed())
    {
  private static bool _reentrant;
  private int _d3ErrInvalidCallCounter;
      {
        _resetOnResize = true;
      }
    }
    base.OnResizeEnd(e);
  }

    if (_reentrant)

  private static bool reentrant = false;
  private int d3ErrInvalidCallCounter = 0;

  protected override void Render(float timePassed)
  {
    if (_suspended)
    {
      return;
    }
    if (reentrant)
    {
      Log.Info("Main: DX9 re-entrant"); //remove
      return;
    }
    if (GUIGraphicsContext.InVmr9Render)
    {
      Log.Error("Main: MediaPortal.Render() called while VMR9 render - {0} / {1}",
                GUIGraphicsContext.Vmr9Active, GUIGraphicsContext.Vmr9FPS);
      return;
    }
      _reentrant = true;
      // if there's no DX9 device (during resizing for example) then just return
      Log.Error("Main: MediaPortal.Render() called while VMR9 active");
      return;
        _reentrant = false;
    if (GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.LOST)
    {
      return;
    }
    try
        _reentrant = false;
      reentrant = true;
      // if there's no DX9 device (during resizing for exmaple) then just return
      if (GUIGraphicsContext.DX9Device == null)
      Frames++;
        reentrant = false;
        //Log.Info("dx9 device=null");//remove
        return;
      }
      if (GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.LOST)
      {
        reentrant = false;
        //Log.Info("dx9 state=lost");//remove
        return;
      }
      _d3ErrInvalidCallCounter = 0;
      // clear the surface
      GUIGraphicsContext.DX9Device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
      GUIGraphicsContext.DX9Device.BeginScene();
      CreateStateBlock();
      GUIGraphicsContext.SetScalingResolution(0, 0, false);
      // ask the layer manager to render all layers
      GUILayerManager.Render(timePassed);
      RenderStats();
      GUIFontManager.Present();
      GUIGraphicsContext.DX9Device.EndScene();
      d3ErrInvalidCallCounter = 0;
      try
      {
        // Show the frame on the primary surface.
        GUIGraphicsContext.DX9Device.Present(); //SLOW
      }
      catch (DeviceLostException ex)
      switch (dex.ErrorCode)
      {
        case D3DERR_INVALIDCALL:
          {
            _d3ErrInvalidCallCounter++;
        {
            var currentScreenNr = GUIGraphicsContext.currentScreenNumber;
        }
            if ((currentScreenNr > -1) && (Manager.Adapters.Count > currentScreenNr))
            {
              double currentRefreshRate = Manager.Adapters[currentScreenNr].CurrentDisplayMode.RefreshRate;
    catch (DirectXException dex)
              if (currentRefreshRate > 0 && _d3ErrInvalidCallCounter > (5 * currentRefreshRate))
              {
                _d3ErrInvalidCallCounter = 0; //reset counter
                Log.Info("Main: D3DERR_INVALIDCALL - {0}", dex.ToString());
                GUIGraphicsContext.DX9ExRealDeviceLost = true;
                GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.LOST;
              }
            }
          }
          break;
        case GPU_REMOVED:
        case GPU_HUNG:
          Log.Info("Main: GPU_HUNG - {0}", dex.ToString());
          GUIGraphicsContext.DX9ExRealDeviceLost = true;
          if (!RefreshRateChanger.RefreshRateChangePending)
      if (dex.ErrorCode == D3DERR_INVALIDCALL)
            g_Player.Stop();
          }
          GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.LOST;
          break;
        default:
          Log.Error(dex);
          break;
        Log.Info("Main: GPU_HUNG - {0}", dex.ToString());
        GUIGraphicsContext.DX9ExRealDeviceLost = true;
        if (!RefreshRateChanger.RefreshRateChangePending)
        {
          g_Player.Stop();
        }
        GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.LOST;
      _reentrant = false;
      {
        Log.Error(dex);
      }
    }
    catch (Exception ex)
    {
      Log.Error(ex);
    }
    finally
    {
    if (DateTime.Now.Second != _updateTimer.Second)
      reentrant = false;
      _updateTimer = DateTime.Now;
  }

  #endregion

  #region OnProcess()

  protected override void OnProcess()
  {
    // Set the date & time
    if (DateTime.Now.Second != m_updateTimer.Second)
    {
      m_updateTimer = DateTime.Now;
      GUIPropertyManager.SetProperty("#date", GetDate());
      GUIPropertyManager.SetProperty("#time", GetTime());
    }
      _playingState = true;
    CheckForNewUpdate();
#endif
    g_Player.Process();
    }

    if (g_Player.Playing)
    {
      m_bPlayingState = true;
      if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO)
      {
        GUIGraphicsContext.IsFullScreenVideo = true;
        /*if (GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.LOST)
        {
          RecoverDevice();
        }*/
      }
      GUIGraphicsContext.IsPlaying = true;
      GUIGraphicsContext.IsPlayingVideo = (g_Player.IsVideo || g_Player.IsTV);
      if (g_Player.Paused)
      {
        GUIPropertyManager.SetProperty("#playlogo", "logo_pause.png");
      }
      else if (g_Player.Speed > 1)
      {
        GUIPropertyManager.SetProperty("#playlogo", "logo_fastforward.png");
      }
      else if (g_Player.Speed < 1)
      {
        GUIPropertyManager.SetProperty("#playlogo", "logo_rewind.png");
      }
      else if (g_Player.Playing)
      {
        GUIPropertyManager.SetProperty("#playlogo", "logo_play.png");
      }
      if (g_Player.IsTV && !g_Player.IsTVRecording)
      {
        GUIPropertyManager.SetProperty("#currentplaytime", GUIPropertyManager.GetProperty("#TV.Record.current"));
        GUIPropertyManager.SetProperty("#shortcurrentplaytime",
                                       GUIPropertyManager.GetProperty("#TV.Record.current"));
      }
      else
      {
        GUIPropertyManager.SetProperty("#currentplaytime",
                                       Utils.SecondsToHMSString((int)g_Player.CurrentPosition));
        GUIPropertyManager.SetProperty("#currentremaining",
        var fPercentage = (float)(100.0d * g_Player.CurrentPosition / g_Player.Duration);
        GUIPropertyManager.SetProperty("#percentage", fPercentage.ToString(CultureInfo.InvariantCulture));
        GUIPropertyManager.SetProperty("#shortcurrentremaining",
                                       Utils.SecondsToShortHMSString(
                                         (int)(g_Player.Duration - g_Player.CurrentPosition)));
        GUIPropertyManager.SetProperty("#shortcurrentplaytime",
                                       Utils.SecondsToShortHMSString((int)g_Player.CurrentPosition));
      }
      if (g_Player.Duration > 0)
      GUIPropertyManager.SetProperty("#playspeed", g_Player.Speed.ToString(CultureInfo.InvariantCulture));
        GUIPropertyManager.SetProperty("#duration", Utils.SecondsToHMSString((int)g_Player.Duration));
        GUIPropertyManager.SetProperty("#shortduration", Utils.SecondsToShortHMSString((int)g_Player.Duration));
        float fPercentage = (float)(100.0d * g_Player.CurrentPosition / g_Player.Duration);
        GUIPropertyManager.SetProperty("#percentage", fPercentage.ToString());
      if (_playingState)
      else
      {
        _playingState = false;
        GUIPropertyManager.SetProperty("#shortduration", string.Empty);
        GUIPropertyManager.SetProperty("#percentage", "0.0");
      }
      GUIPropertyManager.SetProperty("#playspeed", g_Player.Speed.ToString());
    }
    else
    {
      GUIGraphicsContext.IsPlaying = false;
      if (m_bPlayingState)
      {
        GUIPropertyManager.RemovePlayerProperties();
        m_bPlayingState = false;
      }
    }
  }

  #endregion

  #region FrameMove()

  protected override void FrameMove()
  {
    if (_suspended)
    {
      return;
    } //we are suspended/hibernated
#if !DEBUG
    try
#endif
    {
      if (GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.STOPPING)
      {
        Log.Info("Main: Stopping FrameMove");
        Close();
        return;
      }
      if (_resetOnResize)
      {
        // sync reset with rendering loop
        _resetOnResize = false;
        if (g_Player.Playing) GUIGraphicsContext.BlankScreen = true; // stop FrameMove calls
        SwitchFullScreenOrWindowed(false);
        OnDeviceReset(null, null);
        GUIGraphicsContext.BlankScreen = false;
      }

      if (_useScreenSaver)
      {
        GUIWindowManager.DispatchThreadMessages();
        GUIWindowManager.ProcessWindows();
      }
      catch (FileNotFoundException ex)
      {
        Log.Error(ex);
        MessageBox.Show("File not found:" + ex.FileName, "MediaPortal", MessageBoxButtons.OK,
          if (Maximized)
        Close();
      }
            if (ts.TotalSeconds >= _timeScreenSaver)
      {
              if (_useIdleblankScreen)
            GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_SLIDESHOW)
        {
          GUIGraphicsContext.ResetLastActivity();
        }
        if (!GUIGraphicsContext.BlankScreen)
        {
          if (isMaximized)
          {
            TimeSpan ts = DateTime.Now - GUIGraphicsContext.LastActivity;
            if (ts.TotalSeconds >= timeScreenSaver)
            {
              if (useIdleblankScreen)
              {
                if (!GUIGraphicsContext.BlankScreen)
                {
                  Log.Debug("Main: Idle timer is blanking the screen after {0} seconds of inactivity",
                            ts.TotalSeconds.ToString("n0"));
                }
                GUIGraphicsContext.BlankScreen = true;
              }
              else
              {
                // Slower rendering will have an impact on scrolling labels or list items
                // As long as we're e.g. listening to music on "Playing Now" screen
                // we might not want to slow things down here.
                // This feature is mainly intended to save energy on idle 24/7 rigs.
                if (GUIWindowManager.ActiveWindow != (int)GUIWindow.Window.WINDOW_MUSIC_PLAYING_NOW)
                {
                  if (!GUIGraphicsContext.SaveRenderCycles)
                  {
                    Log.Debug("Main: Idle timer is entering power save mode after {0} seconds of inactivity",
                              ts.TotalSeconds.ToString("n0"));
                  }
                  GUIGraphicsContext.SaveRenderCycles = true;
                }
              }
            }
          }
        }
      }
    }
#if !DEBUG
    catch (Exception ex)
    {
      Log.Error(ex);
    }
#endif
  }

  #endregion
      if ((action.wID == Action.ActionType.ACTION_CONTEXT_MENU || _suspended) && (_showLastActiveModule))
  #region Handle messages, keypresses, mouse moves etc
        if (_ignoreContextMenuAction)
  {
          _ignoreContextMenuAction = false;
          _lastContextMenuAction = DateTime.Now;
      // hack/fix for lastactivemodulefullscreen
        }
        if (_lastContextMenuAction != DateTime.MaxValue)
      // sometimes more than one F9 keydown event fired.
          var ts = _lastContextMenuAction - DateTime.Now;
      if ((action.wID == Action.ActionType.ACTION_CONTEXT_MENU || _suspended) && (showLastActiveModule))
      {
            _ignoreContextMenuAction = false;
            _lastContextMenuAction = DateTime.Now;
        {
          ignoreContextMenuAction = false;
        }
        _lastContextMenuAction = DateTime.Now;
        }
        else if (lastContextMenuAction != DateTime.MaxValue)
        {
          TimeSpan ts = lastContextMenuAction - DateTime.Now;
          if (ts.TotalMilliseconds > -100)
          {
            ignoreContextMenuAction = false;
            lastContextMenuAction = DateTime.Now;
            return;
        // record current tv program
        }
        lastContextMenuAction = DateTime.Now;
      }

      GUIWindow window;
            var tvHome = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_TV);
      {
        GUIGraphicsContext.ResetLastActivity();
      }
      switch (action.wID)
      {
          // record current tv program
        case Action.ActionType.ACTION_RECORD:
          if ((GUIGraphicsContext.IsTvWindow(GUIWindowManager.ActiveWindowEx) &&
               GUIWindowManager.ActiveWindowEx != (int)GUIWindow.Window.WINDOW_TVGUIDE) &&
              (GUIWindowManager.ActiveWindowEx != (int)GUIWindow.Window.WINDOW_DIALOG_TVGUIDE))
        // TV: zap to previous channel
            if (tvHome != null)
            {
              if (tvHome.GetID != GUIWindowManager.ActiveWindow)
              {
                tvHome.OnAction(action);
                return;
              }
            }
        // TV: zap to next channel

          //TV: zap to previous channel
        case Action.ActionType.ACTION_PREV_CHANNEL:
          if (!GUIWindowManager.IsRouted)
          {
            window = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_TV);
            window.OnAction(action);
            return;
        // TV: zap to last channel viewed

          //TV: zap to next channel
        case Action.ActionType.ACTION_NEXT_CHANNEL:
          if (!GUIWindowManager.IsRouted)
          {
            window = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_TV);
            window.OnAction(action);
            return;
        // toggle between directx windowed and exclusive mode

          //TV: zap to last channel viewed
          return;
        // mute or unmute audio
            window.OnAction(action);
            return;
          }
        // decrease volume 
          //toggle between directx windowed and exclusive mode
        case Action.ActionType.ACTION_TOGGLE_WINDOWED_FULLSCREEN:
          ToggleFullWindowed();
        // increase volume 

          //mute or unmute audio
        case Action.ActionType.ACTION_VOLUME_MUTE:
        // toggle live tv in background

          // show livetv or video as background instead of the static GUI background
        case Action.ActionType.ACTION_VOLUME_DOWN:
          VolumeHandler.Instance.Volume = VolumeHandler.Instance.Previous;
          break;

          //increase volume 
        case Action.ActionType.ACTION_VOLUME_UP:
          VolumeHandler.Instance.Volume = VolumeHandler.Instance.Next;
          break;

          //toggle live tv in background
        case Action.ActionType.ACTION_BACKGROUND_TOGGLE:
          //show livetv or video as background instead of the static GUI background
          // toggle livetv/video in background on/pff
              var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SHOW_WARNING, 0, 0, 0, 0, 0, 0)
                              {Param1 = 727, Param2 = 728, Param3 = 729};
            if (GUIGraphicsContext.Vmr9Active)
            {
              GUIGraphicsContext.ShowBackground = false;
              //GUIGraphicsContext.Overlay = false;
            }
            else
            {
              GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SHOW_WARNING, 0, 0, 0, 0, 0, 0);
              msg.Param1 = 727; //Live tv in background
              msg.Param2 = 728; //No Video/TV playing
        //switch between several home windows
        case Action.ActionType.ACTION_SWITCH_HOME:
          var newHome = _startWithBasicHome
                          ? GUIWindow.Window.WINDOW_SECOND_HOME
                          : GUIWindow.Window.WINDOW_HOME;
          {
            Log.Info("Main: Using GUI as background");
            GUIGraphicsContext.ShowBackground = true;
            //GUIGraphicsContext.Overlay = true;
          }
          return;

          //switch between several home windows
        case Action.ActionType.ACTION_SWITCH_HOME:
          GUIMessage homeMsg;
          GUIWindow.Window newHome = _startWithBasicHome
                                       ? GUIWindow.Window.WINDOW_SECOND_HOME
                                       : GUIWindow.Window.WINDOW_HOME;
            switch (GUIWindowManager.ActiveWindow)
            {
              case (int)GUIWindow.Window.WINDOW_HOME:
                newHome = GUIWindow.Window.WINDOW_SECOND_HOME;
                break;
              case (int)GUIWindow.Window.WINDOW_SECOND_HOME:
                newHome = GUIWindow.Window.WINDOW_HOME;
                break;
            }
          var homeMsg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_GOTO_WINDOW, 0, 0, 0, (int)newHome, 0, null);
            // we like both 
          else
            // if already in one home switch to the other
            if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_HOME)
            {
            RestoreFromTray();
            if ((g_Player.IsVideo || g_Player.IsTV || g_Player.IsDVD) && Volume > 0)
            else if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_SECOND_HOME)
              g_Player.Volume = Volume;
              newHome = GUIWindow.Window.WINDOW_HOME;
            }
          }
          homeMsg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_GOTO_WINDOW, 0, 0, 0, (int)newHome, 0, null);
          GUIWindowManager.SendThreadMessage(homeMsg);
          return;

        case Action.ActionType.ACTION_MPRESTORE:
          {
        // reboot PC
            Restore();
            if ((g_Player.IsVideo || g_Player.IsTV || g_Player.IsDVD) && m_iVolume > 0)
            {
              g_Player.Volume = m_iVolume;
              g_Player.ContinueGraph();
            // reboot
            Log.Info("Main: Reboot requested");
            var okToChangePowermode = action.fAmount1 == 1;
              }
            }
          }
          return;

          //reboot pc
        case Action.ActionType.ACTION_POWER_OFF:
        case Action.ActionType.ACTION_SUSPEND:
        case Action.ActionType.ACTION_HIBERNATE:
        case Action.ActionType.ACTION_REBOOT:
          {
                  _restartOptions = RestartOptions.Reboot;
                  _useRestartOptions = true;
            bool okToChangePowermode = (action.fAmount1 == 1);

            {
                  _restartOptions = RestartOptions.PowerOff;
                  _useRestartOptions = true;

                  ShuttingDown = true;
            {
              {
                case Action.ActionType.ACTION_REBOOT:
                  restartOptions = RestartOptions.Reboot;
                    _restartOptions = RestartOptions.Suspend;
                  GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
                  break;

                case Action.ActionType.ACTION_POWER_OFF:
                  restartOptions = RestartOptions.PowerOff;
                  useRestartOptions = true;
                  GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
                  break;

                case Action.ActionType.ACTION_SUSPEND:
                    _restartOptions = RestartOptions.Hibernate;
                  {
                    restartOptions = RestartOptions.Suspend;
                    Utils.SuspendSystem(false);
                  }
                  else
                  {
                    Log.Info("Main: SUSPEND ignored since suspend graceperiod of {0} sec. is violated.", _suspendGracePeriodSec); 
                  }
                  break;

                case Action.ActionType.ACTION_HIBERNATE:
                  if (IsSuspendOrHibernationAllowed())
        // eject CD
                    restartOptions = RestartOptions.Hibernate;
                    Utils.HibernateSystem(false);
                  }
                  else
        // shutdown PC
                    Log.Info("Main: HIBERNATE ignored since hibernate graceperiod of {0} sec. is violated.", _suspendGracePeriodSec);
                  }
                  break;
            var dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            }
          }
          return;

          //eject cd
        case Action.ActionType.ACTION_EJECTCD:
          Utils.EjectCDROM();
          return;

          //shutdown pc
        case Action.ActionType.ACTION_SHUTDOWN:
            Log.Info("Main: Shutdown dialog");
            GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                var win = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_HOME);
            {
              dlg.Reset();
              dlg.SetHeading(GUILocalizeStrings.Get(498)); //Menu
              dlg.AddLocalizedString(1057); //Exit MediaPortal
              dlg.AddLocalizedString(1058); //Restart MediaPortal
              dlg.AddLocalizedString(1032); //Suspend
              dlg.AddLocalizedString(1049); //Hibernate
              dlg.AddLocalizedString(1031); //Reboot
                case 1057:
                  ExitMePo();
                  return;
                case 1058:
                  GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
                  Utils.RestartMePo();
                  break;
                }
                  _restartOptions = RestartOptions.PowerOff;
                  _useRestartOptions = true;
              switch (dlg.SelectedId)
                  ShuttingDown = true;
                case 1057:
                  return;
                  _restartOptions = RestartOptions.Reboot;
                  _useRestartOptions = true;
                  GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
                  ShuttingDown = true;
                  break;
                case 1030:
                  _restartOptions = RestartOptions.Suspend;
                  useRestartOptions = true;
                  GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
                  break;
                  _restartOptions = RestartOptions.Hibernate;
                case 1031:
                  restartOptions = RestartOptions.Reboot;
                  useRestartOptions = true;
                  GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
                  base._shuttingDown = true;
                  break;
        // exit MediaPortal
        case Action.ActionType.ACTION_EXIT:
          ExitMePo();
          return;
        // stop radio
                  restartOptions = RestartOptions.Hibernate;
                  Utils.HibernateSystem(false);
        // Take screen shot
            }
            break;
          }

              var directory =
        case Action.ActionType.ACTION_EXIT:
          ExitMePo();
          return;

        //stop radio
        case Action.ActionType.ACTION_STOP:
          break;

          // Take Screenshot
              var fileName =
          {
            try
            {
              var backbuffer = GUIGraphicsContext.DX9Device.GetBackBuffer(0, 0, BackBufferType.Mono);
                string.Format("{0}\\MediaPortal Screenshots\\{1:0000}-{2:00}-{3:00}",
                              Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                              DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
              if (!Directory.Exists(directory))
              {
                Log.Info("Main: Taking screenshot - Creating directory: {0}", directory);
                Directory.CreateDirectory(directory);
              }

              string fileName =
                              DateTime.Now.Minute, DateTime.Now.Second);
              Log.Info("Main: Taking screenshot - Target: {0}.png", fileName);
              Surface backbuffer = GUIGraphicsContext.DX9Device.GetBackBuffer(0, 0, BackBufferType.Mono);
              SurfaceLoader.Save(fileName + ".png", ImageFileFormat.Png, backbuffer);
              backbuffer.Dispose();
              Log.Info("Main: Taking screenshot done");
            }
            catch (Exception ex)
            {
              Log.Info("Main: Error taking screenshot: {0}", ex.Message);
            }
          }
          break;

          // show DVD menu
          {
            // can we handle the switch to fullscreen?
            if (!GUIGraphicsContext.IsFullScreenVideo && g_Player.ShowFullScreenWindow())
            {
              return;
            }
          }
          // DVD: goto previous chapter
          // play previous item from playlist;
      {
        switch (action.wID)
        {
            //show DVD menu
          case Action.ActionType.ACTION_DVD_MENU:
            if (g_Player.IsDVD)
            {
              g_Player.OnAction(action);
              return;
            }
            break;
              PlaylistPlayer.PlayPrevious();
            //DVD: goto previous chapter
            //play previous item from playlist;
          // play next item from playlist;
          // DVD: goto next chapter
            {
              action = new Action(Action.ActionType.ACTION_PREV_CHAPTER, 0, 0);
              g_Player.OnAction(action);
              return;
            }

            if (!ActionTranslator.HasKeyMapped(GUIWindowManager.ActiveWindowEx, action.m_key))
            {
              playlistPlayer.PlayPrevious();
            }
            break;
              PlaylistPlayer.PlayNext();
            //play next item from playlist;
            //DVD: goto next chapter
          //  stop playback
            if (g_Player.IsDVD || g_Player.HasChapters)
            {
            // When MyPictures Plugin shows the pictures we want to stop the slide show only, not the player
              g_Player.OnAction(action);
              (GUIWindow.Window)(Enum.Parse(typeof (GUIWindow.Window), GUIWindowManager.ActiveWindow.ToString(CultureInfo.InvariantCulture))) ==
            }

            if (!ActionTranslator.HasKeyMapped(GUIWindowManager.ActiveWindowEx, action.m_key))
            {
              playlistPlayer.PlayNext();
            }
            break;

            //stop playback
          case Action.ActionType.ACTION_STOP:

            //When MyPictures Plugin shows the pictures we want to stop the slide show only, not the player
          // Jump to Music Now Playing
              GUIWindow.Window.WINDOW_SLIDESHOW)
            {
              break;
            }

            if (!g_Player.IsTV || !GUIGraphicsContext.IsFullScreenVideo)
          // play music
          // resume playback
              return;
            }
            break;

            //Jump to Music Now Playing
          case Action.ActionType.ACTION_JUMP_MUSIC_NOW_PLAYING:
            if (g_Player.IsMusic && GUIWindowManager.ActiveWindow != (int)GUIWindow.Window.WINDOW_MUSIC_PLAYING_NOW)
            {
              GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_MUSIC_PLAYING_NOW);
            }
            break;

            //play music
            //resume playback
          case Action.ActionType.ACTION_PLAY:
          case Action.ActionType.ACTION_MUSIC_PLAY:
            // Don't start playing from the beginning if we press play to return to normal speed
            if (g_Player.IsMusic && g_Player.Speed != 1)
            {
              // Attention: GUIMusicGenre / GUIMusicFiles need to be handled differently. we reset the speed there
              if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_MUSIC_FILES ||
                  GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_MUSIC_GENRE)
              {
          // pause (or resume playback)
              g_Player.Speed = 1;
              return;
            }
          // fast forward...
            g_Player.Speed = 1;

            if (g_Player.Paused)
            {
              g_Player.Pause();
            }
            break;

            //pause (or resume playback)
          case Action.ActionType.ACTION_PAUSE:
          // fast rewind...

            //fast forward...
          case Action.ActionType.ACTION_FORWARD:
          case Action.ActionType.ACTION_MUSIC_FORWARD:
            {
              if (g_Player.Paused)
              {
                g_Player.Pause();
              }
              g_Player.Speed = Utils.GetNextForwardSpeed(g_Player.Speed);
              break;
            }

            //fast rewind...
          case Action.ActionType.ACTION_REWIND:
          case Action.ActionType.ACTION_MUSIC_REWIND:
            {
              if (g_Player.Paused)
              {
                g_Player.Pause();
              }
              g_Player.Speed = Utils.GetNextRewindSpeed(g_Player.Speed);
              break;
            }
        }
      throw new Exception("exception occurred", ex);
      GUIWindowManager.OnAction(action);
    }
    catch (FileNotFoundException ex)
    {
  private void ExitMePo()
  {
    Log.Info("Main: Exit requested");
    // is the minimize on GUI option set? If so, minimize to tray...
    if (MinimizeOnGuiExit && !ShuttingDown)
    {
      if (WindowState != FormWindowState.Minimized)
      {
        Log.Info("Main: Minimizing to tray on GUI exit and restoring task bar");
        MinimizeToTray();
      }
      if (g_Player.IsVideo || g_Player.IsTV || g_Player.IsDVD)
      {
        if (g_Player.Volume > 0)
        {
          Volume = g_Player.Volume;
          g_Player.Volume = 0;
        }
        if (g_Player.Paused == false && !GUIGraphicsContext.IsVMR9Exclusive)
        {
          g_Player.Pause();
        }
      }
      return;
    }
    GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
  }
  
  private static bool PromptUserBeforeChangingPowermode(Action action)
          m_iVolume = g_Player.Volume;
    var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_ASKYESNO, 0, 0, 0, 0, 0, 0);
        }
        if (g_Player.Paused == false && !GUIGraphicsContext.IsVMR9Exclusive)
        {
          g_Player.Pause();
        }
      return;
    }
    GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;

  private bool PromptUserBeforeChangingPowermode(Action action)
  {
    switch (action.wID)
    {
      case Action.ActionType.ACTION_REBOOT:
        msg.Param1 = 630;
        break;

      case Action.ActionType.ACTION_POWER_OFF:
        msg.Param1 = 1600;
        break;

      case Action.ActionType.ACTION_SUSPEND:
        msg.Param1 = 1601;
        break;
    var ts = DateTime.Now - _lastOnresume;
      case Action.ActionType.ACTION_HIBERNATE:
        msg.Param1 = 1602;
        break;
    }
    msg.Param2 = 0;
  protected override void KeyPressEvent(KeyPressEventArgs e)
    GUIWindowManager.SendMessage(msg);

    var key = new Key(e.KeyChar, 0);
    var action = new Action();
    // is a dialog open or maybe the tv schedule search (GUISMSInputControl)?
  private bool IsSuspendOrHibernationAllowed()
    TimeSpan ts = DateTime.Now - _lastOnresume;
    return (ts.TotalSeconds > _suspendGracePeriodSec);
  }

  #region keypress handlers

  protected override void keypressed(KeyPressEventArgs e)
  {
    GUIGraphicsContext.BlankScreen = false;
    // Log.Info("key:{0} 0x{1:X} (2)", (int)keyc, (int)keyc, keyc);
    Key key = new Key(e.KeyChar, 0);
    Action action = new Action();
    if (GUIWindowManager.IsRouted || GUIWindowManager.ActiveWindowEx == (int)GUIWindow.Window.WINDOW_TV_SEARCH)
      // is a dialog open or maybe the tv schedule search (GUISMSInputControl)?
    {
      GUIGraphicsContext.ResetLastActivity();
      if (ActionTranslator.GetAction(GUIWindowManager.ActiveWindowEx, key, ref action) &&
          (GUIWindowManager.ActiveWindowEx != (int)GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD) &&
          (GUIWindowManager.ActiveWindowEx != (int)GUIWindow.Window.WINDOW_TV_SEARCH))
      {
        if (action.SoundFileName.Length > 0 && !g_Player.Playing)
      _showStats = !_showStats;
          Utils.PlaySound(action.SoundFileName, false, true);
        }
        GUIGraphicsContext.OnAction(action);
      }
      else
      {
        action = new Action(key, Action.ActionType.ACTION_KEY_PRESSED, 0, 0);
        GUIGraphicsContext.OnAction(action);
      }
      return;
    }
    if (key.KeyChar == '!')
    {
      m_bShowStats = !m_bShowStats;
    }
    if (key.KeyChar == '|' && g_Player.Playing == false)
    {
      g_Player.Play("rtsp://localhost/stream0");
      g_Player.ShowFullScreenWindow();
      return;
    }
    if (ActionTranslator.GetAction(GUIWindowManager.ActiveWindowEx, key, ref action))
    {
      if (action.ShouldDisableScreenSaver)
      {
        GUIGraphicsContext.ResetLastActivity();
      }
  protected override void KeyDownEvent(KeyEventArgs e)
      {
        Utils.PlaySound(action.SoundFileName, false, true);
      }
      GUIGraphicsContext.OnAction(action);
    }
    else
    var key = new Key(0, (int)e.KeyCode);
    var action = new Action();
    }
    action = new Action(key, Action.ActionType.ACTION_KEY_PRESSED, 0, 0);
    GUIGraphicsContext.OnAction(action);
  }

  protected override void keydown(KeyEventArgs e)
  {
    if (_suspended)
    {
      return;
    }
    GUIGraphicsContext.ResetLastActivity();
    Key key = new Key(0, (int)e.KeyCode);
    Action action = new Action();
    if (ActionTranslator.GetAction(GUIWindowManager.ActiveWindowEx, key, ref action))
    {
      if (action.SoundFileName.Length > 0 && !g_Player.Playing)
    var screen = new Point {X = Cursor.Position.X, Y = Cursor.Position.Y};
    var client = PointToClient(screen);
    var cursorX = client.X;
    var cursorY = client.Y;
    var fX = ((float)GUIGraphicsContext.Width) / ClientSize.Width;
    var fY = ((float)GUIGraphicsContext.Height) / ClientSize.Height;
    var x = (fX * cursorX) - GUIGraphicsContext.OffsetX;
    var y = (fY * cursorY) - GUIGraphicsContext.OffsetY;
  protected override void OnMouseWheel(MouseEventArgs e)
  {
      var action = new Action(Action.ActionType.ACTION_MOVE_UP, x, y) {MouseButton = e.Button};
    Point ptScreenUL = new Point();
    ptScreenUL.X = Cursor.Position.X;
    ptScreenUL.Y = Cursor.Position.Y;
    ptClientUL = PointToClient(ptScreenUL);
      var action = new Action(Action.ActionType.ACTION_MOVE_DOWN, x, y) {MouseButton = e.Button};
    float fX = ((float)GUIGraphicsContext.Width) / ((float)ClientSize.Width);
    float fY = ((float)GUIGraphicsContext.Height) / ((float)ClientSize.Height);
    float x = (fX * iCursorX) - GUIGraphicsContext.OffsetX;
    float y = (fY * iCursorY) - GUIGraphicsContext.OffsetY;
    if (e.Delta > 0)
  protected override void MouseMoveEvent(MouseEventArgs e)
      Action action = new Action(Action.ActionType.ACTION_MOVE_UP, x, y);
      action.MouseButton = e.Button;
    base.MouseMoveEvent(e);
    if (!ShowCursor)
    }
    else if (e.Delta < 0)
    {
      Action action = new Action(Action.ActionType.ACTION_MOVE_DOWN, x, y);
    var screen = new Point {X = Cursor.Position.X, Y = Cursor.Position.Y};
    var client = PointToClient(screen);
    var cursorX = client.X;
    var cursorY = client.Y;
    if (_lastMousePositionX != cursorX || _lastMousePositionY != cursorY)
  {
      if ((Math.Abs(_lastMousePositionX - cursorX) > 10) || (Math.Abs(_lastMousePositionY - cursorY) > 10))
    base.mousemove(e);
    if (!_showCursor)
    {
      // check any still waiting single click events
      if (GUIGraphicsContext.DBLClickAsRightClick && _mouseClickFired)
    // Calculate Mouse position
        if ((Math.Abs(_lastMousePositionX - cursorX) > 10) ||
            (Math.Abs(_lastMousePositionY - cursorY) > 10))
    ptScreenUL.X = Cursor.Position.X;
    ptScreenUL.Y = Cursor.Position.Y;
    ptClientUL = PointToClient(ptScreenUL);
    int iCursorX = ptClientUL.X;
    int iCursorY = ptClientUL.Y;
      _lastMousePositionX = cursorX;
      _lastMousePositionY = cursorY;
      var fX = ((float)GUIGraphicsContext.Width) / ClientSize.Width;
      var fY = ((float)GUIGraphicsContext.Height) / ClientSize.Height;
      var x = (fX * cursorX) - GUIGraphicsContext.OffsetX;
      var y = (fY * cursorY) - GUIGraphicsContext.OffsetY;
      var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
      {
        if ((Math.Abs(m_iLastMousePositionX - iCursorX) > 10) ||
        var action = new Action(Action.ActionType.ACTION_MOUSE_MOVE, x, y) {MouseButton = e.Button};
          CheckSingleClick();
        }
      }
      // Save last position
  protected override void MouseDoubleClickEvent(MouseEventArgs e)
      m_iLastMousePositionY = iCursorY;
      //this.Text=String.Format("show {0},{1} {2},{3}",e.X,e.Y,m_iLastMousePositionX,m_iLastMousePositionY);
      float fX = ((float)GUIGraphicsContext.Width) / ((float)ClientSize.Width);
      float fY = ((float)GUIGraphicsContext.Height) / ((float)ClientSize.Height);
      float x = (fX * iCursorX) - GUIGraphicsContext.OffsetX;
      float y = (fY * iCursorY) - GUIGraphicsContext.OffsetY;
      GUIWindow window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
    if (!ShowCursor)
      {
      base.MouseClickEvent(e);
        action.MouseButton = e.Button;
    }
  
    var screen = new Point {X = Cursor.Position.X, Y = Cursor.Position.Y};
    var client = PointToClient(screen);
    var cursorX = client.X;
    var cursorY = client.Y;
    }
    var fX = ((float)GUIGraphicsContext.Width) / ClientSize.Width;
    var fY = ((float)GUIGraphicsContext.Height) / ClientSize.Height;
    var x = (fX * cursorX) - GUIGraphicsContext.OffsetX;
    var y = (fY * cursorY) - GUIGraphicsContext.OffsetY;
      base.mouseclick(e);
    _lastMousePositionX = cursorX;
    _lastMousePositionY = cursorY;
    Action actionMove;
    var actionMove = new Action(Action.ActionType.ACTION_MOUSE_MOVE, x, y);
    // Calculate Mouse position
    var action = new Action(Action.ActionType.ACTION_MOUSE_DOUBLECLICK, x, y)
                   {MouseButton = e.Button, SoundFileName = "click.wav"};
    ptScreenUL.Y = Cursor.Position.Y;
    ptClientUL = PointToClient(ptScreenUL);
    int iCursorX = ptClientUL.X;
    GUIGraphicsContext.OnAction(action);
    float fX = ((float)GUIGraphicsContext.Width) / ((float)ClientSize.Width);
  protected override void MouseClickEvent(MouseEventArgs e)
    float x = (fX * iCursorX) - GUIGraphicsContext.OffsetX;
    float y = (fY * iCursorY) - GUIGraphicsContext.OffsetY;
    // Save last position
    if (!ShowCursor)
    m_iLastMousePositionY = iCursorY;
      base.MouseClickEvent(e);
    actionMove = new Action(Action.ActionType.ACTION_MOUSE_MOVE, x, y);
    }
    var mouseButtonRightClick = false;
    action.SoundFileName = "click.wav";
    var screen = new Point {X = Cursor.Position.X, Y = Cursor.Position.Y};
    var client = PointToClient(screen);
    var cursorX = client.X;
    var cursorY = client.Y;

    var fX = ((float)GUIGraphicsContext.Width) / ClientSize.Width;
    var fY = ((float)GUIGraphicsContext.Height) / ClientSize.Height;
    var x = (fX * cursorX) - GUIGraphicsContext.OffsetX;
    var y = (fY * cursorY) - GUIGraphicsContext.OffsetY;
    {
    _lastMousePositionX = cursorX;
    _lastMousePositionY = cursorY;
    }
    var actionMove = new Action(Action.ActionType.ACTION_MOUSE_MOVE, x, y);
    Action action;
    bool MouseButtonRightClick = false;
    // Calculate Mouse position
    Point ptClientUL;
    Point ptScreenUL = new Point();
        if (_mouseClickTimer != null)
    ptScreenUL.Y = Cursor.Position.Y;
          _mouseClickFired = false;
    int iCursorX = ptClientUL.X;
    int iCursorY = ptClientUL.Y;
            _lastMouseClickEvent = e;
            _mouseClickFired = true;
            _mouseClickTimer.Start();
    float x = (fX * iCursorX) - GUIGraphicsContext.OffsetX;
          }
          // Double click used as right click
          _lastMouseClickEvent = null;
          _mouseClickTimer.Stop();
          mouseButtonRightClick = true;
    if (e.Button == MouseButtons.Left)
    {
      if (GUIGraphicsContext.DBLClickAsRightClick)
        action = new Action(Action.ActionType.ACTION_MOUSE_CLICK, x, y)
                   {MouseButton = e.Button, SoundFileName = "click.wav"};
          bMouseClickFired = false;
          if (e.Clicks < 2)
          {
            eLastMouseClickEvent = e;
            bMouseClickFired = true;
            tMouseClickTimer.Start();
            return;
          }
    if ((e.Button == MouseButtons.Right) || (mouseButtonRightClick))
          {
      var window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
            eLastMouseClickEvent = null;
            tMouseClickTimer.Stop();
            MouseButtonRightClick = true;
          }
        action = new Action(Action.ActionType.ACTION_CONTEXT_MENU, x, y)
                   {MouseButton = e.Button, SoundFileName = "click.wav"};
      {
        action = new Action(Action.ActionType.ACTION_MOUSE_CLICK, x, y);
        action.MouseButton = e.Button;
        action.SoundFileName = "click.wav";
        if (action.SoundFileName.Length > 0 && !g_Player.Playing)
        {
          Utils.PlaySound(action.SoundFileName, false, true);
        var key = new Key(0, (int)Keys.Escape);
        GUIGraphicsContext.OnAction(action);
        return;
      }
    }
    // right mouse button=back
    if ((e.Button == MouseButtons.Right) || (MouseButtonRightClick))
    {
      GUIWindow window = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
      if ((window.GetFocusControlId() != -1) || GUIGraphicsContext.IsFullScreenVideo ||
          (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_SLIDESHOW))
      {
        // Get context menu
        action = new Action(Action.ActionType.ACTION_CONTEXT_MENU, x, y);
        action.MouseButton = e.Button;
        action.SoundFileName = "click.wav";
      var key = new Key('y', 0);
        {
          Utils.PlaySound(action.SoundFileName, false, true);
        }
        GUIGraphicsContext.OnAction(action);
      }
      else
      {
        GUIGraphicsContext.OnAction(action);
        if (ActionTranslator.GetAction(GUIWindowManager.ActiveWindowEx, key, ref action))
        {
          if (action.SoundFileName.Length > 0 && !g_Player.Playing)
  private void MouseClickTimerElapsed(object sender, ElapsedEventArgs e)
            Utils.PlaySound(action.SoundFileName, false, true);
          }
          GUIGraphicsContext.OnAction(action);
          return;
        }
  {
    //middle mouse button=Y
    if (e.Button == MouseButtons.Middle)
    {
      Key key = new Key('y', 0);
      action = new Action();
        // Don't send single click (only the mouse move event is send)
        _mouseClickFired = false;
        if (action.SoundFileName.Length > 0 && !g_Player.Playing)
        {
          Utils.PlaySound(action.SoundFileName, false, true);
    if (_mouseClickTimer != null)
        GUIGraphicsContext.OnAction(action);
      _mouseClickTimer.Stop();
      if (_mouseClickFired)
    }
        var fX = ((float)GUIGraphicsContext.Width) / ClientSize.Width;
        var fY = ((float)GUIGraphicsContext.Height) / ClientSize.Height;
        var x = (fX * _lastMousePositionX) - GUIGraphicsContext.OffsetX;
        var y = (fY * _lastMousePositionY) - GUIGraphicsContext.OffsetY;
        _mouseClickFired = false;
        var action = new Action(Action.ActionType.ACTION_MOUSE_CLICK, x, y)
                       {MouseButton = _lastMouseClickEvent.Button, SoundFileName = "click.wav"};
  {
    Action action;
    // Check for touchscreen users and TVGuide items
    if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_TVGUIDE)
    {
      GUIWindow pWindow = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
      if ((pWindow.GetFocusControlId() == 1) && (GUIWindowManager.RoutedWindow == -1))
      {
        // Dont send single click (only the mouse move event is send)
        bMouseClickFired = false;
    if (Volume > 0 && (g_Player.IsVideo || g_Player.IsTV))
      }
      g_Player.Volume = Volume;
    if (tMouseClickTimer != null)
    {
      tMouseClickTimer.Stop();
      if (bMouseClickFired)
      {
    RestoreFromTray();
        float fY = ((float)GUIGraphicsContext.Height) / ((float)ClientSize.Height);
        float x = (fX * m_iLastMousePositionX) - GUIGraphicsContext.OffsetX;
        float y = (fY * m_iLastMousePositionY) - GUIGraphicsContext.OffsetY;
        bMouseClickFired = false;
        action = new Action(Action.ActionType.ACTION_MOUSE_CLICK, x, y);
        action.MouseButton = eLastMouseClickEvent.Button;
        action.SoundFileName = "click.wav";
        if (action.SoundFileName.Length > 0 && !g_Player.Playing)
        {
          Utils.PlaySound(action.SoundFileName, false, true);
        }
        GUIGraphicsContext.OnAction(action);
      }
    }
  }

  protected override void Restore_OnClick(Object sender, EventArgs e)
  {
    if (m_iVolume > 0 && (g_Player.IsVideo || g_Player.IsTV))
    {
      g_Player.Volume = m_iVolume;
      if (g_Player.Paused)
      {
        g_Player.Pause();
      }
    }
    Restore();
  }

  #endregion

#if AUTOUPDATE
  private void MediaPortal_Closed(object sender, EventArgs e)
  {
    StopUpdater();
  }
		
  private void CurrentDomain_ProcessExit(object sender, EventArgs e)
  {
    StopUpdater();
  }

  private delegate void MarshalEventDelegate(object sender, UpdaterActionEventArgs e);
 
  private void OnUpdaterDownloadStartedHandler(object sender, UpdaterActionEventArgs e) 
  {		
    Log.Info("Main: Update - Download started for: {0}",e.ApplicationName);
  }

  private void OnUpdaterDownloadStarted(object sender, UpdaterActionEventArgs e)
  { 
    this.Invoke(
      new MarshalEventDelegate(this.OnUpdaterDownloadStartedHandler), 
      new object[] { sender, e });
  }

  private void CheckForNewUpdate()
  {
    if (!m_bNewVersionAvailable) return;
    if (GUIWindowManager.IsRouted) return;
    g_Player.Stop();
    GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_ASKYESNO,0,0,0,0,0,0);
    msg.Param1=709;
    msg.Param2=710;
    msg.Param3=0;
    GUIWindowManager.SendMessage(msg);
    if (msg.Param1==0) 
    {
      Log.Info("Main: Update - User canceled download");
      m_bCancelVersion = true;
      m_bNewVersionAvailable = false;
      return;
    }
    m_bCancelVersion = false;
    m_bNewVersionAvailable = false;
  }

  private void OnUpdaterUpdateAvailable(object sender, UpdaterActionEventArgs e)
  {
    Log.Info("Main: Update - New version available: {0}", e.ApplicationName);
    m_strNewVersion = e.ServerInformation.AvailableVersion;
    m_bNewVersionAvailable = true;
    while (m_bNewVersionAvailable) System.Threading.Thread.Sleep(100);
    if (m_bCancelVersion)
    {
      _updater.StopUpdater(e.ApplicationName);
    }
  }

  private void OnUpdaterDownloadCompletedHandler(object sender, UpdaterActionEventArgs e)
  {
    Log.Info("Main: Update - Download completed");
    StartNewVersion();
  }

  private void OnUpdaterDownloadCompleted(object sender, UpdaterActionEventArgs e)
  {
    //  using the synchronous "Invoke".  This marshals from the eventing thread--which comes from the Updater and should not
    //  be allowed to enter and "touch" the UI's window thread
    //  so we use Invoke which allows us to block the Updater thread at will while only allowing window thread to update UI
    this.Invoke(
      new MarshalEventDelegate(this.OnUpdaterDownloadCompletedHandler), 
      new object[] { sender, e });
  }

  private void StartNewVersion()
  {
    Log.Info("Main: Update - Starting appstart.exe");
    XmlDocument doc = new XmlDocument();
    //  load config file to get base dir
    doc.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
    //  get the base dir
    string baseDir = System.IO.Directory.GetCurrentDirectory(); //doc.SelectSingleNode("configuration/appUpdater/UpdaterConfiguration/application/client/baseDir").InnerText;
    string newDir = Path.Combine(baseDir, "AppStart.exe");
		ClientApplicationInfo clientInfoNow = ClientApplicationInfo.Deserialize("MediaPortal.exe.config");
    ClientApplicationInfo clientInfo = ClientApplicationInfo.Deserialize("AppStart.exe.config");
    clientInfo.AppFolderName = System.IO.Directory.GetCurrentDirectory();
    ClientApplicationInfo.Save("AppStart.exe.config",clientInfo.AppFolderName, clientInfoNow.InstalledVersion);
    ProcessStartInfo process = new ProcessStartInfo(newDir);
    process.WorkingDirectory = baseDir;
    process.Arguments = clientInfoNow.InstalledVersion;
    //  launch new version (actually, launch AppStart.exe which HAS pointer to new version )
    System.Diagnostics.Process.Start(process);
    //  tell updater to stop
    Log.Info("Main: Update - Stopping MP");
    CurrentDomain_ProcessExit(null, null);
    //  leave this app
    Environment.Exit(0);
  }

  private void btnStop_Click(object sender, System.EventArgs e)
  {
    StopUpdater();
  }

  private void StopUpdater()
  {
    if (_updater==null) return;
    //  tell updater to stop
    _updater.StopUpdater();
    {
      //  join the updater thread with a suitable timeout
      bool isThreadJoined = _updaterThread.Join(UPDATERTHREAD_JOIN_TIMEOUT);
      //  check if we joined, if we didn't interrupt the thread
      if (!isThreadJoined)
      {
        _updaterThread.Interrupt();	
      }
      _updaterThread = null;
    }
  }
#endif

  private void OnMessage(GUIMessage message)
    if (_suspended)
    {
      return;
    switch (message.Message)
    {
      case GUIMessage.MessageType.GUI_MSG_RESTART_REMOTE_CONTROLS:
      case GUIMessage.MessageType.GUI_MSG_TUNE_EXTERNAL_CHANNEL:
        var bIsInteger = Double.TryParse(message.Label, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out retNum);
      case GUIMessage.MessageType.GUI_MSG_GOTO_WINDOW:
        GUIWindowManager.ActivateWindow(message.Param1);
        if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_TVFULLSCREEN ||
            GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_TELETEXT ||
            _usbuirtdevice.ChangeTunerChannel(message.Label);
            GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_MUSIC)
        {
          GUIGraphicsContext.IsFullScreenVideo = true;
        }
        else
          _winlircdevice.ChangeTunerChannel(message.Label);
          GUIGraphicsContext.IsFullScreenVideo = false;
        }
        break;

      case GUIMessage.MessageType.GUI_MSG_CD_INSERTED:
        AutoPlay.ExamineCD(message.Label);
            _redeyedevice.ChangeTunerChannel(message.Label);

      case GUIMessage.MessageType.GUI_MSG_VOLUME_INSERTED:
        AutoPlay.ExamineVolume(message.Label);
        break;
      case GUIMessage.MessageType.GUI_MSG_TUNE_EXTERNAL_CHANNEL:
        if (GUIGraphicsContext.IsDirectX9ExUsed() && UseEnhancedVideoRenderer)
        double retNum;
        bIsInteger =
          Double.TryParse(message.Label, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out retNum);
        var fullscreen = (message.Param1 != 0);
        {
                  fullscreen && Maximized);
        if (Maximized == false || GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.STOPPING)
            usbuirtdevice.ChangeTunerChannel(message.Label);
          }
        }
        catch (Exception) {}
        try
        {
          winlircdevice.ChangeTunerChannel(message.Label);
        }
        catch (Exception) {}
        try
        {
          if (bIsInteger)
          {
            redeyedevice.ChangeTunerChannel(message.Label);
          }
        }
        catch (Exception) {}
        break;

      case GUIMessage.MessageType.GUI_MSG_SWITCH_FULL_WINDOWED:
        if (GUIGraphicsContext.IsDirectX9ExUsed() && useEnhancedVideoRenderer)
        {
          return;
        }
        bool fullscreen = (message.Param1 != 0);
                  fullscreen && isMaximized);
        if (isMaximized == false || GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.STOPPING)
        {
          return;
          if (Volume > 0 && (g_Player.IsVideo || g_Player.IsTV))
        if (fullscreen)
            g_Player.Volume = Volume;
          //switch to fullscreen mode
          Log.Debug("Main: Goto fullscreen: {0}", GUIGraphicsContext.DX9Device.PresentationParameters.Windowed);
          if (GUIGraphicsContext.DX9Device.PresentationParameters.Windowed)
          {
            SwitchFullScreenOrWindowed(false);
          RestoreFromTray();
        }
        else
        {
          //switch to windowed mode
          Log.Debug("Main: Goto windowed mode: {0}",
          if (!GUIGraphicsContext.DX9Device.PresentationParameters.Windowed)
          {
            SwitchFullScreenOrWindowed(true);
        var dlgOk = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
        }
        // Must set the FVF after reset
        GUIFontManager.SetDevice();
        break;

      case GUIMessage.MessageType.GUI_MSG_GETFOCUS:
        Log.Debug("Main: Setting focus");
        {
        var dlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
            if (g_Player.Paused)
            {
              g_Player.Pause();
            }
          }
          Restore();
        }
        else
        {
        }
        //Force.SetForegroundWindow(this.Handle, true);
        break;

      case GUIMessage.MessageType.GUI_MSG_CODEC_MISSING:
        GUIDialogOK dlgOk = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
        dlgOk.SetHeading(string.Empty);
        dlgOk.SetLine(1, message.Label);
        dlgOk.SetLine(2, string.Empty);
        dlgOk.SetLine(3, message.Label2);
        dlgOk.SetLine(4, message.Label3);
        dlgOk.DoModal(GUIWindowManager.ActiveWindow);
        break;
      _restoreTopMost = true;
      case GUIMessage.MessageType.GUI_MSG_REFRESHRATE_CHANGED:

        GUIDialogNotify dlgNotify =
          (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
        if (dlgNotify != null)
    if (_restoreTopMost)
          dlgNotify.Reset();
          dlgNotify.ClearAll();
      _restoreTopMost = false;
          dlgNotify.SetText(message.Label2);
          dlgNotify.TimeOut = message.Param1;
          dlgNotify.DoModal(GUIWindowManager.ActiveWindow);
        }

        break;
    }
  }

  #endregion

  #region External process start / stop handling

  public void OnStartExternal(Process proc, bool waitForExit)
  {
    if (TopMost && waitForExit)
    {
      TopMost = false;
      restoreTopMost = true;
    }
  }

    if (_supportsFiltering)
  {
    if (restoreTopMost)
    {
      TopMost = true;
      GUIGraphicsContext.DX9Device.SamplerState[0].MaxAnisotropy = _anisotropy;
    }
  }

      GUIGraphicsContext.DX9Device.SamplerState[1].MaxAnisotropy = _anisotropy;

  #region helper funcs

  private void CreateStateBlock()
  {
    GUIGraphicsContext.DX9Device.RenderState.CullMode = Cull.None;
    GUIGraphicsContext.DX9Device.RenderState.Lighting = false;
    GUIGraphicsContext.DX9Device.RenderState.ZBufferEnable = true;
    GUIGraphicsContext.DX9Device.RenderState.FogEnable = false;
    GUIGraphicsContext.DX9Device.RenderState.FillMode = FillMode.Solid;
    if (_supportsAlphaBlend)
    GUIGraphicsContext.DX9Device.RenderState.DestinationBlend = Blend.InvSourceAlpha;
    GUIGraphicsContext.DX9Device.TextureState[0].ColorOperation = TextureOperation.Modulate;
    GUIGraphicsContext.DX9Device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
    GUIGraphicsContext.DX9Device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
    }
    GUIGraphicsContext.DX9Device.TextureState[0].AlphaArgument2 = TextureArgument.Diffuse;
    if (supportsFiltering)
    {
      GUIGraphicsContext.DX9Device.SamplerState[0].MinFilter = TextureFilter.Linear;
      GUIGraphicsContext.DX9Device.SamplerState[0].MagFilter = TextureFilter.Linear;
      GUIGraphicsContext.DX9Device.SamplerState[0].MipFilter = TextureFilter.Linear;
      GUIGraphicsContext.DX9Device.SamplerState[0].MaxAnisotropy = g_nAnisotropy;
    var dateString = DateFormat;
    if (string.IsNullOrEmpty(dateString))
      GUIGraphicsContext.DX9Device.SamplerState[1].MipFilter = TextureFilter.Linear;
      GUIGraphicsContext.DX9Device.SamplerState[1].MaxAnisotropy = g_nAnisotropy;
    }
    var cur = DateTime.Now;
    {
      GUIGraphicsContext.DX9Device.SamplerState[0].MinFilter = TextureFilter.Point;
      GUIGraphicsContext.DX9Device.SamplerState[0].MagFilter = TextureFilter.Point;
      GUIGraphicsContext.DX9Device.SamplerState[0].MipFilter = TextureFilter.Point;
      GUIGraphicsContext.DX9Device.SamplerState[1].MinFilter = TextureFilter.Point;
      GUIGraphicsContext.DX9Device.SamplerState[1].MagFilter = TextureFilter.Point;
      GUIGraphicsContext.DX9Device.SamplerState[1].MipFilter = TextureFilter.Point;
    }
    if (bSupportsAlphaBlend)
    {
      GUIGraphicsContext.DX9Device.RenderState.AlphaTestEnable = true;
      GUIGraphicsContext.DX9Device.RenderState.ReferenceAlpha = 0x01;
      GUIGraphicsContext.DX9Device.RenderState.AlphaFunction = Compare.GreaterEqual;
    }
    return;
  }

  /// <summary>
  /// Get the current date from the system and localize it based on the user preferences.
  /// </summary>
  /// <returns>A string containing the localized version of the date.</returns>
  protected string GetDate()
  {
    string dateString = _dateFormat;
    if ((dateString == null) || (dateString.Length == 0))
    {
      return string.Empty;
    }
    DateTime cur = DateTime.Now;
    string day;
    switch (cur.DayOfWeek)
    {
      case DayOfWeek.Monday:
        day = GUILocalizeStrings.Get(11);
        break;
      case DayOfWeek.Tuesday:
        day = GUILocalizeStrings.Get(12);
        break;
      case DayOfWeek.Wednesday:
        day = GUILocalizeStrings.Get(13);
        break;
      case DayOfWeek.Thursday:
        day = GUILocalizeStrings.Get(14);
        break;
      case DayOfWeek.Friday:
        day = GUILocalizeStrings.Get(15);
        break;
      case DayOfWeek.Saturday:
        day = GUILocalizeStrings.Get(16);
        break;
      default:
        day = GUILocalizeStrings.Get(17);
        break;
    }
    string month;
    switch (cur.Month)
    {
      case 1:
        month = GUILocalizeStrings.Get(21);
        break;
      case 2:
        month = GUILocalizeStrings.Get(22);
        break;
      case 3:
        month = GUILocalizeStrings.Get(23);
        break;
    dateString = Utils.ReplaceTag(dateString, "<DD>", cur.Day.ToString(CultureInfo.InvariantCulture), "unknown");
        month = GUILocalizeStrings.Get(24);
    dateString = Utils.ReplaceTag(dateString, "<MM>", cur.Month.ToString(CultureInfo.InvariantCulture), "unknown");
    dateString = Utils.ReplaceTag(dateString, "<Year>", cur.Year.ToString(CultureInfo.InvariantCulture), "unknown");
        month = GUILocalizeStrings.Get(25);
        break;
      case 6:
        month = GUILocalizeStrings.Get(26);
        break;
      case 7:
        month = GUILocalizeStrings.Get(27);
        break;
      case 8:
  protected string GetTime()
  {
    return DateTime.Now.ToString(UseLongDateFormat 
      ? Thread.CurrentThread.CurrentCulture.DateTimeFormat.LongTimePattern 
      : Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortTimePattern);
  }

        month = GUILocalizeStrings.Get(32);
    var cur = DateTime.Now;
    }
    dateString = Utils.ReplaceTag(dateString, "<Day>", day, "unknown");
    dateString = Utils.ReplaceTag(dateString, "<DD>", cur.Day.ToString(), "unknown");
    dateString = Utils.ReplaceTag(dateString, "<Month>", month, "unknown");
    dateString = Utils.ReplaceTag(dateString, "<MM>", cur.Month.ToString(), "unknown");
    var cur = DateTime.Now;
    dateString = Utils.ReplaceTag(dateString, "<YY>", (cur.Year - 2000).ToString("00"), "unknown");
    GUIPropertyManager.SetProperty("#date", dateString);
    return dateString;
  }

  /// <summary>
  /// Get the current time from the system. Set the format in the Home plugin's config
  /// </summary>
  /// <returns>A string containing the current time.</returns>
  protected string GetTime()
  {
    if (_useLongDateFormat)
    {
      return DateTime.Now.ToString(Thread.CurrentThread.CurrentCulture.DateTimeFormat.LongTimePattern);
    }
    else
    {
      return DateTime.Now.ToString(Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortTimePattern);
    }
  }

  protected string GetDay()
  {
    DateTime cur = DateTime.Now;
    return String.Format("{0}", cur.Day);
  }

  protected string GetShortDayOfWeek()
  {
    DateTime cur = DateTime.Now;
    var cur = DateTime.Now;
    switch (cur.DayOfWeek)
    {
      case DayOfWeek.Monday:
        ddd = GUILocalizeStrings.Get(657);
        break;
      case DayOfWeek.Tuesday:
        ddd = GUILocalizeStrings.Get(658);
        break;
      case DayOfWeek.Wednesday:
        ddd = GUILocalizeStrings.Get(659);
        break;
      case DayOfWeek.Thursday:
        ddd = GUILocalizeStrings.Get(660);
        break;
      case DayOfWeek.Friday:
        ddd = GUILocalizeStrings.Get(661);
        break;
      case DayOfWeek.Saturday:
        ddd = GUILocalizeStrings.Get(662);
        break;
      default:
        ddd = GUILocalizeStrings.Get(663);
        break;
    }
    return ddd;
  }

  protected string GetDayOfWeek()
  {
    DateTime cur = DateTime.Now;
    var cur = DateTime.Now;
    switch (cur.DayOfWeek)
    {
      case DayOfWeek.Monday:
        dddd = GUILocalizeStrings.Get(11);
        break;
    var shortMonthOfYear = GetMonthOfYear();
    shortMonthOfYear = shortMonthOfYear.Substring(0, 3);
    return shortMonthOfYear;
        dddd = GUILocalizeStrings.Get(13);
        break;
      case DayOfWeek.Thursday:
        dddd = GUILocalizeStrings.Get(14);
    var cur = DateTime.Now;
    string monthOfYear;
        dddd = GUILocalizeStrings.Get(15);
        break;
      case DayOfWeek.Saturday:
        monthOfYear = GUILocalizeStrings.Get(21);
        break;
      default:
        monthOfYear = GUILocalizeStrings.Get(22);
        break;
    }
        monthOfYear = GUILocalizeStrings.Get(23);
  }

        monthOfYear = GUILocalizeStrings.Get(24);
  {
    DateTime cur = DateTime.Now;
        monthOfYear = GUILocalizeStrings.Get(25);
  }

        monthOfYear = GUILocalizeStrings.Get(26);
  {
    DateTime cur = DateTime.Now;
        monthOfYear = GUILocalizeStrings.Get(27);
    SMOY = SMOY.Substring(0, 3);
    return SMOY;
        monthOfYear = GUILocalizeStrings.Get(28);

  protected string GetMonthOfYear()
        monthOfYear = GUILocalizeStrings.Get(29);
    DateTime cur = DateTime.Now;
    string MMMM;
        monthOfYear = GUILocalizeStrings.Get(30);
    {
      case 1:
        monthOfYear = GUILocalizeStrings.Get(31);
        break;
      case 2:
        monthOfYear = GUILocalizeStrings.Get(32);
        break;
      case 3:
    return monthOfYear;
        break;
      case 4:
        MMMM = GUILocalizeStrings.Get(24);
        break;
    var cur = DateTime.Now;
        MMMM = GUILocalizeStrings.Get(25);
        break;
      case 6:
        MMMM = GUILocalizeStrings.Get(26);
        break;
    var cur = DateTime.Now;
        MMMM = GUILocalizeStrings.Get(27);
        break;
      case 8:
        MMMM = GUILocalizeStrings.Get(28);
        break;
    using (Settings xmlreader = new MPSettings())
    {
      var ignoreErrors = xmlreader.GetValueAsBool("general", "dontshowskinversion", false);
        MMMM = GUILocalizeStrings.Get(30);
        break;
      }
    }

    var filename = GUIGraphicsContext.GetThemedSkinFile(@"\references.xml");
        MMMM = GUILocalizeStrings.Get(32);
        break;
      var doc = new XmlDocument();
    return MMMM;
      var node = doc.SelectSingleNode("/controls/skin/version");
      if (node != null)
  protected string GetShortYear()
  {
    DateTime cur = DateTime.Now;
    return cur.ToString("yy");
  }

  protected string GetYear()
  {
    DateTime cur = DateTime.Now;
    return cur.ToString("yyyy");
    _outdatedSkinName = Skin;

  protected void CheckSkinVersion()
    var screenRatio = (screenWidth / screenHeight);
    Skin = screenRatio > 1.5 ? "DefaultWide" : "Default";
    Config.SkinName = Skin;
    GUIGraphicsContext.Skin = Skin;
      ignoreErrors = xmlreader.GetValueAsBool("general", "dontshowskinversion", false);
      if (ignoreErrors)
      {
    var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SKIN_CHANGED, 0, 0, 0, 0, 0, null);
      }
    }
    Log.Info("Main: User skin is not compatable, using skin {0} with theme {1}", Skin, GUIThemeManager.CurrentTheme);
    Version versionSkin = null;
    string filename = GUIGraphicsContext.GetThemedSkinFile(@"\references.xml");
    if (File.Exists(filename))
    {
      XmlDocument doc = new XmlDocument();
  public static void SetDWORDRegKey(RegistryKey hklm, string key, string name, Int32 value)
      XmlNode node = doc.SelectSingleNode("/controls/skin/version");
      if (node != null && node.InnerText != null)
      {
      using (var subkey = hklm.CreateSubKey(key))
      }
    }
    if (CompatibilityManager.SkinVersion == versionSkin)
          subkey.SetValue(name, value);
      return;
    }

    // Skin is incompatible, switch to default
    _OutdatedSkinName = m_strSkin;
      Log.Error(@"User does not have sufficient rights to modify registry key HKLM\{0}", key);
    float screenWidth = GUIGraphicsContext.currentScreen.Bounds.Width;
    float screenRatio = (screenWidth / screenHeight);
    m_strSkin = screenRatio > 1.5 ? "DefaultWide" : "Default";
      Log.Error(@"User does not have sufficient rights to modify registry key HKLM\{0}", key);
    GUIGraphicsContext.Skin = m_strSkin;
    SkinSettings.Load();

  public static void SetREGSZRegKey(RegistryKey hklm, string key, string name, string value)
    GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SKIN_CHANGED, 0, 0, 0, 0, 0, null);
    GUIGraphicsContext.SendMessage(msg);

      using (var subkey = hklm.CreateSubKey(key))
  }


          subkey.SetValue(name, value);

  public static void SetDWORDRegKey(RegistryKey hklm, string Key, string Value, Int32 iValue)
  {
    try
    {
      Log.Error(@"User does not have sufficient rights to modify registry key HKLM\{0}", key);
      {
        if (subkey != null)
        {
      Log.Error(@"User does not have sufficient rights to modify registry key HKLM\{0}", key);
        }
      }
    }
    catch (SecurityException)
    {
      Log.Error(@"User does not have sufficient rights to modify registry key HKLM\{0}", Key);
    }
    catch (UnauthorizedAccessException)
    {
      Log.Error(@"User does not have sufficient rights to modify registry key HKLM\{0}", Key);
    }
  }

  public static void SetREGSZRegKey(RegistryKey hklm, string Key, string Name, string Value)
  {
    try
    {
      using (RegistryKey subkey = hklm.CreateSubKey(Key))
      var errorMsg =
        if (subkey != null)
                      aParamVersion);
      Log.Info("Util: quartz.dll error - {0}", errorMsg);
        }
        MessageBox.Show(errorMsg, "Core directshow component (quartz.dll) is outdated!", MessageBoxButtons.OKCancel,
    }
    catch (SecurityException)
    {
      Log.Error(@"User does not have sufficient rights to modify registry key HKLM\{0}", Key);
    }
    catch (UnauthorizedAccessException)
    {
    GUIWindowManager.OnNewAction += OnAction;
    GUIWindowManager.Receivers += OnMessage;
    GUIWindowManager.Callbacks += MediaPortalProcess;

    Utils.OnStartExternal += OnStartExternal;
    Utils.OnStopExternal += OnStopExternal;
  #endregion

  private void DoStartupJobs()
  {
    FilterChecker.CheckInstalledVersions();
    PlaylistPlayer.Init();
    Version aParamVersion;
    //
    // 6.5.2600.3243 = KB941568,   6.5.2600.3024 = KB927544
      var inputEnabled = xmlreader.GetValueAsBool("USBUIRT", "internal", false);
      var outputEnabled = xmlreader.GetValueAsBool("USBUIRT", "external", false);
      !FilterChecker.CheckFileVersion(Environment.SystemDirectory + "\\quartz.dll", "6.5.2600.3024", out aParamVersion))
    {
      string ErrorMsg =
        _usbuirtdevice = USBUIRT.Create(OnRemoteCommand);
                      aParamVersion.ToString());
      Log.Info("Util: quartz.dll error - {0}", ErrorMsg);
      if (
      var winlircInputEnabled = xmlreader.GetValueAsString("WINLIRC", "enabled", "false") == "true";
                        MessageBoxIcon.Exclamation) == DialogResult.OK)
      {
        Process.Start(@"http://wiki.team-mediaportal.com/GeneralRequirements");
        _winlircdevice = new WinLirc();
    }

    EnableS3Trick();
    GUIWindowManager.OnNewAction += new OnActionHandler(OnAction);
    GUIWindowManager.Receivers += new SendMessageHandler(OnMessage);
    GUIWindowManager.Callbacks += new GUIWindowManager.OnCallBackHandler(MPProcess);
    GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STARTING;
        _redeyedevice = RedEye.Create(OnRemoteCommand);
    Utils.OnStopExternal += new Utils.UtilEventHandler(OnStopExternal);
    // load keymapping from keymap.xml
    ActionTranslator.Load();
    //register the playlistplayer for thread messages (like playback stopped,ended)
    Log.Info("Main: Init playlist player");
    g_Player.Factory = new PlayerFactory();
        _serialuirdevice = SerialUIR.Create(OnRemoteCommand);
    // Only load the USBUIRT device if it has been enabled in the configuration
    using (Settings xmlreader = new MPSettings())
    {
      bool inputEnabled = xmlreader.GetValueAsBool("USBUIRT", "internal", false);
      bool outputEnabled = xmlreader.GetValueAsBool("USBUIRT", "external", false);
      if (inputEnabled || outputEnabled)
      {
        Log.Info("Main: Creating the USBUIRT device");
        usbuirtdevice = USBUIRT.Create(new USBUIRT.OnRemoteCommand(OnRemoteCommand));
        Log.Info("Main: Creating the USBUIRT device done");
      }
      //Load Winlirc if enabled.
      bool winlircInputEnabled = xmlreader.GetValueAsString("WINLIRC", "enabled", "false") == "true";
    var doc = new XmlDocument();
      {
        Log.Info("Main: Creating the WINLIRC device");
        winlircdevice = new WinLirc();
      var node = doc.SelectSingleNode("/configuration/appStart/ClientApplicationInfo/appFolderName");
      }
      //Load RedEye if enabled.
      bool redeyeInputEnabled = xmlreader.GetValueAsString("RedEye", "internal", "false") == "true";
      if (redeyeInputEnabled)
      {
        Log.Info("Main: Creating the REDEYE device");
        redeyedevice = RedEye.Create(new RedEye.OnRemoteCommand(OnRemoteCommand));
        Log.Info("Main: Creating the RedEye device done");
      }
      inputEnabled = xmlreader.GetValueAsString("SerialUIR", "internal", "false") == "true";
      if (inputEnabled)
      {
        Log.Info("Main: Creating the SerialUIR device");
        serialuirdevice = SerialUIR.Create(new SerialUIR.OnRemoteCommand(OnRemoteCommand));
        Log.Info("Main: Creating the SerialUIR device done");
      }
    }
    //registers the player for video window size notifications
    Log.Info("Main: Init players");
    g_Player.Init();
    GUIGraphicsContext.ActiveForm = Handle;
    //  hook ProcessExit for a chance to clean up when closed peremptorily
#if AUTOUPDATE
    AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
    //  hook form close to stop updater too
    this.Closed += new EventHandler(MediaPortal_Closed);
#endif
    XmlDocument doc = new XmlDocument();
    try
    {
      doc.Load("mediaportal.exe.config");
      XmlNode node = doc.SelectSingleNode("/configuration/appStart/ClientApplicationInfo/appFolderName");
      if (node != null)
      {
        node.InnerText = Directory.GetCurrentDirectory();
      }
      node = doc.SelectSingleNode("/configuration/appUpdater/UpdaterConfiguration/application/client/baseDir");
      if (node != null)
      {
        node.InnerText = Directory.GetCurrentDirectory();
      }
      node = doc.SelectSingleNode("/configuration/appUpdater/UpdaterConfiguration/application/client/tempDir");
      if (node != null)
      {
        node.InnerText = Directory.GetCurrentDirectory();
      }
      doc.Save("MediaPortal.exe.config");
    }
    catch (Exception) {}
    Thumbs.CreateFolders();
    try
    {
#if DEBUG
#else
#if AUTOUPDATE
      config.Applications[0].Client.XmlFile =  Config.Get(Config.Dir.Base) + "MediaPortal.exe.config";
      config.Applications[0].Server.ServerManifestFileDestination =  Config.Get(Config.Dir.Base) + @"xml\ServerManifest.xml";
      try
      {
        System.IO.Directory.CreateDirectory(config.Applications[0].Client.BaseDir + @"temp");
				System.IO.Directory.CreateDirectory(config.Applications[0].Client.BaseDir + @"xml");
				System.IO.Directory.CreateDirectory(config.Applications[0].Client.BaseDir + @"log");
			}
			catch(Exception){}
			Utils.DeleteFiles(config.Applications[0].Client.BaseDir + "log", "*.log");
			ClientApplicationInfo clientInfo = ClientApplicationInfo.Deserialize("MediaPortal.exe.config");
			clientInfo.AppFolderName = System.IO.Directory.GetCurrentDirectory();
  /// the value.  The reason for this configuration option is a defect in the S3 implementation
			m_strCurrentVersion = clientInfo.InstalledVersion;
			Text += (" - [v" + m_strCurrentVersion + "]");
			//  make an Updater for use in-process with us
			_updater = new ApplicationUpdateManager();
			//  hook Updater events
			_updater.DownloadStarted += new UpdaterActionEventHandler(OnUpdaterDownloadStarted);
			_updater.UpdateAvailable += new UpdaterActionEventHandler(OnUpdaterUpdateAvailable);
			_updater.DownloadCompleted += new UpdaterActionEventHandler(OnUpdaterDownloadCompleted);
			//  start the updater on a separate thread so that our UI remains responsive
			_updaterThread = new Thread(new ThreadStart(_updater.StartUpdater));
			_updaterThread.Start();
      using (var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", true))
      {
        if (services != null)
        {
          var usb = services.OpenSubKey("usb", true) ?? services.CreateSubKey("usb");
          //  Delete the USBBIOSHACKS value if it is still there.  See the remarks.
          if (usb != null && usb.GetValue("USBBIOSHacks") != null)
  /// <para>This method checks whether the <b>enables3trick</b> option in the <b>general</b>
            usb.DeleteValue("USBBIOSHacks");
  /// the value.  The reason for this configuration option is a bug in the S3 implementation
          //Check the general.enables3trick configuration option and create/delete the USBBIOSx
          //value accordingly
          using (Settings xmlreader = new MPSettings())
  /// reboot immediately after hibernating.</para>
            if (usb != null)
  /// which according to this article http://support.microsoft.com/kb/841858/en-us should
              var enableS3Trick = xmlreader.GetValueAsBool("general", "enables3trick", true);
              if (enableS3Trick)
              {
                usb.SetValue("USBBIOSx", 0);
              }
              else if (usb.GetValue("USBBIOSx") != null)
              {
                usb.DeleteValue("USBBIOSx");
              }
            }
          }
        }
  {
    try
    {
      using (RegistryKey services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", true))
      {
        RegistryKey usb = services.OpenSubKey("usb", true);
        if (usb == null)
        {
          usb = services.CreateSubKey("usb");
        }
        //Delete the USBBIOSHACKS value if it is still there.  See the remarks.
        if (usb.GetValue("USBBIOSHacks") != null)
        {
          usb.DeleteValue("USBBIOSHacks");
        }
        //Check the general.enables3trick configuration option and create/delete the USBBIOSx
        //value accordingly
        using (Settings xmlreader = new MPSettings())
        {
          bool enableS3Trick = xmlreader.GetValueAsBool("general", "enables3trick", true);
          if (enableS3Trick)
          {
            usb.SetValue("USBBIOSx", 0);
          }
          else
          {
            if (usb.GetValue("USBBIOSx") != null)
            {
              usb.DeleteValue("USBBIOSx");
            }
          }
        }
      }
    }
    catch (SecurityException)
    {
      Log.Info("Not enough permissions to enable/disable the S3 standby trick");
    }
    catch (UnauthorizedAccessException)
    {
      Log.Info("No write permissions to enable/disable the S3 standby trick");
    }
  }
}