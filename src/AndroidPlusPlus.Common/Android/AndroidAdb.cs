﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public static class AndroidAdb
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public interface StateListener
    {
      void DeviceConnected (AndroidDevice device);

      void DeviceDisconnected (AndroidDevice device);

      void DevicePervasive (AndroidDevice device);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static readonly object m_updateLockMutex = new object ();

    private static Dictionary<string, AndroidDevice> m_connectedDevices = new Dictionary<string, AndroidDevice> ();

    private static List<StateListener> m_registeredDeviceStateListeners = new List<StateListener> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void Refresh ()
    {
      LoggingUtils.PrintFunction ();

      lock (m_updateLockMutex)
      {
        // 
        // Start an ADB instance, if required.
        // 

        using (SyncRedirectProcess adbStartServer = new SyncRedirectProcess (AndroidSettings.SdkRoot + @"\platform-tools\adb.exe", "start-server"))
        {
          adbStartServer.StartAndWaitForExit (30000);
        }

        using (SyncRedirectProcess adbDevices = new SyncRedirectProcess (AndroidSettings.SdkRoot + @"\platform-tools\adb.exe", "devices"))
        {
          adbDevices.StartAndWaitForExit (30000);

          // 
          // Parse 'devices' output, skipping headers and potential 'start-server' output.
          // 

          Dictionary<string, string> currentAdbDevices = new Dictionary<string, string> ();

          LoggingUtils.Print (string.Format ("[AndroidAdb] Devices output: {0}", adbDevices.StandardOutput));

          if (!String.IsNullOrEmpty (adbDevices.StandardOutput))
          {
            string [] deviceOutputLines = adbDevices.StandardOutput.Replace ("\r", "").Split (new char [] { '\n' });

            foreach (string line in deviceOutputLines)
            {
              if (Regex.IsMatch (line, "^[A-Za-z0-9.:\\-]+[\t][a-z]+$"))
              {
                string [] segments = line.Split (new char [] { '\t' });

                string deviceName = segments [0];

                string deviceType = segments [1];

                currentAdbDevices.Add (deviceName, deviceType);
              }
            }
          }

          // 
          // First identify any previously tracked devices which aren't in 'devices' output.
          // 

          HashSet<string> disconnectedDevices = new HashSet<string> ();

          foreach (string key in m_connectedDevices.Keys)
          {
            string deviceName = (string) key;

            if (!currentAdbDevices.ContainsKey (deviceName))
            {
              disconnectedDevices.Add (deviceName);
            }
          }

          // 
          // Identify whether any devices have changed state; connected/persisted/disconnected.
          // 

          foreach (KeyValuePair <string, string> devicePair in currentAdbDevices)
          {
            string deviceName = devicePair.Key;

            string deviceType = devicePair.Value;

            if (deviceType.Equals ("offline"))
            {
              disconnectedDevices.Add (deviceName);
            }
            else
            {
              AndroidDevice connectedDevice;

              if (m_connectedDevices.TryGetValue (deviceName, out connectedDevice))
              {
                // 
                // Device is pervasive. Refresh internal properties.
                // 

                LoggingUtils.Print (string.Format ("[AndroidAdb] Device pervaded: {0} - {1}", deviceName, deviceType));

                connectedDevice.Refresh ();

                foreach (StateListener deviceListener in m_registeredDeviceStateListeners)
                {
                  deviceListener.DevicePervasive (connectedDevice);
                }
              }
              else
              {
                // 
                // Device connected.
                // 

                LoggingUtils.Print (string.Format ("[AndroidAdb] Device connected: {0} - {1}", deviceName, deviceType));

                connectedDevice = new AndroidDevice (deviceName);

                connectedDevice.Refresh ();

                m_connectedDevices.Add (deviceName, connectedDevice);

                foreach (StateListener deviceListener in m_registeredDeviceStateListeners)
                {
                  deviceListener.DeviceConnected (connectedDevice);
                }
              }
            }
          }

          // 
          // Finally, handle device disconnection.
          // 

          foreach (string deviceName in disconnectedDevices)
          {
            AndroidDevice disconnectedDevice;

            if (m_connectedDevices.TryGetValue (deviceName, out disconnectedDevice))
            {
              LoggingUtils.Print (string.Format ("[AndroidAdb] Device disconnected: {0}", deviceName));

              m_connectedDevices.Remove (deviceName);

              foreach (StateListener deviceListener in m_registeredDeviceStateListeners)
              {
                deviceListener.DeviceDisconnected (disconnectedDevice);
              }
            }
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static AndroidDevice [] GetConnectedDevices ()
    {
      lock (m_connectedDevices)
      {
        AndroidDevice [] deviceArray = new AndroidDevice [m_connectedDevices.Count];

        m_connectedDevices.Values.CopyTo (deviceArray, 0);

        return deviceArray;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static SyncRedirectProcess AdbCommand (string command, string arguments)
    {
      LoggingUtils.Print (string.Format ("[AndroidDevice] AdbCommand: Cmd={0} Args={1}", command, arguments));

      SyncRedirectProcess adbCommand = new SyncRedirectProcess (AndroidSettings.SdkRoot + @"\platform-tools\adb.exe", string.Format ("{0} {1}", command, arguments));

      return adbCommand;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static SyncRedirectProcess AdbCommand (AndroidDevice target, string command, string arguments)
    {
      LoggingUtils.Print (string.Format ("[AndroidDevice] AdbCommand: Target={0} Cmd={1} Args={2}", target.ID, command, arguments));

      SyncRedirectProcess adbCommand = new SyncRedirectProcess (AndroidSettings.SdkRoot + @"\platform-tools\adb.exe", string.Format ("-s {0} {1} {2}", target.ID, command, arguments));

      return adbCommand;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static AsyncRedirectProcess AdbCommandAsync (AndroidDevice target, string command, string arguments)
    {
      LoggingUtils.Print (string.Format ("[AndroidDevice] AdbCommandAsync: Target={0} Cmd={1} Args={2}", target.ID, command, arguments));

      AsyncRedirectProcess adbCommand = new AsyncRedirectProcess (AndroidSettings.SdkRoot + @"\platform-tools\adb.exe", string.Format ("-s {0} {1} {2}", target.ID, command, arguments));

      return adbCommand;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static bool IsDeviceConnected (AndroidDevice queryDevice)
    {
      LoggingUtils.PrintFunction ();

      lock (m_connectedDevices)
      {
        return m_connectedDevices.ContainsKey (queryDevice.ID);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void RegisterDeviceStateListener (StateListener listner)
    {
      LoggingUtils.PrintFunction ();

      lock (m_registeredDeviceStateListeners)
      {
        m_registeredDeviceStateListeners.Add (listner);
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
