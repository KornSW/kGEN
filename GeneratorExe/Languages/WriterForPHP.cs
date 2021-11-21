using CodeGeneration.Inspection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public class WriterForPHP : CodeWriterBase {

    public WriterForPHP(TextWriter targetWriter, CodeWritingSettings cfg) : base(targetWriter, cfg) {
    }

    public override void Import(string @namespace) {
      if (@namespace.StartsWith("System")) {
        return;
      }
      this.WriteLine($"use \\{@namespace.Replace(".", "\\")};");
    }

    public override void BeginNamespace(string name) {
      name = this.Escape(name);
      this.WriteLineAndPush($"namespace {name.Replace(".","\\")} {{");
    }

    public override void EndNamespace() {
      this.PopAndWriteLine("}");
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

    public override void BeginMethod(AccessModifier access, string methodName, string returnTypeName = null, bool isInterfaceDeclartion = false) {
      methodName = this.Escape(methodName);

      if (!this.Cfg.GenerateTypeNamesInPhp || string.IsNullOrWhiteSpace(returnTypeName)) {
        returnTypeName = "";
      }
      else {
        returnTypeName = ": " + returnTypeName;
      }

      if (isInterfaceDeclartion) {
        this.WriteLine($"function {methodName}(){returnTypeName};");
      }
      else {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}function {methodName}(){returnTypeName} {{");
      }
    }

    public override void EndMethod() {
      this.PopAndWriteLine("}");
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

    public override void Summary(string text, bool dumpToSingleLine) {
      if (string.IsNullOrWhiteSpace(text)) {
        return;
      }
      if (!dumpToSingleLine) {
        this.WriteLine($"/*");
        this.WriteLine($"* " + text.Replace("\n", "\n* "));
        this.WriteLine($"*/");
      }
      else {
        this.WriteLine($"// " + text.Replace("\n", " ").Replace("  ", " "));
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

      if (!this.Cfg.GenerateTypeNamesInPhp) {
        propType = "";
      }
      else {
        propType = propType + " ";
      }

      var line = $"{this.GetAccessModifierString(access)}{propType}${this.Escape(this.Ftl(propName))};";

      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        line = line + " = " + defaultValue + ";";
      }

      this.WriteLine(line.Trim());
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
      if (t == CommonType.Object)
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
    }

    public override void EndFile() {
    }
  }

}
