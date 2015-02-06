﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class MiResultValueTuple : MiResultValue
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public MiResultValueTuple (string variable, List<MiResultValue> values)
      : base (variable)
    {
      if (values == null)
      {
        throw new ArgumentNullException ("values");
      }

      m_valueList = values;

      // 
      // Build a searchable dictionary of available result variables (fields).
      // 

      m_fieldDictionary = new Dictionary<string, List <MiResultValue>> ();

      foreach (MiResultValue value in m_valueList)
      {
        List <MiResultValue> fieldList;

        if (!m_fieldDictionary.TryGetValue (value.Variable, out fieldList))
        {
          fieldList = new List <MiResultValue> ();
        }

        fieldList.Add (value);

        m_fieldDictionary [value.Variable] = fieldList;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override string ToString ()
    {
      StringBuilder valueListBuilder = new StringBuilder (Variable);

      valueListBuilder.Append ("={");

      foreach (MiResultValue value in m_valueList)
      {
        valueListBuilder.Append (value.ToString ());
      }

      valueListBuilder.Append ("}");

      return valueListBuilder.ToString ();
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
