using CodeGeneration.Inspection;
using CodeGeneration.Languages;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public abstract class CodeWriterBase {

    private TextWriter _Wtr;
    private CodeWritingSettings _Cfg;
    private int _CurrentIndentLevel = 0;

    public static CodeWriterBase GetForLanguage(string lang,TextWriter target, CodeWritingSettings settings) {
      if (lang == "C#" || lang == "CS") {
        return new WriterForCS(target, settings);
      }
      else if (lang == "TS") {
        return new WriterForTS(target, settings);
      }
      else if (lang == "VB") {
        return new WriterForVB(target, settings);
      }
      else if (lang == "PHP") {
        return new WriterForPHP(target, settings);
      }
      else {
        throw new Exception($"Unknown Language '{lang}' ");
      }
    }

    protected CodeWritingSettings Cfg {
      get {
        return _Cfg;
      }
    }

    protected CodeWriterBase(TextWriter targetWriter, CodeWritingSettings cfg) {
      _Wtr = targetWriter;
      _Cfg = cfg;
    }

    #region convenience

    public void PopAndWrite(string output) {
      _CurrentIndentLevel -= 1;
      this.WriteCore(output);
    }
    public void Write(string output) {
      this.WriteCore(output);
    }
    public void WriteIndented(string output) {
      _CurrentIndentLevel += 1;
      this.WriteCore(output);
      _CurrentIndentLevel -= 1;
    }
    public void WriteAndPush(string output) {
      this.WriteCore(output);
      _CurrentIndentLevel += 1;
    }
    public void PopAndWriteLine(string output) {
      _CurrentIndentLevel -= 1;
      this.WriteCore(output + Environment.NewLine);
    }
    public void WriteLine(string output = "") {
      this.WriteCore(output + Environment.NewLine);
    }
    public void WriteLineIndented(string output) {
      _CurrentIndentLevel += 1;
      this.WriteCore(output + Environment.NewLine);
      _CurrentIndentLevel -= 1;
    }
    public void WriteLineAndPush(string output) {
      this.WriteCore(output + Environment.NewLine);
      _CurrentIndentLevel += 1;
    }

    #endregion

    private void WriteCore(string output,bool suppressIndentOnFirstLine = false) {
      if(_CurrentIndentLevel < 0) {
        throw new Exception("Indent-Level < 0");
      }

      if (output.Contains(Environment.NewLine)) {
        bool endBreak = output.EndsWith(Environment.NewLine);
        using (StringReader rdr = new StringReader(output)) {
          string line = rdr.ReadLine();
          bool isFirst = true;
          while (line != null) {
            if (isFirst) {
              isFirst = false;
              if (!suppressIndentOnFirstLine) {
                _Wtr.Write(new String(' ', _Cfg.indentDepthPerLevel * _CurrentIndentLevel));
              }
            }
            else {
              _Wtr.WriteLine();
              _Wtr.Write(new String(' ', _Cfg.indentDepthPerLevel * _CurrentIndentLevel));
            }
            if (line.Length > 0) {
              _Wtr.Write(line);
            }
            line = rdr.ReadLine();
          }
          if (endBreak) {
            _Wtr.WriteLine();
          }
        }
      }
      else {
        if (!suppressIndentOnFirstLine) {
          _Wtr.Write(new String(' ', _Cfg.indentDepthPerLevel * _CurrentIndentLevel));
        }
        _Wtr.Write(output);
      }
    }

    /// <summary>
    /// FIRST TO LOWER
    /// </summary>
    public string Ftl(string input) {
      if (Char.IsUpper(input[0])) {
        input = Char.ToLower(input[0]) + input.Substring(1);
      }
      return input;
    }

    /// <summary>
    /// FIRST TO UPPER
    /// </summary>
    public string Ftu(string input) {
      if (Char.IsLower(input[0])) {
        input = Char.ToUpper(input[0]) + input.Substring(1);
      }
      return input;
    }

    public abstract void BeginFile();
    public abstract void EndFile();
    public abstract void Import(string @namespace);
    public abstract void BeginNamespace(string name);
    public abstract void EndNamespace();


    public abstract void BeginClass(AccessModifier access, string typeName, string inherits = null, bool partial = false);
    public abstract void BeginInterface(AccessModifier access, string typeName, string inherits = null, bool partial = false);

    public abstract void EndClass();
    public abstract void EndInterface();


    public abstract void BeginMethod(AccessModifier access, string methodName, string returnTypeName = null, bool isInterfaceDeclartion = false);
    public abstract void EndMethod();

    public abstract void Comment(string text, bool dumpToSingleLine = false);
    public abstract void Summary(string text, bool dumpToSingleLine);
    public abstract void AttributesLine(params string[] attribs);

    public abstract void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null);

    public string EscapeTypeName(Type t) {

      if(t == null) {
        return "";
      }

      if (t.IsArray) {
        Type et = t.GetElementType();
        string etName = this.EscapeTypeName(et);
        return this.GetArrayTypeName(etName);
      }
      else {

        bool isNullable = (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        if (isNullable) {
          Type nt = t.GetGenericArguments()[0];
          string ntName = this.EscapeTypeName(nt);
          return this.GetNullableTypeName(ntName);
        }
        else {

          CommonType ct = CommonType.String;
          if (this.TryResolveToCommonType(t,ref ct)) {
            return this.GetCommonTypeName(ct);
          }
          else {
            return t.Name;
          }

        }

      }
    }

    public void Field(CommonType t, string fieldName, string defaultValue = null ,bool readOnly = false) {
      this.Field(this.GetCommonTypeName(t), fieldName, defaultValue, readOnly);
    }
    public abstract void Field(string typeName, string fieldName, string defaultValue = null, bool readOnly = false);

    public abstract string GetAccessModifierString(AccessModifier access);
    public abstract string GetCommonTypeName(CommonType t);
    public abstract string GetGenericTypeName(string sourceTypeName,params string[] genericArguments);
    public abstract string GetArrayTypeName(string sourceTypeName);
    public abstract string GetNullableTypeName(string sourceTypeName);

    public bool TryResolveToCommonType(Type t, ref CommonType commonType) {
      if (t == typeof(string)) {
        commonType = CommonType.String;
      }
      else if (t == typeof(bool)) {
        commonType = CommonType.Boolean;
      }
      else if (t == typeof(string)) {
        commonType = CommonType.String;
      }
      else if (t == typeof(byte)) {
        commonType = CommonType.Byte;
      }
      else if (t == typeof(Int32)) {
        commonType = CommonType.Int32;
      }
      else if (t == typeof(Int16)) {
        commonType = CommonType.Int16;
      }
      else if (t == typeof(Int64)) {
        commonType = CommonType.Int64;
      }
      else if (t == typeof(Decimal)) {
        commonType = CommonType.Decimal;
      }
      else if (t == typeof(Double)) {
        commonType = CommonType.Double;
      }
      else if (t == typeof(DateTime)) {
        commonType = CommonType.DateTime;
      }
      else if (t == typeof(Guid)) {
        commonType = CommonType.Guid;
      }
      else if (t == typeof(object)) {
        commonType = CommonType.Object;
      }
      else {
        return false;
      }
      return true;
    }

  }

  [Flags]
  public enum AccessModifier {
    None = 0,
    Private = 1,
    Public = 2,
    Internal =4,
    Protected = 8,
    Absttract = 16
  }


  public enum CommonType {
    Object = 0,
    String = 1,
    Boolean = 2,
    Byte = 3,
    Int16 = 4,
    Int32 = 5,
    Int64 = 6,
    Decimal =7,
    Double = 8,
    DateTime =9,
    Guid = 10,
  }

}
