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

    public static CodeWriterBase GetForLanguage(string lang,TextWriter target,CodeWritingSettings settings) {
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
    public abstract void WriteImport(string @namespace);
    public abstract void WriteBeginNamespace(string name);
    public abstract void WriteEndNamespace();


    public abstract void BeginClass(string typeName,string inherits = null);
    public abstract void BeginInterface(string typeName, string inherits = null);

    public abstract void EndClass();
    public abstract void EndInterface();
    public abstract void Summary(string text, bool multiLine);
    public abstract void AttributesLine(params string[] attribs);

    public abstract void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null);

    public abstract string GetAccessModifierString(AccessModifier access);


    public abstract string GetCommonTypeName(CommonType t);
    public abstract string GetGenericTypeName(string sourceTypeName,params string[] genericArguments);
    public abstract string GetArrayTypeName(string sourceTypeName);
    public abstract string GetNullableTypeName(string sourceTypeName);

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
    String = 0,
    Boolean = 1,
    Byte = 2,
    Int16 = 3,
    Int32 = 4,
    Int64 = 5,
    Decimal = 6,
    Double = 7,
    DateTime = 8,
    Guid = 9
  }

}
