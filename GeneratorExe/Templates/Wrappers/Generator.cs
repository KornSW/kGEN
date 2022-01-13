using CodeGeneration.Inspection;
using CodeGeneration.Languages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CodeGeneration.Wrappers {

  public class Generator {

    public void Generate(CodeWriterBase writer, Cfg cfg) {

      writer.RequireImport("System");
      writer.RequireImport("System.Collections.Generic");
      writer.RequireImport("System.ComponentModel.DataAnnotations");

      var inputFileFullPath = Path.GetFullPath(cfg.inputFile);
      Program.AddResolvePath(Path.GetDirectoryName(inputFileFullPath));
      Assembly ass = Assembly.LoadFile(inputFileFullPath);

      if (!String.IsNullOrWhiteSpace(writer.HeaderComment)) {
        writer.HeaderComment = writer.HeaderComment.Replace("{InputAssemblyVersion}", ass.GetName().Version.ToString());
      }

      Type[] svcInterfaces;
      try {
        svcInterfaces = ass.GetTypes();
      }
      catch (ReflectionTypeLoadException ex) {
        svcInterfaces = ex.Types.Where((t) => t != null).ToArray();
      }

      //transform patterns to regex
      if (!cfg.interfaceTypeNamePattern.StartsWith("^(")) {
        //if it is not alrady a regex, transform it to an regex:
        cfg.interfaceTypeNamePattern = "^(" + Regex.Escape(cfg.interfaceTypeNamePattern).Replace("\\*", ".*?") + ")$";
      }

      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace)) {
        writer.WriteLine();
        writer.BeginNamespace(cfg.outputNamespace);
      }

      svcInterfaces = svcInterfaces.Where((Type i) => Regex.IsMatch(i.FullName, cfg.interfaceTypeNamePattern)).ToArray();

      //collect models
      foreach (Type svcInt in svcInterfaces) {

        if (cfg.useInterfaceTypeNameToGenerateSubNamespace) {
          var name = svcInt.Name;
          if(cfg.removeLeadingCharCountForSubNamespace > 0 && name.Length >= cfg.removeLeadingCharCountForSubNamespace) {
            name = name.Substring(cfg.removeLeadingCharCountForSubNamespace);
          }
          if (cfg.removeTrailingCharCountForSubNamespace > 0 && name.Length >= cfg.removeTrailingCharCountForSubNamespace) {
            name = name.Substring(0, name.Length - cfg.removeTrailingCharCountForSubNamespace);
          }
          writer.WriteLine();
          writer.BeginNamespace(name);
        }

        string svcIntDoc = XmlCommentAccessExtensions.GetDocumentation(svcInt);

        foreach (MethodInfo svcMth in svcInt.GetMethods()) {
          string svcMthDoc = XmlCommentAccessExtensions.GetDocumentation(svcMth, false);

          if (svcMth.ReturnType != null && svcMth.ReturnType != typeof(void)) {
            //directlyUsedModelTypes.Add(svcMth.ReturnType);
            writer.RequireImport(svcMth.ReturnType.Namespace);
          }

          string reqStr = "Required";
          bool nullable;
          String pType;
          String initializer = "";






          #region REQUEST

          writer.WriteLine();

          string requestSummaryText = $"Contains arguments for calling '{svcMth.Name}'.";
          if (!String.IsNullOrWhiteSpace(svcMthDoc)) {
            requestSummaryText = requestSummaryText + "\nMethod: " + svcMthDoc;
          }
          writer.Summary(requestSummaryText, false);
          writer.BeginClass(AccessModifier.Public, svcMth.Name + "Request");

          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            string svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm);
            if (String.IsNullOrWhiteSpace(svcMthPrmDoc)) {
              svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm.ParameterType);
            }

            writer.RequireImport(svcMthPrm.ParameterType.Namespace);

            reqStr = "Required";
            if (svcMthPrm.IsOptional) {
              reqStr = "Optional";
            }

            pType = null;
            initializer = "";

            nullable = false;
            if (svcMthPrm.IsOut) {
              //pType = svcMthPrm.ParameterType.GetElementType().GetTypeNameSave(out nullable);
              nullable = svcMthPrm.ParameterType.GetElementType().IsNullableType();
              pType = writer.EscapeTypeName(svcMthPrm.ParameterType.GetElementType());
            }
            else {
              //pType = svcMthPrm.ParameterType.GetTypeNameSave(out nullable);
              nullable = svcMthPrm.ParameterType.IsNullableType();
              pType = writer.EscapeTypeName(svcMthPrm.ParameterType);
            }
   
            if (nullable) {
              initializer = writer.GetNull();
            }
            else if (svcMthPrm.IsOptional && svcMthPrm.ParameterType.IsValueType) {
              pType = writer.GetNullableTypeName(pType);
              initializer = writer.GetNull();
            }

            if (!svcMthPrm.IsOut) {
              writer.WriteLine();
              if (!String.IsNullOrWhiteSpace(svcMthPrmDoc)) {
                writer.Summary($"{reqStr} Argument for '{svcMth.Name}' ({pType.Replace("<", "(").Replace(">", ")")}): {svcMthPrmDoc}",true);
              }
              else {
                writer.Summary($"{reqStr} Argument for '{svcMth.Name}' ({pType.Replace("<", "(").Replace(">", ")")})", true);
              }
              if (!svcMthPrm.IsOptional && cfg.generateDataAnnotationsForLocalModels) {
                writer.AttributesLine("Required");
              }

              writer.InlineProperty(AccessModifier.Public, svcMthPrm.Name, pType, initializer);
              //writer.WriteLine("  public " + pType + " " + svcMthPrm.Name + " { get; set; }" + initializer);
            }

          }//foreach Param

          writer.WriteLine();
          writer.EndClass();

          #endregion 

          #region RESPONSE

          writer.WriteLine();

          string responseSummaryText = $"Contains results from calling '{svcMth.Name}'.";
          if (!String.IsNullOrWhiteSpace(svcMthDoc)) {
            responseSummaryText = responseSummaryText + "\nMethod: " + svcMthDoc;
          }
          writer.Summary(responseSummaryText, false);
          writer.BeginClass(AccessModifier.Public, svcMth.Name + "Response");

          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            string svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm);
            if (String.IsNullOrWhiteSpace(svcMthPrmDoc)) {
              svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm.ParameterType);
            }

            reqStr = "Required";
            if (svcMthPrm.IsOptional) {
              reqStr = "Optional";
            }

            pType = null;
            initializer = "";

            nullable = false;
            if (svcMthPrm.IsOut) {
              //pType = svcMthPrm.ParameterType.GetElementType().GetTypeNameSave(out nullable);
              nullable = svcMthPrm.ParameterType.GetElementType().IsNullableType();
              pType = writer.EscapeTypeName(svcMthPrm.ParameterType.GetElementType());
            }
            else {
              //pType = svcMthPrm.ParameterType.GetTypeNameSave(out nullable);
              nullable = svcMthPrm.ParameterType.IsNullableType();
              pType = writer.EscapeTypeName(svcMthPrm.ParameterType);
            }

            if (nullable) {
              initializer = writer.GetNull();
            }
            else if (svcMthPrm.IsOptional && svcMthPrm.ParameterType.IsValueType) {
              pType  = writer.GetNullableTypeName(pType);
              initializer = writer.GetNull();
            }

            if (svcMthPrm.IsOut) {
              writer.WriteLine();
              if (!String.IsNullOrWhiteSpace(svcMthPrmDoc)) {
                writer.Summary($"Out-Argument of '{svcMth.Name}' ({pType}): {svcMthPrmDoc}",true);
              }
              else {
                writer.Summary($"Out-Argument of '{svcMth.Name}' ({pType})", true);
              }
              if (!svcMthPrm.IsOptional) {
                writer.AttributesLine("Required");
              }

              writer.InlineProperty(AccessModifier.Public, svcMthPrm.Name, pType, initializer);
              //writer.WriteLine("  public " + pType + " " + svcMthPrm.Name + " { get; set; }" + initializer);
            }

          }//foreach Param

          if (cfg.generateFaultProperty) {
            writer.WriteLine();
            writer.Summary($"This field contains error text equivalent to an Exception message! (note that only 'fault' XOR 'return' can have a value != null)", true);
            writer.InlineProperty(
              AccessModifier.Public,
              "fault",
              writer.GetCommonTypeName(CommonType.String),
              writer.GetNull()
            );
            //writer.WriteLine("  public string fault { get; set; } = null;");
          }

          if (svcMth.ReturnType != null && svcMth.ReturnType != typeof(void)) {
            writer.WriteLine();
            string retTypeDoc = XmlCommentAccessExtensions.GetDocumentation(svcMth.ReturnType);
            if (!String.IsNullOrWhiteSpace(retTypeDoc)) {
              writer.Summary($"Return-Value of '{svcMth.Name}' ({svcMth.ReturnType.Name}): {retTypeDoc}", true);
            }
            else {
              writer.Summary($"Return-Value of '{svcMth.Name}' ({svcMth.ReturnType.Name})", true);
            }
            if (!cfg.generateFaultProperty) {
              writer.AttributesLine("Required");
            }
            writer.InlineProperty(
              AccessModifier.Public,
              writer.EscapeSymbolName("return"),
              writer.EscapeTypeName(svcMth.ReturnType)
            );
            //writer.WriteLine("  public " + svcMth.ReturnType.Name + " @return { get; set; }");
          }

          writer.WriteLine();
          writer.EndClass();

          #endregion

        }//foreach Method

        if (cfg.useInterfaceTypeNameToGenerateSubNamespace) {
          writer.WriteLine();
          writer.EndNamespace();
        }

      }//foreach Interface

      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace)) {
        writer.WriteLine();
        writer.EndNamespace();
      }

    }
  }
}
