using CodeGeneration.Inspection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public class WriterForTS : CodeWriterBase {

    public WriterForTS(TextWriter targetWriter, RootCfg cfg) : base(targetWriter, cfg) {
    }
    protected override void Import(string @namespace) {
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
        this.WriteLineAndPush($"export class {typeName} : {inherits} {{");
      }
    }

    public override void BeginInterface(AccessModifier access, string typeName, string inherits = null, bool partial = false) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"export interface {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"export interface {typeName} : {inherits} {{");
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
      if (string.IsNullOrWhiteSpace(returnTypeName)) {
        returnTypeName = "void";
      }
      string prms = "";
      if (parameters != null && parameters.Any()) {
        prms = String.Join(", ", parameters.Select((p) => {
          if (p.CommonType == CommonType.NotCommon) {
            return p.ParamName + " : " + p.CustomType;
          }
          else {
            return p.ParamName + " : " + this.GetCommonTypeName(p.CommonType);
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
      if (!dumpToSingleLine) {
        this.WriteLine($"/**");
        this.WriteLine($" * " + text.Replace("\n", "\n * "));
        if (parameters != null && parameters.Any()) {
          this.WriteLine(" *");
          foreach (var paramSummary in parameters) {
            this.WriteLine($" * @param {paramSummary.ParamName} " + paramSummary.Description.Replace("\n", " ").Replace("  ", " "));
          }
        }
        this.WriteLine($" */");
      }
      else {
        this.WriteLine($"// " + text.Replace("\n", " ").Replace("  ", " "));
        if (parameters != null && parameters.Any()) {
          //this.WriteLine($"//");
          foreach (var paramSummary in parameters) {
            this.WriteLine($"// @param {paramSummary.ParamName} " + paramSummary.Description.Replace("\n", " ").Replace("  ", " "));
          }
        }
      }



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

    public override void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null) {
      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        this.WriteLine($"public {this.Ftl(propName)} : {propType} = {defaultValue};");
      }
      else {
        this.WriteLine($"public {this.Ftl(propName)} : {propType};");
      }
    }

    public override string GetCommonTypeName(CommonType t) {

      if (t == CommonType.Boolean)
        return "boolean";
      if (t == CommonType.Byte)
        return "byte";
      if (t == CommonType.DateTime)
        return "date";
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
      if (t == CommonType.Object)
        return "object";
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
      return sourceTypeName + "<" + String.Join(", ", genericArguments) + ">";
    }

    public override string GetArrayTypeName(string sourceTypeName) {
      return sourceTypeName + "[]";
    }

    public override string GetNullableTypeName(string sourceTypeName) {
      //return sourceTypeName + "?"; falsch - muss dan das symbol: public page? : number = null;     public page? : number = undefined;
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
