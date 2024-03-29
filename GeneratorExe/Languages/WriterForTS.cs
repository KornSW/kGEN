﻿using CodeGeneration.Inspection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public class WriterForTS : CodeWriterBase {

    public override bool IsDotNet {
      get {
        return false;
      }
    }

    public WriterForTS(TextWriter targetWriter, RootCfg cfg) : base(targetWriter, cfg) {
    }
    protected override void Import(string @namespace) {

      //HACK exclude .NET wellknown-namespaces
      if (@namespace.StartsWith("System")) {
        return;
      }

      this.WriteLine($"import {@namespace};");
    }

    public override void BeginNamespace(string name) {
      name = this.Escape(name);
      this.WriteLineAndPush("namespace " + name + " {");
    }

    public override void EndNamespace() {
      this.PopAndWriteLine("}");
    }

    protected override string GetSymbolEscapingPattern() {
      return "{0}"; //unglaublicher weise ist der parse so schlau, dass wir das noch nie brauchten...
    }

    private string Escape(string input) {
      //TODO: implement escaping
      return input;
    }
    public override void BeginClass(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"export class {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"export class {typeName} extends {inherits} {{");
      }
    }

    public override void BeginInterface(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"export interface {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"export interface {typeName} extends {inherits} {{");
      }
    }

    public override void Enum(
      AccessModifier access,
      string typeName,
      Dictionary<string,int> enumValues,
      Dictionary<string,string> enumComments) {
      this.WriteLineAndPush($"export enum {typeName} {{");
      bool first = true;
      foreach (string fieldName in enumValues.Keys) {
        if (first) {
          first = false;
        }
        else {
          this.Write(",");
          this.WriteLine();
        }
        if (enumComments.ContainsKey(fieldName)) {
          this.Summary(enumComments[fieldName], true);
        }
        this.Write($"{fieldName} = {enumValues[fieldName]}");
      }
      this.WriteLine();
      this.PopAndWriteLine("}");
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
        defaultValue = "\"\"";
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
      else {
        defaultValue = null;
        return false;
      }
      return true;
    }

    public override string GetDefaultValueFromObject(object defaultValue) {
      if (defaultValue == null) {
        return this.GetNull();
      }
      else if (defaultValue.GetType() == typeof(string)) {
        return "'" + defaultValue.ToString() + "'";
      }
      else if (defaultValue.GetType() == typeof(bool)) {
        return "false";
      }
      else if (defaultValue.GetType() == typeof(int)) {
        return "0";
      }
      else {
        return defaultValue.ToString();
      }
    }

    protected override void MethodCore(AccessModifier access, string methodName, string returnTypeName = null, bool isInterfaceDeclartion = false, MethodParamDescriptor[] parameters = null, bool async = false) {
      methodName = this.Escape(methodName);

      if (string.IsNullOrWhiteSpace(returnTypeName)) {
        if (async) {
          returnTypeName = "Promise<void>";
        }
        else {
          returnTypeName = "void";
        }
      }
      else {
        if (async) {
          returnTypeName = "Promise<" + returnTypeName + ">";
        }
      }

      string prms = "";
      if (parameters != null && parameters.Any()) {
        prms = String.Join(", ", parameters.Select((p) => {

          string defaultSuffix = "";
          string paramNullableSuffix = "";
          if (p.IsOptional) {
            //in TS interfaces must not have parameter-initializers!
            if(p.DefaultValue != null && !isInterfaceDeclartion) {
              var def = this.GetDefaultValueFromObject(p.DefaultValue);
              if(def != this.GetNull()) {
                defaultSuffix = " = " + def;
              }
            }
            else {
              paramNullableSuffix = "?";
            }
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
              return p.ParamName + paramNullableSuffix + " :  { ref : " + typeName + " }" + defaultSuffix;
            }
            else {
              //OUT
             return p.ParamName + paramNullableSuffix + " : (out: " + typeName  + ") => void " + defaultSuffix;
            }
          }
          else {
            //IN
            return p.ParamName + paramNullableSuffix + " : " + typeName + defaultSuffix;
          }
        }).ToArray());
      }
      if (isInterfaceDeclartion) {
        this.WriteLine($"{this.GetAccessModifierString(access)}{methodName}({prms}) : {returnTypeName};");
      }
      else {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}{methodName}({prms}) : {returnTypeName} {{");
      }
    }

    public override void EndMethod() {
      this.PopAndWriteLine("}");
    }
    public override void Return(string result = null) {
      this.WriteLine($"return{this.Ppnd(" ", result)};");
    }

    public override void Assign(string target, string source, string trailingComment = null) {
      this.WriteLine($"{target} = {source};{this.Ppnd(" // ",trailingComment)}");
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

    //https://typedoc.org/guides/doccomments/
    public override void Summary(string text, bool dumpToSingleLine, MethodParamDescriptor[] parameters = null) {
      if (string.IsNullOrWhiteSpace(text)) {
        return;
      }

      text = this.XmlDocToMd(text);

      if (!dumpToSingleLine) {
        this.WriteLine($"/**");
        this.WriteLine($" * " + text.Replace("\n", "\n * "));
        if (parameters != null && parameters.Any()) {
          this.WriteLine(" *");
          foreach (var paramSummary in parameters) {
            if (!string.IsNullOrWhiteSpace(paramSummary.Description)) {
              this.WriteLine($" * @param {paramSummary.ParamName} " + paramSummary.Description.Replace("\n", " ").Replace("  ", " "));
            }
            else {
              this.WriteLine($" * @param {paramSummary.ParamName}");
            }
          }
        }
        this.WriteLine($" */");
      }
      else {
        this.Write($"/** " + text.Replace("\n", " ").Replace("  ", " "));
        if (parameters != null && parameters.Any()) {
          this.WriteLine();
          foreach (var paramSummary in parameters) {
            if (!string.IsNullOrWhiteSpace(paramSummary.Description)) {
              this.WriteLine($" * @param {paramSummary.ParamName} " + paramSummary.Description.Replace("\n", " ").Replace("  ", " "));
            }
            else {
              this.WriteLine($" * @param {paramSummary.ParamName}");
            }
          }
        }
        this.WriteLine($" */");
      }
    }

    public override string GenerateAnonymousTypeDeclaration(Dictionary<string, string> fieldTypesByName, bool inline) {
      var sb = new StringBuilder();
      sb.Append("{");
      bool first = true;
      foreach (var fieldName in fieldTypesByName.Keys) {
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
        var typeName = fieldTypesByName[fieldName];
        sb.Append($"{fieldName}: {typeName}");
      }
      if (!inline) {
        sb.AppendLine();
      }
      sb.Append('}');
      return sb.ToString();
    }

    public override string GenerateAnonymousTypeInitialization(Dictionary<string, string> fieldValuesByName, bool inline) {
      var sb = new StringBuilder();
      sb.Append("{");
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
        sb.Append($"{fieldName}: {value}");
      }
      if (!inline) {
        sb.AppendLine();
      }
      sb.Append("}");
      return sb.ToString();
    }

    public override void AttributesLine(params string[] attribs) {
      if (attribs == null || attribs.Length < 1) {
        return;
      }
      this.WriteLine($"[{string.Join(", ", attribs)}]");
    }

    public override string GetAccessModifierString(AccessModifier access) {
      if (access == AccessModifier.None) {
        return "";
      }
      else if (access == AccessModifier.Private) {
        return "private ";
      }
      else if (access == AccessModifier.Protected) {
        return "protected ";
      }
      else if (access == AccessModifier.Public) {
        return "public ";
      }
      else if (access == AccessModifier.Internal) {
        return "internal ";
      }
      else if (access == AccessModifier.Absttract) {
        return "absttract ";
      }
      else {
        throw new NotImplementedException();
      }
    }

    public override void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null, bool makeOptional = false) {

      if (makeOptional) {
        //in ts, the name is declaringthe nullability
        propName = propName + "?";
      }
      else if (string.IsNullOrWhiteSpace(defaultValue) || defaultValue == this.GetNull()) {
        CommonType ct = CommonType.NotCommon;
        if(CodeWriterBase.TryResolveToCommonType(propType, ref ct)) {
          defaultValue = this.GetDefaultValueFromCommonType(ct);
        }
        else if(propType.EndsWith("[]")) {
          defaultValue = "[]";
        }
        else {
          defaultValue = "new " + propType + "()";
        }
      }

      if (!string.IsNullOrWhiteSpace(defaultValue) && defaultValue != this.GetNull()) {
        this.WriteLine($"public {this.Ftl(propName)}: {propType} = {defaultValue};");
      }
      else {
        this.WriteLine($"public {this.Ftl(propName)}: {propType};");
      }

    }

    public  string GetDefaultValueFromCommonType(CommonType t) {
      if (t == CommonType.Boolean)
        return "false";
      if (t == CommonType.Byte)
        return "0";
      if (t == CommonType.DateTime)
        return "new Date(1900,1,1)";
      if (t == CommonType.Decimal)
        return "0";
      if (t == CommonType.Double)
        return "0";
      if (t == CommonType.Guid)
        return "'00000000-0000-0000-0000-000000000000'";
      if (t == CommonType.Int16)
        return "0";
      if (t == CommonType.Int32)
        return "0";
      if (t == CommonType.Int64)
        return "0";
      if (t == CommonType.String)
        return "''";
      if (t == CommonType.Any)
        return "{}";
      if (t == CommonType.DynamicStructure)
        return "{}";
      if (t == CommonType.StringDict)
        return "{}";
      return "<UNKNOWN_TYPE>";
    }

    public override string GetCommonTypeName(CommonType t) {

      if (t == CommonType.Boolean)
        return "boolean";
      if (t == CommonType.Byte)
        return "number";
      if (t == CommonType.DateTime)
        return "Date";
      if (t == CommonType.Decimal)
        return "number";
      if (t == CommonType.Double)
        return "number";
      if (t == CommonType.Guid)
        return "string";
      if (t == CommonType.Int16)
        return "number";
      if (t == CommonType.Int32)
        return "number";
      if (t == CommonType.Int64)
        return "number";
      if (t == CommonType.String)
        return "string";
      if (t == CommonType.Any)
        return "any";
      if (t == CommonType.DynamicStructure)
        return "Object";
      if (t == CommonType.StringDict)
        return "Object";
      return "<UNKNOWN_TYPE>";
    }

    public override void Field(string typeName, string fieldName, string defaultValue = null, bool readOnly = false) {
      string roString = "";
      if (readOnly) {
        roString = " readonly";
      }
      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        this.WriteLine($"private{roString} _{this.Ftu(fieldName)} : {typeName} = {defaultValue};");
      }
      else {
        this.WriteLine($"private{roString} _{this.Ftu(fieldName)} : {typeName};");
      }
    }

    public override string GetGenericTypeName(string sourceTypeName, params string[] genericArguments) {
      //HACK: in typescript there is no List or Dictionary
      if (sourceTypeName == "List" && genericArguments.Length == 1) {
        return this.GetArrayTypeName(genericArguments[0]);
      }
      else if (sourceTypeName == "Dictionary" && genericArguments.Length == 2) {
        return "Object";
      }
      else {
        return sourceTypeName + "<" + String.Join(", ", genericArguments) + ">";
      }
    }

    public override string GetArrayTypeName(string sourceTypeName) {
      return sourceTypeName + "[]";
    }

    public override string GetNullableTypeName(string sourceTypeName) {
      //return sourceTypeName + "?"; falsch - muss dann das symbol: public page? : number = null;     public page? : number = undefined;
      return sourceTypeName;
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
