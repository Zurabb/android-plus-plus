﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Interop;
using AndroidPlusPlus.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public delegate int DebuggerEventDelegate (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public interface DebugEngineCallbackInterface
  {
    int OnBreakpoint (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnBreakpointBound (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnBreakpointError (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnError (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnException (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnLoadComplete (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnModuleLoad (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnOutputString (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnProgramCreate (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnProgramDestroy (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnStepComplete (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnSymbolSearch (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnThreadCreate (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);

    int OnThreadDestroy (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib);
  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebugEngineCallback : DebugEngineCallbackInterface, IDebugEventCallback2
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly IDebugEventCallback2 m_ad7EventCallback;

    private readonly CLangDebuggerCallback m_cLangEventCallback;

    private readonly JavaLangDebuggerCallback m_javaLangEventCallback;

    private readonly Dictionary<Guid, DebuggerEventDelegate> m_debuggerCallback;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebugEngineCallback (DebugEngine engine, IDebugEventCallback2 ad7EventCallback)
    {
      Engine = engine;

      m_ad7EventCallback = ad7EventCallback;

      m_cLangEventCallback = new CLangDebuggerCallback ();

      m_javaLangEventCallback = new JavaLangDebuggerCallback ();

      m_debuggerCallback = new Dictionary<Guid, DebuggerEventDelegate> ();

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.BreakpointHit)), OnBreakpoint);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.BreakpointBound)), OnBreakpointBound);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.BreakpointError)), OnBreakpointError);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.Error)), OnError);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.Exception)), OnException);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.LoadComplete)), OnLoadComplete);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.ModuleLoad)), OnModuleLoad);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.OutputString)), OnOutputString);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.ProcessCreate)), OnProgramCreate);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.ProgramDestroy)), OnProgramDestroy);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.StepComplete)), OnStepComplete);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.SymbolSearch)), OnSymbolSearch);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.ThreadCreate)), OnThreadCreate);

      m_debuggerCallback.Add (ComUtils.GuidOf (typeof (DebugEngineEvent.ThreadDestroy)), OnThreadDestroy);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebugEngine Engine { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region DebugEngineCallbackInterface Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Event (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.Print ("[DebugEngineCallback] Event: " + riidEvent.ToString ());

      try
      {
        DebuggerEventDelegate eventCallback;

        int handle = DebugEngineConstants.E_NOTIMPL;

        if (m_debuggerCallback.TryGetValue (riidEvent, out eventCallback))
        {
          handle = eventCallback (pEngine, pProcess, pProgram, pThread, pEvent, ref riidEvent, dwAttrib);
        }

        if (handle != DebugEngineConstants.E_NOTIMPL)
        {
          LoggingUtils.RequireOk (handle);
        }

        handle = m_cLangEventCallback.Event (pEngine, pProcess, pProgram, pThread, pEvent, ref riidEvent, dwAttrib);

        if (handle != DebugEngineConstants.E_NOTIMPL)
        {
          LoggingUtils.RequireOk (handle);
        }

        handle = m_javaLangEventCallback.Event (pEngine, pProcess, pProgram, pThread, pEvent, ref riidEvent, dwAttrib);

        if (handle != DebugEngineConstants.E_NOTIMPL)
        {
          LoggingUtils.RequireOk (handle);
        }

        handle = m_ad7EventCallback.Event (pEngine, pProcess, pProgram, pThread, pEvent, ref riidEvent, dwAttrib);

        if (handle != DebugEngineConstants.E_NOTIMPL)
        {
          LoggingUtils.RequireOk (handle);
        }

        // 
        // (Managed Code) It is strongly advised that ReleaseComObject be invoked on the various interfaces that are passed to IDebugEventCallback2::Event.
        // 

        /*Marshal.ReleaseComObject (pEngine);

        Marshal.ReleaseComObject (pProcess);

        Marshal.ReleaseComObject (pProgram);

        Marshal.ReleaseComObject (pThread);

        Marshal.ReleaseComObject (pEvent);*/

        return handle;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        throw;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region DebugEngineCallbackInterface Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnAsyncBreakComplete (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnBreakpoint (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnBreakpointBound (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnBreakpointError (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnError (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      // 
      // IDebugErrorEvent2 is used to report error messages to the user when something goes wrong in the debug engine.
      // 

      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnException (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnLoadComplete (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnModuleLoad (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnOutputString (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnProgramCreate (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnProgramDestroy (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnStepComplete (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnSymbolSearch (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnThreadCreate (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int OnThreadDestroy (IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

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
