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

      //svcInterfaces = svcInterfaces.SortByUsage().ToArray();

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

        foreach (MethodInfo svcMth in svcInt.GetMethods()) {
          string svcMthDoc = XmlCommentAccessExtensions.GetDocumentation(svcMth, true);

          writer.WriteLine();
          if (String.IsNullOrWhiteSpace(svcMthDoc)) {
            svcMthDoc = svcMth.Name;
          }

          Func<Type,string> nsPrefixGetter = (t)=> cfg.nsPrefixForModelTypesUsage;

          var prms = new List<MethodParamDescriptor>();
          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            var desc = MethodParamDescriptor.FromParameterInfo(svcMthPrm, (t)=> writer.EscapeTypeName (t, nsPrefixGetter), writer);

            prms.Add(desc);
            if (!cfg.writeCustomImportsOnly) {
              writer.RequireImport(svcMthPrm.ParameterType.Namespace);
            }
          }

          writer.Summary(svcMthDoc, false, prms.ToArray());

          if(svcMth.ReturnType.FullName == "System.Void") {
            writer.MethodInterface(svcMth.Name, null, prms.ToArray());
          }
          else {
            var returnTypeName = writer.EscapeTypeName(svcMth.ReturnType, nsPrefixGetter);
            writer.MethodInterface(svcMth.Name, returnTypeName, prms.ToArray());
          }

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
