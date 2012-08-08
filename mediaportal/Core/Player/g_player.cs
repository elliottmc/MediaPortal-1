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

using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using MediaPortal.ExtensionMethods;
using MediaPortal.GUI.Library;
using MediaPortal.Playlists;
using MediaPortal.Profile;
using MediaPortal.Subtitle;
using MediaPortal.Util;
using MediaPortal.Visualization;
using Un4seen.Bass.AddOn.Cd;
using Action = MediaPortal.GUI.Library.Action;

namespace MediaPortal.Player
{
  // ReSharper disable InconsistentNaming
  public class g_Player
  // ReSharper restore InconsistentNaming
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
    private static int _currentStepIndex = -1;
    private static DateTime _seekTimer = DateTime.MinValue;
    private static IPlayer _player;
    private static IPlayer _prevPlayer;
    private static SubTitles _subs;
    private static bool _isInitialized;
    private static string _currentFilePlaying = "";
    private static MediaType _currentMedia;
    private static IPlayerFactory _factory;
    public static bool Starting = false;
    private static ArrayList _seekStepList = new ArrayList();
    private static int _seekStepTimeout;
    public static bool ConfigLoaded = false;
    private static string[] _driveSpeedCD;
    private static string[] _driveSpeedDVD;
    private static string[] _disableCDSpeed;
    private static string[] _disableDVDSpeed;
    private static int _driveCount;
    private static string _driveLetters;
    private static bool _driveSpeedLoaded;
    private static bool _driveSpeedReduced;
    private static bool _driveSpeedControlEnabled;
    private static string _currentTitle = ""; //actual program metadata - useful for TV - avoids extra DB lookups

    private static string _currentDescription = "";
    //actual program metadata - useful for TV - avoids extra DB Lookups. 

    private static string _currentFileName = ""; //holds the actual file being played. Useful for rtsp streams. 
    private static double[] _chapters;
    private static string[] _chaptersname;
    private static double[] _jumpPoints;
    private static bool _autoComSkip;
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
    private g_Player()
    {
      _factory = new PlayerFactory();
    }

    static g_Player() { }

    public static IPlayer Player
    {
      get
      {
        return _player;
      }
    }

    public static IPlayerFactory Factory
    {
      get
      {
        return _factory;
      }
      set
      {
        _factory = value;
      }
    }

    public static string CurrentTitle
    {
      get
      {
        return _currentTitle;
      }
      set
      {
        _currentTitle = value;
      }
    }

    public static string CurrentFileName
    {
      get
      {
        return _currentFileName;
      }
      set
      {
        _currentFileName = value;
      }
    }

    public static string CurrentDescription
    {
      get
      {
        return _currentDescription;
      }
      set
      {
        _currentDescription = value;
      }
    }

    #endregion

    #region Serialisation

    /// <summary>
    /// Retrieve the CD/DVD Speed set in the config file
    /// </summary>
    public static void LoadDriveSpeed()
    {
      string speedTableCD;
      string speedTableDVD;
      string disableCD;
      string disableDVD;
      using (Settings xmlreader = new MPSettings())
      {
        speedTableCD = xmlreader.GetValueAsString("cdspeed", "drivespeedCD", string.Empty);
        disableCD = xmlreader.GetValueAsString("cdspeed", "disableCD", string.Empty);
        speedTableDVD = xmlreader.GetValueAsString("cdspeed", "drivespeedDVD", string.Empty);
        disableDVD = xmlreader.GetValueAsString("cdspeed", "disableDVD", string.Empty);
        _driveSpeedControlEnabled = xmlreader.GetValueAsBool("cdspeed", "enabled", false);
      }
      if (!_driveSpeedControlEnabled)
      {
        return;
      }
      // if BASS is not the default audio engine, we need to load the CD Plugin first
      if (!BassMusicPlayer.IsDefaultMusicPlayer)
      {
        // Load the CD Plugin
        BassRegistration.BassRegistration.Register();
      }
      // Get the number of CD/DVD drives
      _driveCount = BassCd.BASS_CD_GetDriveCount();
      var builderDriveLetter = new StringBuilder();
      // Get Drive letters assigned
      for (var i = 0; i < _driveCount; i++)
      {
        builderDriveLetter.Append(BassCd.BASS_CD_GetInfo(i).DriveLetter);
      }
      _driveLetters = builderDriveLetter.ToString();
      if (speedTableCD == string.Empty || speedTableDVD == string.Empty)
      {
        var cdinfo = new BASS_CD_INFO();
        var builder = new StringBuilder();
        var builderDisable = new StringBuilder();
        for (var i = 0; i < _driveCount; i++)
        {
          if (builder.Length != 0)
          {
            builder.Append(",");
          }
          if (builderDisable.Length != 0)
          {
            builderDisable.Append(", ");
          }
          BassCd.BASS_CD_GetInfo(i, cdinfo);
          var maxspeed = (int)(cdinfo.maxspeed / 176.4);
          builder.Append(Convert.ToInt32(maxspeed).ToString(CultureInfo.InvariantCulture));
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
      _disableDVDSpeed = disableDVD.Split(',');
      _driveSpeedLoaded = true;
      BassMusicPlayer.ReleaseCDDrives();
    }

    /// <summary>
    /// Read the configuration file to get the skip steps
    /// </summary>
    public static ArrayList LoadSettings()
    {
      var stepArray = new ArrayList();
      using (Settings xmlreader = new MPSettings())
      {
        var strFromXml = xmlreader.GetValueAsString("movieplayer", "skipsteps", "15,30,60,180,300,600,900,1800,3600,7200");
        if (strFromXml == string.Empty) // config after wizard run 1st
        {
          strFromXml = "15,30,60,180,300,600,900,1800,3600,7200";
          Log.Info("g_player - creating new Skip-Settings {0}", "");
        }
        else if (OldStyle(strFromXml))
        {
          strFromXml = ConvertToNewStyle(strFromXml);
        }
        foreach (var token in strFromXml.Split(new[] { ',', ';', ' ' }).Where(token => token != string.Empty))
        {
          stepArray.Add(Convert.ToInt32(token));
        }
        _seekStepList = stepArray;
        var timeout = (xmlreader.GetValueAsString("movieplayer", "skipsteptimeout", "1500"));
        _seekStepTimeout = timeout == string.Empty ? 1500 : Convert.ToInt16(timeout);
      }
      ConfigLoaded = true;
      return stepArray; // Sorted list of step times
    }

    private static bool OldStyle(string strSteps)
    {
      var count = 0;
      var foundOtherThanZeroOrOne = false;
      foreach (var curInt in from token in strSteps.Split(new[] { ',', ';', ' ' }) where token != string.Empty select Convert.ToInt16(token))
      {
        if (curInt != 0 && curInt != 1)
        {
          foundOtherThanZeroOrOne = true;
        }
        count++;
      }
      return (count == 16 && !foundOtherThanZeroOrOne);
    }

    private static string ConvertToNewStyle(string strSteps)
    {
      var count = 0;
      var newStyle = string.Empty;
      foreach (var token in strSteps.Split(new[] { ',', ';', ' ' }))
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
              newStyle += "7200,";
              break;
            case 16:
              newStyle += "10800,";
              break;
          }
        }
      }
      return (newStyle == string.Empty ? string.Empty : newStyle.Substring(0, newStyle.Length - 1));
    }

    /// <summary>
    /// Changes the speed of a drive to the value set in configuration
    /// </summary>
    /// <param name="strFile"></param>
    /// <param name="drivetype"> </param>
    private static void ChangeDriveSpeed(string strFile, DriveType drivetype)
    {
      if (!_driveSpeedLoaded)
      {
        LoadDriveSpeed();
      }
      if (!_driveSpeedControlEnabled)
      {
        return;
      }
      try
      {
        // is the DVD inserted in a Drive for which we need to control the speed
        var rootPath = Path.GetPathRoot(strFile);
        if (rootPath != null)
        {
          if (rootPath.Length > 1)
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
      }
      catch (Exception) { }
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
        AudioTracksReady();
      }
      else
      {
        CurrentAudioStream = 0;
      }
    }

    // called when current playing file is stopped
    private static void OnChanged(string newFile)
    {
      if (string.IsNullOrEmpty(newFile))
      {
        return;
      }

      if (!newFile.Equals(CurrentFile))
      {
        // yes, then raise event
        Log.Info("g_Player.OnChanged()");
        if (PlayBackChanged != null)
        {
          if ((_currentMedia == MediaType.TV || _currentMedia == MediaType.Video || _currentMedia == MediaType.Recording) &&
              (!Util.Utils.IsVideo(newFile)))
          {
            RefreshRateChanger.AdaptRefreshRate();
          }
          PlayBackChanged(_currentMedia, (int)CurrentPosition, (!String.IsNullOrEmpty(CurrentFileName) ? CurrentFileName : CurrentFile));
          CurrentFileName = String.Empty;
        }
      }
    }

    // called when current playing file is stopped
    private static void OnStopped()
    {
      // check if we're playing
      if (Playing && PlayBackStopped != null)
      {
        // yes, then raise event
        Log.Info("g_Player.OnStopped()");
        if (PlayBackStopped != null)
        {
          PlayBackStopped(_currentMedia, (int)CurrentPosition, (!String.IsNullOrEmpty(CurrentFileName) ? CurrentFileName : CurrentFile));
          CurrentFileName = String.Empty;
          _mediaInfo = null;
        }
      }
    }

    // called when current playing file ends
    private static void OnEnded()
    {
      // check if we're playing
      if (PlayBackEnded != null)
      {
        // yes, then raise event
        Log.Info("g_Player.OnEnded()");
        PlayBackEnded(_currentMedia, (!String.IsNullOrEmpty(CurrentFileName) ? CurrentFileName : _currentFilePlaying));
        RefreshRateChanger.AdaptRefreshRate();
        CurrentFileName = String.Empty;
        _mediaInfo = null;
      }
    }

    // called when starting playing a file
    private static void OnStarted()
    {
      // check if we're playing
      if (_player == null)
      {
        return;
      }
      if (_player.Playing)
      {
        // yes, then raise event 
        _currentMedia = MediaType.Music;
        if (_player.IsTV)
        {
          _currentMedia = MediaType.TV;
          if (!_player.IsTimeShifting)
          {
            _currentMedia = MediaType.Recording;
          }
        }
        else if (_player.IsRadio)
        {
          _currentMedia = MediaType.Radio;
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

    public static void ContinueGraph()
    {
      if (_player != null)
      {
        _player.ContinueGraph();
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private static void DoStop(bool keepTimeShifting, bool keepExclusiveModeOn)
    {
      if (_driveSpeedReduced)
      {
        // Set the CD/DVD Speed back to Max Speed
        var cdinfo = new BASS_CD_INFO();
        for (var i = 0; i < _driveCount; i++)
        {
          BassCd.BASS_CD_GetInfo(i, cdinfo);
          var maxspeed = (int)(cdinfo.maxspeed / 176.4);
          BassCd.BASS_CD_SetSpeed(i, maxspeed);
          BassCd.BASS_CD_Release(i);
        }
      }
      if (_player != null)
      {
        Log.Debug("g_Player.doStop() keepTimeShifting = {0} keepExclusiveModeOn = {1}", keepTimeShifting, keepExclusiveModeOn);
        // Get playing file for unmount handling
        var currentFile = CurrentFile;
        OnStopped();

        // since plugins could stop playback, we need to make sure that _player is not null.
        if (_player == null)
        {
          return;
        }

        GUIGraphicsContext.ShowBackground = true;
        if (!keepTimeShifting && !keepExclusiveModeOn)
        {
          Log.Debug("g_Player.doStop() - stop");
          _player.Stop();
        }
        else if (keepExclusiveModeOn)
        {
          Log.Debug("g_Player.doStop() - stop, keep exclusive mode on");
          _player.Stop(true);
        }
        else
        {
          Log.Debug("g_Player.doStop() - StopAndKeepTimeShifting");
          _player.StopAndKeepTimeShifting();
        }
        if (GUIGraphicsContext.form != null)
        {
          GUIGraphicsContext.form.Invalidate(true);
        }
        var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STOPPED, 0, 0, 0, 0, 0, null);
        GUIWindowManager.SendThreadMessage(msg);
        GUIGraphicsContext.IsFullScreenVideo = false;
        GUIGraphicsContext.IsPlaying = false;
        GUIGraphicsContext.IsPlayingVideo = false;
        CachePlayer();
        _chapters = null;
        _chaptersname = null;
        _jumpPoints = null;

        if (!keepExclusiveModeOn && !keepTimeShifting)
        {
          RefreshRateChanger.AdaptRefreshRate();
        }

        // No unmount for other ISO (avi-mkv ISO-crash in playlist after)
        Util.Utils.IsDVDImage(currentFile, ref currentFile);
        if (Util.Utils.IsISOImage(currentFile))
        {
          if (!String.IsNullOrEmpty(DaemonTools.GetVirtualDrive()) &&
              IsBDDirectory(DaemonTools.GetVirtualDrive()) ||
              IsDvdDirectory(DaemonTools.GetVirtualDrive()))
          {
            DaemonTools.UnMount();
          }
        }
      }
    }

    public static bool IsBDDirectory(string path)
    {
      return File.Exists(path + @"\BDMV\index.bdmv");
    }

    public static bool IsDvdDirectory(string path)
    {
      return File.Exists(path + @"\VIDEO_TS\VIDEO_TS.IFO");
    }

    public static void StopAndKeepTimeShifting()
    {
      DoStop(true, false);
    }

    public static void Stop(bool keepExclusiveModeOn)
    {
      Log.Info("g_Player.Stop() - keepExclusiveModeOn = {0}", keepExclusiveModeOn);
      if (keepExclusiveModeOn)
      {
        DoStop(false, true);
      }
      else
      {
        Stop();
      }
    }

    public static void Stop()
    {
      // we have to save the fullscreen status of the tv3 plugin for later use for the lastactivemodulefullscreen feature.
      var currentmodulefullscreen = (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_TVFULLSCREEN ||
                                     GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_MUSIC ||
                                     GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO ||
                                     GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_FULLSCREEN_TELETEXT);
      GUIPropertyManager.SetProperty("#currentmodulefullscreenstate", Convert.ToString(currentmodulefullscreen));
      DoStop(false, false);
    }

    private static void CachePlayer()
    {
      if (_player == null)
      {
        return;
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

    public static void Pause()
    {
      if (_player != null)
      {
        _currentStep = 0;
        _currentStepIndex = -1;
        _seekTimer = DateTime.MinValue;
        _player.Speed = 1; //default back to 1x speed.


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
            case Action.ActionType.ACTION_NEXT_CHAPTER:
              JumpToNextChapter();
              return true;
            case Action.ActionType.ACTION_PREV_CHAPTER:
              JumpToPrevChapter();
              return true;
          }
        }
        return _player.OnAction(action);
      }
      return false;
    }

    public static bool IsCDA
    {
      get
      {
        return _player != null && _player.IsCDA;
      }
    }

    public static bool IsDVD
    {
      get
      {
        return _player != null && _player.IsDVD;
      }
    }

    public static bool IsDVDMenu
    {
      get
      {
        return _player != null && _player.IsDVDMenu;
      }
    }

    public static MenuItems ShowMenuItems
    {
      get
      {
        return _player == null ? MenuItems.All : _player.ShowMenuItems;
      }
    }

    public static bool HasChapters
    {
      get
      {
        if (_player == null)
        {
          return false;
        }
        return Chapters != null;
      }
    }

    public static bool IsTV
    {
      get
      {
        if (RefreshRateChanger.RefreshRateChangePending && RefreshRateChanger.RefreshRateChangeMediaType == RefreshRateChanger.MediaType.TV)
        {
          return true;
        }
        return _player != null && _player.IsTV;
      }
    }

    public static bool IsTVRecording
    {
      get
      {
        if (RefreshRateChanger.RefreshRateChangePending && RefreshRateChanger.RefreshRateChangeMediaType == RefreshRateChanger.MediaType.Recording)
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
        return _player != null && _player.IsTimeShifting;
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
            GUIGraphicsContext.IsFullScreenVideo = true;
            GUIWindowManager.ActivateWindow((int) GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO);
          }
          _currentFilePlaying = _player.CurrentFile;
          OnStarted();
          return true;
        }
        Log.Info("dvdplayer:sendmsg");
        //show dialog:unable to play dvd,
        GUIWindowManager.ShowWarning(722, 723, -1);
      }
      finally
      {
        Starting = false;
      }
      return false;       
    } */

    public static bool PlayAudioStream(string strURL)
    {
      return PlayAudioStream(strURL, false);
    }

    // GUIMusicVideos and GUITRailer called this function, although they want to play a Video.
    // When BASS is enabled this resulted in no Picture being shown. they should now indicate that a Video is to be played.
    public static bool PlayAudioStream(string strURL, bool isMusicVideo)
    {
      try
      {
        _mediaInfo = null;
        string strAudioPlayer;
        using (Settings xmlreader = new MPSettings())
        {
          strAudioPlayer = xmlreader.GetValueAsString("audioplayer", "player", "Internal dshow player");
        }
        Starting = true;
        //stop radio
        var msgRadio = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_RADIO, 0, 0, 0, 0, 0, null);
        GUIWindowManager.SendMessage(msgRadio);
        var currentList = PlayListPlayer.SingletonPlayer.CurrentPlaylistType;
        if (isMusicVideo)
        {
          // Clear any temp. playlists before starting playback
          if (currentList == PlayListType.PLAYLIST_MUSIC_TEMP || currentList == PlayListType.PLAYLIST_VIDEO_TEMP)
          {
            PlayListPlayer.SingletonPlayer.GetPlaylist(currentList).Clear();
            PlayListPlayer.SingletonPlayer.Reset();
          }
        }
        // stop time shifting TV
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
          OnStopped();
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
        var result = _player.Play(strURL);
        if (!result)
        {
          Log.Info("player:ended");
          _player.SafeDispose();
          _player = null;
          _subs = null;
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
        if (!result)
        {
          UnableToPlay(strURL, MediaType.Unknown);
        }
        return result;
      }
      finally
      {
        Starting = false;
      }
    }

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
        var msgRadio = new GUIMessage(GUIMessage.MessageType.GUI_MSG_RECORDER_STOP_RADIO, 0, 0, 0, 0, 0, null);
        GUIWindowManager.SendMessage(msgRadio);
        _currentStep = 0;
        _currentStepIndex = -1;
        _seekTimer = DateTime.MinValue;
        if (string.IsNullOrEmpty(strURL))
        {
          UnableToPlay(strURL, MediaType.Unknown);
          return false;
        }

        _isInitialized = true;
        _subs = null;
        Log.Info("g_Player.PlayVideoStream({0})", strURL);
        if (_player != null)
        {
          GUIGraphicsContext.ShowBackground = true;
          OnChanged(strURL);
          OnStopped();
          if (_player != null)
          {
            _player.Stop();
          }
          CachePlayer();
          GUIGraphicsContext.form.Invalidate(true);
          _player = null;
          GC.Collect();
          GC.Collect();
          GC.Collect();
          GC.Collect();
        }

        _player = new VideoPlayerVMR9();
        _player = CachePreviousPlayer(_player);
        bool isPlaybackPossible;
        if (streamName != null)
        {
          isPlaybackPossible = streamName != "" ? _player.PlayStream(strURL, streamName) : _player.Play(strURL);
        }
        else
        {
          isPlaybackPossible = _player.Play(strURL);
        }
        if (!isPlaybackPossible)
        {
          Log.Info("player:ended");
          _player.SafeDispose();
          _player = null;
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
            GC.Collect();
            GC.Collect();
          }
        }
        else if (_player.Playing)
        {
          _currentFilePlaying = _player.CurrentFile;
          OnStarted();
        }
        _isInitialized = false;
        if (!isPlaybackPossible)
        {
          UnableToPlay(strURL, MediaType.Unknown);
        }
        return isPlaybackPossible;
      }
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
        if (_prevPlayer != null && (_prevPlayer.GetType() == newPlayer.GetType() && _prevPlayer.SupportsReplay))
        {
          player = _prevPlayer;
          _prevPlayer = null;
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
      return Play(strFile, MediaType.Unknown);
    }

    public static bool Play(string strFile, MediaType type)
    {
      return Play(strFile, type, (TextReader)null);
    }

    public static bool Play(string strFile, MediaType type, string chapters)
    {
      using (var stream = String.IsNullOrEmpty(chapters) ? null : new StringReader(chapters))
      {
        return Play(strFile, type, stream);
      }
    }

    public static bool Play(string strFile, MediaType type, TextReader chapters)
    {
      try
      {
        if (string.IsNullOrEmpty(strFile))
        {
          Log.Error("g_Player.Play() called without file attribute");
          return false;
        }

        var s = Path.GetExtension(strFile);
        if (s != null)
        {
          var extension = s.ToLower();
          var isImageFile = VirtualDirectory.IsImageFile(extension);
          if (isImageFile)
          {
            if (!File.Exists(DaemonTools.GetVirtualDrive() + @"\VIDEO_TS\VIDEO_TS.IFO") &&
                !File.Exists(DaemonTools.GetVirtualDrive() + @"\BDMV\index.bdmv"))
            {
              _currentFilePlaying = strFile;
              Ripper.AutoPlay.ExamineCD(DaemonTools.GetVirtualDrive(), true);
              return true;
            }
          }

          if (Util.Utils.IsDVD(strFile))
          {
            ChangeDriveSpeed(strFile, DriveType.CD);
          }

          _mediaInfo = new MediaInfoWrapper(strFile);
          Starting = true;

          if (Util.Utils.IsVideo(strFile) || Util.Utils.IsLiveTv(strFile)) // video, TV, rtsp
          {
            if (type == MediaType.Unknown)
            {
              Log.Debug("g_Player.Play - Media Type Unknown, forcing detection as Video");
              type = MediaType.Video;
            }

            // Refresh rate change done here. Blu-ray player will handle the refresh rate changes by itself
            if (strFile.ToUpper().IndexOf(@"\BDMV\INDEX.BDMV", StringComparison.Ordinal) == -1)
            {
              RefreshRateChanger.AdaptRefreshRate(strFile, (RefreshRateChanger.MediaType)(int)type);
            }

            if (RefreshRateChanger.RefreshRateChangePending)
            {
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

          _currentStep = 0;
          _currentStepIndex = -1;
          _seekTimer = DateTime.MinValue;
          _isInitialized = true;
          _subs = null;

          if (_player != null)
          {
            GUIGraphicsContext.ShowBackground = true;
            OnChanged(strFile);
            OnStopped();
            var doStop = true;
            if (type != MediaType.Video && Util.Utils.IsAudio(strFile))
            {
              if (type == MediaType.Unknown)
              {
                type = MediaType.Music;
              }
              if (BassMusicPlayer.IsDefaultMusicPlayer && BassMusicPlayer.Player.Playing)
              {
                doStop = !BassMusicPlayer.Player.CrossFadingEnabled;
              }
            }
            if (doStop)
            {
              if (_player != null)
              {
                _player.Stop();
              }
              CachePlayer();
              _player = null;
            }
          }

          Log.Info("g_Player.Play({0} {1})", strFile, type);
          if (!Util.Utils.IsAVStream(strFile) && Util.Utils.IsVideo(strFile))
          {
            if (!Util.Utils.IsRTSP(strFile) && extension != ".ts") // do not play recorded TV with external player
            {
              using (Settings xmlreader = new MPSettings())
              {
                var internalPlayer = xmlreader.GetValueAsBool("movieplayer", "internal", true);
                var internalDVD = xmlreader.GetValueAsBool("dvdplayer", "internal", true);

                // External player extension filter
                _externalPlayerExtensions = xmlreader.GetValueAsString("movieplayer", "extensions", "");
                if (!internalPlayer && !string.IsNullOrEmpty(_externalPlayerExtensions) &&
                    extension != ".ifo" && extension != ".vob" && !Util.Utils.IsDVDImage(strFile) &&
                    !CheckExtension(strFile))
                {
                  internalPlayer = true;
                }

                if ((!internalDVD && !isImageFile && (extension == ".ifo" || extension == ".vob")) ||
                    (!internalDVD && isImageFile && Util.Utils.IsDVDImage(strFile)) ||
                    // No image and no DVD folder rips
                    (!internalPlayer && !isImageFile && extension != ".ifo" && extension != ".vob") ||
                    // BluRay image
                    (!internalPlayer && isImageFile && Util.Utils.IsBDImage(strFile))) // external player used
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
                      {
                        return false;
                      }
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
        }
        // Still for BDISO strFile = ISO filename, convert it
        Util.Utils.IsBDImage(strFile, ref strFile);

        _currentFileName = strFile;
        _player = _factory.Create(strFile, type);

        if (_player != null)
        {
          if (chapters != null)
          {
            LoadChapters(chapters);
          }
          else
          {
            LoadChapters(strFile);
          }
          _player = CachePreviousPlayer(_player);
          var result = _player.Play(strFile);
          if (!result)
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
            if (_chaptersname == null)
            {
              _chaptersname = _player.ChaptersName;
            }
            OnStarted();
          }
          return result;
        }
      }
      finally
      {
        Starting = false;
      }
      UnableToPlay(strFile, type);
      return false;
    }

    public static bool IsExternalPlayer
    {
      get
      {
        return _player != null && _player.IsExternal;
      }
    }

    public static bool IsRadio
    {
      get
      {
        return _player != null && (_player.IsRadio);
      }
    }

    public static bool IsMusic
    {
      get
      {
        if (_player == null)
        {
          return false;
        }
        return (_currentMedia == MediaType.Music);
      }
    }

    public static bool Playing
    {
      get
      {
        if (_player == null)
        {
          return false;
        }
        return !_isInitialized && _player.Playing;
      }
    }

    public static int PlaybackType
    {
      get
      {
        if (_player == null)
        {
          return -1;
        }
        return _player.PlaybackType;
      }
    }

    public static bool Paused
    {
      get
      {
        return _player != null && _player.Paused;
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
        return _player != null && _player.Stopped;
      }
    }

    public static int Speed
    {
      get
      {
        return _player == null ? 1 : _player.Speed;
      }
      set
      {
        if (_player == null)
        {
          return;
        }
        _player.Speed = value;
        _currentStep = 0;
        _currentStepIndex = -1;
        _seekTimer = DateTime.MinValue;
      }
    }

    public static string CurrentFile
    {
      get
      {
        return _player == null ? "" : _player.CurrentFile;
      }
    }

    public static int Volume
    {
      get
      {
        if (_player == null)
        {
          return -1;
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

    public static Geometry.Type ARType
    {
      get { return GUIGraphicsContext.ARType; }
      set
      {
        if (_player != null)
        {
          _player.ARType = value;
        }
      }
    }

    public static int PositionX
    {
      get
      {
        return _player == null ? 0 : _player.PositionX;
      }
      set
      {
        if (_player != null)
        {
          _player.PositionX = value;
        }
      }
    }

    public static int PositionY
    {
      get
      {
        return _player == null ? 0 : _player.PositionY;
      }
      set
      {
        if (_player != null)
        {
          _player.PositionY = value;
        }
      }
    }

    public static int RenderWidth
    {
      get
      {
        return _player == null ? 0 : _player.RenderWidth;
      }
      set
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
        return _player != null && _player.Visible;
      }
      set
      {
        if (_player != null)
        {
          _player.Visible = value;
        }
      }
    }

    public static int RenderHeight
    {
      get
      {
        return _player == null ? 0 : _player.RenderHeight;
      }
      set
      {
        if (_player != null)
        {
          _player.RenderHeight = value;
        }
      }
    }

    public static double Duration
    {
      get
      {
        return _player == null ? 0 : _player.Duration;
      }
    }

    public static double CurrentPosition
    {
      get
      {
        return _player == null ? 0 : _player.CurrentPosition;
      }
    }

    public static double StreamPosition
    {
      get
      {
        return _player == null ? 0 : _player.StreamPosition;
      }
    }

    public static double ContentStart
    {
      get
      {
        return _player == null ? 0 : _player.ContentStart;
      }
    }

    public static bool FullScreen
    {
      get
      {
        return _player == null ? GUIGraphicsContext.IsFullScreenVideo : _player.FullScreen;
      }
      set
      {
        if (_player != null)
        {
          _player.FullScreen = value;
        }
      }
    }

    public static double[] Chapters
    {
      get
      {
        if (_player == null && _chapters == null)
        {
          return null;
        }
        return _chapters ?? _player.Chapters;
      }
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
      }
    }

    public static double[] JumpPoints
    {
      get
      {
        return _jumpPoints;
      }
    }

    public static int Width
    {
      get
      {
        return _player == null ? 0 : _player.Width;
      }
    }

    public static int Height
    {
      get
      {
        return _player == null ? 0 : _player.Height;
      }
    }

    public static void SeekRelative(double dTime)
    {
      if (_player == null)
      {
        return;
      }
      _player.SeekRelative(dTime);
      _currentStep = 0;
      _currentStepIndex = -1;
      _seekTimer = DateTime.MinValue;
      var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
      GUIGraphicsContext.SendMessage(msgUpdate);
    }

    public static void StepNow()
    {
      if (_currentStep != 0 && _player != null)
      {
        if (_currentStep < 0 || (_player.CurrentPosition + 4 < _player.Duration) || !IsTV)
        {
          var dTime = _currentStep + _player.CurrentPosition;
          Log.Debug("g_Player.StepNow() - Preparing to seek to {0}:{1}", _player.CurrentPosition, _player.Duration);
          if (!IsTV && (dTime > _player.Duration)) dTime = _player.Duration - 5;
          if (IsTV && (dTime + 3 > _player.Duration)) dTime = _player.Duration - 3; // Margin for live TV
          if (dTime < 0) dTime = 0d;

          Log.Debug("g_Player.StepNow() - Preparing to seek to {0}:{1}:{2} isTv {3}", (int)(dTime / 3600d),
                    (int)((dTime % 3600d) / 60d), (int)(dTime % 60d), IsTV);
          _player.SeekAbsolute(dTime);
          Speed = Speed;
          var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
          GUIGraphicsContext.SendMessage(msgUpdate);
        }
      }
      _currentStep = 0;
      _currentStepIndex = -1;
      _seekTimer = DateTime.MinValue;
    }

    /// <summary>
    /// This function returns the localized time units for "Step" (seconds) in human readable format.
    /// </summary>
    /// <param name="step"></param>
    /// <returns></returns>
    public static string GetSingleStep(int step)
    {
      if (step >= 0)
      {
        if (step >= 3600)
        {
          // check for 'full' hours
          if ((Convert.ToSingle(step) / 3600) > 1 && (Convert.ToSingle(step) / 3600) != 2 &&
              (Convert.ToSingle(step) / 3600) != 3)
          {
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
      }
      if (step <= -60)
      {
        return "- " + Convert.ToString(Math.Abs(step / 60)) + " " + GUILocalizeStrings.Get(2998); // "min"
      }
      return "- " + Convert.ToString(Math.Abs(step)) + " " + GUILocalizeStrings.Get(2999); // "sec"
    }

    public static string GetStepDescription()
    {
      if (_player == null)
      {
        return "";
      }
      var timeToSleep = _currentStep;
      if (timeToSleep == 0)
      {
        return "";
      }
      _player.Process();
      if (_player.CurrentPosition + timeToSleep <= 0)
      {
        return GUILocalizeStrings.Get(773); // "START"
      }
      if (_player.CurrentPosition + timeToSleep >= _player.Duration)
      {
        return GUILocalizeStrings.Get(774); // "END"
      }
      return GetSingleStep(_currentStep);
    }

    public static int GetSeekStep(out bool start, out bool end)
    {
      start = false;
      end = false;
      if (_player == null)
      {
        return 0;
      }
      var timeToSleep = _currentStep;
      if (_player.CurrentPosition + timeToSleep <= 0)
      {
        start = true; //start
      }
      if (_player.CurrentPosition + timeToSleep >= _player.Duration)
      {
        end = true;
      }
      return timeToSleep;
    }

    public static void SeekStep(bool fastForward)
    {
      if (!ConfigLoaded)
      {
        _seekStepList = LoadSettings();
        Log.Info("g_Player loading seekstep config {0}", ""); // Convert.ToString(_seekStepList[0]));
      }
      if (fastForward)
      {
        if (_currentStep < 0)
        {
          _currentStepIndex--; // E.g. from -30 to -15 
          if (_currentStepIndex == -1)
          {
            _currentStep = 0; // Reached middle, no stepping
          }
          else
          {
            _currentStep = -1 * Convert.ToInt32(_seekStepList[_currentStepIndex]);
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
      else
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
        else
        {
          _currentStepIndex--; // E.g. from 30 to 15
          _currentStep = _currentStepIndex == -1 ? 0 : Convert.ToInt32(_seekStepList[_currentStepIndex]);
        }
      }
      _seekTimer = DateTime.Now;
    }

    public static void SeekRelativePercentage(int iPercentage)
    {
      if (_player == null)
      {
        return;
      }
      _player.SeekRelativePercentage(iPercentage);
      var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
      GUIGraphicsContext.SendMessage(msgUpdate);
      _currentStep = 0;
      _currentStepIndex = -1;
      _seekTimer = DateTime.MinValue;
    }

    public static void SeekAbsolute(double dTime)
    {
      if (_player == null)
      {
        return;
      }
      Log.Debug("g_Player.SeekAbsolute() - Preparing to seek to {0}:{1}:{2}", (int)(dTime / 3600d),
                (int)((dTime % 3600d) / 60d), (int)(dTime % 60d));
      _player.SeekAbsolute(dTime);
      var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
      GUIGraphicsContext.SendMessage(msgUpdate);
      _currentStep = 0;
      _currentStepIndex = -1;
      _seekTimer = DateTime.MinValue;
    }

    public static void SeekAsolutePercentage(int iPercentage)
    {
      if (_player == null)
      {
        return;
      }
      _player.SeekAsolutePercentage(iPercentage);
      var msgUpdate = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYER_POSITION_CHANGED, 0, 0, 0, 0, 0, null);
      GUIGraphicsContext.SendMessage(msgUpdate);
      _currentStep = 0;
      _currentStepIndex = -1;
      _seekTimer = DateTime.MinValue;
    }

    public static bool HasVideo
    {
      get
      {
        if (RefreshRateChanger.RefreshRateChangePending)
        {
          return true;
        }
        return _player != null && _player.HasVideo;
      }
    }

    public static bool HasViz
    {
      get
      {
        if (RefreshRateChanger.RefreshRateChangePending)
        {
          return true;
        }
        return _player != null && _player.HasViz;
      }
    }

    public static bool IsVideo
    {
      get
      {
        if (RefreshRateChanger.RefreshRateChangePending && RefreshRateChanger.RefreshRateChangeMediaType == RefreshRateChanger.MediaType.Video)
        {
          return true;
        }
        if (_player == null)
        {
          return false;
        }
        return _currentMedia == MediaType.Video;
      }
    }

    public static bool HasSubs
    {
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
      {
        return;
      }
      if (_subs == null)
      {
        return;
      }
      if (HasSubs)
      {
        _subs.Render(_player.CurrentPosition);
      }
    }

    public static void WndProc(ref Message m)
    {
      if (_player == null)
      {
        return;
      }
      _player.WndProc(ref m);
    }

    public static void Process()
    {
      if (GUIGraphicsContext.Vmr9Active && VMR9Util.g_vmr9 != null && !GUIGraphicsContext.InVmr9Render)
      {
        VMR9Util.g_vmr9.Process();
        VMR9Util.g_vmr9.Repaint();
      }
      if (_player == null)
      {
        return;
      }
      _player.Process();
      if (_player.Initializing)
      {
        return;
      }
      if (!_player.Playing)
      {
        Log.Info("g_Player.Process() player stopped...");
        if (_player.Ended)
        {
          var msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_ENDED, 0, 0, 0, 0, 0, null);
          GUIWindowManager.SendThreadMessage(msg);
          GUIGraphicsContext.IsFullScreenVideo = false;
          GUIGraphicsContext.IsPlaying = false;
          GUIGraphicsContext.IsPlayingVideo = false;
          OnEnded();
          return;
        }
        Stop();
      }
      else
      {
        if (_currentStep != 0)
        {
          var ts = DateTime.Now - _seekTimer;
          if (ts.TotalMilliseconds > _seekStepTimeout)
          {
            StepNow();
          }
        }
        else if (_autoComSkip && JumpPoints != null && _player.Speed == 1)
        {
          var currentPos = _player.CurrentPosition;
          foreach (var jumpFrom in JumpPoints.Where(jumpFrom => jumpFrom != 0 && currentPos <= jumpFrom + 1.0 && currentPos >= jumpFrom - 0.1))
          {
            Log.Debug("g_Player.Process() - Current Position: {0}, JumpPoint: {1}", currentPos, jumpFrom);

            JumpToNextChapter();
            break;
          }
        }
      }
    }

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

    public static void OnZapping(int info)
    {
      if (_player == null)
      {
        return;
      }
      OnTVChannelChanged();
      _player.OnZapping(info);
    }

    #region Edition selection

    /// <summary>
    /// Property which returns the total number of edition streams available
    /// </summary>
    public static int EditionStreams
    {
      get
      {
        return _player == null ? 0 : _player.EditionStreams;
      }
    }

    /// <summary>
    /// Property to get/set the current edition stream
    /// </summary>
    public static int CurrentEditionStream
    {
      get
      {
        return _player == null ? 0 : _player.CurrentEditionStream;
      }
      set
      {
        if (_player != null)
        {
          _player.CurrentEditionStream = value;
        }
      }
    }

    public static string EditionLanguage(int iStream)
    {
      if (_player == null)
      {
        return Strings.Unknown;
      }

      var stream = _player.EditionLanguage(iStream);
      return Util.Utils.TranslateLanguageString(stream);
    }

    /// <summary>
    /// Property to get the type of an edition stream
    /// </summary>
    public static string EditionType(int iStream)
    {
      if (_player == null)
      {
        return Strings.Unknown;
      }

      var stream = _player.EditionType(iStream);
      return stream;
    }

    #endregion

    #region Video selection

    /// <summary>
    /// Property which returns the total number of video streams available
    /// </summary>
    public static int VideoStreams
    {
      get
      {
        return _player == null ? 0 : _player.VideoStreams;
      }
    }

    /// <summary>
    /// Property to get/set the current video stream
    /// </summary>
    public static int CurrentVideoStream
    {
      get
      {
        return _player == null ? 0 : _player.CurrentVideoStream;
      }
      set
      {
        if (_player != null)
        {
          _player.CurrentVideoStream = value;
        }
      }
    }

    public static string VideoLanguage(int iStream)
    {
      if (_player == null)
      {
        return Strings.Unknown;
      }

      var stream = _player.VideoLanguage(iStream);
      return Util.Utils.TranslateLanguageString(stream);
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

      var stream = _player.VideoType(iStream);
      return stream;
    }

    #endregion

    #region Postprocessing selection

    /// <summary>
    /// Property which returns true if the player is able to perform postprocessing features
    /// </summary>
    public static bool HasPostprocessing
    {
      get
      {
        return _player != null && _player.HasPostprocessing;
      }
    }

    #endregion

    #region subtitle/audio stream selection

    /// <summary>
    /// Property which returns the total number of audio streams available
    /// </summary>
    public static int AudioStreams
    {
      get
      {
        return _player == null ? 0 : _player.AudioStreams;
      }
    }

    /// <summary>
    /// Property to get/set the current audio stream
    /// </summary>
    public static int CurrentAudioStream
    {
      get
      {
        return _player == null ? 0 : _player.CurrentAudioStream;
      }
      set
      {
        if (_player != null)
        {
          _player.CurrentAudioStream = value;
        }
      }
    }

    /// <summary>
    /// Property to get the name for an audio stream
    /// </summary>
    public static string AudioLanguage(int iStream)
    {
      if (_player == null)
      {
        return Strings.Unknown;
      }

      var stream = _player.AudioLanguage(iStream);
      return Util.Utils.TranslateLanguageString(stream);
    }

    /// <summary>
    /// Property to get the type of an audio stream
    /// </summary>
    public static string AudioType(int iStream)
    {
      if (_player == null)
      {
        return Strings.Unknown;
      }

      var stream = _player.AudioType(iStream);
      return stream;
    }

    /// <summary>
    /// Property to get the total number of subtitle streams
    /// </summary>
    public static int SubtitleStreams
    {
      get
      {
        return _player == null ? 0 : _player.SubtitleStreams;
      }
    }

    /// <summary>
    /// Property to get/set the current subtitle stream
    /// </summary>
    public static int CurrentSubtitleStream
    {
      get
      {
        return _player == null ? 0 : _player.CurrentSubtitleStream;
      }
      set
      {
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
        return Strings.Unknown;
      }

      var stream = _player.SubtitleLanguage(iStream);
      return Util.Utils.TranslateLanguageString(stream);
    }

    /// <summary>
    /// Retrieves the name of the subtitle stream
    /// </summary>
    /// <param name="iStream">Index of the stream</param>
    /// <returns>Name of the track</returns>
    public static string SubtitleName(int iStream)
    {
      if (_player == null)
      {
        return null;
      }

      var stream = _player.SubtitleName(iStream);
      return Util.Utils.TranslateLanguageString(stream);
    }

    #endregion

    public static bool EnableSubtitle
    {
      get
      {
        return _player != null && _player.EnableSubtitle;
      }
      set
      {
        if (_player == null)
        {
          return;
        }
        _player.EnableSubtitle = value;
      }
    }

    public static bool EnableForcedSubtitle
    {
      get
      {
        return _player != null && _player.EnableForcedSubtitle;
      }
      set
      {
        if (_player == null)
        {
          return;
        }
        _player.EnableForcedSubtitle = value;
      }
    }

    public static bool SupportsCC
    {
      get
      {
        if (IsDVD || IsTV || IsVideo)
        {
          var dvdPlayer = _player as DVDPlayer;
          var tsReaderPlayer = _player as TSReaderPlayer;
          return dvdPlayer != null ? dvdPlayer.SupportsCC : tsReaderPlayer != null && tsReaderPlayer.SupportsCC;
        }
        return false;
      }
    }

    public static void SetVideoWindow()
    {
      if (_player == null)
      {
        return;
      }
      _player.SetVideoWindow();
    }

    public static void Init()
    {
      GUIGraphicsContext.OnVideoWindowChanged += OnVideoWindowChanged;
      GUIGraphicsContext.OnGammaContrastBrightnessChanged += OnGammaContrastBrightnessChanged;
    }

    private static void OnGammaContrastBrightnessChanged()
    {
      if (!Playing)
      {
        return;
      }
      if (!HasVideo)
      {
        return;
      }
      if (_player == null)
      {
        return;
      }
      _player.Contrast = GUIGraphicsContext.Contrast;
      _player.Brightness = GUIGraphicsContext.Brightness;
      _player.Gamma = GUIGraphicsContext.Gamma;
    }

    private static void OnVideoWindowChanged()
    {
      if (!Playing)
      {
        return;
      }
      if (!HasVideo && !HasViz)
      {
        return;
      }
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
      Visible = (FullScreen || GUIGraphicsContext.Overlay ||
                 windowId == (int)GUIWindow.Window.WINDOW_SCHEDULER || inTV);
      SetVideoWindow();
    }

    /// <summary>
    /// returns video window rectangle
    /// </summary>
    public static Rectangle VideoWindow
    {
      get
      {
        return _player == null ? new Rectangle(0, 0, 0, 0) : _player.VideoWindow;
      }
    }

    /// <summary>
    /// returns video source rectangle displayed
    /// </summary>
    public static Rectangle SourceWindow
    {
      get
      {
        return _player == null ? new Rectangle(0, 0, 0, 0) : _player.SourceWindow;
      }
    }

    public static int GetHDC()
    {
      return _player == null ? 0 : _player.GetHDC();
    }

    public static void ReleaseHDC(int hdc)
    {
      if (_player == null)
      {
        return;
      }
      _player.ReleaseHDC(hdc);
    }

    public static bool CanSeek
    {
      get
      {
        if (_player == null)
        {
          return false;
        }
        return (_player.CanSeek() && !_player.IsDVDMenu);
      }
    }

    public static MediaInfoWrapper MediaInfo
    {
      get { return _mediaInfo; }
    }

    /// <summary>
    /// Switches to the next audio stream.
    /// 
    /// Calls are directly pushed to the embedded player. And care 
    /// is taken not to do multiple calls to the player.
    /// </summary>
    public static void SwitchToNextAudio()
    {
      if (_player != null)
      {
        // take current stream and number of
        var streams = _player.AudioStreams;
        var current = _player.CurrentAudioStream;
        var next = current;
        var success = false;
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
          _player.CurrentAudioStream = next;
          // if the stream is set in, stop the loop
          if (next == _player.CurrentAudioStream)
          {
            success = true;
          }
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
        if (CurrentSubtitleStream == -1)
        {
          EnableSubtitle = false;
        }
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
        // take current stream and number of
        var streams = _player.EditionStreams;
        var current = _player.CurrentEditionStream;
        var next = current;
        var success = false;
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
          }
        } while ((next != current) && (success == false));
        if (success == false)
        {
          Log.Info("g_Player: Failed to switch to next editionstream.");
        }
      }
    }

    /// <summary>
    /// Switches to the next video stream.
    /// 
    /// Calls are directly pushed to the embedded player. And care 
    /// is taken not to do multiple calls to the player.
    /// </summary>
    public static void SwitchToNextVideo()
    {
      if (_player != null)
      {
        // take current stream and number of
        var streams = _player.VideoStreams;
        var current = _player.CurrentVideoStream;
        var next = current;
        var success = false;
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
          // if the stream is set in, stop the loop
          if (next == _player.CurrentVideoStream)
          {
            success = true;
          }
        } while ((next != current) && (success == false));
        if (success == false)
        {
          Log.Info("g_Player: Failed to switch to next Videostream.");
        }
      }
    }

    private static bool IsFileUsedbyAnotherProcess(string file)
    {
      try
      {
        using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None)) { }
      }
      catch (IOException exp)
      {
        Log.Error("g_Player.LoadChapters() - {0}", exp.ToString());
        return true;
      }
      return false;
    }

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

    // ReSharper disable UnusedMethodReturnValue.Local
    private static bool LoadChapters(string videoFile)
    // ReSharper restore UnusedMethodReturnValue.Local
    {
      _chapters = null;
      _jumpPoints = null;

      var chapterFile = Path.ChangeExtension(videoFile, ".txt");
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
    {
      _chapters = null;
      _jumpPoints = null;

      try
      {
        if (_loadAutoComSkipSetting)
        {
          using (Settings xmlreader = new MPSettings())
          {
            _autoComSkip = xmlreader.GetValueAsBool("comskip", "automaticskip", false);
          }

          Log.Debug("g_Player.LoadChapters() - Automatic ComSkip mode is {0}", _autoComSkip ? "on." : "off.");

          _loadAutoComSkipSetting = false;
        }

        var chapters = new ArrayList();
        var jumps = new ArrayList();

        var line = chaptersReader.ReadLine();

        int fps;
        if (String.IsNullOrEmpty(line) || !int.TryParse(line.Substring(line.LastIndexOf(' ') + 1), out fps))
        {
          Log.Warn("g_Player.LoadChapters() - Invalid chapter file");
          return false;
        }

        var framesPerSecond = fps / 100.0;

        while ((line = chaptersReader.ReadLine()) != null)
        {
          if (String.IsNullOrEmpty(line))
          {
            continue;
          }

          var tokens = line.Split(new[] { '\t' });
          if (tokens.Length != 2)
          {
            continue;
          }

          int time;
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
          Log.Warn("g_Player.LoadChapters() - No chapters found in file");
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
      }
      catch (Exception ex)
      {
        Log.Error("g_Player.LoadChapters() - {0}", ex.ToString());
        return false;
      }
    }

    private static double NextChapterTime(double currentPos)
    {
      if (Chapters != null)
      {
        foreach (var t in Chapters.Where(t => currentPos < t))
        {
          return t;
        }
      }

      return -1; // no skip
    }

    private static double PreviousChapterTime(double currentPos)
    {
      if (Chapters != null)
      {
        for (var index = Chapters.Length - 1; index >= 0; index--)
        {
          if (Chapters[index] < currentPos - 5.0)
          {
            return Chapters[index];
          }
        }
      }

      return 0;
    }

    public static bool JumpToNextChapter()
    {
      if (!Playing)
      {
        return false;
      }

      var nextChapter = NextChapterTime(_player.CurrentPosition);
      Log.Debug("g_Player.JumpNextChapter() - Current Position: {0}, Next Chapter: {1}", _player.CurrentPosition, nextChapter);

      if (nextChapter > 0 && nextChapter < _player.Duration)
      {
        SeekAbsolute(nextChapter);
        return true;
      }

      return false;
    }

    public static bool JumpToPrevChapter()
    {
      if (!Playing)
      {
        return false;
      }

      var prevChapter = PreviousChapterTime(_player.CurrentPosition);
      Log.Debug("g_Player.JumpPrevChapter() - Current Position: {0}, Previous Chapter: {1}", _player.CurrentPosition, prevChapter);

      if (prevChapter >= 0 && prevChapter < _player.Duration)
      {
        SeekAbsolute(prevChapter);
        return true;
      }

      return false;
    }

    public static void UnableToPlay(string fileName, MediaType type)
    {
      try
      {
        if (_mediaInfo == null)
        {
          _mediaInfo = new MediaInfoWrapper(fileName);
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
        GUIGraphicsContext.SendMessage(msg);
        _mediaInfo = null;
      }
      catch (Exception ex)
      {
        Log.Error("g_player: Error notifying user about unsuccessful playback of {0} - {1}", fileName, ex.ToString());
      }
    }

    private static bool CheckExtension(string filename)
    {
      char[] splitter = { ';' };
      var extensions = _externalPlayerExtensions.Split(splitter);

      return extensions.Any(extension => extension.Trim().Equals(Path.GetExtension(filename), StringComparison.OrdinalIgnoreCase));
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
    public static ShowFullScreenWindowHandler ShowFullScreenWindowTV
    {
      get
      {
        return _showFullScreenWindowTV;
      }
      set
      {
        _showFullScreenWindowTV = value;
        Log.Debug("g_player: Setting ShowFullScreenWindowTV to {0}", value);
      }
    }

    /// <summary>
    /// This handler gets called by ShowFullScreenWindow.
    /// It should handle general Fullscreen-Video.
    /// By default, it is set to ShowFullScreenWindowVideoDefault.
    /// </summary>
    public static ShowFullScreenWindowHandler ShowFullScreenWindowVideo
    {
      get
      {
        return _showFullScreenWindowVideo;
      }
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
      get
      {
        return _showFullScreenWindowOther;
      }
      set
      {
        _showFullScreenWindowOther = value;
        Log.Debug("g_player: Setting ShowFullScreenWindowOther to {0}", value);
      }
    }

    /// <summary>
    /// The default handler does only work if a player is active.
    /// However, GUITVHome and TvPlugin.TVHOME, both set their
    /// own hook to enable fullscreen TV for non-player live
    /// TV.
    /// </summary>
    /// <returns></returns>
    public static bool ShowFullScreenWindowTVDefault()
    {
      if (Playing && IsTV && !IsTVRecording)
      {
        // close e.g. the TV guide dialog (opened from context menu)
        var actionCloseDialog = new Action(Action.ActionType.ACTION_CLOSE_DIALOG, 0, 0);
        GUIGraphicsContext.OnAction(actionCloseDialog);

        // watching TV
        if (GUIWindowManager.ActiveWindow == (int)GUIWindow.Window.WINDOW_TVFULLSCREEN)
        {
          return true;
        }
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

        // When we don't have any Visualization, switch to Now Playing, instead of showing a black screen
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
      var win = GUIWindowManager.GetWindow(GUIWindowManager.ActiveWindow);
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