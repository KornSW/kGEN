using CodeGeneration.Inspection;
using CodeGeneration.Languages;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace CodeGeneration.Languages {

  public abstract class CodeWriterBase {

    private TextWriter _Wtr;
    private CodeWritingSettings _Cfg;

    private int _CurrentIndentLevel = 0;
    private bool _IncompleteLine = false;

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

  public abstract bool IsDotNet { get; }

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
      if (_IncompleteLine) {
        suppressIndentOnFirstLine = true;
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

      _IncompleteLine = !(output.EndsWith(Environment.NewLine));
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
    public abstract void Enum(AccessModifier access, string typeName, Dictionary<string, int> enumValues, Dictionary<string, string> enumComments);
    public abstract void EndClass();
    public abstract void EndInterface();


    public void MethodInterface(string methodName, string returnTypeName = null,MethodParamDescriptor[] parameters = null, bool async = false) {
      this.MethodCore(AccessModifier.None, methodName, returnTypeName, true, parameters, async);
    }

    protected void BeginMethod(AccessModifier access, string methodName, string returnTypeName = null, MethodParamDescriptor[] parameters = null, bool async = false) {
      this.MethodCore(access, methodName, returnTypeName, false, parameters, async);
    }



    protected abstract void MethodCore(AccessModifier access, string methodName, string returnTypeName = null, bool isInterfaceDeclartion = false, MethodParamDescriptor[] parameters = null, bool async = false);
      
    public abstract string GenerateAnonymousTypeDeclaration(Dictionary<string,string> fieldTypesByName, bool inline);
    public abstract string GenerateAnonymousTypeInitialization(Dictionary<string, string> fieldValuesByName, bool inline);
    
    public string GetDefaultValueFromParameter(ParameterInfo param) {
      return this.GetDefaultValueFromObject(param.DefaultValue);
    }

    public abstract string GetDefaultValueFromObject(object param);

    /// <summary>
    /// arrays wont be initialized
    /// </summary>
    /// <param name="t"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public abstract bool TryGetTypespecificNullValue(Type t, out string defaultValue);

    public abstract void EndMethod();
    public abstract void Return(string result = null);

    public abstract void Assign(string target,string source, string trailingComment = null);

    public abstract void Comment(string text, bool dumpToSingleLine = false);
    public abstract void Summary(string text, bool dumpToSingleLine, MethodParamDescriptor[] parameters = null);
    public abstract void AttributesLine(params string[] attribs);

    public abstract void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null, bool makeOptional = false);

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

    public string EscapeTypeName(Type t, Func<Type,string> nsPrefixGetterForNonCommonTypes = null) {

      if(t == null) {
        return "";
      }

      if (t.IsArray) {
        Type et = t.GetElementType();
        string etName = this.EscapeTypeName(et, nsPrefixGetterForNonCommonTypes);
        return this.GetArrayTypeName(etName);
      }
      else {

        bool isNullable = (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        if (isNullable) {
          Type nt = t.GetGenericArguments()[0];
          string ntName = this.EscapeTypeName(nt, nsPrefixGetterForNonCommonTypes);
          return this.GetNullableTypeName(ntName);
        }
        else {

          CommonType ct = CommonType.String;
          if (TryResolveToCommonType(t, ref ct)) {
            return this.GetCommonTypeName(ct);
          }
          else if (t.IsConstructedGenericType) {
            Type gb = t.GetGenericTypeDefinition();
            string escapedGbTypeName = this.EscapeTypeName(gb, nsPrefixGetterForNonCommonTypes);
            escapedGbTypeName = escapedGbTypeName.Substring(0, escapedGbTypeName.IndexOf('`'));
            string[] escapedGaTypeNames = t.GetGenericArguments().Select(ga => this.EscapeTypeName(ga, nsPrefixGetterForNonCommonTypes)).ToArray();
            return this.GetGenericTypeName(escapedGbTypeName, escapedGaTypeNames);
          }

          if(nsPrefixGetterForNonCommonTypes != null) {
            return nsPrefixGetterForNonCommonTypes.Invoke(t) + t.Name;
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

    public virtual string DateTimeConstructor(DateTime target) {
      //HACK: aktual only C#
      if (target == DateTime.MinValue) {
        return "DateTime.MinValue";
      }
      else {
        return "DateTime.Parse(\"" + target.ToString() + "\")";
      }
    }

    public virtual string GuidConstructor(Guid target) {
      //HACK: aktual only C#
      if(target == Guid.Empty) {
        return "Guid.Empty";
      }
      else {
        return "Guid.Parse(\"" + target.ToString() + "\")";
      }
    }

    public virtual string StringConstructor(string target) {
      //HACK: aktual only C#
      if (target == String.Empty) {
        return "String.Empty";
      }
      else {
        return "\"" + target + "\"";
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t"></param>
    /// <param name="value"></param>
    /// <param name="parseFromString"> defines, that 'value' is alywas a string, which needs to be parsed first!!!</param>
    /// <returns></returns>
    public virtual string GetContantValue(CommonType t, object value, bool parseFromString = false) {
      if (value == null) {
        return null;
      }

      if (t == CommonType.Boolean) { 
        if (parseFromString) {
        value = bool.Parse((string)value);
        }
        if((bool)value) {
          return "true";
        }
        else {
          return "false";
        }
      }
      else if (t == CommonType.Byte) {
        if (parseFromString) {
        value = Byte.Parse((string)value);
        }
        return ((byte)value).ToString();
      }
      else if (t == CommonType.DateTime) {
        if (parseFromString) {
        value = DateTime.Parse((string)value);
        }
        return this.DateTimeConstructor((DateTime)value);

      }
      else if (t == CommonType.Decimal) {
        if (parseFromString) {
        value = Decimal.Parse((string)value);
        }
        return ((Decimal)value).ToString();

      }
      else if (t == CommonType.Double) {
        if (parseFromString) {
        value = Double.Parse((string)value);
        }
        return ((Double)value).ToString();

      }
      else if (t == CommonType.Guid) {
        if (parseFromString) {
        value = Guid.Parse((string)value);
        }
        return this.GuidConstructor((Guid)value);

      }
      else if (t == CommonType.Int16) {
        if (parseFromString) {
        value = Int16.Parse((string)value);
        }
        return ((Int16)value).ToString();

      }
      else if (t == CommonType.Int32) {
        if (parseFromString) {
        value = Int32.Parse((string)value);
        }
        return ((Int32)value).ToString();

      }
      else if (t == CommonType.Int64) {
        if (parseFromString) {
        value = Int64.Parse((string)value);
        }
        return ((Int64)value).ToString();

      }
      else if (t == CommonType.String) {

        return this.StringConstructor((String)value);

      }
      else if (t == CommonType.Any) {
        return value.ToString();

      }
      else if (t == CommonType.DynamicStructure) {
          return null;

      }
      else  if (t == CommonType.StringDict) {
        return null;
      }
      return null;
    }


    public virtual string GetNull() {
      return "null";
    }

    public abstract string GetGenericTypeName(string sourceTypeName,params string[] genericArguments);
    public abstract string GetArrayTypeName(string sourceTypeName);
    public abstract string GetNullableTypeName(string sourceTypeName);

    public static bool TryResolveToCommonType(string typeName, ref CommonType commonType) {
      object result = null;
      if(System.Enum.TryParse (typeof (CommonType), typeName,true, out result)) {
        commonType = (CommonType) result;
        return true;
      } else if (typeName == "number") {
        commonType = CommonType.Int32;
        return true;
      }
      else if (typeName == "boolean") {
        commonType = CommonType.Boolean;
        return true;
      }
      else if (typeName == "number") {
        commonType = CommonType.Int32;
        return true;
      }
      else if (typeName == "date") {
        commonType = CommonType.DateTime;
        return true;
      }
      else if (typeName == "string") {
        commonType = CommonType.String;
        return true;
      }
      return false;
    }

    public virtual bool TryResolveToCommonType(Type t, ref CommonType commonType) {
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
      else if (t == typeof(int)) {
        commonType = CommonType.Int32;
      }
      else if (t == typeof(long)) {
        commonType = CommonType.Int64;
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
        commonType = CommonType.Any;
      }
      else if (t == typeof(Dictionary<String,Object>)) {
        commonType = CommonType.DynamicStructure;
      }
      else if (t == typeof(Dictionary<String,String>)) {
        commonType = CommonType.StringDict;
      }
      else {
        if (this.ConvertGenericDotNetGenericCollectiontypesToCommonTypes() && t.IsConstructedGenericType) {
          var genBase = t.GetGenericTypeDefinition();
          var genArg1 = t.GetGenericArguments()[0];
          //if (typeof(List<>).MakeGenericType(genArg1).IsAssignableFrom(extendee)) {
          //  extendee = genArg1;
          //}
          //else if (typeof(Collection<>).MakeGenericType(genArg1).IsAssignableFrom(extendee)) {
          //  extendee = genArg1;
          //}
          if (genBase == typeof(Dictionary<,>)) {
            commonType = CommonType.DynamicStructure;
            return true;
          }
        }   
        return false;
      }
      return true;
    }

    protected virtual bool ConvertGenericDotNetGenericCollectiontypesToCommonTypes() {
      return true;
    }

    public string XmlDocToMd(string text) {
      var sb = new StringBuilder(text.Length + 1);
      var tokens = text.Replace("</see>", "°").Replace("<see", "°").Split('°');
      //< see href = "https://openid.net/specs/openid-client-initiated-backchannel-authentication-core-1_0.html" > 'CIBA-Flow' </ see >
      bool outSide = true;
      foreach (var token in tokens) {
        if (outSide) {
          sb.Append(token);
        }
        else if (token.Contains(">")) {

          int posOfContent = token.IndexOf(">") + 1;
          var title = token.Substring(posOfContent).Trim();
          var xmlDocNode = token.Substring(0, posOfContent - 1).Trim();

          //only works for hyperlinks
          if (xmlDocNode.Contains("href")) {
            var url = xmlDocNode.Substring(xmlDocNode.IndexOf("\"") + 1);
            url = url.Substring(0, url.IndexOf("\""));
            sb.Append($"[{title}]({url})");
          }
          else {
            sb.Append(title);
          }
        }
        outSide = !outSide;//toggle
      }
      return sb.ToString();
    }

  }

}
