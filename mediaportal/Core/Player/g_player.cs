#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
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

using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

using System.Runtime.CompilerServices;
using System.Text;
using MediaPortal.Configuration;
using MediaPortal.ExtensionMethods;
using MediaPortal.GUI.Library;
using MediaPortal.Playlists;
using MediaPortal.Profile;
using MediaPortal.Subtitle;
using MediaPortal.Util;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Cd;
using MediaPortal.Player.Subtitles;

namespace MediaPortal.Player
  // ReSharper disable InconsistentNaming
  public class g_Player
  // ReSharper restore InconsistentNaming
  public class g_Player
  {
    #region enums

    public enum MediaType
    {
      Video,
      TV,
      Radio,
      RadioRecording,
      Music,
      Recording,
      Unknown
    } ;

    public enum DriveType
    {
      CD,
      DVD
    } ;

    #endregion

    #region variables
    private static MediaInfoWrapper _mediaInfo;
    private static int _currentStep;
    private static int _currentStep = 0;
    private static int _currentStepIndex = -1;
    private static IPlayer _player;
    private static IPlayer _prevPlayer;
    private static SubTitles _subs;
    private static bool _isInitialized;
    private static bool _isInitialized = false;
    private static string _currentFilePlaying = "";
    private static MediaType _currentMedia;
    private static IPlayerFactory _factory;
    public static bool Starting = false;
    private static ArrayList _seekStepList = new ArrayList();
    public static bool ConfigLoaded = false;
    public static bool configLoaded = false;
    private static string[] _driveSpeedCD;
    private static string[] _driveSpeedDVD;
    private static string[] _disableCDSpeed;
    private static int _driveCount;
    private static int _driveCount = 0;
    private static bool _driveSpeedLoaded;
    private static bool _driveSpeedReduced;
    private static bool _driveSpeedControlEnabled;
    private static string _currentTitle = ""; //actual program metadata - useful for TV - avoids extra DB lookups
    private static string _currentTitle = ""; //actual program metadata - usefull for tv - avoids extra DB lookups

    //actual program metadata - useful for TV - avoids extra DB Lookups. 
    //actual program metadata - usefull for tv - avoids extra DB Lookups. 
    private static string _currentFileName = ""; //holds the actual file being played. Useful for RTSP streams. 
    private static double[] _chapters;
    private static string[] _chaptersname;
    private static double[] _jumpPoints;
    private static bool _autoComSkip;
    private static bool _autoComSkip = false;
    private static bool _loadAutoComSkipSetting = true;

    private static string _externalPlayerExtensions = string.Empty;

    #endregion

    #region events

    public delegate void StoppedHandler(MediaType type, int stoptime, string filename);

    public delegate void EndedHandler(MediaType type, string filename);

    public delegate void StartedHandler(MediaType type, string filename);

    public delegate void ChangedHandler(MediaType type, int stoptime, string filename);

    public delegate void AudioTracksReadyHandler();

    public delegate void TVChannelChangeHandler();

    // when a user is already playing a file without stopping the user selects another file for playback.
    // in this case we do not receive the onstopped event.
    // so saving the resume point of a video file is not happening, since this relies on the onstopped event.
    // instead a plugin now has to listen to ChangedHandler event instead.
    public static event ChangedHandler PlayBackChanged;
    public static event StoppedHandler PlayBackStopped;
    public static event EndedHandler PlayBackEnded;
    public static event StartedHandler PlayBackStarted;
    public static event AudioTracksReadyHandler AudioTracksReady;
    public static event TVChannelChangeHandler TVChannelChanged;

    #endregion

    #region Delegates

    #endregion

    #region ctor/dtor
    // singleton. Don't allow any instance of this class
    // singleton. Dont allow any instance of this class
    private g_Player()
    {
      _factory = new PlayerFactory();
    }

    static g_Player() {}

    public static IPlayer Player
      get
      {
        return _player;
      }
      get { return _player; }
    }

    public static IPlayerFactory Factory
      get
      {
        return _factory;
      }
      set
      {
        _factory = value;
      }
      set { _factory = value; }
    }
    public static string CurrentTitle
    public static string currentTitle
      get
      {
        return _currentTitle;
      }
      set
      {
        _currentTitle = value;
      }
      set { _currentTitle = value; }
    }
    public static string CurrentFileName
    public static string currentFileName
      get
      {
        return _currentFileName;
      }
      set
      {
        _currentFileName = value;
      }
      set { _currentFileName = value; }
    }
    public static string CurrentDescription
    public static string currentDescription
      get
      {
        return _currentDescription;
      }
      set
      {
        _currentDescription = value;
      }
      set { _currentDescription = value; }
    }

    #endregion

    #region Serialisation

    /// <summary>
    /// Retrieve the CD/DVD Speed set in the config file
    /// </summary>
    public static void LoadDriveSpeed()
      string speedTableCD;
      string speedTableDVD;
      string disableCD;
      string disableDVD;
      string disableDVD = string.Empty;
      using (Settings xmlreader = new MPSettings())
      {
        speedTableCD = xmlreader.GetValueAsString("cdspeed", "drivespeedCD", string.Empty);
        disableCD = xmlreader.GetValueAsString("cdspeed", "disableCD", string.Empty);
        speedTableDVD = xmlreader.GetValueAsString("cdspeed", "drivespeedDVD", string.Empty);
        _driveSpeedControlEnabled = xmlreader.GetValueAsBool("cdspeed", "enabled", false);
        driveSpeedControlEnabled = xmlreader.GetValueAsBool("cdspeed", "enabled", false);
      if (!_driveSpeedControlEnabled)
      if (!driveSpeedControlEnabled)
      {
        return;
      }
      // if BASS is not the default audio engine, we need to load the CD Plugin first
      if (!BassMusicPlayer.IsDefaultMusicPlayer)
      {
        string decoderFolderPath = Path.Combine(appPath, @"musicplayer\plugins\audio decoders");
        int pluginHandle = Bass.BASS_PluginLoad(decoderFolderPath + "\\basscd.dll");
      }
      // Get the number of CD/DVD drives
      var builderDriveLetter = new StringBuilder();
      StringBuilder builderDriveLetter = new StringBuilder();
      for (var i = 0; i < _driveCount; i++)
      for (int i = 0; i < _driveCount; i++)
      {
        builderDriveLetter.Append(BassCd.BASS_CD_GetInfo(i).DriveLetter);
      }
      _driveLetters = builderDriveLetter.ToString();
      if (speedTableCD == string.Empty || speedTableDVD == string.Empty)
        var cdinfo = new BASS_CD_INFO();
        var builder = new StringBuilder();
        var builderDisable = new StringBuilder();
        for (var i = 0; i < _driveCount; i++)
        for (int i = 0; i < _driveCount; i++)
        {
          if (builder.Length != 0)
          {
            builder.Append(",");
          }
          if (builderDisable.Length != 0)
          {
            builderDisable.Append(", ");
          }
          var maxspeed = (int)(cdinfo.maxspeed / 176.4);
          builder.Append(Convert.ToInt32(maxspeed).ToString(CultureInfo.InvariantCulture));
          builder.Append(Convert.ToInt32(maxspeed).ToString());
          builderDisable.Append("N");
        }
        speedTableCD = builder.ToString();
        speedTableDVD = builder.ToString();
        disableCD = builderDisable.ToString();
        disableDVD = builderDisable.ToString();
      }
      _driveSpeedCD = speedTableCD.Split(',');
      _driveSpeedDVD = speedTableDVD.Split(',');
      _disableCDSpeed = disableCD.Split(',');
      _driveSpeedLoaded = true;
      driveSpeedLoaded = true;
      BassMusicPlayer.ReleaseCDDrives();
    }

    /// <summary>
    /// Read the configuration file to get the skip steps
    /// </summary>
    public static ArrayList LoadSettings()
      var stepArray = new ArrayList();
      ArrayList StepArray = new ArrayList();
      using (Settings xmlreader = new MPSettings())
        var strFromXml = xmlreader.GetValueAsString("movieplayer", "skipsteps",
                                                    "15,30,60,180,300,600,900,1800,3600,7200");
                                                       "15,30,60,180,300,600,900,1800,3600,7200");
        if (strFromXml == string.Empty) // config after wizard run 1st
        {
          strFromXml = "15,30,60,180,300,600,900,1800,3600,7200";
          Log.Info("g_player - creating new Skip-Settings {0}", "");
        }
        else if (OldStyle(strFromXml))
        {
          strFromXml = ConvertToNewStyle(strFromXml);
        foreach (var token in strFromXml.Split(new[] {',', ';', ' '}).Where(token => token != string.Empty))
        {
          stepArray.Add(Convert.ToInt32(token));
        _seekStepList = stepArray;
        var timeout = (xmlreader.GetValueAsString("movieplayer", "skipsteptimeout", "1500"));
        _seekStepTimeout = timeout == string.Empty ? 1500 : Convert.ToInt16(timeout);
          _seekStepTimeout = Convert.ToInt16(timeout);
      ConfigLoaded = true;
      return stepArray; // Sorted list of step times
      configLoaded = true;
      return StepArray; // Sorted list of step times
    }

      var count = 0;
      var foundOtherThanZeroOrOne = false;
      foreach (var curInt in from token in strSteps.Split(new[] {',', ';', ' '}) where token != string.Empty select Convert.ToInt16(token))
      {
        }
        int curInt = Convert.ToInt16(token);
        if (curInt != 0 && curInt != 1)
        count++;
          foundOtherThanZeroOrOne = true;
        }
        count++;
      }
      return (count == 16 && !foundOtherThanZeroOrOne);
      var count = 0;
      var newStyle = string.Empty;
      foreach (var token in strSteps.Split(new[] {',', ';', ' '}))
    {
      int count = 0;
      string newStyle = string.Empty;
      foreach (string token in strSteps.Split(new char[] {',', ';', ' '}))
      {
        if (token == string.Empty)
        {
          count++;
          continue;
        }
        int curInt = Convert.ToInt16(token);
        count++;
        if (curInt == 1)
        {
          switch (count)
          {
            case 1:
              newStyle += "5,";
              break;
            case 2:
              newStyle += "15,";
              break;
            case 3:
              newStyle += "30,";
              break;
            case 4:
              newStyle += "45,";
              break;
            case 5:
              newStyle += "60,";
              break;
            case 6:
              newStyle += "180,";
              break;
            case 7:
              newStyle += "300,";
              break;
            case 8:
              newStyle += "420,";
              break;
            case 9:
              newStyle += "600,";
              break;
            case 10:
              newStyle += "900,";
              break;
            case 11:
              newStyle += "1800,";
              break;
            case 12:
              newStyle += "2700,";
              break;
            case 13:
              newStyle += "3600,";
              break;
            case 14:
              newStyle += "5400,";
              break;
            case 15:
              break;
              newStyle += "10800,";
              break;
            default:
    }

    /// <summary>
    /// Changes the speed of a drive to the value set in configuration
    /// </summary>
    /// <param name="strFile"></param>
    /// <param name="drivetype"> </param>

      if (!_driveSpeedLoaded)
    /// Changes the speed of a drive to the value set in configuration
    /// </summary>
    /// <param name="strFile"></param>
      if (!_driveSpeedControlEnabled)
    {
      if (!driveSpeedLoaded)
      {
      try
      {
        var rootPath = Path.GetPathRoot(strFile);
        if (rootPath != null && rootPath.Length > 1)
        {
          var driveindex = _driveLetters.IndexOf(rootPath.Substring(0, 1), StringComparison.Ordinal);
          if (driveindex > -1 && driveindex < _driveSpeedCD.Length)
          {
            string speed;
            if (drivetype == DriveType.CD && _disableCDSpeed[driveindex] == "N")
            {
              speed = _driveSpeedCD[driveindex];
            }
            else if (drivetype == DriveType.DVD && _disableDVDSpeed[driveindex] == "N")
            {
              speed = _driveSpeedDVD[driveindex];
            }
            else
            {
              return;
            }
            BassCd.BASS_CD_SetSpeed(driveindex, Convert.ToSingle(speed));
            Log.Info("g_player: Playback Speed on Drive {0} reduced to {1}", rootPath.Substring(0, 1), speed);
            _driveSpeedReduced = true;
          }
        }
      }
              BassCd.BASS_CD_SetSpeed(driveindex, Convert.ToSingle(speed));
              Log.Info("g_player: Playback Speed on Drive {0} reduced to {1}", rootPath.Substring(0, 1), speed);
              driveSpeedReduced = true;
            }
          }
        }
      }
      catch (Exception) {}
    }

    #endregion

    #region public members

    //called when TV channel is changed
    private static void OnTVChannelChanged()
    {
      if (TVChannelChanged != null)
      {
        TVChannelChanged();
      }
    }

    internal static void OnAudioTracksReady()
    {
      if (AudioTracksReady != null) // FIXME: the event handler might not be set if TV plugin is not installed!
      {
    // called when current playing file is stopped
      }
      else
      if (string.IsNullOrEmpty(newFile))
        CurrentAudioStream = 0;
      }
    }

    //called when current playing file is stopped
    private static void OnChanged(string newFile)
        // yes, then raise event
      if (newFile == null || newFile.Length == 0)
      {
        return;
      }

      if (!newFile.Equals(CurrentFile))
      {
        //yes, then raise event
        Log.Info("g_Player.OnChanged()");
                          (!String.IsNullOrEmpty(CurrentFileName) ? CurrentFileName : CurrentFile));
          CurrentFileName = String.Empty;
          if ((_currentMedia == MediaType.TV || _currentMedia == MediaType.Video || _currentMedia == MediaType.Recording) &&
              (!Util.Utils.IsVideo(newFile)))
          {
            RefreshRateChanger.AdaptRefreshRate();
          }
          PlayBackChanged(_currentMedia, (int)CurrentPosition,
                          (!String.IsNullOrEmpty(currentFileName) ? currentFileName : CurrentFile));
          currentFileName = String.Empty;
        }
      }
    }

        PlayBackStopped(_currentMedia, (int)CurrentPosition,
                        (!String.IsNullOrEmpty(CurrentFileName) ? CurrentFileName : CurrentFile));
        CurrentFileName = String.Empty;
        _mediaInfo = null;
        Log.Info("g_Player.OnStopped()");
        if (PlayBackStopped != null)
        {
          PlayBackStopped(_currentMedia, (int)CurrentPosition,
                          (!String.IsNullOrEmpty(currentFileName) ? currentFileName : CurrentFile));
          currentFileName = String.Empty;
          _mediaInfo = null;
        }
      }
    }

        PlayBackEnded(_currentMedia, (!String.IsNullOrEmpty(CurrentFileName) ? CurrentFileName : _currentFilePlaying));
    private static void OnEnded()
        CurrentFileName = String.Empty;
      //check if we're playing
      if (PlayBackEnded != null)
      {
        //yes, then raise event
        Log.Info("g_Player.OnEnded()");
        PlayBackEnded(_currentMedia, (!String.IsNullOrEmpty(currentFileName) ? currentFileName : _currentFilePlaying));
        RefreshRateChanger.AdaptRefreshRate();
        currentFileName = String.Empty;
        _mediaInfo = null;
      }
    }

    //called when starting playing a file
    private static void OnStarted()
        // yes, then raise event 
      //check if we're playing
      if (_player == null)
      {
        return;
      }
      if (_player.Playing)
      {
        //yes, then raise event 
        _currentMedia = MediaType.Music;
        if (_player.IsTV)
        {
          _currentMedia = MediaType.TV;
          if (!_player.IsTimeShifting)
        else if (_player.HasVideo && _player.ToString() != "MediaPortal.Player.BassAudioEngine")
        {
          _currentMedia = MediaType.Video;
        }
        }
        else if (_player.HasVideo)
        {
          if (_player.ToString() != "MediaPortal.Player.BassAudioEngine")
          {
            _currentMedia = MediaType.Video;
          }
        }
        Log.Info("g_Player.OnStarted() {0} media:{1}", _currentFilePlaying, _currentMedia.ToString());
        if (PlayBackStarted != null)
        {
          PlayBackStarted(_currentMedia, _currentFilePlaying);
        }
      }
    }

    public static void PauseGraph()
    {
      if (_player != null)
      {
        _player.PauseGraph();
      }
    }

    private static void DoStop(bool keepTimeShifting, bool keepExclusiveModeOn)
    {
      if (_driveSpeedReduced)
      {
        _player.ContinueGraph();
        var cdinfo = new BASS_CD_INFO();
        for (var i = 0; i < _driveCount; i++)

    [MethodImpl(MethodImplOptions.Synchronized)]
          var maxspeed = (int)(cdinfo.maxspeed / 176.4);
    {
      if (driveSpeedReduced)
      {
        // Set the CD/DVD Speed back to Max Speed
        BASS_CD_INFO cdinfo = new BASS_CD_INFO();
        for (int i = 0; i < _driveCount; i++)
        {
          BassCd.BASS_CD_GetInfo(i, cdinfo);
          int maxspeed = (int)(cdinfo.maxspeed / 176.4);
        var currentFile = CurrentFile;
          BassCd.BASS_CD_Release(i);
        }
      }
      if (_player != null)
      {
        Log.Debug("g_Player.doStop() keepTimeShifting = {0} keepExclusiveModeOn = {1}", keepTimeShifting,
                  keepExclusiveModeOn);
        // Get playing file for unmount handling
        GUIGraphicsContext.ShowBackground = true;
        if (keepTimeShifting || keepExclusiveModeOn)
        {
          if (keepExclusiveModeOn)
          {
            Log.Debug("g_Player.doStop() - stop, keep exclusive mode on");
            _player.Stop(true);
          }
          else
          {
            Log.Debug("g_Player.doStop() - StopAndKeepTimeShifting");
            _player.StopAndKeepTimeShifting();
          }
        }
        else
        {
          Log.Debug("g_Player.doStop() - stop");
          _player.Stop();
        }
        {
          Log.Debug("g_Player.doStop() - stop, keep exclusive mode on");
          _player.Stop(true);
        var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED, 0, 0, 0, 0, 0, null);
        else
        {
          Log.Debug("g_Player.doStop() - StopAndKeepTimeShifting");
          _player.StopAndKeepTimeShifting();
        }
        if (GUIGraphicsContext.form != null)
        {
          GUIGraphicsContext.form.Invalidate(true);
        }
        GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED, 0, 0, 0, 0, 0, null);
        GUIWindowManager.SendThreadMessage(msg);
        GUIGraphicsContext.IsFullScreenVideo = false;
        GUIGraphicsContext.IsPlaying = false;
        GUIGraphicsContext.IsPlayingVideo = false;
        CachePlayer();
        Util.Utils.IsDVDImage(currentFile, ref currentFile);
        if (Util.Utils.IsISOImage(currentFile) && (!String.IsNullOrEmpty(DaemonTools.GetVirtualDrive()) &&
                                                   IsBDDirectory(DaemonTools.GetVirtualDrive()) ||
                                                   IsDvdDirectory(DaemonTools.GetVirtualDrive())))
        {
          DaemonTools.UnMount();
        }
        Util.Utils.IsDVDImage(currentFile, ref currentFile);
        if (Util.Utils.IsISOImage(currentFile))
    public static bool IsBDDirectory(string path)
    {
      return File.Exists(path + @"\BDMV\index.bdmv");
    }

    public static bool IsDvdDirectory(string path)
    {
      return File.Exists(path + @"\VIDEO_TS\VIDEO_TS.IFO");
    }

    }
      DoStop(true, false);
    public static bool IsDvdDirectory(string path)
    {
      if (File.Exists(path + @"\VIDEO_TS\VIDEO_TS.IFO"))
      {
        return true;
      }
      return false;
        DoStop(false, true);

    public static void StopAndKeepTimeShifting()
    {
      doStop(true, false);
    }

    public static void Stop(bool keepExclusiveModeOn)
    {
      Log.Info("g_Player.Stop() - keepExclusiveModeOn = {0}", keepExclusiveModeOn);
      if (keepExclusiveModeOn)
      var currentmodulefullscreen = (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_TVFULLSCREEN ||
                                     GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_MUSIC ||
                                     GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO ||
                                     GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_TELETEXT);
      {
      DoStop(false, false);
      }
    }
    private static void CachePlayer()
    {
    {
      // we have to save the fullscreen status of the tv3 plugin for later use for the lastactivemodulefullscreen feature.
      }
      if (_player.SupportsReplay)
      {
        _prevPlayer = _player;
        _player = null;
      }
      else
      {
        _player.SafeDispose();
        _player = null;
        _prevPlayer = null;
      }
    }

      if (_player.SupportsReplay)
      {
        _prevPlayer = _player;
        _player = null;
      }
      else
      {
        _player.SafeDispose();
        _player = null;
        _player.Pause();
        if (VMR9Util.g_vmr9 != null && _player.Paused)
        {
          VMR9Util.g_vmr9.SetRepaint();
        }
        _currentStep = 0;
        _currentStepIndex = -1;
        _seekTimer = DateTime.MinValue;
        _player.Speed = 1; //default back to 1x speed.

        if (!_player.IsDVD && _chapters != null)
        _player.Pause();
        if (VMR9Util.g_vmr9 != null)
        {
          if (_player.Paused)
          {
            VMR9Util.g_vmr9.SetRepaint();
          }
        }
      }
    }

    public static bool OnAction(Action action)
    {
      if (_player != null)
      {
        if (!_player.IsDVD && Chapters != null)
        {
          switch (action.wID)
          {
      get
      {
        return _player != null && _player.IsCDA;
      }
        return _player.OnAction(action);
      }
      return false;
      get
      {
        return _player != null && _player.IsDVD;
      }
          return false;
        }
        return _player.IsCDA;
      get
      {
        return _player != null && _player.IsDVDMenu;
      }
        {
          return false;
        }
      get
      {
        return _player == null ? MenuItems.All : _player.ShowMenuItems;
      }
        if (_player == null)
        {
          return false;
      get { return _player != null && Chapters != null; }
        }
        return _player.ShowMenuItems;
      }
      get
      {
        return RefreshRateChanger.RefreshRateChangePending &&
               RefreshRateChanger.RefreshRateChangeMediaType == RefreshRateChanger.MediaType.TV ||
               _player != null && _player.IsTV;
      }
        }
        return true;
      }
      get
      {
        return RefreshRateChanger.RefreshRateChangePending &&
               RefreshRateChanger.RefreshRateChangeMediaType == RefreshRateChanger.MediaType.Recording ||
               _player != null && _currentMedia == MediaType.Recording;
      }
          return false;
        }
        return _player.IsTV;
      get
      {
        return _player != null && _player.IsTimeShifting;
      }
            RefreshRateChanger.RefreshRateChangeMediaType == RefreshRateChanger.MediaType.Recording)
        {
          return true;
        }
        if (_player == null)
        {
          return false;
        }
        return (_currentMedia == MediaType.Recording);
      }
    }

    public static bool IsTimeShifting
    {
      get
      {
        if (_player == null)
        {
          return false;
        }
        return _player.IsTimeShifting;
      }
    }

    public static void Release()
    {
      if (_player != null)
      {
        _player.Stop();
        CachePlayer();
      }
    }

    public static bool PlayDVD()
    {
      return PlayDVD("");
    }

    public static bool PlayDVD(string strPath)
    {
      return Play(strPath, MediaType.Video);
    }

    public static bool PlayBD(string strPath)
    {
      return Play(strPath, MediaType.Video);
    }

    /* using Play function from new PlayDVD
    public static bool PlayDVD(string strPath)
    {     
      try
      {
        _mediaInfo = new MediaInfoWrapper(strPath);
        Starting = true;

        RefreshRateChanger.AdaptRefreshRate(strPath, RefreshRateChanger.MediaType.Video);
        if (RefreshRateChanger.RefreshRateChangePending)
        {
          TimeSpan ts = DateTime.Now - RefreshRateChanger.RefreshRateChangeExecutionTime;
          //_refreshrateChangeExecutionTime;
          if (ts.TotalSeconds > RefreshRateChanger.WAIT_FOR_REFRESHRATE_RESET_MAX)
          {
            Log.Info(
              "g_Player.PlayDVD - waited {0}s for refreshrate change, but it never took place (check your config). Proceeding with playback.",
              RefreshRateChanger.WAIT_FOR_REFRESHRATE_RESET_MAX);
            RefreshRateChanger.ResetRefreshRateState();
          }
          else
          {
            return true;
          }
        }

        // Stop the BASS engine to avoid problems with Digital Audio
        BassMusicPlayer.Player.FreeBass();
        ChangeDriveSpeed(strPath, DriveType.DVD);
        //stop playing radio
        GUIMessage msgRadio = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_RADIO, 0, 0, 0, 0, 0, null);
        GUIWindowManager.SendMessage(msgRadio);
        //stop timeshifting tv
        //GUIMessage msgTv = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_TIMESHIFT, 0, 0, 0, 0, 0, null);
        //GUIWindowManager.SendMessage(msgTv);
        Log.Info("g_Player.PlayDVD()");
        _currentStep = 0;
        _currentStepIndex = -1;
        _seekTimer = DateTime.MinValue;
        _subs = null;
        if (_player != null)
        {
          GUIGraphicsContext.ShowBackground = true;
          OnChanged(strPath);
          OnStopped();
          if (_player != null)
          {
            _player.Stop();
          }
          CachePlayer();
          GUIGraphicsContext.form.Invalidate(true);
          _player = null;
        }
        if (Util.Utils.PlayDVD())
        {
          return true;
        }
        _isInitialized = true;
        int iUseVMR9 = 0;
        using (Settings xmlreader = new MPSettings())
        {
          iUseVMR9 = xmlreader.GetValueAsInt("dvdplayer", "vmr9", 0);
        }
        _player = new DVDPlayer9();
        _player = CachePreviousPlayer(_player);
        bool bResult = _player.Play(strPath);
        if (!bResult)
        {
          Log.Error("g_Player.PlayDVD():failed to play");
          _player.Release();
          _player = null;
          _subs = null;
          GC.Collect();
          GC.Collect();
          GC.Collect();
          Log.Info("dvdplayer:bla");
        }
        else if (_player.Playing)
        {
          _isInitialized = false;
          if (!_player.IsTV)
          {
    // GUIMusicVideos and GUITRailer called this function, although they want to play a Video.
            GUIWindowManager.ActivateWindow((int) GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO);
          }
          _currentFilePlaying = _player.CurrentFile;
          OnStarted();
          return true;
        _mediaInfo = null;
        string strAudioPlayer;
        //show dialog:unable to play dvd,
        GUIWindowManager.ShowWarning(722, 723, -1);
      }
      finally
      {
        // stop radio
        var msgRadio = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_RADIO, 0, 0, 0, 0, 0, null);
      return false;       
        var currentList = PlayListPlayer.SingletonPlayer.CurrentPlaylistType;
        if (isMusicVideo && (currentList == PlayListType.PLAYLIST_MUSIC_TEMP || currentList == PlayListType.PLAYLIST_VIDEO_TEMP))
        {
          PlayListPlayer.SingletonPlayer.GetPlaylist(currentList).Clear();
          PlayListPlayer.SingletonPlayer.Reset();
        }
        // stop time shifting TV
        // GUIMessage msgTv = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_TIMESHIFT, 0, 0, 0, 0, 0, null);
        // GUIWindowManager.SendMessage(msgTv);
        _mediaInfo = null;
        string strAudioPlayer = string.Empty;
        using (Settings xmlreader = new MPSettings())
        {
          strAudioPlayer = xmlreader.GetValueAsString("audioplayer", "player", "Internal dshow player");
        }
        Starting = true;
        //stop radio
        GUIMessage msgRadio = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_RADIO, 0, 0, 0, 0, 0, null);
        GUIWindowManager.SendMessage(msgRadio);
        PlayListType currentList = PlayListPlayer.SingletonPlayer.CurrentPlaylistType;
        if (isMusicVideo)
        {
          // Clear any temp. playlists before starting playback
          if (currentList == PlayListType.PLAYLIST_MUSIC_TEMP || currentList == PlayListType.PLAYLIST_VIDEO_TEMP)
          {
            PlayListPlayer.SingletonPlayer.GetPlaylist(currentList).Clear();
            PlayListPlayer.SingletonPlayer.Reset();
          }
        }
        //stop timeshifting tv
        //GUIMessage msgTv = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_TIMESHIFT, 0, 0, 0, 0, 0, null);
        //GUIWindowManager.SendMessage(msgTv);
        _currentStep = 0;
        _currentStepIndex = -1;
        _seekTimer = DateTime.MinValue;
        _isInitialized = true;
        _subs = null;
        Log.Info("g_Player.PlayAudioStream({0})", strURL);
        if (_player != null)
        {
          GUIGraphicsContext.ShowBackground = true;
          OnChanged(strURL);
        var bResult = _player.Play(strURL);
          if (_player != null)
          {
            _player.Stop();
          }
          CachePlayer();
          GUIGraphicsContext.form.Invalidate(true);
          _player = null;
        }

        if (strAudioPlayer == "BASS engine" && !isMusicVideo)
        {
          if (BassMusicPlayer.BassFreed)
          {
            BassMusicPlayer.Player.InitBass();
          }
          _player = BassMusicPlayer.Player;
        }
        else
        {
          _player = new AudioPlayerWMP9();
        }
        _player = CachePreviousPlayer(_player);
        bool bResult = _player.Play(strURL);
        if (!bResult)
        {
          Log.Info("player:ended");
          _player.SafeDispose();
          _player = null;
          GC.Collect();
          GC.Collect();
          GC.Collect();
        }
        else if (_player.Playing)
        {
          _currentFilePlaying = _player.CurrentFile;
          OnStarted();
        }
        _isInitialized = false;
        if (!bResult)
        // stop radio
        var msgRadio = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_RADIO, 0, 0, 0, 0, 0, null);
        }
        return bResult;
      }
      finally
      {
        Starting = false;
      }
    }

    //Added by juvinious 19/02/2005
    public static bool PlayVideoStream(string strURL)
    {
      return PlayVideoStream(strURL, "");
    }

    public static bool PlayVideoStream(string strURL, string streamName)
    {
      try
      {
        _mediaInfo = null;
        Starting = true;
        //stop radio
        GUIMessage msgRadio = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_RADIO, 0, 0, 0, 0, 0, null);
        GUIWindowManager.SendMessage(msgRadio);
        _currentStep = 0;
        _currentStepIndex = -1;
        _seekTimer = DateTime.MinValue;
        if (string.IsNullOrEmpty(strURL))
        {
          UnableToPlay(strURL, MediaType.Unknown);
          return false;
          OnChanged(strURL);
        _player = CachePreviousPlayer(_player);
        var isPlaybackPossible = streamName != null
                                   ? (streamName != "" ? _player.PlayStream(strURL, streamName) : _player.Play(strURL))
                                   : _player.Play(strURL);
        if (!isPlaybackPossible)
        {
          Log.Info("player:ended");
          _player.SafeDispose();
          _player = null;
          _subs = null;
          GC.Collect();
          GC.Collect();
          GC.Collect();

          // 2nd try
          _player = new VideoPlayerVMR9();
          isPlaybackPossible = _player.Play(strURL);
          if (!isPlaybackPossible)
          {
            Log.Info("player2:ended");
            _player.SafeDispose();
            _player = null;
            _subs = null;
            GC.Collect();
            GC.Collect();
            GC.Collect();
          }
        }
        else
        {
          if (_player.Playing)
          {
            _currentFilePlaying = _player.CurrentFile;
            OnStarted();
          }
        }
          _subs = null;
          GC.Collect();
          GC.Collect();
          GC.Collect();
          //2nd try
          _player = new VideoPlayerVMR9();
          isPlaybackPossible = _player.Play(strURL);
          if (!isPlaybackPossible)
          {
            Log.Info("player2:ended");
            _player.SafeDispose();
            _player = null;
            _subs = null;
            GC.Collect();
      var player = newPlayer;
            GC.Collect();
          }
        }
        else if (_player.Playing)
          if (_prevPlayer.GetType() == newPlayer.GetType() && _prevPlayer.SupportsReplay)
          _currentFilePlaying = _player.CurrentFile;
            player = _prevPlayer;
            _prevPlayer = null;
          UnableToPlay(strURL, MediaType.Unknown);
      finally
      {
        Starting = false;
      }
    }

    private static IPlayer CachePreviousPlayer(IPlayer newPlayer)
    {
      IPlayer player = newPlayer;
      if (newPlayer != null)
      {
        if (_prevPlayer != null)
        {
          if (_prevPlayer.GetType() == newPlayer.GetType())
          {
            if (_prevPlayer.SupportsReplay)
            {
              player = _prevPlayer;
              _prevPlayer = null;
            }
          }
        }
        if (_prevPlayer != null)
        {
          _prevPlayer.SafeDispose();
          _prevPlayer = null;
        }
      }
      return player;
    }

    public static bool Play(string strFile)
    {
        }

        var s = Path.GetExtension(strFile);
        if (s != null)
        {
          var extension = s.ToLower();
          var isImageFile = VirtualDirectory.IsImageFile(extension);
          if (isImageFile && (!File.Exists(DaemonTools.GetVirtualDrive() + @"\VIDEO_TS\VIDEO_TS.IFO") &&
                              !File.Exists(DaemonTools.GetVirtualDrive() + @"\BDMV\index.bdmv")))
          {
            _currentFilePlaying = strFile;
            Ripper.AutoPlay.ExamineCD(DaemonTools.GetVirtualDrive(), true);
            return true;
          }

          if (Util.Utils.IsDVD(strFile))
        string extension = Path.GetExtension(strFile).ToLower();
            ChangeDriveSpeed(strFile, DriveType.CD);
        {
            {
          _mediaInfo = new MediaInfoWrapper(strFile);
          Starting = true;
          if (type == MediaType.Unknown)
          if (Util.Utils.IsVideo(strFile) || Util.Utils.IsLiveTv(strFile)) // video, TV, RTSP
                "g_Player.Play - waited {0}s for refreshrate change, but it never took place (check your config). Proceeding with playback.",
                RefreshRateChanger.WAIT_FOR_REFRESHRATE_RESET_MAX);
              RefreshRateChanger.ResetRefreshRateState();
              Log.Debug("g_Player.Play - Mediatype Unknown, forcing detection as Video");
              type = MediaType.Video;
            else
            // refresh rate change done here.
            RefreshRateChanger.AdaptRefreshRate(strFile, (RefreshRateChanger.MediaType)(int)type);

            if (RefreshRateChanger.RefreshRateChangePending)
              return true;
              var ts = DateTime.Now - RefreshRateChanger.RefreshRateChangeExecutionTime;
              if (ts.TotalSeconds > RefreshRateChanger.WAIT_FOR_REFRESHRATE_RESET_MAX)
              {
                Log.Info(
                  "g_Player.Play - waited {0}s for refreshrate change, but it never took place (check your config). Proceeding with playback.",
                  RefreshRateChanger.WAIT_FOR_REFRESHRATE_RESET_MAX);
                RefreshRateChanger.ResetRefreshRateState();
              }
              else
              {
                return true;
              }
          }
        }
          OnChanged(strFile);
          _currentStep = 0;
          _currentStepIndex = -1;
          _seekTimer = DateTime.MinValue;
          _isInitialized = true;
          _subs = null;

          if (_player != null)
            if (type == MediaType.Unknown)
            GUIGraphicsContext.ShowBackground = true;
            OnChanged(strFile);
            OnStopped();
            var doStop = true;
            if (type != MediaType.Video && Util.Utils.IsAudio(strFile))
              type = MediaType.Music;
              if (type == MediaType.Unknown)
          {
                type = MediaType.Music;
            }
              if (BassMusicPlayer.IsDefaultMusicPlayer && BassMusicPlayer.Player.Playing)
        {
                doStop = !BassMusicPlayer.Player.CrossFadingEnabled;
                  // BluRay image
                  (!bInternal && isImageFile && Util.Utils.IsBDImage(strFile))) // external player used
            if (doStop)
            {
              if (_player != null)
              {
                _player.Stop();
              }
              CachePlayer();
              _player = null;
            }
              {

          Log.Info("g_Player.Play({0} {1})", strFile, type);
          if (!Util.Utils.IsAVStream(strFile) && Util.Utils.IsVideo(strFile) && (!Util.Utils.IsRTSP(strFile) && extension != ".ts"))
          {
            using (Settings xmlreader = new MPSettings())
            {
              var bInternal = xmlreader.GetValueAsBool("movieplayer", "internal", true);
              var bInternalDVD = xmlreader.GetValueAsBool("dvdplayer", "internal", true);

              // External player extension filter
              _externalPlayerExtensions = xmlreader.GetValueAsString("movieplayer", "extensions", "");
              if (!bInternal && !string.IsNullOrEmpty(_externalPlayerExtensions) &&
                  extension != ".ifo" && extension != ".vob" && !Util.Utils.IsDVDImage(strFile) &&
                  !CheckExtension(strFile))
              {
                bInternal = true;
              }

              if ((!bInternalDVD && !isImageFile && (extension == ".ifo" || extension == ".vob")) ||
                  (!bInternalDVD && isImageFile && Util.Utils.IsDVDImage(strFile)) ||
                  // No image and no DVD folder rips
                  (!bInternal && !isImageFile && extension != ".ifo" && extension != ".vob") ||
                  // BluRay image
                  (!bInternal && isImageFile && Util.Utils.IsBDImage(strFile))) // external player used
              {
                if (isImageFile)
                {
                  // Check for DVD ISO
                  strFile = DaemonTools.GetVirtualDrive() + @"\VIDEO_TS\VIDEO_TS.IFO";
                  if (!File.Exists(strFile))
                  {
                    // Check for BluRayISO
                    strFile = DaemonTools.GetVirtualDrive() + (@"\BDMV\index.bdmv");
                    if (!File.Exists(strFile))
                      return false;
                  }
                }
                if (Util.Utils.PlayMovie(strFile))
                {
                  return true;
                }
                else // external player error
                {
                  UnableToPlay(strFile, type);
                  return false;
                }
              }
            }
          }
        }
                {
                  // Check for DVD ISO
                  strFile = Util.DaemonTools.GetVirtualDrive() + @"\VIDEO_TS\VIDEO_TS.IFO";
                  if (!File.Exists(strFile))
                  {
                    // Check for BluRayISO
                    strFile = Util.DaemonTools.GetVirtualDrive() + (@"\BDMV\index.bdmv");
                    if (!File.Exists(strFile))
                      return false;
                  }
                }
                if (Util.Utils.PlayMovie(strFile))
                {
                  return true;
                }
          bool bResult = _player.Play(strFile);
          if (!bResult)
          {
            Log.Info("g_Player: ended");
            _player.SafeDispose();
            _player = null;
            _subs = null;
            UnableToPlay(strFile, type);
          }
          else if (_player.Playing)
          {
            _isInitialized = false;
            _currentFilePlaying = _player.CurrentFile;
            //if (_chapters == null)
            //{
            //  _chapters = _player.Chapters;
            //}
            if (_chaptersname == null)
            {
              _chaptersname = _player.ChaptersName;
            }
            OnStarted();
          }

            LoadChapters(strFile);
          }
          _player = CachePreviousPlayer(_player);
          bool bResult = _player.Play(strFile);
          if (!bResult)
          {
            Log.Info("g_Player: ended");
            _player.SafeDispose();
            _player = null;
            _subs = null;
            UnableToPlay(strFile, type);
          }
          else if (_player.Playing)
          {
      get
      {
        return _player != null && _player.IsExternal;
      }
              _chaptersname = _player.ChaptersName;
            }
            OnStarted();
      get
      {
        return _player != null && (_player.IsRadio);
      }
      UnableToPlay(strFile, type);
      return false;
    }
      get
      {
        return _player != null && _currentMedia == MediaType.Music;
      }
        }
        return _player.IsExternal;
      }
    }

        if (_player == null || _isInitialized)
    {
      get
      {
        var bResult = _player.Playing;
      }
    }

    public static bool IsMusic
    {
      get
      get { return _player == null ? -1 : _player.PlaybackType; }

    public static bool Playing
    {
      get
      {
        return _player != null && _player.Paused;
      }
          return false;
        }
        bool bResult = _player.Playing;
        return bResult;
      }
        if (_player == null || _isInitialized)

    public static int PlaybackType
    {
        var bResult = _player.Stopped;
        }
        return _player.PlaybackType;
      }
    }

    public static bool Paused
      get
      {
        return _player == null ? 1 : _player.Speed;
      }
      }
    }

    public static bool Stopped
    {
      get
      {
        if (_isInitialized)
        {
          return false;
        }
        if (_player == null)
        {
          return false;
      get
      {
        return _player == null ? "" : _player.CurrentFile;
      }
      get
      {
        if (_player == null)
      get { return _player == null ? -1 : _player.Volume; }
        {
          return;
        }
        _player.Speed = value;
        _currentStep = 0;
        _currentStepIndex = -1;
        _seekTimer = DateTime.MinValue;
      }
    }

      get
      {
        return GUIGraphicsContext.ARType;
      }
    {
      get
      {
        if (_player == null)
        {
          return "";
        }
        return _player.CurrentFile;
      }
    }

      get
      {
        return _player == null ? 0 : _player.PositionX;
      }
        return _player.Volume;
      }
      set
      {
        if (_player != null)
        {
          _player.Volume = value;
        }
      }
    }
      get
      {
        return _player == null ? 0 : _player.PositionY;
      }
          _player.ARType = value;
        }
      }
    }

    public static int PositionX
    {
      get
      {
        if (_player == null)
      get
      {
        return _player == null ? 0 : _player.RenderWidth;
      }
        {
          _player.PositionX = value;
        }
      }
    }

    public static int PositionY
    {
      get
      {
      get
      {
        return _player != null && _player.Visible;
      }
        if (_player != null)
        {
          _player.PositionY = value;
        }
      }
    }

    public static int RenderWidth
    {
      get
      get
      {
        return _player == null ? 0 : _player.RenderHeight;
      }
      {
        if (_player != null)
        {
          _player.RenderWidth = value;
        }
      }
    }

    public static bool Visible
    {
      get
      {
        return _player == null ? 0 : _player.Duration;
      }
      set
      {
        if (_player != null)
      get
      {
        return _player == null ? 0 : _player.CurrentPosition;
      }
      get
      {
        if (_player == null)
      get
      {
        return _player == null ? 0 : _player.StreamPosition;
      }
        {
          _player.RenderHeight = value;
        }
      get
      {
        return _player == null ? 0 : _player.ContentStart;
      }
        {
          return 0;
        }
      get
      {
        return _player == null ? GUIGraphicsContext.IsFullScreenVideo : _player.FullScreen;
      }
        if (_player == null)
        {
          return 0;
        }
        return _player.CurrentPosition;
      }
    }

    public static double StreamPosition
      {
        if (_player == null && _chapters == null)
        {
          return null;
        }
        if (_chapters != null)
        {
          return _chapters;
        }
        else
        {
          return _player.Chapters;
        }

      get
      {
        if (_player == null)
        {
          return 0;
        }
        return _player.ContentStart;
      }
    }

    public static bool FullScreen
    {
      get
      {
        if (_player == null)
        {
          _player.FullScreen = value;
        }
      }
    }
      get
      {
        return _player == null ? 0 : _player.Width;
      }
        }
        if (_chapters != null)
        {
      get
      {
        return _player == null ? 0 : _player.Height;
      }

    public static string[] ChaptersName
    {
      get
      {
        if (_player == null)
        {
          return null;
        }
        _chaptersname = _player.ChaptersName;
        return _chaptersname;
      var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
    }
    
    public static double[] JumpPoints
    {
    {
      if (_currentStep != 0 && _player != null && (_currentStep < 0 || (_player.CurrentPosition + 4 < _player.Duration) || !IsTV))
      {
        var dTime = _currentStep + _player.CurrentPosition;
        Log.Debug("g_Player.StepNow() - Preparing to seek to {0}:{1}", _player.CurrentPosition, _player.Duration);
        if (!IsTV && (dTime > _player.Duration))
        {
          dTime = _player.Duration - 5;
        if (IsTV && (dTime + 3 > _player.Duration))
        {
           dTime = _player.Duration - 3; // Margin for live TV
        }
        if (dTime < 0)
        {
          dTime = 0d;
        }
        Log.Debug("g_Player.StepNow() - Preparing to seek to {0}:{1}:{2} isTv {3}", (int) (dTime/3600d),
                  (int) ((dTime%3600d)/60d), (int) (dTime%60d), IsTV);
        _player.SeekAbsolute(dTime);
        Speed = Speed;
        var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
        GUIGraphicsContext.SendMessage(msgUpdate);
      }
    {
      get
      {
        if (_player == null)
        {
          return 0;
        }
    /// <param name="step"></param>
      }
    public static string GetSingleStep(int step)
    {
      if (step >= 0)
      {
        if (step >= 3600)
        {
          if ((Convert.ToSingle(step) / 3600) > 1 && (Convert.ToSingle(step) / 3600) != 2 &&
              (Convert.ToSingle(step) / 3600) != 3)
      _player.SeekRelative(dTime);
            return "+ " + Convert.ToString(step / 60) + " " + GUILocalizeStrings.Get(2998); // "min"
          }
          return "+ " + Convert.ToString(step / 3600) + " " + GUILocalizeStrings.Get(2997); // "hrs"
        }

        if (step >= 60)
    {
          return "+ " + Convert.ToString(step / 60) + " " + GUILocalizeStrings.Get(2998); // "min"
        }
        return "+ " + Convert.ToString(step) + " " + GUILocalizeStrings.Get(2999); // "sec"
      }

      if (step <= -3600)
      {
        if ((Convert.ToSingle(step) / 3600) < -1 && (Convert.ToSingle(step) / 3600) != -2 &&
            (Convert.ToSingle(step) / 3600) != -3)
        {
          return "- " + Convert.ToString(Math.Abs(step / 60)) + " " + GUILocalizeStrings.Get(2998); // "min"
        }
        return "- " + Convert.ToString(Math.Abs(step / 3600)) + " " + GUILocalizeStrings.Get(2997); // "hrs"
      
      if (step <= -60)
          if (IsTV && (dTime + 3 > _player.Duration)) dTime = _player.Duration - 3; // Margin for live Tv
        return "- " + Convert.ToString(Math.Abs(step / 60)) + " " + GUILocalizeStrings.Get(2998); // "min"
      }
      return "- " + Convert.ToString(Math.Abs(step)) + " " + GUILocalizeStrings.Get(2999); // "sec"
    }

      if (Step >= 0)
      {
        if (Step >= 3600)
        {
          // check for 'full' hours
      var timeToStep = _currentStep;
      if (timeToStep == 0)
          {
            return "+ " + Convert.ToString(Step / 60) + " " + GUILocalizeStrings.Get(2998); // "min"
          }
          else
      if (_player.CurrentPosition + timeToStep <= 0)
            return "+ " + Convert.ToString(Step / 3600) + " " + GUILocalizeStrings.Get(2997); // "hrs"
          }
        }
      return _player.CurrentPosition + timeToStep >= _player.Duration ? GUILocalizeStrings.Get(774) : GetSingleStep(_currentStep);
        {
          return "+ " + Convert.ToString(Step) + " " + GUILocalizeStrings.Get(2999); // "sec"
        }
      }
      else // back = negative
      {
        if (Step <= -3600)
        {
          if ((Convert.ToSingle(Step) / 3600) < -1 && (Convert.ToSingle(Step) / 3600) != -2 &&
      var timeToStep = _currentStep;
      if (_player.CurrentPosition + timeToStep <= 0)
            return "- " + Convert.ToString(Math.Abs(Step / 60)) + " " + GUILocalizeStrings.Get(2998); // "min"
          }
          else
      if (_player.CurrentPosition + timeToStep >= _player.Duration)
            return "- " + Convert.ToString(Math.Abs(Step / 3600)) + " " + GUILocalizeStrings.Get(2997); // "hrs"
          }
        }
      return timeToStep;
        {
          return "- " + Convert.ToString(Math.Abs(Step / 60)) + " " + GUILocalizeStrings.Get(2998); // "min"
    public static void SeekStep(bool fastForward)
        else
      if (!ConfigLoaded)
          return "- " + Convert.ToString(Math.Abs(Step)) + " " + GUILocalizeStrings.Get(2999); // "sec"
        }
        Log.Info("g_Player loading seekstep config {0}", "");
    }
      if (fastForward)
    public static string GetStepDescription()
        if (_currentStep < 0)
        {
          _currentStepIndex--; // E.g. from -30 to -15 
          _currentStep = _currentStepIndex == -1 ? 0 : -1 * Convert.ToInt32(_seekStepList[_currentStepIndex]);
        }
      {
        return GUILocalizeStrings.Get(773); // "START"
      }
      if (_player.CurrentPosition + m_iTimeToStep >= _player.Duration)
      {
        return GUILocalizeStrings.Get(774); // "END"
      }
      return GetSingleStep(_currentStep);
    }

    public static int GetSeekStep(out bool bStart, out bool bEnd)
    {
      bStart = false;
      bEnd = false;
      if (_player == null)
      {
        return 0;
      }
      int m_iTimeToStep = (int)_currentStep;
      if (_player.CurrentPosition + m_iTimeToStep <= 0)
      {
        bStart = true; //start
      }
      if (_player.CurrentPosition + m_iTimeToStep >= _player.Duration)
      {
        bEnd = true;
        else
        {
          _currentStepIndex--; // E.g. from 30 to 15
          _currentStep = _currentStepIndex == -1 ? 0 : Convert.ToInt32(_seekStepList[_currentStepIndex]);
        }
      {
        if (_currentStep < 0)
        {
    public static void SeekRelativePercentage(int percentage)
          if (_currentStepIndex == -1)
          {
            _currentStep = 0; // Reached middle, no stepping
          }
          else
      _player.SeekRelativePercentage(percentage);
      var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
          }
        }
        else
        {
          _currentStepIndex++; // E.g. from 15 to 30
          if (_currentStepIndex >= _seekStepList.Count)
          {
            _currentStepIndex--; // Reached maximum step, don't change _currentStep
          }
          else
          {
            _currentStep = Convert.ToInt32(_seekStepList[_currentStepIndex]);
          }
        }
      }
      var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
      {
        if (_currentStep <= 0)
        {
          _currentStepIndex++; // E.g. from -15 to -30
          if (_currentStepIndex >= _seekStepList.Count)
          {
            _currentStepIndex--; // Reached maximum step, don't change _currentStep
          }
          else
          {
            _currentStep = -1 * Convert.ToInt32(_seekStepList[_currentStepIndex]);
          }
        }
      var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
        {
          _currentStepIndex--; // E.g. from 30 to 15
          if (_currentStepIndex == -1)
          {
            _currentStep = 0; // Reached middle, no stepping
          }
          else
          {
      get
      {
        return RefreshRateChanger.RefreshRateChangePending || _player != null && _player.HasVideo;
      }
      }
      _player.SeekRelativePercentage(iPercentage);
      GUIMessage msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
      get
      {
        return RefreshRateChanger.RefreshRateChangePending || _player != null && _player.HasViz;
      }
      Log.Debug("g_Player.SeekAbsolute() - Preparing to seek to {0}:{1}:{2}", (int)(dTime / 3600d),
                (int)((dTime % 3600d) / 60d), (int)(dTime % 60d));
      _player.SeekAbsolute(dTime);
      get
      {
        if (RefreshRateChanger.RefreshRateChangePending && RefreshRateChanger.RefreshRateChangeMediaType == RefreshRateChanger.MediaType.Video)
      _seekTimer = DateTime.MinValue;
    }
        }
        return _player != null && _currentMedia == MediaType.Video;
      }
      _currentStepIndex = -1;
      _seekTimer = DateTime.MinValue;
    }
      get
      {
        return _player != null && _subs != null;
      }
        }
        if (_player == null)
        {
      if (_player == null || _subs == null)
        }
        return _player.HasVideo;
      }
      get
      {
        if (RefreshRateChanger.RefreshRateChangePending)
        {
          return true;
        }
        if (_player == null)
        {
          return false;
        }
        return _player.HasViz;
      }
    }

    public static bool IsVideo
    {
      get
      {
        if (RefreshRateChanger.RefreshRateChangePending &&
            RefreshRateChanger.RefreshRateChangeMediaType == RefreshRateChanger.MediaType.Video)
        {
          return true;
        }
        if (_player == null)
        {
          return false;
        }
        if (_currentMedia == MediaType.Video)
        {
          return true;
        }
        return false;
      }
    }

    public static bool HasSubs
          var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED, 0, 0, 0, 0, 0, null);
      get
      {
        if (_player == null)
        {
          return false;
        }
        return (_subs != null);
      }
    }

    public static void RenderSubtitles()
    {
      if (_player == null)
          var ts = DateTime.Now - _seekTimer;
        return;
      }
        else if (_autoComSkip && JumpPoints != null && _player.Speed == 1)
        {
          double currentPos = _player.CurrentPosition;
          foreach (double jumpFrom in JumpPoints)
          {
            if (jumpFrom != 0 && currentPos <= jumpFrom + 1.0 && currentPos >= jumpFrom - 0.1)
            {
              Log.Debug("g_Player.Process() - Current Position: {0}, JumpPoint: {1}", currentPos, jumpFrom);

              JumpToNextChapter();
              break;
            }
          }
        }
        }
      }
      _player.WndProc(ref m);
    }

    public static void Process()
    public static VideoStreamFormat GetVideoFormat()
    {
      return _player == null ? new VideoStreamFormat() : _player.GetVideoFormat();
    }

    public static eAudioDualMonoMode GetAudioDualMonoMode()
    {
      return _player == null ? eAudioDualMonoMode.UNSUPPORTED : _player.GetAudioDualMonoMode();
    }

    public static bool SetAudioDualMonoMode(eAudioDualMonoMode mode)
    {
      return _player != null && _player.SetAudioDualMonoMode(mode);
    }

          {
            StepNow();
          }
        }
      }
      OnTVChannelChanged();
      _player.OnZapping(info);
              Log.Debug("g_Player.Process() - Current Position: {0}, JumpPoint: {1}", currentPos, jumpFrom);

              JumpToNextChapter();
              break;
            }
          }
        }
      }
      get
      {
        return _player == null ? 0 : _player.EditionStreams;
      }
      else
      {
        return _player.GetVideoFormat();
      }
    }

      get
      {
        return _player == null ? 0 : _player.CurrentEditionStream;
      }
        return _player.GetAudioDualMonoMode();
      }
    }

    public static bool SetAudioDualMonoMode(eAudioDualMonoMode mode)
    {
      if (_player == null)
      {
    public static string EditionLanguage(int value)
      }
      else
      {
        return _player.SetAudioDualMonoMode(mode);
      }
    }
      var stream = _player.EditionLanguage(value);
    public static void OnZapping(int info)
    {
      if (_player == null)
      {
        return;
      }
    public static string EditionType(int value)
      {
        OnTVChannelChanged();
        _player.OnZapping(info);
        return;
      }
    }
      var stream = _player.EditionType(value);
    #region Edition selection

    /// <summary>
    /// Property which returns the total number of edition streams available
    /// </summary>
    public static int EditionStreams
    {
      get
      {
        if (_player == null)
        {
          return 0;
      get
      {
        return _player == null ? 0 : _player.VideoStreams;
      }
    public static int CurrentEditionStream
    {
      get
      {
        if (_player == null)
        {
      get
      {
        return _player == null ? 0 : _player.CurrentVideoStream;
      }
          _player.CurrentEditionStream = value;
        }
      }
    }

    public static string EditionLanguage(int iStream)
    {
      if (_player == null)
    public static string VideoLanguage(int value)
        return Strings.Unknown;
      }

      string stream = _player.EditionLanguage(iStream);
      return Util.Utils.TranslateLanguageString(stream);
    }
      var stream = _player.VideoLanguage(value);
    /// <summary>
    /// Property to get the type of an edition stream
    /// </summary>
    public static string EditionType(int iStream)
    {
      if (_player == null)
    public static string VideoType(int value)
        return Strings.Unknown;
      }

      string stream = _player.EditionType(iStream);
      return stream;
    }
      var stream = _player.VideoType(value);
    #endregion

    #region Video selection

    /// <summary>
    /// Property which returns the total number of video streams available
    /// </summary>
    public static int VideoStreams
    /// Property which returns true if the player is able to perform post processing features
      get
      {
        if (_player == null)
      get
      {
        return _player != null && _player.HasPostprocessing;
      }
    /// Property to get/set the current video stream
    /// </summary>
    public static int CurrentVideoStream
    {
      get
      {
        if (_player == null)
        {
          return 0;
        }
      get
      {
        return _player == null ? 0 : _player.AudioStreams;
      }
      }
    }

    public static string VideoLanguage(int iStream)
    {
      if (_player == null)
      get
      {
        return _player == null ? 0 : _player.CurrentAudioStream;
      }
    /// <summary>
    /// Property to get the type of an edition stream
    /// </summary>
    public static string VideoType(int iStream)
    {
      if (_player == null)
      {
        return Strings.Unknown;
      }

      string stream = _player.VideoType(iStream);
    public static string AudioLanguage(int value)
    }

    #endregion

    #region Postprocessing selection

      var stream = _player.AudioLanguage(value);
    /// Property which returns true if the player is able to perform postprocessing features
    /// </summary>
    public static bool HasPostprocessing
    {
      get
      {
    public static string AudioType(int value)
        {
          return false;
        }
        return _player.HasPostprocessing;
      }
    }
      var stream = _player.AudioType(value);
    #endregion

    #region subtitle/audio stream selection

    /// <summary>
    /// Property which returns the total number of audio streams available
    /// </summary>
    public static int AudioStreams
      get
      {
        return _player == null ? 0 : _player.SubtitleStreams;
      }
      }
    }

    /// <summary>
    /// Property to get/set the current audio stream
    /// </summary>
      get
      {
        return _player == null ? 0 : _player.CurrentSubtitleStream;
      }
        return _player.CurrentAudioStream;
      }
      set
      {
        if (_player != null)
        {
          _player.CurrentAudioStream = value;
        }
      }
    }

    public static string SubtitleLanguage(int value)
    /// Property to get the name for an audio stream
    /// </summary>
    public static string AudioLanguage(int iStream)
    {
      if (_player == null)
      {
      var stream = _player.SubtitleLanguage(value);
      }
    }

    /// <summary>
    /// Retrieves the name of the subtitle stream
    /// </summary>
    /// <param name="value"> </param>
    /// <returns>Name of the track</returns>
    public static string SubtitleName(int value)
    public static string AudioType(int iStream)
    {
      if (_player == null)
      {
        return Strings.Unknown;
      }
      var stream = _player.SubtitleName(value);
      string stream = _player.AudioType(iStream);
      return stream;
    }

    /// <summary>
    /// Property to get the total number of subtitle streams
    /// </summary>
      get
      {
        return _player != null && _player.EnableSubtitle;
      }
        return _player.SubtitleStreams;
      }
    }

    /// <summary>
    /// Property to get/set the current subtitle stream
    /// </summary>
    public static int CurrentSubtitleStream
    {
      get
      {
      get
      {
        return _player != null && _player.EnableForcedSubtitle;
      }
        if (_player != null)
        {
          _player.CurrentSubtitleStream = value;
        }
      }
    }

    /// <summary>
    /// Property to get/set the name for a subtitle stream
    /// </summary>
    public static string SubtitleLanguage(int iStream)
    {
      if (_player == null)
      {
        {
          var dvdPlayer = _player as DVDPlayer;
          if (dvdPlayer == null)
          {
            var tsReaderPlayer = _player as TSReaderPlayer;
            return tsReaderPlayer != null && tsReaderPlayer.SupportsCC;
          }
          return dvdPlayer.SupportsCC;
    /// </summary>
    /// <param name="iStream">Index of the stream</param>
    /// <returns>Name of the track</returns>
    public static string SubtitleName(int iStream)
    {
      if (_player == null)
      {
        return null;
      }

      string stream = _player.SubtitleName(iStream);
      return Util.Utils.TranslateLanguageString(stream);
    }

    #endregion
      GUIGraphicsContext.OnVideoWindowChanged += OnVideoWindowChanged;
      GUIGraphicsContext.OnGammaContrastBrightnessChanged += OnGammaContrastBrightnessChanged;
      get
      {
        if (_player == null)
        {
      if (!Playing || !HasVideo ||_player == null)
        }
        return _player.EnableSubtitle;
      }
    }

    public static bool EnableForcedSubtitle
    {
      get
    private static void OnVideoWindowChanged()
    {
      if (Playing && (HasVideo || HasViz))
      {
        FullScreen = GUIGraphicsContext.IsFullScreenVideo;
        ARType = GUIGraphicsContext.ARType;
        if (!FullScreen)
        {
          PositionX = GUIGraphicsContext.VideoWindow.Left;
          PositionY = GUIGraphicsContext.VideoWindow.Top;
          RenderWidth = GUIGraphicsContext.VideoWindow.Width;
          RenderHeight = GUIGraphicsContext.VideoWindow.Height;
        }
        var inTV = false;
        var windowId = GUIWindowManager.ActiveWindow;
        if (windowId == (int)GUIWindow.Window.WINDOW_TV ||
            windowId == (int)GUIWindow.Window.WINDOW_TVGUIDE ||
            windowId == (int)GUIWindow.Window.WINDOW_SEARCHTV ||
            windowId == (int)GUIWindow.Window.WINDOW_SCHEDULER ||
            windowId == (int)GUIWindow.Window.WINDOW_RECORDEDTV)
        {
          inTV = true;
        }
        Visible = (FullScreen || GUIGraphicsContext.Overlay || windowId == (int) GUIWindow.Window.WINDOW_SCHEDULER || inTV);
        SetVideoWindow();
      }
    }

    }

    public static void SetVideoWindow()
    {
      get
      {
        return _player == null ? new Rectangle(0, 0, 0, 0) : _player.VideoWindow;
      }
    {
      GUIGraphicsContext.OnVideoWindowChanged += new VideoWindowChangedHandler(OnVideoWindowChanged);
      GUIGraphicsContext.OnGammaContrastBrightnessChanged +=
        new VideoGammaContrastBrightnessHandler(OnGammaContrastBrightnessChanged);
    }

      get
      {
        return _player == null ? new Rectangle(0, 0, 0, 0) : _player.SourceWindow;
      }
        return;
    public static int GetHDC()
    {
      return _player == null ? 0 : _player.GetHDC();
    }

    public static void ReleaseHDC(int hdc)
      if (_player == null)
      {
        return;
      {
        return;
      _player.ReleaseHDC(hdc);
      if (!HasVideo && !HasViz)
      {
        return;
      }
      get
      {
        return _player != null && (_player.CanSeek() && !_player.IsDVDMenu);
      }
      }
      bool inTV = false;
      int windowId = GUIWindowManager.ActiveWindow;
      get
      {
        return _mediaInfo;
      }
          windowId == (int)GUIWindow.Window.WINDOW_TVGUIDE ||
          windowId == (int)GUIWindow.Window.WINDOW_SEARCHTV ||
          windowId == (int)GUIWindow.Window.WINDOW_SCHEDULER ||
          windowId == (int)GUIWindow.Window.WINDOW_RECORDEDTV)
      {
        inTV = true;
      }
      Visible = (FullScreen || GUIGraphicsContext.Overlay ||
                 windowId == (int)GUIWindow.Window.WINDOW_SCHEDULER || inTV);
      SetVideoWindow();
    }

    /// <summary>
        var streams = _player.AudioStreams;
        var current = _player.CurrentAudioStream;
        var next = current;
        var success = false;
      get
      {
        if (_player == null)
        {
          return new Rectangle(0, 0, 0, 0);
        }
        return _player.VideoWindow;
      }
    }

    /// <summary>
    /// returns video source rectangle displayed
    /// </summary>
    public static Rectangle SourceWindow
    {
      get
      {
        if (_player == null)
        {
          return new Rectangle(0, 0, 0, 0);
          Log.Info("g_Player: Failed to switch to next audio stream.");
        return _player.SourceWindow;
      }
    }

    public static int GetHDC()
    {
      if (_player == null)
      {
        return 0;
    public static void SwitchToNextSubtitle()
    {
      if (SupportsCC)
      {
        if (EnableSubtitle)
        {
          if (CurrentSubtitleStream == -1)
          {
            EnableSubtitle = false;
          }
          else
          {
            if (CurrentSubtitleStream < SubtitleStreams - 1)
            {
              CurrentSubtitleStream++;
            }
            else
            {
              EnableSubtitle = false;
              CurrentSubtitleStream = -1;
            }
          }
        }
        else if (!EnableSubtitle && CurrentSubtitleStream == SubtitleStreams)
        {
          CurrentSubtitleStream = -1;
        }
        else
        {
          CurrentSubtitleStream = 0;
          EnableSubtitle = true;
        }
      }
      else
      {
        if (EnableSubtitle)
        {
          if (CurrentSubtitleStream < SubtitleStreams - 1)
          {
            CurrentSubtitleStream++;
          }
          else
          {
            EnableSubtitle = false;
          }
        }
        else
        {
          CurrentSubtitleStream = 0;
          EnableSubtitle = true;
        }
      }
    }

        {
          // if next stream is greater then the amount of stream
          // take first
          if (++next >= streams)
          {
            next = 0;
          }
          // set the next stream
          _player.CurrentAudioStream = next;
          // if the stream is set in, stop the loop
        var streams = _player.EditionStreams;
        var current = _player.CurrentEditionStream;
        var next = current;
        var success = false;
        } while ((next != current) && (success == false));
        if (success == false)
        {
          Log.Info("g_Player: Failed to switch to next audiostream.");
        }
      }
    }

    //}

    /// <summary>
    /// Switches to the next subtitle stream
    /// </summary>
    public static void SwitchToNextSubtitle()
    {
      if (!SupportsCC)
      {
        if (EnableSubtitle)
        {
          if (CurrentSubtitleStream < SubtitleStreams - 1)
          {
            CurrentSubtitleStream++;
          }
          else
          {
            EnableSubtitle = false;
          }
        }
        else
        {
          CurrentSubtitleStream = 0;
          EnableSubtitle = true;
        }
      }
      else if (EnableSubtitle && SupportsCC)
      {
        var streams = _player.VideoStreams;
        var current = _player.CurrentVideoStream;
        var next = current;
        var success = false;
        else if (CurrentSubtitleStream < SubtitleStreams - 1)
        {
          CurrentSubtitleStream++;
        }
        else if (SupportsCC)
        {
          EnableSubtitle = false;
          CurrentSubtitleStream = -1;
        }
      }
      else if (!EnableSubtitle && CurrentSubtitleStream == SubtitleStreams && SupportsCC)
      {
        CurrentSubtitleStream = -1;
      }
      else
      {
        CurrentSubtitleStream = 0;
        EnableSubtitle = true;
      }
    }
          Log.Info("g_Player: Failed to switch to next video stream.");
    /// <summary>
    /// Switches to the next edition stream.
    /// 
    /// Calls are directly pushed to the embedded player. And care 
    /// is taken not to do multiple calls to the player.
    /// </summary>
    public static void SwitchToNextEdition()
    {
      if (_player != null)
      {
      catch (IOException exp)
        int streams = _player.EditionStreams;
        int current = _player.CurrentEditionStream;
        int next = current;
        bool success = false;
        // Loop over the stream, so we skip the disabled streams
        // stops if the loop is over the current stream again.
        do
        {
          // if next stream is greater then the amount of stream
          // take first
          if (++next >= streams)
          {
            next = 0;
          }
          // set the next stream
          _player.CurrentEditionStream = next;
          // if the stream is set in, stop the loop
          if (next == _player.CurrentEditionStream)
          {
            success = true;
    private static void LoadChapters(string videoFile)
        } while ((next != current) && (success == false));
        if (success == false)
        {
          Log.Info("g_Player: Failed to switch to next editionstream.");
      var chapterFile = Path.ChangeExtension(videoFile, ".txt");
      }
      {
        return;
    /// <summary>
    /// Switches to the next video stream.
      using (TextReader chapters = new StreamReader(chapterFile))
      {
        var result = LoadChapters(chapters);
        if (!result)
        {
          Log.Info("g_Player.LoadChapters() - Could not read chapters from file \"{0}\"", videoFile);
        }
      }
    {
      if (_player != null)
      {
        // take current stream and number of
        int streams = _player.VideoStreams;
        int current = _player.CurrentVideoStream;
        int next = current;
        bool success = false;
        // Loop over the stream, so we skip the disabled streams
        // stops if the loop is over the current stream again.
        do
        {
          // if next stream is greater then the amount of stream
          // take first
          if (++next >= streams)
          {
            next = 0;
          }
          // set the next stream
          _player.CurrentVideoStream = next;
        var chapters = new ArrayList();
        var jumps = new ArrayList();
          {
        var line = chaptersReader.ReadLine();
          }
        } while ((next != current) && (success == false));
        if (success == false)
        {
          Log.Info("g_Player: Failed to switch to next Videostream.");
        }
      }
    }
        var framesPerSecond = fps / 100.0;

      try
      {
        using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None)) {}
      }
      catch (System.IO.IOException exp)
      {
          var tokens = line.Split(new[] {'\t'});
        return true;
      }
      return false;
          }

          int time;
    public static bool LoadChaptersFromString(string chapters)
    {
      if (String.IsNullOrEmpty(chapters))
      {
        return false;
      }
      Log.Debug("g_Player.LoadChaptersFromString() - Chapters provided externally");
      using (TextReader stream = new StringReader(chapters))
      {
        return LoadChapters(stream);
      }
    }

    private static bool LoadChapters(string videoFile)
    {
      _chapters = null;
      _jumpPoints = null;

      string chapterFile = Path.ChangeExtension(videoFile, ".txt");
      if (!File.Exists(chapterFile) || IsFileUsedbyAnotherProcess(chapterFile))
      {
        return false;
      }

      Log.Debug("g_Player.LoadChapters() - Chapter file found for video \"{0}\"", videoFile);

      using (TextReader chapters = new StreamReader(chapterFile))
      {
        return LoadChapters(chapters);
      }
    }

    private static bool LoadChapters(TextReader chaptersReader)
        for (int index = 0; index < Chapters.Length; index++)
        {
          if (currentPos < Chapters[index])
          {
            return Chapters[index];
          }
        }
          return t;
        }
          {
            _autoComSkip = xmlreader.GetValueAsBool("comskip", "automaticskip", false);
          }

          Log.Debug("g_Player.LoadChapters() - Automatic ComSkip mode is {0}", _autoComSkip ? "on." : "off.");

          _loadAutoComSkipSetting = false;
      if (_chapters != null)

        for (var index = _chapters.Length - 1; index >= 0; index--)
        ArrayList jumps = new ArrayList();
          if (_chapters[index] < currentPos - 5.0)
        string line = chaptersReader.ReadLine();
            return _chapters[index];
        int fps;
        if (String.IsNullOrEmpty(line) || !int.TryParse(line.Substring(line.LastIndexOf(' ') + 1), out fps))
        {
          Log.Warn("g_Player.LoadChapters() - Invalid chapter file");
          return false;
        }

        double framesPerSecond = fps / 100.0;
        int time;

        while ((line = chaptersReader.ReadLine()) != null)
        {
          if (String.IsNullOrEmpty(line))
          {
            continue;
          }
      var nextChapter = NextChapterTime(_player.CurrentPosition);
          string[] tokens = line.Split(new char[] {'\t'});
          if (tokens.Length != 2)
          {
            continue;
          }

          if (int.TryParse(tokens[0], NumberStyles.Float, NumberFormatInfo.InvariantInfo, out time))
          {
            jumps.Add(time / framesPerSecond);
          }

          if (int.TryParse(tokens[1], NumberStyles.Float, NumberFormatInfo.InvariantInfo, out time))
          {
            chapters.Add(time / framesPerSecond);
          }
        }

        if (chapters.Count == 0)
        {
      var prevChapter = PreviousChapterTime(_player.CurrentPosition);
          return false;
        }
        _chapters = new double[chapters.Count];
        chapters.CopyTo(_chapters);

        if (jumps.Count > 0)
        {
          _jumpPoints = new double[jumps.Count];
          jumps.CopyTo(_jumpPoints);
        }

        return true;
    public static void UnableToPlay(string fileName, MediaType type)
      catch (Exception ex)
      {
        Log.Error("g_Player.LoadChapters() - {0}", ex.ToString());
        return false;
        {
          _mediaInfo = new MediaInfoWrapper(fileName);
        }
    }
        var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_CODEC_MISSING, 0, 0, 0, 0, 0, null)
                    {
                      Label = string.Format("{0}: {1}", GUILocalizeStrings.Get(1451), Path.GetFileName(fileName)),
                      Label2 = string.IsNullOrEmpty(_mediaInfo.VideoCodec)
                                 ? string.Empty
                                 : string.Format("Video codec: {0}", _mediaInfo.VideoCodec),
                      Label3 = string.IsNullOrEmpty(_mediaInfo.AudioCodec)
                                 ? string.Empty
                                 : string.Format("Audio codec: {0}", _mediaInfo.AudioCodec)
                    };
          {
            return Chapters[index];
          }
        }
        Log.Error("g_player: Error notifying user about unsuccessful playback of {0} - {1}", fileName, ex.ToString());

      return -1; // no skip
    }

    private static double PreviousChapterTime(double currentPos)
    {
      var extensions = _externalPlayerExtensions.Split(splitter);

      return extensions.Any(extension => extension.Trim().Equals(Path.GetExtension(filename), StringComparison.OrdinalIgnoreCase));

      return 0;
    }

    public static bool JumpToNextChapter()
    {
      if (!Playing)
      {
        return false;
      }

      double nextChapter = NextChapterTime(_player.CurrentPosition);
      Log.Debug("g_Player.JumpNextChapter() - Current Position: {0}, Next Chapter: {1}", _player.CurrentPosition,
                nextChapter);

      if (nextChapter > 0 && nextChapter < _player.Duration)
      {
        SeekAbsolute(nextChapter);
      get
      {
        return _showFullScreenWindowTV;
      }
      }

      return false;
    }

    public static bool JumpToPrevChapter()
    {
      if (!Playing)
      {
        return false;
      }

      double prevChapter = PreviousChapterTime(_player.CurrentPosition);
      Log.Debug("g_Player.JumpPrevChapter() - Current Position: {0}, Previous Chapter: {1}", _player.CurrentPosition,
      get
      {
        return _showFullScreenWindowVideo;
      }

      if (prevChapter >= 0 && prevChapter < _player.Duration)
      {
        SeekAbsolute(prevChapter);
        return true;
      }

      return false;
    }

    public static void UnableToPlay(string FileName, MediaType type)
    {
      try
      {
      get
      {
        return _showFullScreenWindowOther;
      }
          _mediaInfo = new MediaInfoWrapper(FileName);

        GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_CODEC_MISSING, 0, 0, 0, 0, 0, null);
        msg.Label = string.Format("{0}: {1}", GUILocalizeStrings.Get(1451), Path.GetFileName(FileName));
        msg.Label2 = string.IsNullOrEmpty(_mediaInfo.VideoCodec)
                       ? string.Empty
                       : string.Format("Video codec: {0}", _mediaInfo.VideoCodec);
        msg.Label3 = string.IsNullOrEmpty(_mediaInfo.AudioCodec)
                       ? string.Empty
                       : string.Format("Audio codec: {0}", _mediaInfo.AudioCodec);
        GUIGraphicsContext.SendMessage(msg);
        _mediaInfo = null;
      }
      catch (Exception ex)
      {
        Log.Error("g_player: Error notifying user about unsuccessful playback of {0} - {1}", FileName, ex.ToString());
      }
    }
        // close e.g. the TV guide dialog (opened from context menu)
        var actionCloseDialog = new Action(Action.ActionType.ACTION_CLOSE_DIALOG, 0, 0);
    {
      char[] splitter = { ';' };
      string[] extensions = _externalPlayerExtensions.Split(splitter);

      foreach (string extension in extensions)
      {
        if (extension.Trim().Equals(Path.GetExtension(filename),StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }
      return false;
    }

    #endregion

    #region FullScreenWindow

    public delegate bool ShowFullScreenWindowHandler();

    private static ShowFullScreenWindowHandler _showFullScreenWindowTV = ShowFullScreenWindowTVDefault;
    private static ShowFullScreenWindowHandler _showFullScreenWindowVideo = ShowFullScreenWindowVideoDefault;
    private static ShowFullScreenWindowHandler _showFullScreenWindowOther = ShowFullScreenWindowOtherDefault;

    /// <summary>
    /// This handler gets called by ShowFullScreenWindow.
    /// It should handle Fullscreen-TV.
    /// By default, it is set to ShowFullScreenWindowTVDefault.
    /// </summary>
        // When we don't have any Visualization, switch to Now Playing, instead of showing a black screen
    {
      get { return _showFullScreenWindowTV; }
      set
      {
        _showFullScreenWindowTV = value;
        Log.Debug("g_player: Setting ShowFullScreenWindowTV to {0}", value);
      }
          Log.Info("g_Player: ShowFullScreenWindow: No Visualization defined. Switching to Now Playing");

    /// <summary>
    /// This handler gets called by ShowFullScreenWindow.
    /// It should handle general Fullscreen-Video.
    /// By default, it is set to ShowFullScreenWindowVideoDefault.
    /// </summary>
    public static ShowFullScreenWindowHandler ShowFullScreenWindowVideo
    {
      get { return _showFullScreenWindowVideo; }
      set
      {
        _showFullScreenWindowVideo = value;
        Log.Debug("g_player: Setting ShowFullScreenWindowVideo to {0}", value);
      }
    }

    /// <summary>
    /// This handler gets called by ShowFullScreenOther.
    /// It should handle general Fullscreen.
    /// By default, it is set to ShowFullScreenWindowOtherDefault.
    /// </summary>
    public static ShowFullScreenWindowHandler ShowFullScreenWindowOther
    {
      get { return _showFullScreenWindowOther; }
      set
      {
        _showFullScreenWindowOther = value;
        Log.Debug("g_player: Setting ShowFullScreenWindowOther to {0}", value);
      }
    }

    /// <summary>
    /// The default handler does only work if a player is active.
    /// However, GUITVHome and TvPlugin.TVHOME, both set their
    /// own hook to enable fullscreen tv for non-player live
    /// TV.
    /// </summary>
    /// <returns></returns>
    public static bool ShowFullScreenWindowTVDefault()
    {
      if (Playing && IsTV && !IsTVRecording)
      {
        // close e.g. the tv guide dialog (opened from context menu)
        Action actionCloseDialog = new Action(Action.ActionType.ACTION_CLOSE_DIALOG, 0, 0);
        GUIGraphicsContext.OnAction(actionCloseDialog);

        // watching TV
        if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_TVFULLSCREEN)
        {
          return true;
      var win = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
        Log.Info("g_Player: ShowFullScreenWindow switching to fullscreen tv");
        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_TVFULLSCREEN);
        GUIGraphicsContext.IsFullScreenVideo = true;
        return true;
      }
      return false;
    }

    public static bool ShowFullScreenWindowVideoDefault()
    {
      if (!HasVideo && !IsMusic)
      {
        return false;
      }
      // are we playing music and got the fancy BassMusicPlayer?
      if (IsMusic && BassMusicPlayer.IsDefaultMusicPlayer)
      {
        if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_MUSIC)
        {
          return true;
        }

        // When we don't have any Visualisation, switch to Now Playing, instead of showing a black screen
        if (BassMusicPlayer.Player.IVizManager.CurrentVisualizationType == VisualizationInfo.PluginType.None)
        {
          if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_MUSIC_PLAYING_NOW)
          {
            return true;
          }

          Log.Info("g_Player: ShowFullScreenWindow: No Visualisation defined. Switching to Now Playing");
          GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_MUSIC_PLAYING_NOW);
          BassMusicPlayer.Player.VisualizationWindow.Size = new Size(1, 1); // Hide the Vis Window
          return true;
        }

        Log.Info("g_Player: ShowFullScreenWindow switching to fullscreen music");
        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_FULLSCREEN_MUSIC);
      }
      else
      {
        if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO)
        {
          return true;
        }
        Log.Info("g_Player: ShowFullScreenWindow switching to fullscreen video");
        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO);
      }
      GUIGraphicsContext.IsFullScreenVideo = true;
      return true;
    }

    public static bool ShowFullScreenWindowOtherDefault()
    {
      return false;
    }

    /// <summary>
    /// This function opens a fullscreen window for the current
    /// player. It returns whether a fullscreen window could
    /// be opened.
    /// 
    /// It tries the three handlers in this order:
    ///  - ShowFullScreenWindowTV
    ///  - ShowFullScreenWindowVideo
    ///  - ShowFullScreenWindowOther
    /// 
    /// The idea is to have a central location for deciding what window to
    /// open for fullscreen.
    /// </summary>
    /// <returns></returns>
    public static bool ShowFullScreenWindow()
    {
      Log.Debug("g_Player: ShowFullScreenWindow");

      if (RefreshRateChanger.RefreshRateChangePending)
      {
        RefreshRateChanger.RefreshRateChangeFullscreenVideo = true;
        return true;
      }
      // does window allow switch to fullscreen?
      GUIWindow win = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
      if (!win.FullScreenVideoAllowed)
      {
        Log.Error("g_Player: ShowFullScreenWindow not allowed by current window");
        return false;
      }
      // try TV
      if (_showFullScreenWindowTV != null && _showFullScreenWindowTV())
      {
        return true;
      }
      // try Video
      if (_showFullScreenWindowVideo != null && _showFullScreenWindowVideo())
      {
        return true;
      }
      // try Other
      if (_showFullScreenWindowOther != null && _showFullScreenWindowOther())
      {
        return true;
      }

      Log.Debug("g_Player: ShowFullScreenWindow cannot switch to fullscreen");
      return false;
    }

    #endregion

    #region private members

    #endregion
  }
}