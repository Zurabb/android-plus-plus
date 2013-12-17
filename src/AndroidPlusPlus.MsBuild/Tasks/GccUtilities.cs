﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Win32;
using Microsoft.Build.Utilities;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public static class GccUtilities
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public const int CommandLineLength = 8191;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertPathWindowsToPosix (string path)
    {
      // 
      // Convert Windows path in to a Cygwin path suitable for passing to GCC command line.
      // 

      string rtn = path.Replace ('\\', '/');

      return QuoteIfNeeded (rtn);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertPathPosixToWindows (string path)
    {
      // 
      // Convert a Cygwin path in to a Windows path.
      // 

      StringBuilder workingBuffer = new StringBuilder (path);

      workingBuffer.Replace ('/', '\\');

      workingBuffer.Replace ("\\\\", "\\");

      return workingBuffer.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string QuoteIfNeeded (string arg)
    {
      // 
      // Add quotes around a string, if they are needed.
      // 

      var match = arg.IndexOfAny (new char [] { ' ', '\t', ';', '&' }) != -1;

      if (!match)
      {
        return arg;
      }

      return "\"" + arg + "\"";
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertGccOutputToVS (string line)
    {
      // 
      // Parse and reformat GCC error and warning output into a Visual Studio 'jump to line' style.
      // 
      //    CppSource/demo.c:51: error: conflicting types for 'seedRandom'
      // becomes:
      //    c:\Projects\san-angeles\CppSource\demo.c(51) error: conflicting types for 'seedRandom'
      // 

      string [] GCC_REGEX_ERROR_MATCH = 
      {
        @"^\s*In file included from (.?.?[^:]*.*?):([1-9]\d*):(.*$)",   // "In file included from CppSource/demo.c:32:"
        @"^\s*(.?.?[^:]*.*?):([1-9]\d*):([1-9]\d*):(.*$)",              // "CppSource/importgl.c:25:17: error: new.h: No such file or directory"
        @"^\s*(.?.?[^:]*.*?):([1-9]\d*):(.*$)",                         // "CppSource/demo.c:51: error: conflicting types for 'seedRandom'"
        @"^\s*(.?.?[^:]*.*?):(.?.?[^:]*.*?):([1-9]\d*):(.*$)",          // "Android/Debug/app-android.o:C:\Projects\vs-android_samples\san-angeles/CppSource/app-android.c:38: first defined here"
      };

      string [] GCC_REGEX_FILENAME_GROUP = 
      {
        @"$1",
        @"$1",
        @"$1",
        @"$2",
      };

      string [] GCC_REGEX_ERROR_TO_VS_REPLACE = 
      {
        @"($2): includes this header: $3",
        @"($2,$3): $4",
        @"($2): $3",
        @"($3): '$1' $4",
      };

      for (int i = 0; i < GCC_REGEX_ERROR_MATCH.Length; ++i)
      {
        Regex regExMatcher = new Regex (GCC_REGEX_ERROR_MATCH [i]);

        if (regExMatcher.IsMatch (line))
        {
          string filename = regExMatcher.Replace (line, GCC_REGEX_FILENAME_GROUP [i]);

          filename = ConvertPathPosixToWindows (filename);

          string description = regExMatcher.Replace (line, GCC_REGEX_ERROR_TO_VS_REPLACE [i]);

          try
          {
            filename = Path.GetFullPath (filename);
          }
          catch (Exception)
          {
            // Not really concerned if this fails at the moment.
          }

          return filename + description;
        }
      }

      return line;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class DependencyParser
    {

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      private ITaskItem m_outputFile = new TaskItem ();

      private List<ITaskItem> m_dependencies = new List<ITaskItem> ();

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      public DependencyParser (string dependencyFile)
      {
        using (TextReader reader = new StreamReader (dependencyFile, Encoding.ASCII))
        {
          Parse (reader);
        }
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      public List<ITaskItem> Dependencies
      {
        get
        {
          return m_dependencies;
        }
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      private void Parse (TextReader reader)
      {
        // 
        // Parse first line(s) for the target output file.
        // 

        string line = reader.ReadLine ();

        if ((m_dependencies.Count == 0) && (line.Contains (": ")))
        {
          // 
          // Parse traditional GCC style dependency headers. First dependency is seperated by ': ' with first dependency on the same or a new line.
          // 
          //  AndroidMT/Debug/native-media-jni.obj: \
          //   C:/Users/Justin/documents/visual\ studio\ 2010/Projects/native-media/native-media/jni/native-media-jni.c \
          // 

          string [] segments = line.Split (new string [] { ": " }, StringSplitOptions.RemoveEmptyEntries);

          m_outputFile = new TaskItem (segments [0]);

          if (segments [1] != "\\")
          {
            line = segments [1];
          }
          else
          {
            line = reader.ReadLine ();
          }
        }
        else
        {
          // 
          // Parse Java style dependency headers. First dependency is seperated by a new line and ': ', with whitespace padding.
          // 
          //  AndroidMT\Debug\gen\R.java \
          //   : C:\Users\Justin\documents\visual studio 2010\Projects\native-media\native-media\res\drawable\icon.png \
          // 

          line = line.Substring (0, line.Length - 1); // clear trailing '\'

          line.Trim ();

          m_outputFile = new TaskItem (line);

          string line2 = reader.ReadLine ();

          line2 = line2.Substring (0, line2.Length - 1); // clear trailing '\'

          line2.Trim ();

          string [] segments = line2.Split (new string [] { ": " }, StringSplitOptions.RemoveEmptyEntries);

          line = segments [0];
        }

        // 
        // Parse each line for more dependencies.
        // 

        while (!string.IsNullOrWhiteSpace (line))
        {
          ParseDependencyLine (line);

          line = reader.ReadLine ();
        }

        reader.Close ();
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      private void ParseDependencyLine (string line)
      {
        if (string.IsNullOrWhiteSpace (line))
        {
          return;
        }

        // 
        // Remove a trailing '\' character used to signify the list continues on the next line.
        // 

        if (line.EndsWith (@"\"))
        {
          line = line.Substring (0, line.Length - 1);
        }

        line.Trim ();

        // 
        // Now iterate through the line which can contain zero or more dependencies.
        // Although they are seperated by spaces we can't use Split() here since filenames
        // can also contain spaces that are escaped using backslash.  For some reason only
        // spaces are escaped.  Even literal backslash chars don't need escaping.
        // 

        while ((line.Length > 0) && (!string.IsNullOrWhiteSpace (line)))
        {
          int end = FindEndOfFilename (line);

          string filename = line.Substring (0, end);

          if (!string.IsNullOrWhiteSpace (filename))
          {
            filename = GccUtilities.ConvertPathPosixToWindows (filename);

            // 
            // Files with spaces in look like this:
            //  C:\Program\ Files\ (x86)\ARM\Mali\ Developer\ Tools\OpenGL\ ES\ 1.1\ Emulator\ v1.0\include/GLES2/gl2ext.h \
            // 

            filename = filename.Replace ("\\ ", " ");

            m_dependencies.Add (new TaskItem (filename));
          }

          if (end == line.Length)
          {
            break;
          }

          line = line.Substring (end + 1).Trim ();
        }
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      private static int FindEndOfFilename (string line)
      {
        // 
        // Search line for an unescaped space character (which represents the end of file), or EOF.
        // 

        int i;

        bool escapedSequence = false;

        for (i = 0; i < line.Length; ++i)
        {
          if (line [i] == '\\')
          {
            escapedSequence = true;
          }
          else if ((line [i] == ' ') && !escapedSequence)
          {
            break;
          }
          else if (escapedSequence)
          {
            escapedSequence = false;
          }
        }

        return i;
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

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////