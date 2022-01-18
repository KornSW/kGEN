using CodeGeneration.Inspection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public class WriterForPHP : CodeWriterBase {

    public override bool IsDotNet {
      get {
        return false;
      }
    }

    public WriterForPHP(TextWriter targetWriter, RootCfg cfg) : base(targetWriter, cfg) {
    }

    protected override void Import(string @namespace) {

      //HACK exclude .NET wellknown-namespaces
      if (@namespace.StartsWith("System")) {
        return;
      }

      if(@namespace.EndsWith(".php") || @namespace.EndsWith(".php'")) {
        //in php we also allow files to be imported
        this.WriteLine($"include '{@namespace.Replace("'","")}';");
      }
      else {
        this.WriteLine($"use \\{@namespace.Replace(".", "\\")};");
      }
    }

    public override void BeginNamespace(string name) {
      name = this.Escape(name);
      this.WriteLineAndPush($"namespace {name.Replace(".","\\")} {{");
    }

    public override void EndNamespace() {
      this.PopAndWriteLine("}");
    }

    protected override string GetSymbolEscapingPattern() {
      return "{0}";
    }

    private string Escape(string input) {
      //in PHP is no escaping functionallity present
      return input;
    }
    public override void BeginClass(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}class {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}class {typeName} implements {inherits} {{");
      }
    }

    public override void BeginInterface(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}interface {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}interface {typeName} extends {inherits} {{");
      }
    }

    public override void EndClass() {
      this.PopAndWriteLine("}");
    }

    public override void EndInterface() {
      this.PopAndWriteLine("}");
    }

    protected override void MethodCore(AccessModifier access, string methodName, string returnTypeName = null, bool isInterfaceDeclartion = false, MethodParamDescriptor[] parameters = null) {
      methodName = this.Escape(methodName);

      if (!this.Cfg.generateTypeNamesInPhp || string.IsNullOrWhiteSpace(returnTypeName)) {
        returnTypeName = "";
      }
      else {
        returnTypeName = ": " + returnTypeName;
      }

      string prms = "";
      if (parameters != null && parameters.Any()) {
        prms = String.Join(", ", parameters.Select((p) => {
          string typeName;
          if (p.CommonType == CommonType.NotCommon) {
            typeName = p.CustomType;
          }
          else {
            typeName = this.GetCommonTypeName(p.CommonType);
          }

          string byRefPrefix = "";
          if (p.IsOut) {
            byRefPrefix = "&";
          }

          if (this.Cfg.generateTypeNamesInPhp) {
            if (p.CommonType == CommonType.NotCommon) {
              return p.CustomType + " " + byRefPrefix + "$" + p.ParamName;
            }
            else {
              return this.GetCommonTypeName(p.CommonType) + " " + byRefPrefix + "$" + p.ParamName;
            }
          }
          else {
            return byRefPrefix + "$" + p.ParamName;
          }
        }).ToArray());
      }

      if (isInterfaceDeclartion) {
        this.WriteLine($"function {methodName}({prms}){returnTypeName};");
      }
      else {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}function {methodName}({prms}){returnTypeName} {{");
      }
    }

    public override void EndMethod() {
      this.PopAndWriteLine("}");
    }

    public override void Return(string result = null) {
      this.WriteLine($"return{this.Ppnd(" ", result)};");
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

    //https://developer.wordpress.org/coding-standards/inline-documentation-standards/php/
    public override void Summary(string text, bool dumpToSingleLine, MethodParamDescriptor[] parameters = null) {
      if (string.IsNullOrWhiteSpace(text)) {
        return;
      }
      if (!dumpToSingleLine) {
        this.WriteLine($"/*");
        this.WriteLine($"* " + text.Replace("\n", "\n* "));

        if (parameters != null && parameters.Any()) {
          this.WriteLine("*");
          foreach (var paramSummary in parameters) {
            if (!string.IsNullOrWhiteSpace(paramSummary.Description)) {
              this.WriteLine($"* @param ${paramSummary.ParamName} " + paramSummary.Description.Replace("\n", " ").Replace("  ", " "));
            }
            else {
              this.WriteLine($"* @param ${paramSummary.ParamName}");
            }
          }
        }

        this.WriteLine($"*/");
      }
      else {
        this.WriteLine($"// " + text.Replace("\n", " ").Replace("  ", " "));
        //this.WriteLine($"//");
        if (parameters != null && parameters.Any()) {
          foreach (var paramSummary in parameters) {
            if (!string.IsNullOrWhiteSpace(paramSummary.Description)) {
              this.WriteLine($"// @param ${paramSummary.ParamName} " + paramSummary.Description.Replace("\n", " ").Replace("  ", " "));
            }
            else {
              this.WriteLine($"// @param ${paramSummary.ParamName}");
            }
          }
        }

      }

    }

    public override void AttributesLine(params string[] attribs) {
      if (attribs == null || attribs.Length < 1) {
        return;
      }
      this.WriteLine($"#[{string.Join(", ", attribs)}]");
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
        return "public ";//there is no internal at PHP
      }
      else if (access == AccessModifier.Absttract) {
        return "abstract ";
      }
      else {
        throw new NotImplementedException();
      }
    }

    public override void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null) {

      if (!this.Cfg.generateTypeNamesInPhp) {
        propType = "";
      }
      else {
        propType = propType + " ";
      }

      var line = $"{this.GetAccessModifierString(access)}{propType}${this.Escape(this.Ftl(propName))}";

      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        line = line + " = " + defaultValue;
      }

      this.WriteLine(line.Trim() + ";");
    }
    public override string GetCommonTypeName(CommonType t) {
      // https://www.w3schools.com/php/php_datatypes.asp
      // https://www.php.net/manual/de/language.types.declarations.php

      //  String
      //  Integer
      //Float(floating point numbers - also called double)
      //Boolean
      //Array
      //Object
      //NULL
      //Resource

      if (t == CommonType.Boolean)
        return "bool";
      if (t == CommonType.Byte)
        return "int";
      if (t == CommonType.DateTime)
        return "string";
      if (t == CommonType.Decimal)
        return "float"; //??????????????????????????
      if (t == CommonType.Double)
        return "float";
      if (t == CommonType.Guid)
        return "string";
      if (t == CommonType.Int16)
        return "int";
      if (t == CommonType.Int32)
        return "int";
      if (t == CommonType.Int64)
        return "int";
      if (t == CommonType.String)
        return "string";
      if (t == CommonType.Any)
        return "object";
      if (t == CommonType.DynamicStructure)
        return "object";
      if (t == CommonType.StringDict)
        return "object";
      return "<UNKNOWN_TYPE>";
    }

    public override void Field(string typeName, string fieldName, string defaultValue = null, bool readOnly = false) {
      string roString = "";
      if (readOnly) {
        //roString = " readonly";
      }

      this.Summary("@var " + typeName, true);
      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        this.WriteLine($"private{roString} {typeName} ${this.Ftl(fieldName)} = {defaultValue};");
      }
      else {
        this.WriteLine($"private{roString} {typeName} ${this.Ftl(fieldName)}");
      }
    }

    public override string GetGenericTypeName(string sourceTypeName, params string[] genericArguments) {
      return sourceTypeName + "<" + String.Join(", ", genericArguments) + ">";
    }

    public override string GetArrayTypeName(string sourceTypeName) {
      return "array";
      //return sourceTypeName + "[]";
    }

    public override string GetNullableTypeName(string sourceTypeName) {
      return "?" + sourceTypeName;
    }

    public override void BeginFile() {
      this.WriteLine("<?php");
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
