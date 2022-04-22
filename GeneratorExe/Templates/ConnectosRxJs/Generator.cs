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

namespace CodeGeneration.ConnectorsRxQs {

  public class Generator {

    public void Generate(CodeWriterBase writer, Cfg cfg) {

      if(writer.GetType() != typeof(WriterForTS)) {
        throw new Exception("For the selected template is currenty only language 'TS' supported!");
      }

      writer.WriteLine("import { Observable, Subscription, Subject, BehaviorSubject } from 'rxjs';");
      writer.WriteLine("import { map } from 'rxjs/operators';");
      writer.WriteLine("");
      writer.WriteLine($"import * as DTOs from '{cfg.ImportDtosFrom}';");
      writer.WriteLine($"import * as Models from '{cfg.ImportModelsFrom}';");
      writer.WriteLine($"import * as Interfaces from '{cfg.ImportInterfacesFrom}';");
      writer.WriteLine("");

      var nsImports = new List<string>();
      //nsImports.Add("{ Observable, Subscription, Subject, BehaviorSubject } from 'rxjs'");
      //nsImports.Add("{ map } from 'rxjs/operators';");


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

      svcInterfaces = svcInterfaces.Where((Type i) => Regex.IsMatch(i.FullName, cfg.interfaceTypeNamePattern)).ToArray();

      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace) && cfg.customImports.Contains(cfg.outputNamespace)) {
        nsImports.Remove(cfg.outputNamespace);
      }

      if (cfg.appendOwnerNameAsNamespace) {
        foreach (Type svcInt in svcInterfaces) {
          var name = svcInt.Name;
          if (cfg.removeLeadingCharCountForOwnerName > 0 && name.Length >= cfg.removeLeadingCharCountForOwnerName) {
            name = name.Substring(cfg.removeLeadingCharCountForOwnerName);
          }
          if (cfg.removeTrailingCharCountForOwnerName > 0 && name.Length >= cfg.removeTrailingCharCountForOwnerName) {
            name = name.Substring(0, name.Length - cfg.removeTrailingCharCountForOwnerName);
          }
          if (!String.IsNullOrWhiteSpace(cfg.outputNamespace)) {
            name = cfg.outputNamespace  + "." + name;
          }
          nsImports.Add(name);
        }
      }

      if (cfg.writeCustomImportsOnly) {
        nsImports.Clear();
      }
      foreach (string import in cfg.customImports.Union(nsImports).Distinct().OrderBy((s) => s)) {
        writer.RequireImport(import);
      }

      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace)) {
        writer.WriteLine();
        writer.BeginNamespace(cfg.outputNamespace);
      }

      foreach (Type svcInt in svcInterfaces) {
        string endpointName = svcInt.Name;
        if (endpointName[0] == 'I' && Char.IsUpper(endpointName[1])) {
          endpointName = endpointName.Substring(1);
        }
        string svcIntDoc = XmlCommentAccessExtensions.GetDocumentation(svcInt);

        if (cfg.appendOwnerNameAsNamespace) {
          var name = svcInt.Name;
          if (cfg.removeLeadingCharCountForOwnerName > 0 && name.Length >= cfg.removeLeadingCharCountForOwnerName) {
            name = name.Substring(cfg.removeLeadingCharCountForOwnerName);
          }
          if (cfg.removeTrailingCharCountForOwnerName > 0 && name.Length >= cfg.removeTrailingCharCountForOwnerName) {
            name = name.Substring(0, name.Length - cfg.removeTrailingCharCountForOwnerName);
          }
          writer.WriteLine();
          writer.WriteLineAndPush("namespace " + name + " {");
        }

        writer.WriteLine();
        if (!String.IsNullOrWhiteSpace(svcIntDoc)) {
          writer.Summary(svcIntDoc, false);
        }
        //writer.WriteLineAndPush($"export class {endpointName}Client implements Interfaces.{svcInt.Name} {{");
        writer.WriteLineAndPush($"export class {endpointName}Client {{");
        writer.WriteLine();
        writer.WriteLineAndPush($"constructor(");
        writer.WriteLine("private rootUrlResolver: () => string,");
        writer.WriteLine("private apiTokenResolver: () => string,");
        writer.WriteLine("private httpPostMethod: (url: string, requestObject: any, apiToken: string) => Observable<any>");
        writer.PopAndWriteLine("){}");

        writer.WriteLine();

        writer.WriteLineAndPush($"private getEndpointUrl(): string {{");
        writer.WriteLine("let rootUrl = this.rootUrlResolver();");
        writer.WriteLineAndPush($"if(rootUrl.endsWith('/')){{");
        writer.WriteLine($"return rootUrl + '{writer.Ftl(endpointName)}/';");
        writer.PopAndWriteLine("}");
        writer.WriteLineAndPush($"else{{");
        writer.WriteLine($"return rootUrl + '/{writer.Ftl(endpointName)}/';");
        writer.PopAndWriteLine("}");
        writer.PopAndWriteLine("}");

        writer.WriteLine();

        foreach (MethodInfo svcMth in svcInt.GetMethods()) {
          string svcMthDoc = XmlCommentAccessExtensions.GetDocumentation(svcMth, true);

          writer.WriteLine();
          if (String.IsNullOrWhiteSpace(svcMthDoc)) {
            svcMthDoc = svcMth.Name;
          }
          writer.Summary(svcMthDoc, false);

          var outParams = new List<Tuple<string, string>>();
          var paramSignature = new List<string>();
          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            string svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm);
            if (String.IsNullOrWhiteSpace(svcMthPrmDoc)) {
              svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm.ParameterType);
            }
            //if (!String.IsNullOrWhiteSpace(svcMthPrmDoc)) {<<  immer, sonst gibts compilerwarnings!
            //writer.WriteLine($"/// <param name=\"{svcMthPrm.Name}\"> {svcMthPrmDoc} </param>");
            //}

           Type pt = svcMthPrm.ParameterType;
            if (svcMthPrm.IsOut) {
              pt = pt.GetElementType();

              var ptName = writer.EscapeTypeName(pt, (t)=>"Models.");

              outParams.Add(new Tuple<string, string>(svcMthPrm.Name, ptName));
            }
            if (svcMthPrm.IsIn) {

              var ptName = writer.EscapeTypeName(pt, (t)=>"Models.");

              //bool nullable;
              //var ptName = pt.GetTypeNameSave(out nullable);
              //if (nullable) {
              //  ptName = ptName + "?";
              //}

              if (svcMthPrm.IsOptional) {
                //were implementing the interface "as it is"

                string defaultValueString = "";

                if (svcMthPrm.DefaultValue == null) {
                  defaultValueString = " = null";
                }
                else if(svcMthPrm.DefaultValue.GetType() == typeof(string)) {
                  defaultValueString = " = \"" + svcMthPrm.DefaultValue.ToString() + "\"";
                }
                else if (svcMthPrm.DefaultValue.GetType() == typeof(bool)) {
                  defaultValueString = " = false";
                }
                else {
                  defaultValueString = " = " + svcMthPrm.DefaultValue.ToString() + "";
                }

                paramSignature.Add($"{svcMthPrm.Name}: {ptName} = {defaultValueString}");

              }
              else {
                paramSignature.Add($"{svcMthPrm.Name}: {ptName}");
              }
            }
          }

          string returnType;
          if (svcMth.ReturnType == null || svcMth.ReturnType == typeof(void)) {
            if (outParams.Any()) {
              returnType = $"Observable<{{{String.Join(", ", outParams.Select((t)=> t.Item1 + ": " + t.Item2))}}}>";
            }
            else {
              returnType = "Observable<void>";
            }
          }
          else {
            var retTypeName = writer.EscapeTypeName(svcMth.ReturnType, (t) => "Models.");
            if (outParams.Any()) {
              returnType = $"Observable<{{{String.Join(", ", outParams.Select((t)=> t.Item1 + ": " + t.Item2))}, return: {retTypeName}}}>";
            }
            else {
              returnType = $"Observable<{retTypeName}>";
            }
          }

          writer.WriteLineAndPush($"public {writer.Ftl(svcMth.Name)}({String.Join(", ", paramSignature.ToArray())}): {returnType} {{");
         
          writer.WriteLine();

          writer.WriteLineAndPush($"let requestWrapper : DTOs.{svcMth.Name}Request = {{");
          int i = 0;
          int pCount = svcMth.GetParameters().Length;
          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            if (svcMthPrm.IsIn) {
              i++;
              if(i < pCount) {
                writer.WriteLine($"{svcMthPrm.Name}: {svcMthPrm.Name},");
              }
              else {
                writer.WriteLine($"{svcMthPrm.Name}: {svcMthPrm.Name}");
              }
            }
          }
          writer.PopAndWriteLine("};");

          writer.WriteLine();
          writer.WriteLine($"let url = this.getEndpointUrl() + '{writer.Ftl(svcMth.Name)}';");

          writer.WriteLineAndPush($"return this.httpPostMethod(url, requestWrapper, this.apiTokenResolver()).pipe(map(");
          writer.WriteLineAndPush($"(r) => {{");
          writer.WriteLine($"let responseWrapper = (r as DTOs.{svcMth.Name}Response);");

          writer.WriteLineAndPush($"if(responseWrapper.fault){{");
          writer.WriteLine($"console.warn('Request to \"' + url + '\" faulted: ' + responseWrapper.fault);");
          if (cfg.throwClientExecptionsFromFaultProperty) {
            writer.WriteLine("throw {message: responseWrapper.fault};");
          }
          writer.PopAndWriteLine("}");

          if (svcMth.ReturnType == null || svcMth.ReturnType == typeof(void)) {
            if (outParams.Any()) {
              writer.WriteLine($"return {{{String.Join(", ", outParams.Select((t) => t.Item1 + ": responseWrapper." + t.Item1))}}};");
            }
            else {
              writer.WriteLine("return;");
            }
          }
          else {
            var retTypeName = svcMth.ReturnType.Name;
            if (outParams.Any()) {
              writer.WriteLine($"return {{{String.Join(", ", outParams.Select((t) => t.Item1 + ": responseWrapper." + t.Item1))}, return: responseWrapper.return}};");
            }
            else {
              writer.WriteLine("return responseWrapper.return;");
            }
          }

         writer.PopAndWriteLine("}");
         writer.PopAndWriteLine("));");

         writer.PopAndWriteLine("}");//method

        }//foreach Method

        writer.WriteLine();
        writer.PopAndWriteLine("}"); //class

        if (cfg.appendOwnerNameAsNamespace) {
          writer.WriteLine();
          writer.PopAndWriteLine("}");
        }

      }//foreach Interface

      #region " CONNECTOR Class (Root) "

      writer.WriteLine();
      writer.WriteLineAndPush($"export class {cfg.connectorClassName} {{");

      foreach (Type svcInt in svcInterfaces) {
        string endpointName = svcInt.Name;
        if (endpointName[0] == 'I' && Char.IsUpper(endpointName[1])) {
          endpointName = endpointName.Substring(1);
        }
        writer.WriteLine();
        writer.WriteLine($"private {writer.Ftl(endpointName)}Client: {endpointName}Client;");
      }

      writer.WriteLine();

      writer.WriteLineAndPush($"constructor(");
      writer.WriteLine("private rootUrlResolver: () => string,");
      writer.WriteLine("private apiTokenResolver: () => string,");
      writer.WriteLine("private httpPostMethod: (url: string, requestObject: any, apiToken: string) => Observable<any>");
      writer.PopAndWriteLine("){");
      writer.WriteLineAndPush("");
      foreach (Type svcInt in svcInterfaces) {
        string endpointName = svcInt.Name;
        if (endpointName[0] == 'I' && Char.IsUpper(endpointName[1])) {
          endpointName = endpointName.Substring(1);
        }
        writer.WriteLine($"this.{writer.Ftl(endpointName)}Client = new {endpointName}Client(this.rootUrlResolver, this.apiTokenResolver, this.httpPostMethod);");    
      }
      writer.WriteLine();
      writer.PopAndWriteLine("}");

      writer.WriteLine();

      writer.WriteLineAndPush($"private getRootUrl(): string {{");
      writer.WriteLine("let rootUrl = this.rootUrlResolver();");
      writer.WriteLineAndPush($"if(rootUrl.endsWith('/')){{");
      writer.WriteLine("return rootUrl;");
      writer.PopAndWriteLine("}");
      writer.WriteLineAndPush($"else{{");
      writer.WriteLine("return rootUrl + '/';");
      writer.PopAndWriteLine("}");
      writer.PopAndWriteLine("}");

      foreach (Type svcInt in svcInterfaces) {

        string endpointName = svcInt.Name;
        if (endpointName[0] == 'I' && Char.IsUpper(endpointName[1])) {
          endpointName = endpointName.Substring(1);
        }
        string svcIntDoc = XmlCommentAccessExtensions.GetDocumentation(svcInt);

        writer.WriteLine();
        if (!String.IsNullOrWhiteSpace(svcIntDoc)) {
          writer.Summary(svcIntDoc, false);
        }
        //writer.WriteLine($"get {writer.Ftl(endpointName)}(): Interfaces.{svcInt.Name} {{ return \"this.{writer.Ftl(endpointName)}Client\" }}");
        writer.WriteLine($"get {writer.Ftl(endpointName)}(): {endpointName}Client {{ return this.{writer.Ftl(endpointName)}Client }}");
      }

      writer.WriteLine();
      writer.PopAndWriteLine("}"); //class

      #endregion

      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace)) {
        writer.WriteLine();
        writer.EndNamespace();
      }

    }
  }
}
