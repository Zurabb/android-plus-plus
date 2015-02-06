﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class AndroidDevice
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private Dictionary<string, string> m_deviceProperties = new Dictionary<string, string> ();

    private Dictionary<string, List<uint>> m_deviceProcessesPidsByName = new Dictionary<string, List<uint>> ();

    private Dictionary<uint, AndroidProcess> m_deviceProcessesByPid = new Dictionary<uint, AndroidProcess> ();

    private Dictionary<uint, List<AndroidProcess>> m_deviceProcessesByPpid = new Dictionary<uint, List<AndroidProcess>> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidDevice (string deviceId)
    {
      ID = deviceId;

      PopulateProperties ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool IsEmulator
    {
      get
      {
        return ID.StartsWith ("emulator-");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool IsOverWiFi
    {
      get
      {
        return ID.Contains (".");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Refresh ()
    {
      LoggingUtils.PrintFunction ();

      PopulateProcesses ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string GetProperty (string property)
    {
      string prop;

      m_deviceProperties.TryGetValue (property, out prop);

      return prop ?? string.Empty;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidProcess GetProcessFromPid (uint processId)
    {
      AndroidProcess process;

      if (m_deviceProcessesByPid.TryGetValue (processId, out process))
      {
        return process;
      }

      return null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidProcess [] GetProcessesFromName (string processName)
    {
      List <uint> processPidList;

      List<AndroidProcess> processList = new List<AndroidProcess> ();

      if (m_deviceProcessesPidsByName.TryGetValue (processName, out processPidList))
      {
        foreach (uint pid in processPidList)
        {
          processList.Add (GetProcessFromPid (pid));
        }
      }

      return processList.ToArray ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public uint [] GetActivePids ()
    {
      uint [] activePids = new uint [m_deviceProcessesByPid.Count];

      m_deviceProcessesByPid.Keys.CopyTo (activePids, 0);

      return activePids;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string Shell (string command, string arguments, int timeout = 30000)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        int exitCode = -1;

        using (SyncRedirectProcess process = AndroidAdb.AdbCommand (this, "shell", string.Format ("{0} {1}", command, arguments)))
        {
          exitCode = process.StartAndWaitForExit (timeout);

          if (exitCode != 0)
          {
            throw new InvalidOperationException (string.Format ("[shell:{0}] returned error code: {1}", command, exitCode));
          }

          return process.StandardOutput;
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw new InvalidOperationException ("Failed shell command", e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string Push (string localPath, string remotePath)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        int exitCode = -1;

        using (SyncRedirectProcess process = AndroidAdb.AdbCommand (this, "push", string.Format ("{0} {1}", PathUtils.SantiseWindowsPath (localPath), remotePath)))
        {
          exitCode = process.StartAndWaitForExit ();

          if (exitCode != 0)
          {
            throw new InvalidOperationException (string.Format ("push returned error code: {0}", exitCode));
          }

          return process.StandardOutput;
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw new InvalidOperationException ("Failed push request", e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string Pull (string remotePath, string localPath)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        int exitCode = -1;

        using (SyncRedirectProcess process = AndroidAdb.AdbCommand (this, "pull", string.Format ("{0} {1}", remotePath, PathUtils.SantiseWindowsPath (localPath))))
        {
          exitCode = process.StartAndWaitForExit ();

          if (exitCode != 0)
          {
            throw new InvalidOperationException (string.Format ("pull returned error code: {0}", exitCode));
          }

          return process.StandardOutput;
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw new InvalidOperationException ("Failed pull request", e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void PopulateProperties ()
    {
      LoggingUtils.PrintFunction ();

      string getPropOutput = Shell ("getprop", "");

      if (!string.IsNullOrEmpty (getPropOutput))
      {
        string pattern = @"^\[(?<key>[^\]:]+)\]:[ ]+\[(?<value>[^\]$]+)";

        Regex regExMatcher = new Regex (pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        string [] properties = getPropOutput.Replace ("\r", "").Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < properties.Length; ++i)
        {
          if (!properties [i].StartsWith ("["))
          {
            continue; // early rejection.
          }

          string unescapedStream = Regex.Unescape (properties [i]);

          Match regExLineMatch = regExMatcher.Match (unescapedStream);

          if (regExLineMatch.Success)
          {
            string key = regExLineMatch.Result ("${key}");

            string value = regExLineMatch.Result ("${value}");

            if (string.IsNullOrEmpty (key) || key.Equals ("${key}"))
            {
              continue;
            }
            else if (value.Equals ("${value}"))
            {
              continue;
            }
            else 
            {
              m_deviceProperties [key] = value;
            }
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void PopulateProcesses ()
    {
      // 
      // Skip the first line, and read in tab-seperated process data.
      // 

      LoggingUtils.PrintFunction ();

      string deviceProcessList = Shell ("ps", "-t");

      if (!String.IsNullOrEmpty (deviceProcessList))
      {
        string [] processesOutputLines = deviceProcessList.Replace ("\r", "").Split (new char [] { '\n' });

        string processesRegExPattern = @"(?<user>[^ ]+)[ ]*(?<pid>[0-9]+)[ ]*(?<ppid>[0-9]+)[ ]*(?<vsize>[0-9]+)[ ]*(?<rss>[0-9]+)[ ]*(?<wchan>[A-Za-z0-9]+)[ ]*(?<pc>[A-Za-z0-9]+)[ ]*(?<s>[^ ]+)[ ]*(?<name>[^\r\n]+)";

        Regex regExMatcher = new Regex (processesRegExPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        m_deviceProcessesByPid.Clear ();

        m_deviceProcessesPidsByName.Clear ();

        for (uint i = 1; i < processesOutputLines.Length; ++i)
        {
          if (!String.IsNullOrEmpty (processesOutputLines [i]))
          {
            Match regExLineMatches = regExMatcher.Match (processesOutputLines [i]);

            string processUser = regExLineMatches.Result ("${user}");

            uint processPid = uint.Parse (regExLineMatches.Result ("${pid}"));

            uint processPpid = uint.Parse (regExLineMatches.Result ("${ppid}"));

            uint processVsize = uint.Parse (regExLineMatches.Result ("${vsize}"));

            uint processRss = uint.Parse (regExLineMatches.Result ("${rss}"));

            uint processWchan = Convert.ToUInt32 (regExLineMatches.Result ("${wchan}"), 16);

            uint processPc = Convert.ToUInt32 (regExLineMatches.Result ("${pc}"), 16);

            string processPcS = regExLineMatches.Result ("${s}");

            string processName = regExLineMatches.Result ("${name}");

            AndroidProcess process = new AndroidProcess (this, processName, processPid, processPpid, processUser);

            m_deviceProcessesByPid [processPid] = process;

            // 
            // Add new process to a fast-lookup collection organised by process name.
            // 

            {
              List<uint> processPidsList;

              if (!m_deviceProcessesPidsByName.TryGetValue (processName, out processPidsList))
              {
                processPidsList = new List<uint> ();
              }

              processPidsList.Add (processPid);

              m_deviceProcessesPidsByName [processName] = processPidsList;
            }

            // 
            // Check whether this process is the child of another; keep it organised if required.
            // 

            if (m_deviceProcessesByPid.ContainsKey (processPpid))
            {
              List<AndroidProcess> processThreadPidsList;

              if (!m_deviceProcessesByPpid.TryGetValue (processPpid, out processThreadPidsList))
              {
                processThreadPidsList = new List<AndroidProcess> ();
              }

              processThreadPidsList.Add (process);

              m_deviceProcessesByPpid [processPpid] = processThreadPidsList;
            }
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string ID { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidSettings.VersionCode SdkVersion 
    {
      get
      {
        // 
        // Query device's current SDK level. If it's not an integer (like some custom ROMs) fall-back to ICS.
        // 

        try
        {
          int sdkLevel = int.Parse (GetProperty ("ro.build.version.sdk"));

          return (AndroidSettings.VersionCode) sdkLevel;
        }
        catch (Exception e)
        {
          LoggingUtils.HandleException (e);

          return AndroidSettings.VersionCode.GINGERBREAD;
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public string [] SupportedCpuAbis
    {
      get
      {
        // 
        // Queries device's supported CPU ABIs. Fallback to using old-style primary/secondary props if list isn't available.
        // 

        string abiList = GetProperty ("ro.product.cpu.abilist");

        if (!string.IsNullOrEmpty (abiList))
        {
          return abiList.Split (',');
        }

        return new string []
        {
          GetProperty ("ro.product.cpu.abi"),
          GetProperty ("ro.product.cpu.abi2")
        };
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
