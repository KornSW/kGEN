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

namespace CodeGeneration.MvcControllers {

  public class Generator {

    public void Generate(CodeWriterBase writer, Cfg cfg) {

      if(writer.GetType() != typeof(WriterForCS)) {
        throw new Exception("For the selected template is currenty only language 'CS' supported!");
      }

      var nsImports = new List<string>();
      nsImports.Add("Microsoft.AspNetCore.Mvc");
      nsImports.Add("Microsoft.Extensions.Logging");
      nsImports.Add("Security");
      if (cfg.generateSwashbuckleAttributesForControllers) {
        nsImports.Add("Swashbuckle.AspNetCore.Annotations");
      }
      nsImports.Add("System");
      nsImports.Add("System.Collections.Generic");
      nsImports.Add("System.Linq");
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

      //collect models
      //var directlyUsedModelTypes = new List<Type>();
      //var wrappers = new Dictionary<String, StringBuilder>();
      foreach (Type svcInt in svcInterfaces) {

        if (cfg.useInterfaceTypeNameToGenerateSubNamespace) {
          var name = svcInt.Name;
          if (cfg.removeLeadingCharCountForSubNamespace > 0 && name.Length >= cfg.removeLeadingCharCountForSubNamespace) {
            name = name.Substring(cfg.removeLeadingCharCountForSubNamespace);
          }
          if (cfg.removeTrailingCharCountForSubNamespace > 0 && name.Length >= cfg.removeTrailingCharCountForSubNamespace) {
            name = name.Substring(0, name.Length - cfg.removeTrailingCharCountForSubNamespace);
          }
          writer.WriteLine();
          writer.BeginNamespace(name);
        }

        //if(!nsImports.Contains(svcInt.Namespace)){
        //  nsImports.Add(svcInt.Namespace);
        //}
        string svcIntDoc = XmlCommentAccessExtensions.GetDocumentation(svcInt);
        string endpointName = svcInt.Name;

        //if (endpointName[0] == 'I' && Char.IsUpper(endpointName[1])) {
        //  endpointName = endpointName.Substring(1);
        //}
        if (cfg.removeLeadingCharCountForControllerName > 0 && endpointName.Length >= cfg.removeLeadingCharCountForControllerName) {
          endpointName = endpointName.Substring(cfg.removeLeadingCharCountForControllerName);
        }
        if (cfg.removeTrailingCharCountForControllerName > 0 && endpointName.Length >= cfg.removeTrailingCharCountForControllerName) {
          endpointName = endpointName.Substring(0, endpointName.Length - cfg.removeTrailingCharCountForControllerName);
        }

        writer.WriteLine();
        writer.AttributesLine("ApiController");
        if (!string.IsNullOrWhiteSpace(cfg.generateGroupName)) {
          writer.AttributesLine($"ApiExplorerSettings(GroupName = \"{cfg.generateGroupName}\")");
        }

        writer.AttributesLine($"Route(\"{cfg.routePrefix + writer.Ftl(endpointName)}\")");


        writer.BeginClass(AccessModifier.Public, $"{endpointName}Controller", "ControllerBase", true);
        writer.WriteLine();

        var gtn = writer.GetGenericTypeName("ILogger", $"{endpointName}Controller");
        writer.Field(gtn, "Logger", readOnly: true);
        writer.Field(svcInt.Name, endpointName, readOnly: true);
        writer.WriteLine();

        writer.WriteLineAndPush($"public {endpointName}Controller(ILogger<{endpointName}Controller> logger, {svcInt.Name} {writer.Ftl(endpointName)}) {{");
        writer.WriteLine($"_Logger = logger;");
        writer.WriteLine($"_{endpointName} = {writer.Ftl(endpointName)};");
        writer.PopAndWriteLine("}");

        foreach (MethodInfo svcMth in svcInt.GetMethods()) {
          string svcMthDoc = XmlCommentAccessExtensions.GetDocumentation(svcMth, true);

          writer.WriteLine();
          if (String.IsNullOrWhiteSpace(svcMthDoc)) {
            svcMthDoc = svcMth.Name;
          }
          writer.WriteLine($"/// <summary> {svcMthDoc} </summary>");
          writer.WriteLine($"/// <param name=\"args\"> request capsule containing the method arguments </param>");

          if (!String.IsNullOrWhiteSpace(cfg.customAttributesPerControllerMethod)) {
            writer.WriteLine("[" + cfg.customAttributesPerControllerMethod.Replace("{C}", endpointName).Replace("{O}", svcMth.Name) + "]");
          }
          writer.WriteLine($"[HttpPost(\"{writer.Ftl(svcMth.Name)}\"), Produces(\"application/json\")]");

          string swaggerBodyAttrib = "";
          if (cfg.generateSwashbuckleAttributesForControllers) {

            string escDesc = svcMthDoc.Replace("\\", "\\\\").Replace("\"", "\\\"");
            writer.WriteLine($"[SwaggerOperation(OperationId = nameof({svcMth.Name}), Description = \"{escDesc}\")]");

            swaggerBodyAttrib = "[SwaggerRequestBody(Required = true)]";
          }

          writer.WriteLineAndPush($"public {svcMth.Name}Response {svcMth.Name}([FromBody]{swaggerBodyAttrib} {svcMth.Name}Request args) {{");
          writer.WriteLineAndPush("try {");
          writer.WriteLine($"var response = new {svcMth.Name}Response();");

          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            if (svcMthPrm.IsOutbound() && svcMthPrm.IsInbound()) {
              writer.WriteLine($"var {writer.Ftl(svcMthPrm.Name)}Buffer = args.{writer.Ftl(svcMthPrm.Name)};");
            }
          }

          var @params = new List<string>();
          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            if (svcMthPrm.IsOutbound()) {
              if (svcMthPrm.IsInbound()) {
                @params.Add($"ref {writer.Ftl(svcMthPrm.Name)}Buffer");
              }
              else {
                @params.Add($"out var {writer.Ftl(svcMthPrm.Name)}Buffer");
              }
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
                else if (svcMthPrm.DefaultValue.GetType() == typeof(bool)) {
                  defaultValueString = "false";
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
            writer.WriteLineAndPush($"response.@return = _{endpointName}.{svcMth.Name}(");
          }
          else {
            writer.WriteLineAndPush($"_{endpointName}.{svcMth.Name}(");
          }
          writer.WriteLine($"{String.Join("," + Environment.NewLine, @params.ToArray())}");
          writer.PopAndWriteLine(");");

          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            if (svcMthPrm.IsOut) {
              writer.WriteLine($"response.{writer.Ftl(svcMthPrm.Name)} = {writer.Ftl(svcMthPrm.Name)}Buffer;");
            }
          }

          writer.WriteLine($"return response;");
          writer.PopAndWriteLine("}");

          writer.WriteLineAndPush("catch (Exception ex) {");
          writer.WriteLine($"_Logger.LogCritical(ex, ex.Message);");
          if (cfg.fillFaultPropertyOnException) {
            writer.WriteLine($"return new {svcMth.Name}Response {{ fault = {cfg.exceptionDisplay} }};");
          }
          else {
            writer.WriteLine($"return new {svcMth.Name}Response();");
          }
          writer.PopAndWriteLine("}");

          writer.PopAndWriteLine("}"); //method

        }//foreach Method

        writer.WriteLine();
        writer.PopAndWriteLine("}"); //controller-class

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
