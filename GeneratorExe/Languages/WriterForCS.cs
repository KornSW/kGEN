using CodeGeneration.Inspection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public class WriterForCS : CodeWriterBase {

    public WriterForCS(TextWriter targetWriter, CodeWritingSettings cfg) : base(targetWriter, cfg) {
    }
    public override void Import(string @namespace) {
      this.WriteLine($"using {@namespace};");
    }

    public override void BeginNamespace(string name) {
      name = this.Escape(name);
      this.WriteLineAndPush($"namespace {name} {{");
    }

    public override void EndNamespace() {
      this.PopAndWriteLine("}");
    }

    private string Escape(string input) {
      //TODO: implement escaping
      return input;
    }

    public override void BeginClass(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);   
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}class {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}class {typeName} : {inherits} {{");
      }
    }

    public override void BeginInterface(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}interface {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}interface {typeName} : {inherits} {{");
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
      if (string.IsNullOrWhiteSpace(returnTypeName)) {
        returnTypeName = "void";
      }
      if (isInterfaceDeclartion) {
        this.WriteLine($"{returnTypeName} {methodName}();");
      }
      else {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}{returnTypeName} {methodName}() {{");
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
      if(string.IsNullOrWhiteSpace(text)) {
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

    public override void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null) {

      var line = $"{this.GetAccessModifierString(access)}{propType} {this.Escape(propName)} {{ get; set; }}";

      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        line = line + " = " + defaultValue + ";";
      }

      this.WriteLine(line.Trim());
    }

    public override string GetCommonTypeName(CommonType t) {

      if (t == CommonType.Boolean)  return "bool";
      if (t == CommonType.Byte)     return "byte";
      if (t == CommonType.DateTime) return "DateTime";
      if (t == CommonType.Decimal)  return "decimal";
      if (t == CommonType.Double)   return "double";
      if (t == CommonType.Guid)     return "Guid";
      if (t == CommonType.Int16)    return "Int16";
      if (t == CommonType.Int32)    return "Int32";
      if (t == CommonType.Int64)    return "Int64";
      if (t == CommonType.String)   return "string";
      if (t == CommonType.Object)   return "Object";
      return "<UNKNOWN_TYPE>";
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
      return sourceTypeName + "?";
    }
    public override void BeginFile() {
    }
    public override void EndFile() {
    }

  }

}
