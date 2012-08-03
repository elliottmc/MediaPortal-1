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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Playlists;
using MediaPortal.Profile;
using MediaPortal.Properties;
using MediaPortal.UserInterface.Controls;
using MediaPortal.Util;
using MediaPortal.Video.Database;
using Microsoft.DirectX.Direct3D;
using WPFMediaKit.DirectX;

#endregion

namespace MediaPortal
{
  /// <summary>
  /// The base class for all the graphics (D3D) samples, it derives from windows forms
  /// </summary>
  public class D3DApp : MPForm
  {
    internal static int ScreenNumberOverride; // 0 or higher means it is set

    protected static bool FullscreenOverride;
    protected static bool WindowedOverride;
    protected static string StrSkinOverride;
    protected string Skin;
    protected PlayListPlayer PlaylistPlayer;
    protected Size OurClientSize;
    protected bool MinimizeOnStartup; // Minimize to tray on startup and on GUI exit
    protected bool MinimizeOnGuiExit;
    protected bool ShuttingDown;
    protected bool FirstTimeWindowDisplayed;
    protected bool AutoHideMouse;
    protected DateTime MouseTimeOutTimer;
    protected bool ResizeOngoing; // this is true only when user is resizing the form
    protected int Frames; // Number of frames since our last update
    protected int Volume;
    protected bool Maximized; // Are we maximized?
    protected bool ShowCursor;
    protected bool Windowed; // Internal variables for the state of the app
    protected bool Ready;
    protected string FrameStatsLine1; // 1st string to hold frame stats
    protected string FrameStatsLine2; // 2nd string to hold frame stats
    protected bool AutoHideTaskbar = true;
    protected bool UseEnhancedVideoRenderer;

    private static Stopwatch _clockWatch;
    private readonly Control _renderTarget;
    private readonly bool _useExclusiveDirectXMode;
    private readonly bool _alwaysOnTopConfig;
    private readonly bool _disableMouseEvents;
    private readonly bool _showCursorWhenFullscreen; // Whether to show cursor when fullscreen
    private readonly PresentParameters _presentParams; // Parameters for CreateDevice/Reset
    private readonly D3DSettings _graphicsSettings;
    private readonly D3DEnumeration _enumerationSettings; // We need to keep track of our enumeration settings

    private MainMenu _menuStripMain;
    private MenuItem _menuItemFile;
    private MenuItem _menuItemChangeDevice;
    private MenuItem _menuBreakFile;
    private MenuItem _menuItemExit;
    private bool _miniTvMode; // minitv means minimum size < 720, always on top, focus may leave
    private bool _isClosing; // Are we closing?
    private bool _lastShowCursor;
    private bool _active;
    private bool _fromTray; // tracks if we restored from tray
    private bool _toggleFullWindowed;

    private bool _needReset;
    private bool _ignoreNextResizeEvent; // True if next event should be ignored (min/max happened)
    private FormWindowState _windowState;
    private Rectangle _oldBounds;
    private MenuItem _menuItemOptions;
    private MenuItem _menuItemConfiguration;
    private MenuItem _menuItemWizards;
    private MenuItem _menuItemDvd;
    private MenuItem _menuItemMovies;
    private MenuItem _menuItemMusic;
    private MenuItem _menuItemPictures;
    private MenuItem _menuItemTelevision;
    private MenuItem _menuItemContext;
    private MenuItem _menuItem5;
    private MenuItem _menuItemFullscreen;
    private MenuItem _menuItemMiniTv;
    private IContainer _components;
    private NotifyIcon _notifyIcon;
    private ContextMenu _contextMenu;
    private bool _wasPlayingVideo;
    private int _lastActiveWindow;
    private double _currentPlayerPos;
    private string _strCurrentFile;
    private PlayListType _currentPlayListType;
    private PlayList _currentPlayList;
    private bool _alwaysOnTop;
    private Win32API.MSG _msgApi;
    private long _lastTime;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="caps"></param>
    /// <param name="vertexProcessingType"></param>
    /// <param name="adapterFormat"></param>
    /// <param name="backBufferFormat"></param>
    /// <returns></returns>
    protected virtual bool ConfirmDevice(Caps caps, VertexProcessingType vertexProcessingType,
                                         Format adapterFormat, Format backBufferFormat)
    {
      return true;
    }


    /// <summary>
    /// 
    /// </summary>
    protected virtual void OneTimeSceneInitialization() {}


    /// <summary>
    /// 
    /// </summary>
    protected virtual void Initialize() {}


    /// <summary>
    /// 
    /// </summary>
    protected virtual void InitializeDeviceObjects() {}


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void OnDeviceLost(Object sender, EventArgs e) {}


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void OnDeviceReset(Object sender, EventArgs e) {}


    /// <summary>
    /// 
    /// </summary>
    protected virtual void FrameMove() {}


    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnProcess() {}


    /// <summary>
    /// 
    /// </summary>
    /// <param name="timePassed"></param>
    protected virtual void Render(float timePassed) {}


    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnStartup() {}


    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnExit() {}

    /// <summary>
    /// Constructor
    /// </summary>
    public D3DApp()
    {
      _clockWatch = new Stopwatch();
      Skin = "Default";
      StrSkinOverride = string.Empty;
      WindowedOverride = false;
      FullscreenOverride = false;
      ScreenNumberOverride = -1;
      OurClientSize = new Size(0, 0);
      MinimizeOnStartup = false;
      MinimizeOnGuiExit = false;
      ShuttingDown = false;
      FirstTimeWindowDisplayed = true;
      AutoHideMouse = false;
      MouseTimeOutTimer = DateTime.Now;
      ResizeOngoing = false;
      Frames = 0;
      Volume = -1;
      Maximized = false;
      ShowCursor = false;
      _currentPlayListType = PlayListType.PLAYLIST_NONE;
      _lastActiveWindow = -1;
      _windowState = FormWindowState.Normal;
      _lastShowCursor = true;
      _enumerationSettings = new D3DEnumeration();
      _graphicsSettings = new D3DSettings();
      _presentParams = new PresentParameters();
      _active                   = false;
      Ready                    = false;
      _renderTarget             = this;
      FrameStatsLine1          = null;
      FrameStatsLine2          = null;
      Text                     = Resources.D3DApp_NotifyIcon_MediaPortal;
      ClientSize               = new Size(720, 576);
      KeyPreview               = true;
      _showCursorWhenFullscreen = false;

      using (Settings xmlreader = new MPSettings())
      {
        _useExclusiveDirectXMode = xmlreader.GetValueAsBool("general", "exclusivemode", true);
        UseEnhancedVideoRenderer = xmlreader.GetValueAsBool("general", "useEVRenderer", false);
        if (UseEnhancedVideoRenderer)
        {
          _useExclusiveDirectXMode = false;
        }
        AutoHideTaskbar = xmlreader.GetValueAsBool("general", "hidetaskbar", true);
        _alwaysOnTopConfig = _alwaysOnTop = xmlreader.GetValueAsBool("general", "alwaysontop", false);
        _disableMouseEvents = xmlreader.GetValueAsBool("remote", "CentareaJoystickMap", false);
      }

      // When clipCursorWhenFullscreen is TRUE, the cursor is limited to
      // the device window when the app goes fullscreen.  This prevents users
      // from accidentally clicking outside the app window on a multi display system.
      // This flag is turned off by default for debug builds, since it makes 
      // multi display debugging difficult.
      InitializeComponent();

      _menuItemMiniTv.Checked = _miniTvMode;

      GUIGraphicsContext.IsVMR9Exclusive = _useExclusiveDirectXMode;
      GUIGraphicsContext.IsEvr = UseEnhancedVideoRenderer;
      PlaylistPlayer = PlayListPlayer.SingletonPlayer;
    }


    /// <summary>
    /// 
    /// </summary>
    public override sealed string Text
    {
      get
      {
        return base.Text;
      }
      set
      {
        base.Text = value;
      }
    }


    /// <summary>
    /// Picks the best graphics device, and initializes it
    /// </summary>
    /// <returns>true if a good device was found, false otherwise</returns>
    /// 
    public bool CreateGraphicsSample()
    {
      _enumerationSettings.ConfirmDeviceCallback = ConfirmDevice;
      _enumerationSettings.Enumerate();

      if (_renderTarget.Cursor == null)
      {
        // Set up a default cursor
        _renderTarget.Cursor = Cursors.Default;
      }
      // if our render target is the main window and we haven't said 
      // ignore the menus, add our menu
      if (_renderTarget == this)
      {
        Menu = _menuStripMain;
      }

      try
      {
        ChooseInitialSettings();
        DXUtil.Timer(DirectXTimer.Start);

        // Initialize the application timer
        var formOnScreen = Screen.FromRectangle(Bounds);
        if (!formOnScreen.Equals(GUIGraphicsContext.currentScreen))
        {
          var location = Location;
          location.X = location.X - formOnScreen.Bounds.Left + GUIGraphicsContext.currentScreen.Bounds.Left;
          location.Y = location.Y - formOnScreen.Bounds.Top + GUIGraphicsContext.currentScreen.Bounds.Top;
          Location = location;
        }
        _oldBounds = Bounds;

        using (Settings xmlreader = new MPSettings())
        {
          var startFullscreen = !WindowedOverride && (FullscreenOverride || xmlreader.GetValueAsBool("general", "startfullscreen", false));
          if (startFullscreen)
          {
            if (AutoHideTaskbar && !MinimizeOnStartup)
            {
              HideTaskBar();
            }

            Log.Info("D3D: Starting fullscreen");
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Menu = null;
            var newBounds = GUIGraphicsContext.currentScreen.Bounds;
            Bounds = newBounds;
            ClientSize = newBounds.Size;
            Log.Info("D3D: Client size: {0}x{1} - Screen: {2}x{3}",
                     ClientSize.Width, ClientSize.Height,
                     GUIGraphicsContext.currentScreen.Bounds.Width, GUIGraphicsContext.currentScreen.Bounds.Height);
            Maximized = true;
          }
        }

        // Initialize the 3D environment for the app
        InitializeEnvironment();
        // Initialize the app's custom scene stuff
        OneTimeSceneInitialization();
      }
      catch (SampleException exception)
      {
        HandleSampleException(exception, ApplicationMessage.ApplicationMustExit);
        return false;
      }
      catch
      {
        HandleSampleException(new SampleException(), ApplicationMessage.ApplicationMustExit);
        return false;
      }

      // The app is ready to go
      Ready = true;
      return Ready;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hide"></param>
    protected static void HideTaskBar(bool hide = true)
    {
      Log.Info(hide ? "D3D: Hiding Taskbar" : "D3D: Showing Taskbar");
      Win32API.EnableStartBar(!hide);
      Win32API.ShowStartBar(!hide);
    }


    /// <summary>
    /// Finds the adapter that has the specified screen on its primary monitor
    /// </summary>
    /// <returns>The adapter that has the specified screen on its primary monitor</returns>
    public GraphicsAdapterInfo FindAdapterForScreen(Screen screen)
    {
      foreach (GraphicsAdapterInfo adapterInfo in _enumerationSettings.AdapterInfoList)
      {
        var hMon = Manager.GetAdapterMonitor(adapterInfo.AdapterOrdinal);

        var info = new NativeMethods.MonitorInformation();
        info.Size = (uint)Marshal.SizeOf(info);
        NativeMethods.GetMonitorInfo(hMon, ref info);
        var rect = Screen.FromRectangle(info.MonitorRectangle).Bounds;

        if (rect.Equals(screen.Bounds))
        {
          return adapterInfo;
        }
      }
      return null;
    }


    /// <summary>
    /// Sets up graphicsSettings with best available windowed mode, subject to 
    /// the doesRequireHardware and doesRequireReference constraints.  
    /// </summary>
    /// <param name="doesRequireHardware">Does the device require hardware support</param>
    /// <param name="doesRequireReference">Does the device require the ref device</param>
    /// <returns>true if a mode is found, false otherwise</returns>
    public bool FindBestWindowedMode(bool doesRequireHardware, bool doesRequireReference)
    {
      // Get display mode of primary adapter (which is assumed to be where the window 
      // will appear)
      var primaryDesktopDisplayMode = Manager.Adapters[0].CurrentDisplayMode;

      GraphicsAdapterInfo bestAdapterInfo = null;
      GraphicsDeviceInfo bestDeviceInfo = null;
      DeviceCombo bestDeviceCombo = null;
      foreach (GraphicsAdapterInfo adapterInfoIterate in _enumerationSettings.AdapterInfoList)
      {
        var adapterInfo = adapterInfoIterate;

        if (GUIGraphicsContext._useScreenSelector)
        {
          adapterInfo = FindAdapterForScreen(GUIGraphicsContext.currentScreen);
          primaryDesktopDisplayMode = Manager.Adapters[adapterInfo.AdapterOrdinal].CurrentDisplayMode;
          GUIGraphicsContext.currentScreenNumber = adapterInfo.AdapterOrdinal;
        }
        foreach (GraphicsDeviceInfo deviceInfo in adapterInfo.DeviceInfoList)
        {
          if (doesRequireHardware && deviceInfo.DevType != DeviceType.Hardware)
          {
            continue;
          }
          if (doesRequireReference && deviceInfo.DevType != DeviceType.Reference)
          {
            continue;
          }

          foreach (DeviceCombo deviceCombo in deviceInfo.DeviceComboList)
          {
            var adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
            if (!deviceCombo.IsWindowed)
            {
              continue;
            }
            if (deviceCombo.AdapterFormat != primaryDesktopDisplayMode.Format)
            {
              continue;
            }

            // If we haven't found a compatible DeviceCombo yet, or if this set
            // is better (because it's a HAL, and/or because formats match better),
            // save it
            if (bestDeviceCombo == null ||
                bestDeviceCombo.DevType != DeviceType.Hardware && deviceInfo.DevType == DeviceType.Hardware ||
                deviceCombo.DevType == DeviceType.Hardware && adapterMatchesBackBuffer)
            {
              bestAdapterInfo = adapterInfo;
              bestDeviceInfo = deviceInfo;
              bestDeviceCombo = deviceCombo;
              if (deviceInfo.DevType == DeviceType.Hardware && adapterMatchesBackBuffer)
              {
                // This windowed device combo looks great -- take it
                goto EndWindowedDeviceComboSearch;
              }
              // Otherwise keep looking for a better windowed device combo
            }
          }
        }
        if (GUIGraphicsContext._useScreenSelector)
        {
          break; // no need to loop again.. result would be the same
        }
      }

      EndWindowedDeviceComboSearch:
      if (bestDeviceCombo == null)
      {
        return false;
      }

      _graphicsSettings.WindowedAdapterInfo = bestAdapterInfo;
      _graphicsSettings.WindowedDeviceInfo = bestDeviceInfo;
      _graphicsSettings.WindowedDeviceCombo = bestDeviceCombo;
      _graphicsSettings.IsWindowed = true;
      _graphicsSettings.WindowedDisplayMode = primaryDesktopDisplayMode;
      _graphicsSettings.WindowedWidth = _renderTarget.ClientRectangle.Right - _renderTarget.ClientRectangle.Left;
      _graphicsSettings.WindowedHeight = _renderTarget.ClientRectangle.Bottom - _renderTarget.ClientRectangle.Top;
      if (_enumerationSettings.AppUsesDepthBuffer)
      {
        _graphicsSettings.WindowedDepthStencilBufferFormat = (DepthFormat)bestDeviceCombo.DepthStencilFormatList[0];
      }
      const int iQuality = 0;
      _graphicsSettings.WindowedMultisampleType = (MultiSampleType)bestDeviceCombo.MultiSampleTypeList[iQuality];
      _graphicsSettings.WindowedMultisampleQuality = 0;

      _graphicsSettings.WindowedVertexProcessingType = (VertexProcessingType)bestDeviceCombo.VertexProcessingTypeList[0];
      _graphicsSettings.WindowedPresentInterval = (PresentInterval)bestDeviceCombo.PresentIntervalList[0];

      return true;
    }


    /// <summary>
    /// Sets up graphicsSettings with best available fullscreen mode, subject to 
    /// the doesRequireHardware and doesRequireReference constraints.  
    /// </summary>
    /// <param name="doesRequireHardware">Does the device require hardware support</param>
    /// <param name="doesRequireReference">Does the device require the ref device</param>
    /// <returns>true if a mode is found, false otherwise</returns>
    public bool FindBestFullscreenMode(bool doesRequireHardware, bool doesRequireReference)
    {
      // For fullscreen, default to first HAL DeviceCombo that supports the current desktop 
      // display mode, or any display mode if HAL is not compatible with the desktop mode, or 
      // non-HAL if no HAL is available
      var bestAdapterDesktopDisplayMode = new DisplayMode();
      var bestDisplayMode = new DisplayMode();
      bestAdapterDesktopDisplayMode.Width = 0;
      bestAdapterDesktopDisplayMode.Height = 0;
      bestAdapterDesktopDisplayMode.Format = 0;
      bestAdapterDesktopDisplayMode.RefreshRate = 0;

      GraphicsAdapterInfo bestAdapterInfo = null;
      GraphicsDeviceInfo bestDeviceInfo = null;
      DeviceCombo bestDeviceCombo = null;

      foreach (GraphicsAdapterInfo adapterInfoIterate in _enumerationSettings.AdapterInfoList)
      {
        var adapterInfo = adapterInfoIterate;

        if (GUIGraphicsContext._useScreenSelector)
        {
          adapterInfo = FindAdapterForScreen(GUIGraphicsContext.currentScreen);
          GUIGraphicsContext.currentFullscreenAdapterInfo = Manager.Adapters[adapterInfo.AdapterOrdinal];
          GUIGraphicsContext.currentScreenNumber = adapterInfo.AdapterOrdinal;
        }

        foreach (GraphicsDeviceInfo deviceInfo in adapterInfo.DeviceInfoList)
        {
          if (doesRequireHardware && deviceInfo.DevType != DeviceType.Hardware)
          {
            continue;
          }
          if (doesRequireReference && deviceInfo.DevType != DeviceType.Reference)
          {
            continue;
          }

          foreach (DeviceCombo deviceCombo in deviceInfo.DeviceComboList)
          {
            var adapterMatchesBackBuffer = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
            if (deviceCombo.IsWindowed)
            {
              continue;
            }

            // If we haven't found a compatible set yet, or if this set
            // is better (because it's a HAL, and/or because formats match better),
            // save it
            if (bestDeviceCombo == null ||
                bestDeviceCombo.DevType != DeviceType.Hardware && deviceInfo.DevType == DeviceType.Hardware ||
                bestDeviceCombo.DevType == DeviceType.Hardware &&
                bestDeviceCombo.DevType == DeviceType.Hardware && adapterMatchesBackBuffer)
            {
              bestAdapterInfo = adapterInfo;
              bestDeviceInfo = deviceInfo;
              bestDeviceCombo = deviceCombo;
              if (deviceInfo.DevType == DeviceType.Hardware && adapterMatchesBackBuffer)
              {
                // This fullscreen device combo looks great -- take it
                goto EndFullscreenDeviceComboSearch;
              }
              // Otherwise keep looking for a better fullscreen device combo
            }
          }
        }
        if (GUIGraphicsContext._useScreenSelector)
        {
          break; // no need to loop again.. result would be the same
        }
      }

      EndFullscreenDeviceComboSearch:
      if (bestDeviceCombo == null)
      {
        return false;
      }

      // Need to find a display mode on the best adapter that uses pBestDeviceCombo->AdapterFormat
      // and is as close to bestAdapterDesktopDisplayMode's res as possible
      bestDisplayMode.Width = 0;
      bestDisplayMode.Height = 0;
      bestDisplayMode.Format = 0;
      bestDisplayMode.RefreshRate = 0;
      foreach (var displayMode in bestAdapterInfo.DisplayModeList.Cast<DisplayMode>().Where(displayMode => displayMode.Format == bestDeviceCombo.AdapterFormat))
      {
        if (displayMode.Width == bestAdapterDesktopDisplayMode.Width &&
            displayMode.Height == bestAdapterDesktopDisplayMode.Height &&
            displayMode.RefreshRate == bestAdapterDesktopDisplayMode.RefreshRate)
        {
          // found a perfect match, so stop
          bestDisplayMode = displayMode;
          break;
        }
        if (displayMode.Width == bestAdapterDesktopDisplayMode.Width &&
            displayMode.Height == bestAdapterDesktopDisplayMode.Height &&
            displayMode.RefreshRate > bestDisplayMode.RefreshRate)
        {
          // refresh rate doesn't match, but width/height match, so keep this
          // and keep looking
          bestDisplayMode = displayMode;
        }
        else if (bestDisplayMode.Width == bestAdapterDesktopDisplayMode.Width)
        {
          // width matches, so keep this and keep looking
          bestDisplayMode = displayMode;
        }
        else if (bestDisplayMode.Width == 0)
        {
          // we don't have anything better yet, so keep this and keep looking
          bestDisplayMode = displayMode;
        }
      }

      _graphicsSettings.FullscreenAdapterInfo = bestAdapterInfo;
      _graphicsSettings.FullscreenDeviceInfo = bestDeviceInfo;
      _graphicsSettings.FullscreenDeviceCombo = bestDeviceCombo;
      _graphicsSettings.IsWindowed = false;
      _graphicsSettings.FullscreenDisplayMode = bestDisplayMode;
      if (_enumerationSettings.AppUsesDepthBuffer)
      {
        _graphicsSettings.FullscreenDepthStencilBufferFormat = (DepthFormat)bestDeviceCombo.DepthStencilFormatList[0];
      }
      _graphicsSettings.FullscreenMultisampleType = (MultiSampleType)bestDeviceCombo.MultiSampleTypeList[0];
      _graphicsSettings.FullscreenMultisampleQuality = 0;
      _graphicsSettings.FullscreenVertexProcessingType = (VertexProcessingType)bestDeviceCombo.VertexProcessingTypeList[0];
      _graphicsSettings.FullscreenPresentInterval = PresentInterval.Default;

      return true;
    }


    /// <summary>
    /// Choose the initial settings for the application
    /// </summary>
    /// <returns>true if the settings were initialized</returns>
    public bool ChooseInitialSettings()
    {
      var foundFullscreenMode = FindBestFullscreenMode(false, false);
      var foundWindowedMode = FindBestWindowedMode(false, false);

      if (!foundFullscreenMode && !foundWindowedMode)
      {
        throw new NoCompatibleDevicesException();
      }
      return true;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="bwindowed"></param>
    public void BuildPresentParamsFromSettings(bool bwindowed)
    {
      _presentParams.BackBufferCount = 2;
      _presentParams.EnableAutoDepthStencil = false;
      _presentParams.ForceNoMultiThreadedFlag = false;

      if (bwindowed)
      {
        _presentParams.MultiSample = _graphicsSettings.WindowedMultisampleType;
        _presentParams.MultiSampleQuality = _graphicsSettings.WindowedMultisampleQuality;
        _presentParams.AutoDepthStencilFormat = _graphicsSettings.WindowedDepthStencilBufferFormat;

        _presentParams.BackBufferWidth = _renderTarget.ClientRectangle.Right - _renderTarget.ClientRectangle.Left;
        _presentParams.BackBufferHeight = _renderTarget.ClientRectangle.Bottom - _renderTarget.ClientRectangle.Top;
        _presentParams.BackBufferFormat = _graphicsSettings.BackBufferFormat;
        _presentParams.PresentationInterval = PresentInterval.Default;
        _presentParams.FullScreenRefreshRateInHz = 0;
        _presentParams.SwapEffect = SwapEffect.Discard;
        _presentParams.PresentFlag = PresentFlag.Video;
        _presentParams.DeviceWindow = _renderTarget;
        _presentParams.Windowed = true;
      }
      else
      {
        _graphicsSettings.DisplayMode = GUIGraphicsContext.currentFullscreenAdapterInfo.CurrentDisplayMode;

        _presentParams.MultiSample = _graphicsSettings.FullscreenMultisampleType;
        _presentParams.MultiSampleQuality = _graphicsSettings.FullscreenMultisampleQuality;
        _presentParams.AutoDepthStencilFormat = _graphicsSettings.FullscreenDepthStencilBufferFormat;

        _presentParams.BackBufferWidth = _graphicsSettings.DisplayMode.Width;
        _presentParams.BackBufferHeight = _graphicsSettings.DisplayMode.Height;
        _presentParams.BackBufferFormat = _graphicsSettings.DeviceCombo.BackBufferFormat;
        _presentParams.PresentationInterval = PresentInterval.Default;
        _presentParams.FullScreenRefreshRateInHz = _graphicsSettings.DisplayMode.RefreshRate;
        _presentParams.SwapEffect = SwapEffect.Discard;
        _presentParams.PresentFlag = PresentFlag.Video;
        _presentParams.DeviceWindow = this;
        _presentParams.Windowed = false;
        Log.Info("D3D: BuildPresentParamsFromSettings using {0}Hz as RefreshRate", _graphicsSettings.DisplayMode.RefreshRate);
      }
      GUIGraphicsContext.DirectXPresentParameters = _presentParams;
      Windowed = bwindowed;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="bWindowed"></param>
    public void SwitchFullScreenOrWindowed(bool bWindowed)
    {
      if ((!_useExclusiveDirectXMode || UseEnhancedVideoRenderer) && !GUIGraphicsContext.IsDirectX9ExUsed())
      {
        return;
      }

      // Temporary remove the handler
      GUIGraphicsContext.DX9Device.DeviceLost -= OnDeviceLost;
      GUIGraphicsContext.DX9Device.DeviceReset -= OnDeviceReset;

      Log.Debug(bWindowed
        ? "D3D: Switch to windowed mode - Playing media: {0}"
        : "D3D: Switch to exclusive mode - Playing media: {0}", g_Player.Playing);

      if (GUIGraphicsContext.IsDirectX9ExUsed() && (UseEnhancedVideoRenderer || !_useExclusiveDirectXMode))
      {
        BuildPresentParamsFromSettings(true);
      }
      else
      {
        BuildPresentParamsFromSettings(bWindowed);
      }
      try
      {
        GUIGraphicsContext.DX9Device.Reset(_presentParams);
        if (GUIGraphicsContext.IsDirectX9ExUsed() && !UseEnhancedVideoRenderer)
        {
          if (!Skin.Equals(GUIGraphicsContext.Skin))
          {
            Skin = GUIGraphicsContext.Skin;
          }
          GUIFontManager.LoadFonts(GUIGraphicsContext.GetThemedSkinFile(@"\fonts.xml"));
          GUIFontManager.InitializeDeviceObjects();
        }

        Log.Debug(Windowed
          ? "D3D: Switched to windowed mode successfully"
          : "D3D: Switched to exclusive mode successfully");
      }
      catch (Exception ex)
      {
        Log.Warn(Windowed 
          ? "D3D: Switch to windowed mode failed - {0}" 
          : "D3D: Switch to exclusive mode failed - {0}", ex.ToString());

        if (GUIGraphicsContext.IsDirectX9ExUsed() && (UseEnhancedVideoRenderer || !_useExclusiveDirectXMode))
        {
          BuildPresentParamsFromSettings(true);
        }
        else
        {
          BuildPresentParamsFromSettings(!bWindowed);
        }
        try
        {
          GUIGraphicsContext.DX9Device.Reset(_presentParams);
          if (GUIGraphicsContext.IsDirectX9ExUsed() && !UseEnhancedVideoRenderer)
          {
            if (!Skin.Equals(GUIGraphicsContext.Skin))
            {
              Skin = GUIGraphicsContext.Skin;
            }
            GUIFontManager.LoadFonts(GUIGraphicsContext.GetThemedSkinFile("fonts.xml"));
            GUIFontManager.InitializeDeviceObjects();
          }
        }
        catch (Exception e)
        {
          Log.Warn("D3D: mode failed - {0}", e.ToString());
        }
      }
      GUIGraphicsContext.DX9Device.DeviceReset += OnDeviceReset;
      GUIGraphicsContext.DX9Device.DeviceLost += OnDeviceLost;

      if (Windowed)
      {
        TopMost = _alwaysOnTop;
      }
      Activate();
    }


    /// <summary>
    /// Initialize the graphics environment
    /// </summary>
    public void InitializeEnvironment()
    {
      var adapterInfo = _graphicsSettings.AdapterInfo;
      var deviceInfo = _graphicsSettings.DeviceInfo;

      try
      {
        Log.Info("d3dapp: Graphic adapter '{0}' is using driver version '{1}'",
                 adapterInfo.AdapterDetails.Description.Trim(), adapterInfo.AdapterDetails.DriverVersion.ToString());
        Log.Info("d3dapp: Pixel shaders supported: {0} (Version: {1}), Vertex shaders supported: {2} (Version: {3})",
                 deviceInfo.Caps.PixelShaderCaps.NumberInstructionSlots, deviceInfo.Caps.PixelShaderVersion.ToString(),
                 deviceInfo.Caps.VertexShaderCaps.NumberTemps, deviceInfo.Caps.VertexShaderVersion.ToString());
      }
      catch (Exception lex)
      {
        Log.Warn("d3dapp: Error logging graphic device details - {0}", lex.Message);
      }

      // Set up the presentation parameters, we start in none exclusive mode
      BuildPresentParamsFromSettings(true);

      if (deviceInfo.Caps.PrimitiveMiscCaps.IsNullReference)
      {
        // Warn user about null ref device that can't render anything
        HandleSampleException(new NullReferenceDeviceException(), ApplicationMessage.None);
      }

      CreateFlags createFlags;
      switch (_graphicsSettings.VertexProcessingType)
      {
        case VertexProcessingType.Software:
          createFlags = CreateFlags.SoftwareVertexProcessing;
          break;
        case VertexProcessingType.Mixed:
          createFlags = CreateFlags.MixedVertexProcessing;
          break;
        case VertexProcessingType.Hardware:
          createFlags = CreateFlags.HardwareVertexProcessing;
          break;
        case VertexProcessingType.PureHardware:
          createFlags = CreateFlags.HardwareVertexProcessing; // | CreateFlags.PureDevice;
          break;
        default:
          throw new ApplicationException();
      }

      // Make sure to allow multi-threaded apps if we need them
      _presentParams.ForceNoMultiThreadedFlag = false;

      try
      {
        // Create the device
        if (GUIGraphicsContext.IsDirectX9ExUsed())
        {
          // Vista or later, use DirectX9 Ex device
          Log.Info("Creating DirectX9 Ex device");
          CreateDirectX9ExDevice(createFlags);
        }
        else
        {
          Log.Info("Creating DirectX9 device");
          GUIGraphicsContext.DX9Device = new Device(_graphicsSettings.AdapterOrdinal,
                                                    _graphicsSettings.DevType,
                                                    Windowed ? _renderTarget : this,
                                                    createFlags | CreateFlags.MultiThreaded | CreateFlags.FpuPreserve,
                                                    _presentParams);
        }

        if (Windowed)
        {
          // Make sure main window isn't topmost, so error message is visible
          var currentClientSize = ClientSize;

          Size = ClientSize;
          SendToBack();
          BringToFront();
          ClientSize = currentClientSize;
          TopMost = _alwaysOnTop;
        }
  
        // Set up the fullscreen cursor
        if (_showCursorWhenFullscreen && !Windowed)
        {
          var ourCursor = Cursor;
          GUIGraphicsContext.DX9Device.SetCursor(ourCursor, true);
          GUIGraphicsContext.DX9Device.ShowCursor(true);
        }

        // Setup the event handlers for our device
        GUIGraphicsContext.DX9Device.DeviceLost += OnDeviceLost;
        GUIGraphicsContext.DX9Device.DeviceReset += OnDeviceReset;

        // Initialize the app's device-dependent objects
        try
        {
          InitializeDeviceObjects();
          _active = true;
        }
        catch (Exception ex)
        {
          Log.Error("D3D: InitializeDeviceObjects - Exception: {0}", ex.ToString());
          GUIGraphicsContext.DX9Device.Dispose();
          GUIGraphicsContext.DX9Device = null;
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex);
        // If that failed, fall back to the reference rasterizer
        if (deviceInfo.DevType == DeviceType.Hardware && FindBestWindowedMode(false, true))
        {
          Windowed = true;

          // Make sure main window isn't topmost, so error message is visible
          var currentClientSize = ClientSize;
          Size = ClientSize;
          SendToBack();
          BringToFront();
          ClientSize = currentClientSize;
          TopMost = _alwaysOnTop;

          // Let the user know we are switching from HAL to the reference rasterizer
          HandleSampleException(null, ApplicationMessage.WarnSwitchToRef);

          InitializeEnvironment();
        }
      }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="createFlags"></param>
    private void CreateDirectX9ExDevice(CreateFlags createFlags)
    {
      var param = new D3DPRESENT_PARAMETERS {Windowed = 0};
      if (_presentParams.Windowed)
      {
        param.Windowed = 1;
      }

      param.AutoDepthStencilFormat = _presentParams.AutoDepthStencilFormat;
      param.BackBufferCount = (uint)_presentParams.BackBufferCount;
      param.BackBufferFormat = _presentParams.BackBufferFormat;
      param.BackBufferHeight = (uint)_presentParams.BackBufferHeight;
      param.BackBufferWidth = (uint)_presentParams.BackBufferWidth;
      param.hDeviceWindow = _presentParams.DeviceWindow.Handle;

      param.EnableAutoDepthStencil = 0;
      if (_presentParams.EnableAutoDepthStencil)
      {
        param.EnableAutoDepthStencil = 1;
      }

      param.FullScreen_RefreshRateInHz = (uint)_presentParams.FullScreenRefreshRateInHz;
      param.MultiSampleType = _presentParams.MultiSample;
      param.MultiSampleQuality = _presentParams.MultiSampleQuality;
      param.PresentationInterval = (uint)_presentParams.PresentationInterval;
      param.SwapEffect = _presentParams.SwapEffect;

      IDirect3D9Ex direct3D9Ex;
      Direct3D.Direct3DCreate9Ex(32, out direct3D9Ex);
      var o = Marshal.GetIUnknownForObject(direct3D9Ex);
      Marshal.Release(o);

      var displaymodeEx = new D3DDISPLAYMODEEX();

      displaymodeEx.Size = (uint)Marshal.SizeOf(displaymodeEx);
      displaymodeEx.Width = param.BackBufferWidth;
      displaymodeEx.Height = param.BackBufferHeight;
      displaymodeEx.Format = param.BackBufferFormat;
      displaymodeEx.ScanLineOrdering = D3DSCANLINEORDERING.D3DSCANLINEORDERING_UNKNOWN;
      IntPtr dev;
      var prt = Marshal.AllocHGlobal(Marshal.SizeOf(displaymodeEx));
      Marshal.StructureToPtr(displaymodeEx, prt, true);

      var hr = direct3D9Ex.CreateDeviceEx(_graphicsSettings.AdapterOrdinal, _graphicsSettings.DevType,
                                      Windowed ? _renderTarget.Handle : Handle,
                                      createFlags | CreateFlags.MultiThreaded | CreateFlags.FpuPreserve, ref param,
                                      Windowed ? IntPtr.Zero : prt, out dev);
      if (hr == 0)
      {
        GUIGraphicsContext.DX9Device = new Device(dev);
        GUIGraphicsContext.DX9Device.Reset(_presentParams);
      }
      else
      {
        Log.Error("d3dapp: Could not create device");
      }
    }


    /// <summary>
    /// Displays sample exceptions to the user
    /// </summary>
    /// <param name="e">The exception that was thrown</param>
    /// <param name="type">Extra information on how to handle the exception</param>
    public void HandleSampleException(SampleException e, ApplicationMessage type)
    {
      // Build a message to display to the user
      var strMsg    = "";
      var strSource = "";
      var strStack  = "";
      if (e != null)
      {
        strMsg    = e.Message;
        strSource = e.Source;
        strStack  = e.StackTrace;
      }
      Log.Error("D3D: Exception: {0} {1} {2}", strMsg, strSource, strStack);
      switch (type)
      {
        case ApplicationMessage.ApplicationMustExit:
          strMsg += "\n\nMediaPortal has to be closed.";
          MessageBox.Show(strMsg, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
          if (IsHandleCreated)
          {
            Close();
          }
          break;
        case ApplicationMessage.WarnSwitchToRef:
          strMsg = "\n\nSwitching to the reference rasterizer,\n";
          strMsg += "a software device that implements the entire\n";
          strMsg += "Direct3D feature set, but runs very slowly.";
          MessageBox.Show(strMsg, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
          break;
      }
    }


    /// <summary>
    /// Fired when our environment was resized
    /// </summary>
    /// <param name="sender">the device that's resizing our environment</param>
    /// <param name="e">Set the cancel member to true to turn off automatic device reset</param>
    public void EnvironmentResized(object sender, CancelEventArgs e)
    {
      // Check to see if we're closing or changing the form style
      if (_isClosing)
      {
        // We are, cancel our reset, and exit
        e.Cancel = true;
        return;
      }

      // Check to see if we're minimizing and our rendering object
      // is not the form, if so, cancel the resize
      if ((_renderTarget != this) && (WindowState == FormWindowState.Minimized) || !_active)
      {
        e.Cancel = true;
      }

      // Set up the fullscreen cursor
      if (_showCursorWhenFullscreen && !Windowed)
      {
        var ourCursor = Cursor;
        GUIGraphicsContext.DX9Device.SetCursor(ourCursor, true);
        GUIGraphicsContext.DX9Device.ShowCursor(true);
      }
    }


    /// <summary>
    /// Save player state (when form was resized)
    /// </summary>
    protected void SavePlayerState()
    {
      // Is App not minimized to tray and is a player active?
      if (WindowState != FormWindowState.Minimized &&
          !_wasPlayingVideo &&
          (g_Player.Playing && (g_Player.IsTV || g_Player.IsVideo || g_Player.IsDVD)))
      {
        _wasPlayingVideo = true;

        // Some Audio/video is playing
        _currentPlayerPos = g_Player.CurrentPosition;
        _currentPlayListType = PlaylistPlayer.CurrentPlaylistType;
        _currentPlayList = new PlayList();

        Log.Info("D3D: Saving fullscreen state for resume: {0}", Menu == null);
        var tempList = PlaylistPlayer.GetPlaylist(_currentPlayListType);
        if (tempList.Count == 0 && g_Player.IsDVD)
        {
          // DVD is playing
          var itemDVD = new PlayListItem {FileName = g_Player.CurrentFile, Played = true, Type = PlayListItem.PlayListItemType.DVD};
          tempList.Add(itemDVD);
        }
        foreach (var itemNew in tempList)
        {
          _currentPlayList.Add(itemNew);
        }
        _strCurrentFile = PlaylistPlayer.Get(PlaylistPlayer.CurrentSong);
        if (_strCurrentFile.Equals(string.Empty) && g_Player.IsDVD)
        {
          _strCurrentFile = g_Player.CurrentFile;
        }
        Log.Info(
          "D3D: Form resized - Stopping media - Current playlist: Type: {0} / Size: {1} / Current item: {2} / Filename: {3} / Position: {4}",
          _currentPlayListType, _currentPlayList.Count, PlaylistPlayer.CurrentSong, _strCurrentFile, _currentPlayerPos);
        g_Player.Stop();

        _lastActiveWindow = GUIWindowManager.ActiveWindow;
      }
    }


    /// <summary>
    /// Restore player from saved state (after resizing form)
    /// </summary>
    protected void ResumePlayer()
    {
      if (_wasPlayingVideo) // was any player active at all?
      {
        _wasPlayingVideo = false;

        // we were watching some audio/video
        Log.Info("D3D: RestorePlayers - Resuming: {0}", _strCurrentFile);
        PlaylistPlayer.Init();
        PlaylistPlayer.Reset();
        PlaylistPlayer.CurrentPlaylistType = _currentPlayListType;
        var playlist = PlaylistPlayer.GetPlaylist(_currentPlayListType);
        playlist.Clear();
        if (_currentPlayList != null)
        {
          foreach (var itemNew in _currentPlayList)
          {
            playlist.Add(itemNew);
          }
        }
        if (playlist.Count > 0 && playlist[0].Type.Equals(PlayListItem.PlayListItemType.DVD))
        {
          // we were watching DVD
          var movieDetails = new IMDBMovie();
          var fileName = playlist[0].FileName;
          VideoDatabase.GetMovieInfo(fileName, ref movieDetails);
          var idFile = VideoDatabase.GetFileId(fileName);
          var idMovie = VideoDatabase.GetMovieId(fileName);
          if (idMovie >= 0 && idFile >= 0)
          {
            g_Player.PlayDVD(fileName);
            if (g_Player.Playing)
            {
              g_Player.Player.SetResumeState(null);
            }
          }
        }
        else
        {
          PlaylistPlayer.Play(_strCurrentFile); // some standard audio/video
        }

        if (g_Player.Playing)
        {
          g_Player.SeekAbsolute(_currentPlayerPos);
        }

        GUIGraphicsContext.IsFullScreenVideo = Menu == null;
        GUIWindowManager.ReplaceWindow(_lastActiveWindow);
      }
    }


    /// <summary>
    /// Called when our sample has nothing else to do, and it's time to render
    /// </summary>
    protected void FullRender()
    {
      // don't render if minimized and restoring is not in progress
      if (WindowState == FormWindowState.Minimized && !_fromTray)
      {
        Thread.Sleep(100);
        return;
      }

      ResumePlayer();
      HandleCursor();

      // In minitv mode allow to loose focus
      if ((ActiveForm != this) && (_alwaysOnTop) && !_miniTvMode &&
          (GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.RUNNING))
      {
        Activate();
      }

      if (GUIGraphicsContext.Vmr9Active)
      {
        return;
      }

      // Render a frame during idle time (no messages are waiting)
      if (_active && Ready)
      {
#if !DEBUG
        try
        {
#endif
        if (((GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.LOST) || (ActiveForm != this) || (GUIGraphicsContext.SaveRenderCycles)) && !_fromTray)
        {
          Thread.Sleep(100);
        }
        RecoverDevice();
        try
        {
          if (!GUIGraphicsContext.Vmr9Active && GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.RUNNING)
          {
            lock (GUIGraphicsContext.RenderLock)
            {
              Render(GUIGraphicsContext.TimePassed);
            }
          }
        }
        catch (Exception ex)
        {
          Log.Error("d3dapp: Exception: {0}", ex);
        }
#if !DEBUG
        } 
		catch (Exception ee)
        {
          Log.Info("d3dapp: Exception {0}", ee);
          MessageBox.Show("An exception has occurred.  MediaPortal has to be closed.\r\n\r\n" + ee,
                          "Exception",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
          Close();
        }
#endif
      }
      else
      {
        // if we don't got the focus, then don't use all the CPU unless we are restoring from tray
        if (ActiveForm != this || (GUIGraphicsContext.SaveRenderCycles) && !_fromTray)
        {
          Thread.Sleep(100);
        }
      }
    }


    /// <summary>
    /// 
    /// </summary>
    public void RecoverDevice()
    {
      if (GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.LOST)
      {
        if (g_Player.Playing && !RefreshRateChanger.RefreshRateChangePending)
        {
          g_Player.Stop();
        }

        if (!GUIGraphicsContext.IsDirectX9ExUsed())
        {
          try
          {
            Log.Debug("d3dapp: RecoverDevice called");
            // Test the cooperative level to see if it's okay to render
            GUIGraphicsContext.DX9Device.TestCooperativeLevel();
          }
          catch (DeviceLostException)
          {
            // If the device was lost, do not render until we get it back
            _active = false;
            Log.Debug("d3dapp: DeviceLostException");

            return;
          }
          catch (DeviceNotResetException)
          {
            Log.Debug("d3dapp: DeviceNotResetException");
            _needReset = true;
          }
        }
        else
        {
          _needReset = true;
        }

        if (_needReset)
        {
          if (!GUIGraphicsContext.IsDirectX9ExUsed())
          {
            BuildPresentParamsFromSettings(Windowed);
          }

          // Reset the device and resize it
          Log.Warn("d3dapp: Resetting DX9 device");
          try
          {
            GUITextureManager.Dispose();
            GUIFontManager.Dispose();

            if (!GUIGraphicsContext.IsDirectX9ExUsed())
            {
              GUIGraphicsContext.DX9Device.Reset(GUIGraphicsContext.DX9Device.PresentationParameters);
              _needReset = false;
            }
            else
            {
              Log.Warn("d3dapp: DirectX9Ex is lost or GPU hung --> Reinit of DX9Ex is needed.");
              GUIGraphicsContext.DX9ExRealDeviceLost = true;
              InitializeEnvironment();
            }
          }
          catch (Exception ex)
          {
            Log.Error("d3dapp: Reset failed - {0}", ex.ToString());
            GUIGraphicsContext.DX9Device.DeviceLost -= OnDeviceLost;
            GUIGraphicsContext.DX9Device.DeviceReset -= OnDeviceReset;
            InitializeEnvironment();
            return;
          }

          Log.Debug("d3dapp: EnvironmentResized()");
          EnvironmentResized(GUIGraphicsContext.DX9Device, new CancelEventArgs());
        }
        GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.RUNNING;

        if (RefreshRateChanger.RefreshRateChangePending && RefreshRateChanger.RefreshRateChangeStrFile.Length > 0)
        {
          RefreshRateChanger.RefreshRateChangePending = false;
          if (RefreshRateChanger.RefreshRateChangeMediaType != RefreshRateChanger.MediaType.Unknown)
          {
            var t1 = (int) RefreshRateChanger.RefreshRateChangeMediaType;
            var t2 = (g_Player.MediaType) t1;
            g_Player.Play(RefreshRateChanger.RefreshRateChangeStrFile, t2);
          }
          else
          {
            g_Player.Play(RefreshRateChanger.RefreshRateChangeStrFile);
          }
          if ((g_Player.HasVideo || g_Player.HasViz) && RefreshRateChanger.RefreshRateChangeFullscreenVideo)
          {
            g_Player.ShowFullScreenWindow();
          }
        }
      }
    }


    /// <summary>
    /// 
    /// </summary>
    public void HandleCursor()
    {
      if (!Maximized)
      {
        return;
      }

      if (AutoHideMouse)
      {
        if (ShowCursor != _lastShowCursor)
        {
          if (!ShowCursor)
          {
            Cursor.Hide();
          }
          else
          {
            Cursor.Show();
          }
          _lastShowCursor = ShowCursor;
        }

        if (ShowCursor)
        {
          var ts = DateTime.Now - MouseTimeOutTimer;
          if (ts.TotalSeconds >= 3)
          {
            //hide mouse
            Cursor.Hide();
            ShowCursor = false;
            Invalidate(true);
          }
        }
      }
    }


    /// <summary>
    ///Get the  statistics 
    /// </summary>
    public void GetStats()
    {
      var fmtAdapter = _graphicsSettings.DisplayMode.Format;
      var strFmt = String.Format("backbuf {0}, adapter {1}",
                                  GUIGraphicsContext.DX9Device.PresentationParameters.BackBufferFormat,
                                  fmtAdapter);

      var strDepthFmt = _enumerationSettings.AppUsesDepthBuffer ? String.Format(" ({0})", _graphicsSettings.DepthStencilBufferFormat.ToString()) : "";

      string strMultiSample;
      switch (_graphicsSettings.MultisampleType)
      {
        case MultiSampleType.NonMaskable:
          strMultiSample = " (NonMaskable Multisample)";
          break;
        case MultiSampleType.TwoSamples:
          strMultiSample = " (2x Multisample)";
          break;
        case MultiSampleType.ThreeSamples:
          strMultiSample = " (3x Multisample)";
          break;
        case MultiSampleType.FourSamples:
          strMultiSample = " (4x Multisample)";
          break;
        case MultiSampleType.FiveSamples:
          strMultiSample = " (5x Multisample)";
          break;
        case MultiSampleType.SixSamples:
          strMultiSample = " (6x Multisample)";
          break;
        case MultiSampleType.SevenSamples:
          strMultiSample = " (7x Multisample)";
          break;
        case MultiSampleType.EightSamples:
          strMultiSample = " (8x Multisample)";
          break;
        case MultiSampleType.NineSamples:
          strMultiSample = " (9x Multisample)";
          break;
        case MultiSampleType.TenSamples:
          strMultiSample = " (10x Multisample)";
          break;
        case MultiSampleType.ElevenSamples:
          strMultiSample = " (11x Multisample)";
          break;
        case MultiSampleType.TwelveSamples:
          strMultiSample = " (12x Multisample)";
          break;
        case MultiSampleType.ThirteenSamples:
          strMultiSample = " (13x Multisample)";
          break;
        case MultiSampleType.FourteenSamples:
          strMultiSample = " (14x Multisample)";
          break;
        case MultiSampleType.FifteenSamples:
          strMultiSample = " (15x Multisample)";
          break;
        case MultiSampleType.SixteenSamples:
          strMultiSample = " (16x Multisample)";
          break;
        default:
          strMultiSample = string.Empty;
          break;
      }

      FrameStatsLine1 = String.Format("last {0} fps ({1}x{2}), {3}{4}{5}",
                                      GUIGraphicsContext.CurrentFPS.ToString("f2"),
                                      GUIGraphicsContext.DX9Device.PresentationParameters.BackBufferWidth,
                                      GUIGraphicsContext.DX9Device.PresentationParameters.BackBufferHeight,
                                      strFmt, strDepthFmt, strMultiSample);

      FrameStatsLine2 = String.Format("");

      if (GUIGraphicsContext.Vmr9Active)
      {
        FrameStatsLine2 = String.Format(GUIGraphicsContext.IsEvr ? "EVR {0} " : "VMR9 {0} ", GUIGraphicsContext.Vmr9FPS.ToString("f2"));
      }

      var quality = String.Format("avg fps:{0} sync:{1} drawn:{2} dropped:{3} jitter:{4}",
                                     VideoRendererStatistics.AverageFrameRate.ToString("f2"),
                                     VideoRendererStatistics.AverageSyncOffset,
                                     VideoRendererStatistics.FramesDrawn,
                                     VideoRendererStatistics.FramesDropped,
                                     VideoRendererStatistics.Jitter);
      FrameStatsLine2 += quality;
    }


    /// <summary>
    /// Update the various statistics the simulation keeps track of
    /// </summary>
    public void UpdateStats()
    {
      var time = Stopwatch.GetTimestamp();
      var diffTime = (float)(time - _lastTime) / Stopwatch.Frequency;
      // Update the scene stats once per second
      if (diffTime >= 1.0f)
      {
        GUIGraphicsContext.CurrentFPS = Frames / diffTime;
        _lastTime = time;
        Frames = 0;
      }
    }


    /// <summary>
    /// Set our variables to not active and not ready
    /// </summary>
    public void CleanupEnvironment()
    {
      _active = false;
      Ready = false;
      if (GUIGraphicsContext.DX9Device != null)
      {
        // indicate we are shutting down
        App.IsShuttingDown = true;
        // remove the device lost and reset handlers as application is already closing down
        GUIGraphicsContext.DX9Device.DeviceLost -= OnDeviceLost;
        GUIGraphicsContext.DX9Device.DeviceReset -= OnDeviceReset;
        GUIGraphicsContext.DX9Device.Dispose();
      }
    }

    #region Menu EventHandlers

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnSetup(object sender, EventArgs e)
    {
      const string processName = "Configuration.exe";

      if (Process.GetProcesses().Any(process => process.ProcessName.Equals(processName)))
      {
        return;
      }

      Log.Info("D3D: OnSetup - Stopping media");
      g_Player.Stop();

      if (!GUIGraphicsContext.DX9Device.PresentationParameters.Windowed)
      {
        SwitchFullScreenOrWindowed(true);
      }

      AutoHideMouse = false;
      Cursor.Show();
      Invalidate(true);

      using (Settings xmlreader = new MPSettings())
      {
        xmlreader.Clear();
      }

      Util.Utils.StartProcess(Config.GetFile(Config.Dir.Base, "Configuration.exe"), "", false, false);
    }


    /// <summary>
    /// Will end the simulation
    /// </summary>
    private void ExitSample(object sender, EventArgs e)
    {
      ShuttingDown = true;
      Close();
    }

    #endregion

    #region WinForms Overrides

    /// <summary>
    /// Clean up any resources
    /// </summary>
    protected override void Dispose(bool disposing)
    {
      CleanupEnvironment();

      if (_notifyIcon != null)
      {
        //we dispose our tray icon here
        _notifyIcon.Dispose();
      }

      base.Dispose(disposing);

      if (AutoHideTaskbar)
      {
        HideTaskBar(false);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
      // Allow the control to handle the keystroke now
      if (!e.Handled)
      {
        base.OnKeyPress(e);
      }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MenuItem2Click(object sender, EventArgs e)
    {
      OnSetup(sender, e);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void D3DAppLoad(object sender, EventArgs e)
    {
      Application.Idle += ApplicationIdle;

      Initialize();
      OnStartup();

      try
      {
        // give an external app a change to be notified when the application has reached the final stage of startup

        var handle = EventWaitHandle.OpenExisting("MediaPortalHandleCreated");

        if (handle.SafeWaitHandle.IsInvalid)
        {
          return;
        }

        handle.Set();
        handle.Close();
      }
      // suppress any errors
      catch {}
    }


    /// <summary>
    /// 
    /// </summary>
    private static void TvDelayThread()
    {
      // we have to use a small delay before calling tvfullscreen.                              
      Thread.Sleep(200);
      g_Player.ShowFullScreenWindow();
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected bool ShowLastActiveModule()
    {
      bool showLastActiveModule;
      int lastActiveModule;
      bool lastActiveModuleFullscreen;
      using (Settings xmlreader = new MPSettings())
      {
        showLastActiveModule = xmlreader.GetValueAsBool("general", "showlastactivemodule", false);
        lastActiveModule = xmlreader.GetValueAsInt("general", "lastactivemodule", -1);
        lastActiveModuleFullscreen = xmlreader.GetValueAsBool("general", "lastactivemodulefullscreen", false);

        // check if system has been awaken by user or psclient.
        // if by psclient, DO NOT resume last active module
        if (showLastActiveModule)
        {
          var psClientNextwakeupStr = xmlreader.GetValueAsString("psclientplugin", "nextwakeup",
                                                                    DateTime.MaxValue.ToString(CultureInfo.InvariantCulture));
          var now = DateTime.Now;
          var psClientNextwakeupDate = Convert.ToDateTime(psClientNextwakeupStr);
          var ts = psClientNextwakeupDate - now;

          Log.Debug("ShowLastActiveModule() - psclientplugin nextwakeup {0}", psClientNextwakeupStr);
          Log.Debug("ShowLastActiveModule() - timediff in minutes {0}", ts.TotalMinutes);

          if (ts.TotalMinutes < 2 && ts.TotalMinutes > -2)
          {
            Log.Debug("ShowLastActiveModule() - system probably awoken by PSclient, ignoring ShowLastActiveModule");
            return false;
          }
          Log.Debug("ShowLastActiveModule() - system probably awoken by user, continuing with ShowLastActiveModule");
        }
      }

      Log.Debug("d3dapp: ShowLastActiveModule active : {0}", showLastActiveModule);

      if (showLastActiveModule)
      {
        Log.Debug("d3dapp: ShowLastActiveModule module : {0}", lastActiveModule);
        Log.Debug("d3dapp: ShowLastActiveModule fullscreen : {0}", lastActiveModuleFullscreen);
        if (lastActiveModule < 0)
        {
          Log.Error("Error recalling last active module - invalid module name '{0}'", lastActiveModule);
        }
        else
        {
          try
          {
            GUIWindowManager.ActivateWindow(lastActiveModule);

            if (lastActiveModule == (int)GUIWindow.Window.WINDOW_TV && lastActiveModuleFullscreen)
            {
              var tvDelayThread = new Thread(TvDelayThread) {IsBackground = true, Name = "TvDelayThread"};
              tvDelayThread.Start();
            }

            return true;
          }
          catch (Exception e)
          {
            Log.Error("Error recalling last active module '{0}' - {1}", lastActiveModule, e.Message);
          }
        }
      }
      return false;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private static void D3DAppClosing(object sender, CancelEventArgs e)
    {
      GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
      g_Player.Stop();
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void D3DAppClick(object sender, MouseEventArgs e)
    {
      if (ActiveForm != this)
      {
        return;
      }
      MouseClickEvent(e);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void D3DAppMouseMove(object sender, MouseEventArgs e)
    {
      MouseMoveEvent(e);
    }


    /// <summary>
    /// 
    /// </summary>
    protected void ToggleFullWindowed()
    {
      _toggleFullWindowed = true;
      Log.Info("D3D: Fullscreen / windowed mode toggled");
      Maximized = !Maximized;
      // Force player to stop so as not to crash during toggle
      if (GUIGraphicsContext.Vmr9Active)
      {
        Log.Info("D3D: Vmr9Active - Stopping media");
        g_Player.Stop();
      }
      GUITextureManager.CleanupThumbs();
      GUITextureManager.Dispose();
      GUIFontManager.Dispose();
      if (Maximized)
      {
        Log.Info("D3D: Switching windowed mode -> fullscreen");
        if (AutoHideTaskbar)
        {
          HideTaskBar();
        }

        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        Menu = null;
        _oldBounds = Bounds;
        var newBounds = GUIGraphicsContext.currentScreen.Bounds;
        Bounds = newBounds;
        Update();
        Log.Info("D3D: Switching windowed mode -> fullscreen done - Maximized: {0}", Maximized);
        Log.Info("D3D: Client size: {0}x{1} - Screen: {2}x{3}",
                 ClientSize.Width, ClientSize.Height,
                 GUIGraphicsContext.currentScreen.Bounds.Width, GUIGraphicsContext.currentScreen.Bounds.Height);
        SwitchFullScreenOrWindowed(false);
      }
      else
      {
        Log.Info("D3D: Switching fullscreen -> windowed mode");
        if (AutoHideTaskbar)
        {
          HideTaskBar(false);
        }
        WindowState = FormWindowState.Normal;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        Menu = _menuStripMain;
        var newBounds = new Rectangle(_oldBounds.X, _oldBounds.Y, _oldBounds.Width, _oldBounds.Height);
        using (Settings xmlreader = new MPSettings())
        {
          var autosize = xmlreader.GetValueAsBool("gui", "autosize", true);
          if (autosize && !GUIGraphicsContext.Fullscreen)
          {
            newBounds.Height = GUIGraphicsContext.SkinSize.Height;
            newBounds.Width = GUIGraphicsContext.SkinSize.Width;
          }
        }
        Bounds = newBounds;
        Update();
        Log.Info("D3D: Switching fullscreen -> windowed mode done - Maximized: {0}", Maximized);
        Log.Info("D3D: Client size: {0}x{1} - Screen: {2}x{3}",
                 ClientSize.Width, ClientSize.Height,
                 GUIGraphicsContext.currentScreen.Bounds.Width, GUIGraphicsContext.currentScreen.Bounds.Height);
        SwitchFullScreenOrWindowed(true);
      }
      OnDeviceReset(null, null);
      _toggleFullWindowed = false;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Control == false && e.Alt && (e.KeyCode == Keys.Return))
      {
        ToggleFullWindowed();
        e.Handled = true;
        return;
      }
      if (e.Control && e.Alt && e.KeyCode == Keys.Return)
      {
        ToggleMiniTv();
        e.Handled = true;
        return;
      }
      if (e.KeyCode == Keys.F2)
      {
        OnSetup(null, null);
      }
      if (e.Handled == false)
      {
        KeyDownEvent(e);
      }
    }


    /// <summary>
    /// 
    /// </summary>
    private void InitializeComponent()
    {
      _components = new Container();
      var resources = new ComponentResourceManager(typeof (D3DApp));
      _menuStripMain = new MainMenu(_components);
      _menuItemFile = new MenuItem();
      _menuItemExit = new MenuItem();
      _menuItemOptions = new MenuItem();
      _menuItemFullscreen = new MenuItem();
      _menuItemMiniTv = new MenuItem();
      _menuItemConfiguration = new MenuItem();
      _menuItemWizards = new MenuItem();
      _menuItemDvd = new MenuItem();
      _menuItemMovies = new MenuItem();
      _menuItemMusic = new MenuItem();
      _menuItemPictures = new MenuItem();
      _menuItemTelevision = new MenuItem();
      _menuItemChangeDevice = new MenuItem();
      _menuBreakFile = new MenuItem();
      _notifyIcon = new NotifyIcon(_components);
      _contextMenu = new ContextMenu();
      _menuItemContext = new MenuItem();
      _menuItem5 = new MenuItem();
      SuspendLayout();
      // 
      // menuStripMain
      // 
      _menuStripMain.MenuItems.AddRange(new[]
                                              {
                                                _menuItemFile,
                                                _menuItemOptions,
                                                _menuItemWizards
                                              });
      // 
      // menuItemFile
      // 
      _menuItemFile.Index = 0;
      _menuItemFile.MenuItems.AddRange(new[]
                                             {
                                               _menuItemExit
                                             });
      _menuItemFile.Text = Resources.D3DApp_menuItem_File;
      // 
      // menuItemExit
      // 
      _menuItemExit.Index = 0;
      _menuItemExit.Text = Resources.D3DApp_menuItem_Exit;
      _menuItemExit.Click += ExitSample;
      // 
      // menuItemOptions
      // 
      _menuItemOptions.Index = 1;
      _menuItemOptions.MenuItems.AddRange(new[]
                                                {
                                                  _menuItemFullscreen,
                                                  _menuItemMiniTv,
                                                  _menuItemConfiguration
                                                });
      _menuItemOptions.Text = Resources.D3DApp_menuItem_Options;
      // 
      // menuItemFullscreen
      // 
      _menuItemFullscreen.Index = 0;
      _menuItemFullscreen.Text = Resources.D3DApp_menuItem_Fullscreen;
      _menuItemFullscreen.Click += MenuItemFullscreenClick;
      // 
      // menuItemMiniTv
      // 
      _menuItemMiniTv.Index = 1;
      _menuItemMiniTv.Text = Resources.D3DApp_menuItem_MiniTv;
      _menuItemMiniTv.Click += MenuItemMiniTvClick;
      // 
      // menuItemConfiguration
      // 
      _menuItemConfiguration.Index = 2;
      _menuItemConfiguration.Shortcut = Shortcut.F2;
      _menuItemConfiguration.Text = Resources.D3DApp_menuItem_Configuration;
      _menuItemConfiguration.Click += MenuItem2Click;
      // 
      // menuItemWizards
      // 
      _menuItemWizards.Index = 2;
      _menuItemWizards.MenuItems.AddRange(new[]
                                                {
                                                  _menuItemDvd,
                                                  _menuItemMovies,
                                                  _menuItemMusic,
                                                  _menuItemPictures,
                                                  _menuItemTelevision
                                                });
      _menuItemWizards.Text = Resources.D3DApp_menuItem_Wizards;
      // 
      // menuItemDVD
      // 
      _menuItemDvd.Index = 0;
      _menuItemDvd.Text = Resources.D3DApp_menuItem_DVD;
      _menuItemDvd.Click += DvdMenuItemClick;
      // 
      // menuItemMovies
      // 
      _menuItemMovies.Index = 1;
      _menuItemMovies.Text = Resources.D3DApp_menuItem_Movies;
      _menuItemMovies.Click += MoviesMenuItemClick;
      // 
      // menuItemMusic
      // 
      _menuItemMusic.Index = 2;
      _menuItemMusic.Text = Resources.D3DApp_menuItem_Music;
      _menuItemMusic.Click += MusicMenuItemClick;
      // 
      // menuItemPictures
      // 
      _menuItemPictures.Index = 3;
      _menuItemPictures.Text = Resources.D3DApp_menuItem_Pictures;
      _menuItemPictures.Click += PicturesMenuItemClick;
      // 
      // menuItemTelevision
      // 
      _menuItemTelevision.Index = 4;
      _menuItemTelevision.Text = Resources.D3DApp_menuItem_Television;
      _menuItemTelevision.Click += TelevisionMenuItemClick;
      // 
      // menuItemChangeDevice
      // 
      _menuItemChangeDevice.Index = -1;
      _menuItemChangeDevice.Text = "";
      // 
      // menuBreakFile
      // 
      _menuBreakFile.Index = -1;
      _menuBreakFile.Text = Resources.D3DApp_MenuItem_Break;
      // 
      // notifyIcon
      // 
      _contextMenu.MenuItems.Clear();
      _contextMenu.MenuItems.Add(Resources.D3DApp_NotifyIcon_Restore, Restore_OnClick);
      _contextMenu.MenuItems.Add(Resources.D3DApp_NotifyIcon_Exit, ExitOnClick);
      _notifyIcon.Text = Resources.D3DApp_NotifyIcon_MediaPortal;
      _notifyIcon.Icon = ((Icon)(resources.GetObject("_notifyIcon.TrayIcon")));
      _notifyIcon.ContextMenu = _contextMenu;
      _notifyIcon.DoubleClick += Restore_OnClick;
      // 
      // menuItemContext
      // 
      _menuItemContext.Index = 0;
      _menuItemContext.MenuItems.AddRange(new[]
                                                {
                                                  _menuItem5
                                                });
      _menuItemContext.Text = "";
      // 
      // menuItem5
      // 
      _menuItem5.Index = 0;
      _menuItem5.Text = "";
      // 
      // D3DApp
      // 
      AutoScaleDimensions = new SizeF(6F, 13F);
      ClientSize = new Size(720, 576);
      KeyPreview = true;
      MinimumSize = new Size(100, 100);
      Name = "D3DApp";
      Load += D3DAppLoad;
      MouseDoubleClick += D3DAppMouseDoubleClick;
      MouseDown += D3DAppClick;
      Closing += D3DAppClosing;
      KeyPress += OnKeyPress;
      MouseMove += D3DAppMouseMove;
      KeyDown += OnKeyDown;
      ResumeLayout(false);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected override void OnMouseMove(MouseEventArgs e)
    {
      if ((GUIGraphicsContext.DX9Device != null) && (!GUIGraphicsContext.DX9Device.Disposed))
      {
        // Move the D3D cursor
        GUIGraphicsContext.DX9Device.SetCursorPosition(e.X, e.Y, false);
      }
      // Let the control handle the mouse now
      base.OnMouseMove(e);
    }


    /// <summary>
    /// Handles the OnSizeChanged event, which isn't the same as the resize event.
    /// </summary>
    /// <param name="e">Event arguments</param>
    protected override void OnSizeChanged(EventArgs e)
    {
      if (GUIGraphicsContext.IsDirectX9ExUsed() && Visible && !ResizeOngoing && !_toggleFullWindowed && !_ignoreNextResizeEvent && WindowState == _windowState)
      {
        Log.Info("Main: OnSizeChanged - Resetting device");
        SwitchFullScreenOrWindowed(false);
        OnDeviceReset(null, null);
      }
      base.OnSizeChanged(e);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected override void OnResizeBegin(EventArgs e)
    {
      if (GUIGraphicsContext.IsDirectX9ExUsed())
      {
        ResizeOngoing = true;
        OurClientSize = ClientSize;
      }
      base.OnResizeBegin(e);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected override void OnResize(EventArgs e)
    {
      if (GUIGraphicsContext.IsDirectX9ExUsed())
      {
        if (_ignoreNextResizeEvent)
        {
          _ignoreNextResizeEvent = false;
          return;
        }
        if (ResizeOngoing)
        {
          return;
        }
      }
      try
      {
        if (_fromTray)
        {
          _fromTray = false;
        }
        else
        {
          SavePlayerState();
        }
        if (_notifyIcon != null)
        {
          if (_notifyIcon.Visible == false && WindowState == FormWindowState.Minimized)
          {
            _notifyIcon.Visible = true;
            Hide();
            if (g_Player.IsVideo || g_Player.IsTV || g_Player.IsDVD)
            {
              if (g_Player.Volume > 0)
              {
                Volume = g_Player.Volume;
                g_Player.Volume = 0;
              }
              if (g_Player.Paused == false)
              {
                g_Player.Pause();
              }
            }
            return;
          }
          if (_notifyIcon.Visible && WindowState != FormWindowState.Minimized)
          {
            _notifyIcon.Visible = false;
          }
        }
        _active = WindowState != FormWindowState.Minimized;
        base.OnResize(e);
      }
      catch (Exception ex)
      {
        Log.Error("d3dapp: An error occured in OnResize - {0}", ex.ToString());
      }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected override void OnGotFocus(EventArgs e)
    {
      _active = true;
      base.OnGotFocus(e);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected override void OnMove(EventArgs e)
    {
      if (GUIGraphicsContext.IsDirectX9ExUsed())
      {
        // Window maximize / restore button pressed 
        if (WindowState != _windowState && WindowState != FormWindowState.Minimized)
        {
          _windowState = WindowState;
          _ignoreNextResizeEvent = true;
          SwitchFullScreenOrWindowed(false);
          OnDeviceReset(null, null);
        }
      }
      base.OnMove(e);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected override void OnClosing(CancelEventArgs e)
    {
      if (MinimizeOnGuiExit && !ShuttingDown)
      {
        if (WindowState != FormWindowState.Minimized)
        {
          Log.Info("D3D: Minimizing to tray on GUI exit");
        }
        _isClosing = false;
        WindowState = FormWindowState.Minimized;
        Hide();
        e.Cancel = true;

        if (g_Player.IsVideo || g_Player.IsTV || g_Player.IsDVD)
        {
          if (g_Player.Volume > 0)
          {
            Volume = g_Player.Volume;
            g_Player.Volume = 0;
          }
          if (g_Player.Paused == false)
          {
            g_Player.Pause();
          }
        }
        return;
      }
      if (AutoHideTaskbar)
      {
        HideTaskBar(false);
      }
      _isClosing = true;
      base.OnClosing(e);
    }

    #endregion


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected virtual void KeyPressEvent(KeyPressEventArgs e) {}


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected virtual void KeyDownEvent(KeyEventArgs e) {}


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected virtual void MouseMoveEvent(MouseEventArgs e)
    {
      if (!_disableMouseEvents && !ShowCursor)
      {
        Cursor.Show();
        ShowCursor = true;
        Invalidate(true);
      }
      MouseTimeOutTimer = DateTime.Now;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected virtual void MouseClickEvent(MouseEventArgs e)
    {
      if (!ShowCursor)
      {
        Cursor.Show();
        ShowCursor = true;
        Invalidate(true);
      }
      MouseTimeOutTimer = DateTime.Now;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected virtual void MouseDoubleClickEvent(MouseEventArgs e)
    {
      if (!ShowCursor)
      {
        Cursor.Show();
        ShowCursor = true;
        Invalidate(true);
      }
      MouseTimeOutTimer = DateTime.Now;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnKeyPress(object sender, KeyPressEventArgs e)
    {
      KeyPressEvent(e);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TelevisionMenuItemClick(object sender, EventArgs e)
    {
      g_Player.Stop();

      if (GUIGraphicsContext.DX9Device.PresentationParameters.Windowed == false)
      {
        SwitchFullScreenOrWindowed(true);
      }
      using (Settings xmlreader = new MPSettings())
      {
        xmlreader.Clear();
      }
      Process.Start(Config.GetFile(Config.Dir.Base, "configuration.exe"), @"/wizard /section=wizards\television.xml");
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PicturesMenuItemClick(object sender, EventArgs e)
    {
      g_Player.Stop();
      if (GUIGraphicsContext.DX9Device.PresentationParameters.Windowed == false)
      {
        SwitchFullScreenOrWindowed(true);
      }
      using (Settings xmlreader = new MPSettings())
      {
        xmlreader.Clear();
      }
      Process.Start(Config.GetFile(Config.Dir.Base, "configuration.exe"), @"/wizard /section=wizards\pictures.xml");
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MusicMenuItemClick(object sender, EventArgs e)
    {
      g_Player.Stop();
      if (GUIGraphicsContext.DX9Device.PresentationParameters.Windowed == false)
      {
        SwitchFullScreenOrWindowed(true);
      }
      using (Settings xmlreader = new MPSettings())
      {
        xmlreader.Clear();
      }
      Process.Start(Config.GetFile(Config.Dir.Base, "configuration.exe"), @"/wizard /section=wizards\music.xml");
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MoviesMenuItemClick(object sender, EventArgs e)
    {
      g_Player.Stop();
      if (GUIGraphicsContext.DX9Device.PresentationParameters.Windowed == false)
      {
        SwitchFullScreenOrWindowed(true);
      }
      using (Settings xmlreader = new MPSettings())
      {
        xmlreader.Clear();
      }
      Process.Start(Config.GetFile(Config.Dir.Base, "configuration.exe"), @"/wizard /section=wizards\movies.xml");
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DvdMenuItemClick(object sender, EventArgs e)
    {
      g_Player.Stop();
      if (GUIGraphicsContext.DX9Device.PresentationParameters.Windowed == false)
      {
        SwitchFullScreenOrWindowed(true);
      }
      using (Settings xmlreader = new MPSettings())
      {
        xmlreader.Clear();
      }
      Process.Start(Config.GetFile(Config.Dir.Base, "configuration.exe"), @"/wizard /section=wizards\dvd.xml");
    }


    /// <summary>
    /// 
    /// </summary>
    public void HandleMessage()
    {
      try
      {
        while (Win32API.PeekMessage(ref _msgApi, IntPtr.Zero, 0, 0, 0))
        {
          bool flag;
          if (_msgApi.hwnd != IntPtr.Zero && Win32API.IsWindowUnicode(new HandleRef(null, _msgApi.hwnd)))
          {
            flag = true;
            if (!Win32API.GetMessageW(ref _msgApi, IntPtr.Zero, 0, 0))
            {
              continue;
            }
          }
          else
          {
            flag = false;
            if (!Win32API.GetMessageA(ref _msgApi, IntPtr.Zero, 0, 0))
            {
              continue;
            }
          }
          Win32API.TranslateMessage(ref _msgApi);
          if (flag)
          {
            Win32API.DispatchMessageW(ref _msgApi);
          }
          else
          {
            Win32API.DispatchMessageA(ref _msgApi);
          }
        }
      }
#if DEBUG
      catch (Exception ex)
      {
        Log.Info("D3D: Exception: {0}", ex.ToString());
#else
 catch (Exception)
      {
#endif
      }
    }


    /// <summary>
    /// 
    /// </summary>
    private static void StartFrameClock()
    {
      _clockWatch.Reset();
      _clockWatch.Start();
    }


    /// <summary>
    /// 
    /// </summary>
    private static void WaitForFrameClock()
    {
      // frame limiting code.
      // sleep as long as there are ticks left for this frame
      _clockWatch.Stop();
      var timeElapsed = _clockWatch.ElapsedTicks;
      if (timeElapsed < GUIGraphicsContext.DesiredFrameTime)
      {
        var milliSecondsLeft = (((GUIGraphicsContext.DesiredFrameTime - timeElapsed) * 1000) / Stopwatch.Frequency);
        milliSecondsLeft = milliSecondsLeft == 0 ? 1 : milliSecondsLeft;
        Thread.Sleep((int)milliSecondsLeft);
      }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void Restore_OnClick(Object sender, EventArgs e)
    {
      RestoreFromTray();
    }


    /// <summary>
    /// 
    /// </summary>
    public void RestoreFromTray()
    {
      Log.Info("D3D: Restoring from tray");
      _fromTray = true;
      _active = true;
      _notifyIcon.Visible = false;
      Show();
      WindowState = FormWindowState.Normal;
      Activate();
      
      var fullScreenMode = Menu == null;
      if (fullScreenMode && AutoHideTaskbar)
      {
        HideTaskBar();
      }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="force"></param>
    public void MinimizeToTray(bool force = false)
    {
      var fullScreenMode = Menu == null;
      //if (fullScreenMode || force)
      //{
        Log.Info("D3D: Minimizing to tray");
        _fromTray = false;
        _active = false;
        _notifyIcon.Visible = true;
        Hide();
        WindowState = FormWindowState.Minimized;

        if (AutoHideTaskbar)
        {
          HideTaskBar(false);
        }
      //}
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void ExitOnClick(Object sender, EventArgs e)
    {
      ShuttingDown = true;
      GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.STOPPING;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MenuItemFullscreenClick(object sender, EventArgs e)
    {
      ToggleFullWindowed();

      var dialogNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
      if (dialogNotify != null)
      {
        dialogNotify.SetHeading(1020);
        dialogNotify.SetText(String.Format("{0}\n{1}", GUILocalizeStrings.Get(1021), GUILocalizeStrings.Get(1022)));
        dialogNotify.TimeOut = 6;
        dialogNotify.SetImage(String.Format(@"{0}\media\{1}", GUIGraphicsContext.Skin, "dialog_information.png"));
        dialogNotify.DoModal(GUIWindowManager.ActiveWindow);
      }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private bool AppStillIdle()
    {
      var result = Win32API.PeekMessage(ref _msgApi, IntPtr.Zero, 0, 0, 0);
      return !result;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ApplicationIdle(object sender, EventArgs e)
    {
      do
      {
        OnProcess();
        FrameMove();
        StartFrameClock();
        FullRender();
        WaitForFrameClock();

        if (GUIGraphicsContext.CurrentState == GUIGraphicsContext.State.STOPPING)
        {
          break;
        }
      } while (AppStillIdle());
    }


    /// <summary>
    /// 
    /// </summary>
    protected void DoMinimizeOnStartup()
    {
      Log.Info("d3dapp: Minimizing to tray on startup");
      MinimizeToTray(true);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void D3DAppMouseDoubleClick(object sender, MouseEventArgs e)
    {
      if (ActiveForm != this)
      {
        return;
      }
      MouseDoubleClickEvent(e);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="specified"></param>
    protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
    {
      var newBounds = new Rectangle(x, y, width, height);
      if (GUIGraphicsContext._useScreenSelector && Maximized && !newBounds.Equals(GUIGraphicsContext.currentScreen.Bounds))
      {
        Log.Info("d3dapp: Screenselector: skipped SetBoundsCore {0} does not match {1}", newBounds.ToString(), GUIGraphicsContext.currentScreen.Bounds.ToString());
      }
      else
      {
        base.SetBoundsCore(x, y, width, height, specified);
      }
    }


    /// <summary>
    /// toggles default and minitv mode
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MenuItemMiniTvClick(object sender, EventArgs e)
    {
      ToggleMiniTv();
    }


    /// <summary>
    /// 
    /// </summary>
    private void ToggleMiniTv()
    {
      if (!Windowed) return; // only affection window mode

      _miniTvMode = !_miniTvMode; // toggle

      if (_miniTvMode)
      {
        MinimumSize = new Size(720 / 2, 576 / 2);
        _alwaysOnTop = true;
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        Menu = null;
      }
      else
      {
        MinimumSize = new Size(720, 576);
        _alwaysOnTop = _alwaysOnTopConfig;
        FormBorderStyle = FormBorderStyle.Sizable;
        Menu = _menuStripMain;

        SwitchFullScreenOrWindowed(true);
      }

      Size = MinimumSize;
      TopMost = _alwaysOnTop;

      _menuItemMiniTv.Checked = _miniTvMode;
    }
  }

  #region Enums for D3D Applications

  /// <summary>
  /// Messages that can be used when displaying an error
  /// </summary>
  public enum ApplicationMessage
  {
    None,
    ApplicationMustExit,
    WarnSwitchToRef
  }

  #endregion

  #region SampleExceptions

  /// <summary>
  /// The default sample exception type
  /// </summary>
  public class SampleException : ApplicationException
  {
    /// <summary>
    /// Return information about the exception
    /// </summary>
    public override string Message
    {
      get
      {
        var strMsg = "Generic application error. Enable\n";
        strMsg    += "debug output for detailed information.";
        return strMsg;
      }
    }
  }


  /// <summary>
  /// Exception informing user no compatible devices were found
  /// </summary>
  public class NoCompatibleDevicesException : SampleException
  {
    /// <summary>
    /// Return information about the exception
    /// </summary>
    public override string Message
    {
      get
      {
        var strMsg = "This sample cannot run in a desktop\n";
        strMsg    += "window with the current display settings.\n";
        strMsg    += "Please change your desktop settings to a\n";
        strMsg    += "16- or 32-bit display mode and re-run this\n";
        strMsg    += "sample.";
        return strMsg;
      }
    }
  }


  /// <summary>
  /// An exception for when the ReferenceDevice is null
  /// </summary>
  public class NullReferenceDeviceException : SampleException
  {
    /// <summary>
    /// Return information about the exception
    /// </summary>
    public override string Message
    {
      get
      {
        var strMsg = "Warning: Nothing will be rendered.\n";
        strMsg    += "The reference rendering device was selected, but your\n";
        strMsg    += "computer only has a reduced-functionality reference device\n";
        strMsg    += "installed. Please check if your graphics card and\n";
        strMsg    += "drivers meet the minimum system requirements.\n";
        return strMsg;
      }
    }
  }


  /// <summary>
  /// An exception for when reset fails
  /// </summary>
  public class ResetFailedException : SampleException
  {
    /// <summary>
    /// Return information about the exception
    /// </summary>
    public override string Message
    {
      get
      {
        const string strMsg = "Could not reset the Direct3D device.";
        return strMsg;
      }
    }
  }

  #endregion

  #region Native Methods

  /// <summary>
  /// Will hold native methods
  /// </summary>
  public class NativeMethods
  {
    #region Win32 Messages / Structures
    
    /// <summary>
    /// Window messages
    /// </summary>
    public enum WindowMessage : uint
    {
      // Misc messages
      Destroy                 = 0x0002,
      Close                   = 0x0010,
      Quit                    = 0x0012,
      Paint                   = 0x000F,
      SetCursor               = 0x0020,
      ActivateApplication     = 0x001C,
      EnterMenuLoop           = 0x0211,
      ExitMenuLoop            = 0x0212,
      NonClientHitTest        = 0x0084,
      PowerBroadcast          = 0x0218,
      SystemCommand           = 0x0112,
      GetMinMax               = 0x0024,

      // Keyboard messages
      KeyDown                 = 0x0100,
      KeyUp                   = 0x0101,
      Character               = 0x0102,
      SystemKeyDown           = 0x0104,
      SystemKeyUp             = 0x0105,
      SystemCharacter         = 0x0106,

      // Mouse messages
      MouseMove               = 0x0200,
      LeftButtonDown          = 0x0201,
      LeftButtonUp            = 0x0202,
      LeftButtonDoubleClick   = 0x0203,
      RightButtonDown         = 0x0204,
      RightButtonUp           = 0x0205,
      RightButtonDoubleClick  = 0x0206,
      MiddleButtonDown        = 0x0207,
      MiddleButtonUp          = 0x0208,
      MiddleButtonDoubleClick = 0x0209,
      MouseWheel              = 0x020a,
      XButtonDown             = 0x020B,
      XButtonUp               = 0x020c,
      XButtonDoubleClick      = 0x020d,
      MouseFirst              = LeftButtonDown, // Skip mouse move, it happens a lot and there is another message for that
      MouseLast               = XButtonDoubleClick,

      // Sizing
      EnterSizeMove           = 0x0231,
      ExitSizeMove            = 0x0232,
      Size                    = 0x0005,
    }

    
    /// <summary>
    /// Monitor Info structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInformation
    {
      public uint Size; // Size of this structure
      public Rectangle MonitorRectangle;
      public Rectangle WorkRectangle;
      public uint Flags; // Possible flags
    }

    #endregion

    #region Delegates

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="msg"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    public delegate IntPtr WndProcDelegate(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

    #endregion

    #region Windows API calls

    [SuppressUnmanagedCodeSecurity]
    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    public static extern void DisableProcessWindowsGhosting();

    [SuppressUnmanagedCodeSecurity]
    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    public static extern bool GetMonitorInfo(IntPtr hWnd, ref MonitorInformation info);

    #endregion
  }

  #endregion
}