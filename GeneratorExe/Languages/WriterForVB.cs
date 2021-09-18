using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public class WriterForVB : CodeWriterBase {

    public WriterForVB(TextWriter targetWriter, CodeWritingSettings cfg): base (targetWriter, cfg) {
    }
    public override void WriteImport(string @namespace) {
      this.WriteLine($"Imports {@namespace}");
    }

    public override void WriteBeginNamespace(string name) {
      name = this.Escape(name);
      this.WriteLineAndPush("Namespace " + name);
    }

    public override void WriteEndNamespace() {
      this.PopAndWriteLine("End Namespace");
    }

    private string Escape(string input) {
      //TODO: implement escaping
      return input;
    }
    public override void BeginClass(string typeName, string inherits = null) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"Public Class {typeName}");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"Public Class {typeName} Inherits {inherits}");
      }
    }

    public override void BeginInterface(string typeName, string inherits = null) {
      typeName = this.Escape(typeName);
      if (string.IsNullOrWhiteSpace(inherits)) {
        this.WriteLineAndPush($"Public Interface {typeName}");
      }
      else {
        inherits = this.Escape(inherits);
        this.WriteLineAndPush($"Public Interface {typeName} Inherits {inherits}");
      }
    }

    public override void EndClass() {
      this.PopAndWriteLine("End Class");
    }

    public override void EndInterface() {
      this.PopAndWriteLine("End Interface");
    }

    public override void Summary(string text, bool multiLine) {
      if (string.IsNullOrWhiteSpace(text)) {
        return;
      }
      if (multiLine) {
        this.WriteLine($"''' <summary>");
        this.WriteLine($"''' " + text.Replace("\n", "\n''' "));
        this.WriteLine($"''' </summary>");
      }
      else {
        this.WriteLine($"''' <summary> " + text.Replace("\n", " ").Replace("  ", " ") + " </summary>");
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
        return "Private";
      }
      else if (access == AccessModifier.Protected) {
        return "Protected";
      }
      else if (access == AccessModifier.Public) {
        return "Public";
      }
      else if (access == AccessModifier.Internal) {
        return "Friend";
      }
      else if (access == AccessModifier.Absttract) {
        return "MustInherit";
      }
      else {
        throw new NotImplementedException();
      }
    }

    public override void InlineProperty(AccessModifier access, string propName, string propType, string defaultValue = null) {

      var line = $"{this.GetAccessModifierString(access)} Property {this.Escape(propName)} As {propType}";

      if (!string.IsNullOrWhiteSpace(defaultValue)) {
        line = line + " = " + defaultValue;
      }

      this.WriteLine(line.Trim());
    }

    public override string GetCommonTypeName(CommonType t) {
      throw new NotImplementedException();



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

  }

}
