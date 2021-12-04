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

    private List<String> _NamespacesToImport = new List<String>();

    protected String[] NamespacesToImport {
      get {
        return _NamespacesToImport.OrderBy((n)=> n).Distinct().ToArray();
      }
    }

    public String HeaderComment { get; set; } = "";

    //HACK: der state muss in context ausgelagert und mehreren writern bereitgestellt werden
    internal void CopyStateTo(CodeWriterBase target) {
      target._NamespacesToImport = _NamespacesToImport;
      target.HeaderComment = this.HeaderComment;
    }

    public void RequireImport(string namepsace) {
      if (!_NamespacesToImport.Contains(namepsace)) {
        _NamespacesToImport.Add(namepsace);
      }
    }

    public static CodeWriterBase GetForLanguage(string lang,TextWriter target, RootCfg settings) {
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

    protected CodeWriterBase(TextWriter targetWriter, RootCfg cfg) {
      _Wtr = targetWriter;
      _Cfg = cfg;
      this.HeaderComment = cfg.codeGenInfoHeader;

      foreach (var ns in cfg.customImports) {
        _NamespacesToImport.Add(ns);
      }

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
    protected abstract void Import(string @namespace);
    public abstract void BeginNamespace(string name);
    public abstract void EndNamespace();


    public abstract void BeginClass(AccessModifier access, string typeName, string inherits = null, bool partial = false);
    public abstract void BeginInterface(AccessModifier access, string typeName, string inherits = null, bool partial = false);

    public abstract void EndClass();
    public abstract void EndInterface();

    public void MethodInterface(string methodName, string returnTypeName = null,MethodParamDescriptor[] parameters = null) {
      this.MethodCore(AccessModifier.None, methodName, returnTypeName, true, parameters);
    }

    protected void BeginMethod(AccessModifier access, string methodName, string returnTypeName = null, MethodParamDescriptor[] parameters = null) {
      this.MethodCore(access, methodName, returnTypeName, false, parameters);
    }

    protected abstract void MethodCore(AccessModifier access, string methodName, string returnTypeName = null, bool isInterfaceDeclartion = false, MethodParamDescriptor[] parameters = null);

    public abstract void EndMethod();
    public abstract void Return(string result = null);

    public abstract void Assign(string target,string source, string trailingComment = null);

    public abstract void Comment(string text, bool dumpToSingleLine = false);
    public abstract void Summary(string text, bool dumpToSingleLine, MethodParamDescriptor[] parameters = null);
    public abstract void AttributesLine(params string[] attribs);

    public abstract void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null);

    protected string Ppnd(string stringToPrepend, string stringToWrite) {
      if(string.IsNullOrWhiteSpace(stringToWrite)) {
        return "";
      }
      return stringToPrepend + stringToWrite;
    }

    protected virtual string[] GetLangSpecificKeywordsToEscape() {
      return new String[] { };
    }

    protected abstract string GetSymbolEscapingPattern();
    private String[] _KeyWords = new String[] {
      "return", "if", "var" , "next", "for", "loop", "class", "namespace", "interface"
    };
    public string EscapeSymbolName(string name) { 
      if(this.GetLangSpecificKeywordsToEscape().Union(_KeyWords).Contains(name)) {
        return this.GetSymbolEscapingPattern().Replace("{0}", name);
      }
      return name;
    }

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
          if (TryResolveToCommonType(t,ref ct)) {
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

    public virtual string GetNull() {
      return "null";
    }

    public abstract string GetGenericTypeName(string sourceTypeName,params string[] genericArguments);
    public abstract string GetArrayTypeName(string sourceTypeName);
    public abstract string GetNullableTypeName(string sourceTypeName);

    public static bool TryResolveToCommonType(Type t, ref CommonType commonType) {
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

}
