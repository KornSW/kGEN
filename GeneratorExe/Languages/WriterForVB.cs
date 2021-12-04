using CodeGeneration.Inspection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public class WriterForVB : CodeWriterBase {

    public WriterForVB(TextWriter targetWriter, RootCfg cfg): base (targetWriter, cfg) {
    }
    protected override void Import(string @namespace) {
      this.WriteLine($"Imports {@namespace}");
    }

    public override void BeginNamespace(string name) {
      name = this.Escape(name);
      this.WriteLineAndPush("Namespace " + name);
    }

    public override void EndNamespace() {
      this.PopAndWriteLine("End Namespace");
    }

    protected override string GetSymbolEscapingPattern() {
      return "[{0}]";
    }

    private string Escape(string input) {
      //TODO: implement escaping
      return input;
    }
    public override void BeginClass(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}Class {typeName}");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}Class {typeName} Inherits {inherits}");
      }
    }

    public override void BeginInterface(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}Interface {typeName}");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}Interface {typeName} Inherits {inherits}");
      }
    }

    public override void EndClass() {
      this.PopAndWriteLine("End Class");
    }

    public override void EndInterface() {
      this.PopAndWriteLine("End Interface");
    }

    protected override void MethodCore(AccessModifier access, string methodName, string returnTypeName = null, bool isInterfaceDeclartion = false, MethodParamDescriptor[] parameters = null) {
      methodName = this.Escape(methodName);
      var keyWord = "Function";
      if (string.IsNullOrWhiteSpace(returnTypeName)) {
        keyWord = "Sub";
      }
      else{
        returnTypeName = " As " + returnTypeName;
      }
      string prms = "";
      if (parameters != null && parameters.Any()) {
        prms = String.Join(", ", parameters.Select((p) => {
          if (p.CommonType == CommonType.NotCommon) {
            return p.ParamName + " As " + p.CustomType;
          }
          else {
            return p.ParamName + " As " + this.GetCommonTypeName(p.CommonType);
          }
        }).ToArray());
      }
      if (isInterfaceDeclartion) {
        this.WriteLine($"{keyWord} {methodName}({prms}){returnTypeName};");
      }
      else {
        this.WriteLineAndPush($"{this.GetAccessModifierString(access)}{keyWord} {methodName}({prms}){returnTypeName} {{");
      }
    }

    public override void EndMethod() {
      this.PopAndWriteLine("}");
    }

    public override void Return(string result = null) {
      this.WriteLine($"Return{this.Ppnd(" ", result)}");
    }

    public override void Assign(string target, string source, string trailingComment = null) {
      this.WriteLine($"{target} = {source}{this.Ppnd(" ' ", trailingComment)}");
    }

    public override void Comment(string text, bool dumpToSingleLine = false) {
      if (string.IsNullOrWhiteSpace(text)) {
        return;
      }
      if (!dumpToSingleLine) {
        this.WriteLine($"' " + text.Replace("\n", "\n' "));
      }
      else {
        this.WriteLine($"' " + text.Replace("\n", " ").Replace("  ", " "));
      }
    }

    public override void Summary(string text, bool dumpToSingleLine, MethodParamDescriptor[] parameters = null) {
      if (string.IsNullOrWhiteSpace(text)) {
        return;
      }
      if (!dumpToSingleLine) {
        this.WriteLine($"''' <summary>");
        this.WriteLine($"''' " + text.Replace("\n", "\n''' "));
        this.WriteLine($"''' </summary>");
      }
      else {
        this.WriteLine($"''' <summary> " + text.Replace("\n", " ").Replace("  ", " ") + " </summary>");
      }

      if(parameters != null && parameters.Any()) {
        foreach (var paramSummary in parameters) {
          this.WriteLine($"''' <param name=\"{paramSummary.ParamName}\"> " + paramSummary.Description.Replace("\n", " ").Replace("  ", " ") + " </param>");
        }
      }

    }

    public override void AttributesLine(params string[] attribs) {
      if (attribs == null || attribs.Length < 1) {
        return;
      }
      this.WriteLine($"<{string.Join(", ", attribs)}>");
    }

    public override string GetAccessModifierString(AccessModifier access) {
      if (access == AccessModifier.None) {
        return "";
      }
      else if (access == AccessModifier.Private) {
        return "Private ";
      }
      else if (access == AccessModifier.Protected) {
        return "Protected ";
      }
      else if (access == AccessModifier.Public) {
        return "Public ";
      }
      else if (access == AccessModifier.Internal) {
        return "Friend ";
      }
      else if (access == AccessModifier.Absttract) {
        return "MustInherit ";
      }
      else {
        throw new NotImplementedException();
      }
    }

    public override void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null) {

      var line = $"{this.GetAccessModifierString(access)}Property {this.Escape(propName)} As {propType}";

      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        line = line + " = " + defaultValue;
      }

      this.WriteLine(line.Trim());
    }

    public override string GetNull() {
      return "Nothing";
    }

    public override string GetCommonTypeName(CommonType t) {

      if (t == CommonType.Boolean)
        return "Boolean";
      if (t == CommonType.Byte)
        return "Byte";
      if (t == CommonType.DateTime)
        return "DateTime";
      if (t == CommonType.Decimal)
        return "Decimal";
      if (t == CommonType.Double)
        return "Double";
      if (t == CommonType.Guid)
        return "Guid";
      if (t == CommonType.Int16)
        return "Int16";
      if (t == CommonType.Int32)
        return "Int32";
      if (t == CommonType.Int64)
        return "Int64";
      if (t == CommonType.String)
        return "String";
      if (t == CommonType.Object)
        return "Object";
      return "<UNKNOWN_TYPE>";
    }

    public override void Field(string typeName, string fieldName, string defaultValue = null, bool readOnly = false) {
      string roString = "";
      if (readOnly) {
        roString = " ReadOnly";
      }
      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        this.WriteLine($"Private{roString} _{this.Ftu(fieldName)} As {typeName} = {defaultValue}");
      }
      else {
        this.WriteLine($"Private{roString} _{this.Ftu(fieldName)} As {typeName}");
      }
    }

    public override string GetGenericTypeName(string sourceTypeName, params string[] genericArguments) {
      return sourceTypeName + "(Of " + String.Join(", ", genericArguments) + ")";
    }

    public override string GetArrayTypeName(string sourceTypeName) {
      return sourceTypeName + "()";
    }

    public override string GetNullableTypeName(string sourceTypeName) {
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
