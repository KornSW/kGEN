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

namespace CodeGeneration.Clients {

  public class Generator {

    public void Generate(CodeWriterBase writer, Cfg cfg) {

      if(writer.GetType() != typeof(WriterForCS)) {
        throw new Exception("For the selected template is currenty only language 'CS' supported!");
      }

      var nsImports = new List<string>();
      nsImports.Add("Newtonsoft.Json");
      nsImports.Add("System");
      nsImports.Add("System.Net");

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

      writer.WriteLine();

      writer.WriteLineAndPush($"public partial class {cfg.connectorClassName} {{");
      writer.WriteLine();

      // CONSTRUCTOR
      writer.WriteLineAndPush($"public {cfg.connectorClassName}(string url, string apiToken) {{");
      writer.WriteLine();
      writer.WriteLineAndPush("if (!url.EndsWith(\"/\")) {");
      writer.WriteLine("url = url + \"/\";");
      writer.PopAndWriteLine("}");
      writer.WriteLine();
      // CLIENT FIELD INIT (with URL)
      foreach (Type svcInt in svcInterfaces) {
        string endpointName = svcInt.Name;
        if (cfg.removeLeadingCharCountForOwnerName > 0 && endpointName.Length >= cfg.removeLeadingCharCountForOwnerName) {
          endpointName = endpointName.Substring(cfg.removeLeadingCharCountForOwnerName);
        }
        if (cfg.removeTrailingCharCountForOwnerName > 0 && endpointName.Length >= cfg.removeTrailingCharCountForOwnerName) {
          endpointName = endpointName.Substring(0, endpointName.Length - cfg.removeTrailingCharCountForOwnerName);
        }
        writer.WriteLine($"_{endpointName}Client = new {endpointName}Client(url + \"{writer.Ftl(endpointName)}/\", apiToken);");
      }
      writer.WriteLine();
      writer.PopAndWriteLine("}");


      foreach (Type svcInt in svcInterfaces) {

        string endpointName = svcInt.Name;

        if (cfg.removeLeadingCharCountForOwnerName > 0 && endpointName.Length >= cfg.removeLeadingCharCountForOwnerName) {
          endpointName = endpointName.Substring(cfg.removeLeadingCharCountForOwnerName);
        }
        if (cfg.removeTrailingCharCountForOwnerName > 0 && endpointName.Length >= cfg.removeTrailingCharCountForOwnerName) {
          endpointName = endpointName.Substring(0, endpointName.Length - cfg.removeTrailingCharCountForOwnerName);
        }

        string svcIntDoc = XmlCommentAccessExtensions.GetDocumentation(svcInt);

        writer.WriteLine();
        writer.WriteLine($"private {endpointName}Client _{endpointName}Client = null;");
        if (!String.IsNullOrWhiteSpace(svcIntDoc)) {
          writer.WriteLine($"/// <summary> {svcIntDoc} </summary>");
        }
        writer.WriteLineAndPush($"public {svcInt.Name} {endpointName} {{");
        writer.WriteLineAndPush("get {");
        writer.WriteLine($"return _{endpointName}Client;");
        writer.PopAndWriteLine("}");
        writer.PopAndWriteLine("}");

      }
      writer.WriteLine();
      writer.PopAndWriteLine("}"); //class




      foreach (Type svcInt in svcInterfaces) {

        string endpointName = svcInt.Name;

        if (cfg.removeLeadingCharCountForOwnerName > 0 && endpointName.Length >= cfg.removeLeadingCharCountForOwnerName) {
          endpointName = endpointName.Substring(cfg.removeLeadingCharCountForOwnerName);
        }
        if (cfg.removeTrailingCharCountForOwnerName > 0 && endpointName.Length >= cfg.removeTrailingCharCountForOwnerName) {
          endpointName = endpointName.Substring(0, endpointName.Length - cfg.removeTrailingCharCountForOwnerName);
        }

        string svcIntDoc = XmlCommentAccessExtensions.GetDocumentation(svcInt);

        if (cfg.appendOwnerNameAsNamespace) {

          writer.WriteLine();
          writer.WriteLineAndPush("namespace " + endpointName + " {");
        }

        writer.WriteLine();
        if (!String.IsNullOrWhiteSpace(svcIntDoc)) {
          writer.WriteLine($"/// <summary> {svcIntDoc} </summary>");
        }
        writer.WriteLineAndPush($"internal partial class {endpointName}Client : {svcInt.Name} {{");
        writer.WriteLine();
        writer.WriteLine("private string _Url;");
        writer.WriteLine("private string _ApiToken;");
        writer.WriteLine();
        writer.WriteLineAndPush($"public {endpointName}Client(string url, string apiToken) {{");
        writer.WriteLine("_Url = url;");
        writer.WriteLine("_ApiToken = apiToken;");
        writer.PopAndWriteLine("}"); //constructor
        writer.WriteLine();
        writer.WriteLineAndPush($"private WebClient CreateWebClient() {{");
        writer.WriteLine("var wc = new WebClient();");
        //TODO: make customizable
        writer.WriteLine("wc.Headers.Set(\"" + cfg.authHeaderName + "\", _ApiToken);");
        writer.WriteLine("wc.Headers.Set(\"Content-Type\", \"application/json\");");
        writer.WriteLine("return wc;");
        writer.PopAndWriteLine("}"); //constructor

        foreach (MethodInfo svcMth in svcInt.GetMethods()) {
          string svcMthDoc = XmlCommentAccessExtensions.GetDocumentation(svcMth, true);

          writer.WriteLine();
          if (String.IsNullOrWhiteSpace(svcMthDoc)) {
            svcMthDoc = svcMth.Name;
          }
          writer.WriteLine($"/// <summary> {svcMthDoc} </summary>");

          var paramSignature = new List<string>();
          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            string svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm);
            if (String.IsNullOrWhiteSpace(svcMthPrmDoc)) {
              svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm.ParameterType);
            }
            //if (!String.IsNullOrWhiteSpace(svcMthPrmDoc)) {<<  immer, sonst gibts compilerwarnings!
              writer.WriteLine($"/// <param name=\"{svcMthPrm.Name}\"> {svcMthPrmDoc} </param>");
            //}

            Type pt = svcMthPrm.ParameterTypeSafe();
            string pfx = "";

            svcMthPrm.SwitchByDirection(
              (inParam) => pfx = "",
              (refParam) => pfx = "ref ",
              (outParam) => pfx = "out "
            );

            //bool nullable;
            //var ptName = pt.GetTypeNameSave(out nullable);
            //if (nullable) {
            //  ptName = ptName + "?";
            //}

            var ptName = writer.EscapeTypeName(pt);

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

              paramSignature.Add($"{pfx}{ptName} {svcMthPrm.Name}" + defaultValueString);

              //paramSignature.Add($"{pt} {svcMthPrm.Name} = default({ptName})");
              //if (pt.IsValueType) {
              //  paramSignature.Add($"{pfx}{ptName}? {svcMthPrm.Name} = null");
              //}
              //else {
              //  paramSignature.Add($"{pfx}{ptName} {svcMthPrm.Name} = null");
              //}
            }
            else {
              paramSignature.Add($"{pfx}{ptName} {svcMthPrm.Name}");
            }

          }

          if(svcMth.ReturnType == null || svcMth.ReturnType == typeof(void)) {
            writer.WriteLineAndPush($"public void {svcMth.Name}({String.Join(", ", paramSignature.ToArray())}) {{");
          }
          else {
            writer.WriteLineAndPush($"public {svcMth.ReturnType.Name} {svcMth.Name}({String.Join(", ", paramSignature.ToArray())}) {{");
          }

          writer.WriteLineAndPush($"using (var webClient = this.CreateWebClient()) {{");

          writer.WriteLine($"string url = _Url + \"{writer.Ftl(svcMth.Name)}\";");

          writer.WriteLineAndPush($"var requestWrapper = new {svcMth.Name}Request {{");
          int i = 0;
          int pCount = svcMth.GetParameters().Length;
          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters().InboundOnly()) {
            i++;
            if(i < pCount) {
              writer.WriteLine($"{svcMthPrm.Name} = {svcMthPrm.Name},");
            }
            else {
              writer.WriteLine($"{svcMthPrm.Name} = {svcMthPrm.Name}");
            }
          }
          writer.PopAndWriteLine("};");

          writer.WriteLine($"string rawRequest = JsonConvert.SerializeObject(requestWrapper);");
          writer.WriteLine($"string rawResponse = webClient.UploadString(url, rawRequest);");
          writer.WriteLine($"var responseWrapper = JsonConvert.DeserializeObject<{svcMth.Name}Response>(rawResponse);");

          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters().OutboundOnly()) {
            writer.WriteLine($"{svcMthPrm.Name} = responseWrapper.{svcMthPrm.Name};");
          }

          if (cfg.throwClientExecptionsFromFaultProperty) {
            writer.WriteLineAndPush($"if(responseWrapper.fault != null){{");
            writer.WriteLine($"throw new Exception(responseWrapper.fault);");
            writer.PopAndWriteLine("}");
          }

          if (svcMth.ReturnType == null || svcMth.ReturnType == typeof(void)) {
            writer.WriteLine($"return;");
          }
          //else if (cfg.throwClientExecptionsFromFaultProperty && !svcMth.ReturnType.IsNullableType()) {
          //  //wenn wir mit fault properties arbeiten, dann sind die returns
          //  //automatisch nullable, also muss heir der value gelesen werden
          //  writer.WriteLine($"return responseWrapper.@return.Value;");
          //}
          else {
            writer.WriteLine($"return responseWrapper.@return;");
          }

          writer.PopAndWriteLine("}");//using
          writer.PopAndWriteLine("}");//method

        }//foreach Method

        writer.WriteLine();
        writer.PopAndWriteLine("}"); //class

        if (cfg.appendOwnerNameAsNamespace) {
          writer.WriteLine();
          writer.PopAndWriteLine("}");
        }

      }//foreach Interface

      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace)) {
        writer.WriteLine();
        writer.EndNamespace();
      }

    }
  }
}
