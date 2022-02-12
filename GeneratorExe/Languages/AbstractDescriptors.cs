﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using CodeGeneration.Languages;

namespace CodeGeneration {

  [Flags]
  public enum AccessModifier {
    None = 0,
    Private = 1,
    Public = 2,
    Internal = 4,
    Protected = 8,
    Absttract = 16
  }

  public enum CommonType {
    NotCommon = -1,
    Any = 0,
    String = 1,
    Boolean = 2,
    Byte = 3,
    Int16 = 4,
    Int32 = 5,
    Int64 = 6,
    Decimal = 7,
    Double = 8,
    DateTime = 9,
    Guid = 10,
    /// <summary> Dictionary of String,Object </summary>
    DynamicStructure = 11,
    /// <summary> Dictionary of String,String </summary>
    StringDict = 12
  }

  public class MethodParamDescriptor {

    public static MethodParamDescriptor FromParameterInfo(ParameterInfo parameterInfo, Func<Type, string> customTypeNameEscapingMethod, CodeWriterBase wtr) {

      Type pType = parameterInfo.ParameterType;
      if (parameterInfo.IsOut) {
        pType = pType.GetElementType();
      }

      CommonType t = CommonType.NotCommon;
      string ct = null;
      if (!wtr.TryResolveToCommonType(pType, ref t)) {
        ct = customTypeNameEscapingMethod.Invoke(pType);
      }

      return new MethodParamDescriptor {
        ParamName = parameterInfo.Name,
        IsOptional = parameterInfo.IsOptional,
        Description = parameterInfo.GetDocumentation(false),
        IsIn = parameterInfo.IsIn,
        IsOut = parameterInfo.IsOut,
        CommonType = t,
        CustomType = ct
      };

    }

    public MethodParamDescriptor() {
    }

    public String ParamName { get; set; } = null;
    public bool IsOptional { get; set; } = false;
    public bool IsIn { get; set; } = false;
    public bool IsOut { get; set; } = false;
    public String Description { get; set; } = null;
    public CommonType CommonType { get; set; } = CommonType.NotCommon;
    public String CustomType { get; set; } = null;

  }

}
