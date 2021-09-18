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
    public override void WriteImport(string @namespace) {
      this.WriteLine($"#using {@namespace};");
    }

    public override void WriteBeginNamespace(string name) {
      name = this.Escape(name);
      this.WriteLineAndPush($"#namespace {name} {{");
    }

    public override void WriteEndNamespace() {
      //this.PopAndWriteLine("}");
    }

    private string Escape(string input) {
      //TODO: implement escaping
      return input;
    }
    public override void BeginClass(string typeName, string inherits = null) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"public class {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"public class {typeName} : {inherits} {{");
      }
    }

    public override void BeginInterface(string typeName, string inherits = null) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"public interface {typeName} {{");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"public interface {typeName} : {inherits} {{");
      }
    }

    public override void EndClass() {
      this.PopAndWriteLine("}");
    }

    public override void EndInterface() {
      this.PopAndWriteLine("}");
    }

    public override void Summary(string text, bool multiLine) {
      if (string.IsNullOrWhiteSpace(text)) {
        return;
      }
      if (multiLine) {
        this.WriteLine($"/**");
        this.WriteLine($"* " + text.Replace("\n", "\n* "));
        this.WriteLine($"*/");
      }
      else {
        this.WriteLine($"/** " + text.Replace("\n", " ").Replace("  ", " ") + " */");
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
        return "private";
      }
      else if (access == AccessModifier.Protected) {
        return "protected";
      }
      else if (access == AccessModifier.Public) {
        return "public";
      }
      else if (access == AccessModifier.Internal) {
        return "public";//theere is no internal at PHP
      }
      else if (access == AccessModifier.Absttract) {
        return "abstract";
      }
      else {
        throw new NotImplementedException();
      }
    }

    public override void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null) {

      var line = $"{this.GetAccessModifierString(access)} {propType} ${this.Escape(propName)};";

      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        line = line + " = " + defaultValue + ";";
      }

      this.WriteLine(line.Trim());
    }
    public override string GetCommonTypeName(CommonType t) {
      throw new NotImplementedException();



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
  }

}
