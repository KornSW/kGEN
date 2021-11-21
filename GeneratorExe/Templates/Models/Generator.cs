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

namespace CodeGeneration.Models {

  public class Generator {

    public void Generate(CodeWriterBase writer, Cfg cfg) {

      var nsImports = new List<string>();
      nsImports.Add("System");
      nsImports.Add("System.Collections.Generic");

      if (cfg.generateNavigationAnnotationsForLocalModels) {
        nsImports.Add("System.ComponentModel.DataAnnotations"); //+ extended by the "EntityAnnoations" Nuget Package!
      }
      else if (cfg.generateDataAnnotationsForLocalModels) {
        nsImports.Add("System.ComponentModel.DataAnnotations");
      }

      var modelContent = new StringBuilder(10000);
      var innerWriter = CodeWriterBase.GetForLanguage(cfg.outputLanguage,new StringWriter(modelContent), cfg);

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
      for (int i = 0; i < cfg.modelTypeNameIncludePatterns.Length; i++) {
        cfg.modelTypeNameIncludePatterns[i] = "^(" + Regex.Escape(cfg.modelTypeNameIncludePatterns[i]).Replace("\\*", ".*?") + ")$";
      }

      svcInterfaces = svcInterfaces.Where((Type i) => Regex.IsMatch(i.FullName, cfg.interfaceTypeNamePattern)).ToArray();

      //collect models
      var directlyUsedModelTypes = new List<Type>();
      //var wrappers = new Dictionary<String, StringBuilder>();
      foreach (Type svcInt in svcInterfaces) {

        //if(!nsImports.Contains(svcInt.Namespace)){
        //  nsImports.Add(svcInt.Namespace);
        //}
        string svcIntDoc = XmlCommentAccessExtensions.GetDocumentation(svcInt);

        foreach (MethodInfo svcMth in svcInt.GetMethods()) {
          string svcMthDoc = XmlCommentAccessExtensions.GetDocumentation(svcMth, false);

          if (svcMth.ReturnType != null && svcMth.ReturnType != typeof(void)) {
            directlyUsedModelTypes.Add(svcMth.ReturnType);
            if (!nsImports.Contains(svcMth.ReturnType.Namespace)) {
              nsImports.Add(svcMth.ReturnType.Namespace);
            }
          }

          foreach (ParameterInfo svcMthPrm in svcMth.GetParameters()) {
            string svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm);
            if (String.IsNullOrWhiteSpace(svcMthPrmDoc)) {
              svcMthPrmDoc = XmlCommentAccessExtensions.GetDocumentation(svcMthPrm.ParameterType);
            }
            directlyUsedModelTypes.Add(svcMthPrm.ParameterType);

            if (!nsImports.Contains(svcMthPrm.ParameterType.Namespace)) {
              nsImports.Add(svcMthPrm.ParameterType.Namespace);
            }

          }//foreach Param

        }//foreach Method

      }//foreach Interface

      Action<List<Type>, Type> addRecursiveMethod = null;
      addRecursiveMethod = (collector, candidate) => {
        if (candidate.IsArray) {
          candidate = candidate.GetElementType();
        }
        else if (candidate.IsGenericType) {
          var genBase = candidate.GetGenericTypeDefinition();
          var genArg1 = candidate.GetGenericArguments()[0];
          if (typeof(List<>).MakeGenericType(genArg1).IsAssignableFrom(candidate)) {
            candidate = genArg1;
          }
          else if (typeof(Collection<>).MakeGenericType(genArg1).IsAssignableFrom(candidate)) {
            candidate = genArg1;
          }
          if (genBase == typeof(Nullable<>)) {
            candidate = genArg1;
          }
        }

        if (!collector.Contains(candidate)) {
          bool match = false;
          for (int i = 0; i < cfg.modelTypeNameIncludePatterns.Length; i++) {
            if (Regex.IsMatch(candidate.FullName, cfg.modelTypeNameIncludePatterns[i])) {
              match = true;
              break;
            }
          }
          if (match) {
            collector.Add(candidate);
            if (candidate.BaseType != null) {
              addRecursiveMethod.Invoke(collector, candidate.BaseType);
            }
            foreach (PropertyInfo p in candidate.GetProperties()) {
              addRecursiveMethod.Invoke(collector, p.PropertyType);
            }
          }
        }
      };

      var modelTypesToGenerate = new List<Type>();
      foreach (Type canidate in directlyUsedModelTypes.Distinct()) {
        addRecursiveMethod.Invoke(modelTypesToGenerate, canidate);
      }

      foreach (Type modelTypeToGenerate in modelTypesToGenerate.OrderBy((m) => m.Name)) {
        string modelDoc = XmlCommentAccessExtensions.GetDocumentation(modelTypeToGenerate, true);

        innerWriter.WriteLine();

        if (!String.IsNullOrWhiteSpace(modelDoc)) {
          innerWriter.Summary(modelDoc, dumpToSingleLine: false);
        }

        //TODO: Obsolete-Attributes on Class-Level (including comments)

        if (modelTypeToGenerate.IsClass) {
          innerWriter.BeginClass(AccessModifier.Public ,modelTypeToGenerate.Name, innerWriter.EscapeTypeName ( modelTypeToGenerate.BaseType.Obj2Null()));    
        }
        else {
          innerWriter.BeginInterface(AccessModifier.Public, modelTypeToGenerate.Name, innerWriter.EscapeTypeName(modelTypeToGenerate.BaseType.Obj2Null()));
        }

        foreach (PropertyInfo prop in modelTypeToGenerate.GetProperties()) {
          string propDoc = XmlCommentAccessExtensions.GetDocumentation(prop, true);

          innerWriter.WriteLine();

          if (!String.IsNullOrWhiteSpace(propDoc)) {
            innerWriter.Summary(propDoc, dumpToSingleLine: false);
          }

          var attribs = prop.GetCustomAttributes();

          if (cfg.generateDataAnnotationsForLocalModels) {
            if (prop.GetCustomAttributes<RequiredAttribute>().Any()) {
              innerWriter.AttributesLine("Required");
            }
            ObsoleteAttribute oa = prop.GetCustomAttributes<ObsoleteAttribute>().FirstOrDefault();
            if (oa != null) {
              innerWriter.AttributesLine("Obsolete(\"" + oa.Message + "\")");
            }
            MaxLengthAttribute mla = prop.GetCustomAttributes<MaxLengthAttribute>().FirstOrDefault();
            if (mla != null) {
              innerWriter.AttributesLine("MaxLength(" + mla.Length.ToString() + ")");
            }
          }

          if (cfg.generateNavigationAnnotationsForLocalModels) {
            //here we use strings, because we dont want to have a reference to a nuget-pkg within this template
            if (attribs.Where((a) => a.GetType().Name == "FixedAfterCreationAttribute").Any()) {
              innerWriter.AttributesLine("FixedAfterCreation");
            }
            if (attribs.Where((a) => a.GetType().Name == "SystemInternalAttribute").Any()) {
              innerWriter.AttributesLine("SystemInternal");
            }
            if (attribs.Where((a) => a.GetType().Name == "LookupAttribute").Any()) {
              innerWriter.AttributesLine("Lookup");
            }
            if (attribs.Where((a) => a.GetType().Name == "RefererAttribute").Any()) {
              innerWriter.AttributesLine("Referer");
            }
            if (attribs.Where((a) => a.GetType().Name == "PrincipalAttribute").Any()) {
              innerWriter.AttributesLine("Principal");
            }
            if (attribs.Where((a) => a.GetType().Name == "DependentAttribute").Any()) {
              innerWriter.AttributesLine("Dependent");
            }
          }

          if (modelTypeToGenerate.IsClass) {
            innerWriter.InlineProperty( AccessModifier.Public, prop.Name, innerWriter.EscapeTypeName(prop.PropertyType));
          }
          else {
            //interfaces have no a.m.
            innerWriter.InlineProperty(AccessModifier.None , prop.Name, innerWriter.EscapeTypeName(prop.PropertyType));
          }

        }

        innerWriter.WriteLine();
        if (modelTypeToGenerate.IsClass) {
          innerWriter.EndClass();
        }
        else {
          innerWriter.EndInterface();
        }
        //innerWriter.WriteLine();

      }

      //this can be done only here, because the nsImports will be extended during main-logic
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

      using (var sr = new StringReader(modelContent.ToString())) {
        string line = sr.ReadLine();
        while (line != null) {
          writer.WriteLine(line);
          line = sr.ReadLine();
        }
      }
      
      if (!String.IsNullOrWhiteSpace(cfg.outputNamespace)) {
        writer.WriteLine();
        writer.EndNamespace();
      }

    }
  }
}
