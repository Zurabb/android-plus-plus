﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

  // 
  // This class represents a document context to the debugger. A document context represents a location within a source file. 
  // 

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebuggeeDocumentContext : IDebugDocumentContext2
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected readonly DebugEngine m_engine;

    protected readonly string m_fileName;

    protected readonly TEXT_POSITION m_beginPosition;

    protected readonly TEXT_POSITION m_endPosition;

    protected readonly Guid m_languageGuid;

    protected readonly DebuggeeCodeContext m_codeContext;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeDocumentContext (DebugEngine engine, string fileName, TEXT_POSITION beginPosition, TEXT_POSITION endPosition, Guid languageGuid, DebuggeeAddress memoryAddress)
    {
      m_engine = engine;

      m_fileName = PathUtils.ConvertPathCygwinToWindows (fileName);

      m_beginPosition = beginPosition;

      m_endPosition = endPosition;

      m_languageGuid = languageGuid;

      m_codeContext = new DebuggeeCodeContext (m_engine, this, memoryAddress);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeCodeContext GetCodeContext ()
    {
      return m_codeContext;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugDocumentContext2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Compare (enum_DOCCONTEXT_COMPARE compare, IDebugDocumentContext2 [] documentContexts, uint documentContextsLength, out uint matchIndex)
    {
      // 
      // Compares this document context to a given array of document contexts.
      // Returns via 'matchIndex' the index into the 'documentContexts' array of the first document context that satisfies the comparison.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        matchIndex = 0;

        throw new NotImplementedException ();

        return DebugEngineConstants.S_OK;
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        matchIndex = 0;

        return DebugEngineConstants.E_NOTIMPL;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        matchIndex = 0;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EnumCodeContexts (out IEnumDebugCodeContexts2 enumCodeContexts)
    {
      // 
      // Retrieves a list of all code contexts associated with this document context.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        IDebugCodeContext2 [] codeContexts = new IDebugCodeContext2 [] { m_codeContext };

        enumCodeContexts = new DebuggeeCodeContext.Enumerator (codeContexts);

        if (enumCodeContexts == null)
        {
          throw new InvalidOperationException ();
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        enumCodeContexts = null;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetDocument (out IDebugDocument2 document)
    {
      // 
      // Gets the document that contains this document context.
      // This method is for those debug engines that supply documents directly to the IDE. Otherwise, this method should return E_NOTIMPL.
      // 

      LoggingUtils.PrintFunction ();

      document = null;

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetLanguageInfo (ref string languageName, ref Guid languageGuid)
    {
      // 
      // Gets the language associated with this document context.
      // 

      LoggingUtils.PrintFunction ();

      languageGuid = m_languageGuid;

      languageName = DebugEngineGuids.GetLanguageName (m_languageGuid);

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetName (enum_GETNAME_TYPE type, out string fileName)
    {
      // 
      // Gets the displayable name of the document that contains this document context.
      // 

      LoggingUtils.PrintFunction ();

      fileName = string.Empty;

      try
      {
        switch (type)
        {
          case enum_GETNAME_TYPE.GN_NAME:
          {
            // 
            // Specifies a friendly name of the document or context.
            // 

            fileName = Path.GetFileNameWithoutExtension (m_fileName);

            break;
          }

          case enum_GETNAME_TYPE.GN_FILENAME:
          {
            // 
            // Specifies the full path of the document or context.
            // 

            fileName = m_fileName;

            break;
          }

          case enum_GETNAME_TYPE.GN_BASENAME:
          {
            // 
            // Specifies a base file name instead of a full path of the document or context.
            // 

            fileName = Path.GetFileName (m_fileName);

            break;
          }

          case enum_GETNAME_TYPE.GN_MONIKERNAME:
          {
            // 
            // Specifies a unique name of the document or context in the form of a moniker.
            // 

            fileName = m_fileName;

            break;
          }

          case enum_GETNAME_TYPE.GN_URL:
          {
            // 
            // Specifies a URL name of the document or context.
            // 

            fileName = "file://" + m_fileName.Replace ("\\", "/");

            break;
          }

          case enum_GETNAME_TYPE.GN_TITLE:
          {
            // 
            // Specifies a title of the document, if one exists.
            // 

            fileName = Path.GetFileName (m_fileName);

            break;
          }

          case enum_GETNAME_TYPE.GN_STARTPAGEURL:
          {
            // 
            // Gets the starting page URL for processes.
            // 

            fileName = "file://" + m_fileName.Replace ("\\", "/");

            break;
          }
        }

        if (string.IsNullOrEmpty (fileName))
        {
          throw new InvalidOperationException ();
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetSourceRange (TEXT_POSITION [] beginPosition, TEXT_POSITION [] endPosition)
    {
      // 
      // Gets the source code range of this document context.
      // A source range is the entire range of source code, from the current statement back to just after the previous statement that contributed code. 
      // The source range is typically used for mixing source statements, including comments, with code in the disassembly window.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        beginPosition [0].dwLine = m_beginPosition.dwLine;

        beginPosition [0].dwColumn = m_beginPosition.dwColumn;

        endPosition [0].dwLine = m_endPosition.dwLine;

        endPosition [0].dwColumn = m_endPosition.dwColumn;

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetStatementRange (TEXT_POSITION [] beginPosition, TEXT_POSITION [] endPosition)
    {
      // 
      // Gets the file statement range of the document context.
      // A statement range is the range of the lines that contributed the code to which this document context refers.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        beginPosition [0].dwLine = m_beginPosition.dwLine;

        beginPosition [0].dwColumn = m_beginPosition.dwColumn;

        endPosition [0].dwLine = m_endPosition.dwLine;

        endPosition [0].dwColumn = m_endPosition.dwColumn;

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Seek (int nCount, out IDebugDocumentContext2 ppDocContext)
    {
      // 
      // Moves the document context by a given number of statements or lines.
      // 

      LoggingUtils.PrintFunction ();

      ppDocContext = null;

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
