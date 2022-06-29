using CodeGeneration.Inspection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public class WriterForCS : CodeWriterBase {

    public override bool IsDotNet {
      get {
        return true;
      }
    }

    public WriterForCS(TextWriter targetWriter, RootCfg cfg) : base(targetWriter, cfg) {
    }
    protected override void Import(string @namespace) {
      this.WriteLine($"using {@namespace};");
    }

    public override void BeginNamespace(string name) {
      name = this.Escape(name);
      this.WriteLineAndPush($"namespace {name} {{");
    }

    public override void EndNamespace() {
      this.PopAndWriteLine("}");
    }

    protected override string GetSymbolEscapingPattern() {
      return "@{0}";
    }

    private string Escape(string input) {
      //TODO: implement escaping
      return input;
    }

    public override void BeginClass(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);
      var partialString = "";
      if (partial) {
        partialString = "partial ";
      }
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}{partialString}class {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}{partialString}class {typeName} : {inherits} {{");
      }
    }

    public override void BeginInterface(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      var partialString = "";
      if (partial) {
        partialString = "partial ";
      }
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}{partialString}interface {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}{partialString}interface {typeName} : {inherits} {{");
      }
    }

    public override void EndClass() {
      this.PopAndWriteLine("}");
    }

    public override void EndInterface() {
      this.PopAndWriteLine("}");
    }

    public override bool TryGetTypespecificNullValue(Type t, out string defaultValue) {
      if (t.IsNullableType()) {
        defaultValue = this.GetNull();
      }
      else if (t.IsArray) {
        defaultValue = this.GetNull();
      }
      else if (t == typeof(string)) {
        defaultValue = this.GetNull();
      }
      else if (t == typeof(bool)) {
        defaultValue = "false";
      }
      else if (t == typeof(int)) {
        defaultValue = "0";
      }
      else if (t == typeof(decimal)) {
        defaultValue = "0M";
      }
      else if (t == typeof(DateTime)) {
        defaultValue = "new DateTime(1900,01,01)";
      }
      else if (t == typeof(Guid)) {
        defaultValue = "Guid.Empty";
      }
      else if(!t.IsValueType) {
        defaultValue = this.GetNull();
      }
      else {
        defaultValue = null;
        return false;
      }
      return true;
    }

    public override string GetDefaultValueFromObject(object defaultValue) {
      if (defaultValue == null) {
        return "null";
      }
      else if (defaultValue.GetType() == typeof(string)) {
        return "\"" + defaultValue.ToString() + "\"";
      }
      else if (defaultValue.GetType() == typeof(bool)) {
        return "false";
      }
      else {
        return defaultValue.ToString();
      }
    }

    protected override void MethodCore(AccessModifier access, string methodName, string returnTypeName = null, bool isInterfaceDeclartion = false, MethodParamDescriptor[] parameters = null, bool async = false) {
      methodName = this.Escape(methodName);
      if (string.IsNullOrWhiteSpace(returnTypeName)) {
        if (async) {
          returnTypeName = "Task";
        }
        else {
          returnTypeName = "void";
        }
      }
      else {
        if (async) {
          returnTypeName = "Task<" + returnTypeName + ">";
        }
      }

      var asyncPrefix = "";
      if (async) {
        asyncPrefix = "async ";
      }

      string prms = "";
      if (parameters != null && parameters.Any()) {
        prms = String.Join(", ", parameters.Select((p) => {

          string defaultSuffix = "";
          if (p.IsOptional) {
            defaultSuffix = " = " +  this.GetDefaultValueFromObject(p.DefaultValue);
          }

          string typeName;
          if (p.CommonType == CommonType.NotCommon) {
            typeName = p.CustomType;
          }
          else {
            typeName = this.GetCommonTypeName(p.CommonType);
          }
          if (p.IsOutbound) {
            if (p.IsInbound) {
              //REF
              return "ref " + typeName + " " + p.ParamName + defaultSuffix;
            }
            else {
              //OUT
              return "out " + typeName + " " + p.ParamName + defaultSuffix;
            }
          }
          else {
            //IN
            return typeName + " " + p.ParamName + defaultSuffix;
          }
        }).ToArray());
      }

      if (isInterfaceDeclartion) {
        this.WriteLine($"{asyncPrefix}{returnTypeName} {methodName}({prms});");
      }
      else {
        this.WriteLineAndPush($"{asyncPrefix}{this.GetAccessModifierString(access)}{returnTypeName} {methodName}({prms}) {{");
      }
    }

    public override void Return(string result = null) {
      this.WriteLine($"return{this.Ppnd(" ", result)};");
    }

    public override void EndMethod() {
      this.PopAndWriteLine("}");
    }

    public override void Assign(string target, string source, string trailingComment = null) {
      this.WriteLine($"{target} = {source};{this.Ppnd(" // ", trailingComment)}");
    }

    public override void Comment(string text, bool dumpToSingleLine = false) {
      if (string.IsNullOrWhiteSpace(text)) {
        return;
      }
      if (!dumpToSingleLine) {
        this.WriteLine($"/* " + text.Replace("\n", "\n   ") + " */");
      }
      else {
        this.WriteLine($"// " + text.Replace("\n", " ").Replace("  ", " "));
      }
    }
    public override void Summary(string text, bool dumpToSingleLine, MethodParamDescriptor[] parameters = null) {
      if (string.IsNullOrWhiteSpace(text)) {
        return;
      }
      if (!dumpToSingleLine) {
        this.WriteLine($"/// <summary>");
        this.WriteLine($"/// " + text.Replace("\n", "\n/// "));
        this.WriteLine($"/// </summary>");
      }
      else {
        this.WriteLine($"/// <summary> " + text.Replace("\n", " ").Replace("  ", " ") + " </summary>");
      }

      if (parameters != null && parameters.Any()) {
        foreach (var paramSummary in parameters) {
          if (!string.IsNullOrWhiteSpace(paramSummary.Description)) {
            this.WriteLine($"/// <param name=\"{paramSummary.ParamName}\"> " + paramSummary.Description.Replace("\n", " ").Replace("  ", " ") + " </param>");
          }
          else {
            this.WriteLine($"/// <param name=\"{paramSummary.ParamName}\"> </param>");
          }
        }
      }

    }

    public override string GenerateAnonymousTypeDeclaration(Dictionary<string, string> fieldTypesByName, bool inline) {
      return "System.Object";
    }

    public override string GenerateAnonymousTypeInitialization(Dictionary<string, string> fieldValuesByName, bool inline) {
      var sb = new StringBuilder();
      sb.Append("new {");
      bool first = true;
      foreach (var fieldName in fieldValuesByName.Keys) {
        if (first) {
          first = false;
        }
        else {
          sb.Append(", ");
        }
        if (!inline) {
          sb.AppendLine();
          sb.Append(new string(' ', this.Cfg.indentDepthPerLevel));
        }
        var value = fieldValuesByName[fieldName];
        sb.Append($"{fieldName} = {value}");
      }
      if (!inline) {
        sb.AppendLine();
      }
      sb.Append('}');
      return sb.ToString();
    }

    public override void AttributesLine(params string[] attribs) {
      if(attribs == null || attribs.Length < 1) {
        return;
      }
      this.WriteLine($"[{string.Join(", ", attribs)}]");
    }

    public override string GetAccessModifierString(AccessModifier access) {
      if(access == AccessModifier.None) { return ""; }
      else if (access == AccessModifier.Private) {        return "private ";      }
      else if (access == AccessModifier.Protected) {        return "protected ";      }
      else if (access == AccessModifier.Public) {        return "public ";      }
      else if (access == AccessModifier.Internal) {        return "internal ";      }
      else if (access == AccessModifier.Absttract) {        return "abstract ";      }
      else { throw new NotImplementedException();
      }
    }

    public override void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null, bool makeOptional = false) {

      if (makeOptional && string.IsNullOrWhiteSpace(defaultValue)) {
        //wenn es keinen default gibt, dann müssen wir den typ zu nullable machen
        //propType = this.GetNullableTypeName(propType);
      }

      var line = $"{this.GetAccessModifierString(access)}{propType} {this.Escape(propName)} {{ get; set; }}";

      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        line = line + " = " + defaultValue + ";";
      }

      this.WriteLine(line.Trim());
    }

    public override string GetCommonTypeName(CommonType t) {

      if (t == CommonType.Boolean)
        return "bool";
      if (t == CommonType.Byte)
        return "byte";
      if (t == CommonType.DateTime)
        return "DateTime";
      if (t == CommonType.Decimal)
        return "decimal";
      if (t == CommonType.Double)
        return "double";
      if (t == CommonType.Guid)
        return "Guid";
      if (t == CommonType.Int16)
        return "Int16";
      if (t == CommonType.Int32)
        return "Int32";
      if (t == CommonType.Int64)
        return "Int64";
      if (t == CommonType.String)
        return "string";
      if (t == CommonType.Any)
        return "Object";
      if (t == CommonType.DynamicStructure)
        return "Dictionary<String,Object>";
      if (t == CommonType.StringDict)
        return "Dictionary<String,String>";
      return "<UNKNOWN_TYPE>";
    }

    protected override bool ConvertGenericDotNetGenericCollectiontypesToCommonTypes() {
      return false;
    }

    public override void Field(string typeName, string fieldName, string defaultValue = null, bool readOnly = false) {
      string roString = "";
      if (readOnly) {
        roString = " readonly";
      }
      if(!string.IsNullOrWhiteSpace(defaultValue)) {
        this.WriteLine($"private{roString} {typeName} _{this.Ftu(fieldName)} = {defaultValue};");
      }
      else {
        this.WriteLine($"private{roString} {typeName} _{this.Ftu(fieldName)};");
      }
    }


    public override string GetGenericTypeName(string sourceTypeName, params string[] genericArguments) {
      return sourceTypeName + "<" + String.Join(", ", genericArguments) + ">";
    }

    public override string GetArrayTypeName(string sourceTypeName) {
      return sourceTypeName + "[]";
    }

    public override string GetNullableTypeName(string sourceTypeName) {
      if (sourceTypeName.EndsWith("?")) {
        return sourceTypeName;
      }
      return sourceTypeName + "?";
    }
    public override void BeginFile() {
      if (!String.IsNullOrWhiteSpace(this.HeaderComment)) {
        this.Comment(this.HeaderComment);
        this.WriteLine();
      }
      foreach (var ns in this.NamespacesToImport) {
        this.Import(ns);
      }
    }
    public override void EndFile() {
    }

  }

}
