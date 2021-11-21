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

namespace CodeGeneration.Interfaces {

  public class Generator {

    public void Generate(CodeWriterBase writer, Cfg cfg) {

      var nsImports = new List<string>();

      var inputFileFullPath = Path.GetFullPath(cfg.inputFile);
      Program.AddResolvePath(Path.GetDirectoryName(inputFileFullPath));
      Assembly ass = Assembly.LoadFile(inputFileFullPath);

      if (!String.IsNullOrWhiteSpace(cfg.codeGenInfoHeader)) {
        string header = cfg.codeGenInfoHeader;
        header = header.Replace("{InputAssemblyVersion}", ass.GetName().Version.ToString());
        writer.Comment(header);
        writer.WriteLine();
      }

      Type[] svcInterfaces;
      try {
        svcInterfaces = ass.GetTypes();
      }
      catch (ReflectionTypeLoadException ex) {
        svcInterfaces = ex.Types.Where((t) => t != null).ToArray();
      }

      //transform patterns to regex
      cfg.interfaceTypeNamePattern = "^(" + Regex.Escape(cfg.interfaceTypeNamePattern).Replace("\\*", ".*?") + ")$";

      svcInterfaces = svcInterfaces.Where((Type i) => Regex.IsMatch(i.FullName, cfg.interfaceTypeNamePattern)).ToArray();

      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace) && cfg.customImports.Contains(cfg.outputNamespace)) {
        nsImports.Remove(cfg.outputNamespace);
      }
      foreach (string import in cfg.customImports.Union(nsImports).Distinct().OrderBy((s) => s)) {
        writer.Import(import);
      }

      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace)) {
        writer.WriteLine();
        writer.BeginNamespace(cfg.outputNamespace);
      }

      //collect models
      //var directlyUsedModelTypes = new List<Type>();
      //var wrappers = new Dictionary<String, StringBuilder>();
      foreach (Type svcInt in svcInterfaces) {

        //if(!nsImports.Contains(svcInt.Namespace)){
        //  nsImports.Add(svcInt.Namespace);
        //}
        string svcIntDoc = XmlCommentAccessExtensions.GetDocumentation(svcInt);
        string endpointName = svcInt.Name;

        if (endpointName[0] == 'I' && Char.IsUpper(endpointName[1])) {
          endpointName = endpointName.Substring(1);
        }

        writer.WriteLine();

        writer.BeginInterface(AccessModifier.Public, svcInt.Name);
        writer.WriteLine();

        foreach (MethodInfo svcMth in svcInt.GetMethods()) {
          string svcMthDoc = XmlCommentAccessExtensions.GetDocumentation(svcMth, true);

          writer.WriteLine();
          if (String.IsNullOrWhiteSpace(svcMthDoc)) {
            svcMthDoc = svcMth.Name;
          }
          writer.WriteLine($"/// <summary> {svcMthDoc} </summary>");
          writer.WriteLine($"/// <param name=\"args\"> request capsule containing the method arguments </param>");


          //writer.WriteLine($"[HttpPost(\"{writer.Ftl(svcMth.Name)}\"), Produces(\"application/json\")]");






          writer.WriteLineAndPush($"public {svcMth.Name}Response {svcMth.Name}({svcMth.Name}Request args) {{");

          var @params = new List<string>();
          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            if (svcMthPrm.IsOut) {
              if (svcMthPrm.IsIn) {
                writer.WriteLine($"response.{writer.Ftl(svcMthPrm.Name)} = args.{writer.Ftl(svcMthPrm.Name)}; //shift IN-OUT value");
              }
              @params.Add($"response.{writer.Ftl(svcMthPrm.Name)}");
            }
            else {

              if (svcMthPrm.IsOptional) {

                string defaultValueString = "";
                if (svcMthPrm.DefaultValue == null) {
                  defaultValueString = "null";
                }
                else if (svcMthPrm.DefaultValue.GetType() == typeof(string)) {
                  defaultValueString = "\"" + svcMthPrm.DefaultValue.ToString() + "\"";
                }
                else {
                  defaultValueString = svcMthPrm.DefaultValue.ToString();
                }

                if (svcMthPrm.ParameterType.IsValueType) {
                  @params.Add($"(args.{writer.Ftl(svcMthPrm.Name)}.HasValue ? args.{writer.Ftl(svcMthPrm.Name)}.Value : {defaultValueString})");
                }
                else {
                  //here 'null' will be used
                  @params.Add($"args.{writer.Ftl(svcMthPrm.Name)}");

                  //@params.Add($"(args.{writer.Ftl(svcMthPrm.Name)} == null ? args.{writer.Ftl(svcMthPrm.Name)} : {defaultValueString})");
                }
              }
              else {
                @params.Add($"args.{writer.Ftl(svcMthPrm.Name)}");
              }
            }
          }

          if (svcMth.ReturnType != null && svcMth.ReturnType != typeof(void)) {
            writer.WriteLine($"response.@return = _{endpointName}.{svcMth.Name}({Environment.NewLine + String.Join("," + Environment.NewLine, @params.ToArray()) + Environment.NewLine});");
          }
          else {
            writer.WriteLine($"_{endpointName}.{svcMth.Name}({Environment.NewLine + String.Join("," + Environment.NewLine, @params.ToArray()) + Environment.NewLine});");
          }

          writer.WriteLine($"return response;");
          writer.PopAndWriteLine("}");

          writer.WriteLineAndPush("catch (Exception ex) {");
          writer.WriteLine($"_Logger.LogCritical(ex, ex.Message);");

          writer.PopAndWriteLine("}");

          writer.EndMethod(); //method

        }//foreach Method

        writer.WriteLine();
        writer.EndInterface();

      }//foreach Interface

      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace)) {
        writer.WriteLine();
        writer.EndNamespace();
      }

    }

  }
}
