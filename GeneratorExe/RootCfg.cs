﻿using CodeGeneration.Languages;
using System;

namespace CodeGeneration {

  public class RootCfg : CodeWritingSettings {

    //INPUT-BASICS

    public string inputFile = null;
    public string interfaceTypeNamePattern = null;

    public string[] requireXmlDocForNamespaces = new string[] { };

    //OUTPUT-BASICS

    public string template = null;
    public string outputLanguage = "C#";
    public string outputNamespace = "";
    public String[] customImports = new String[] {};
    public bool writeCustomImportsOnly = false;

    public bool generateDataAnnotationsForLocalModels = true; //requires also the "EntityAnnoations" Nuget Package!

    //DEBUGGING
    public int waitForDebuggerSec = 0;

    public string codeGenInfoHeader = "WARNING: THIS IS GENERATED CODE - PLEASE DONT EDIT DIRECTLY - YOUR CHANGES WILL BE LOST!";



    /// <summary>
    /// all generated props are optional/nullable, if there is
    /// no required-attribute in the source assembly
    /// </summary>
    public bool requiredPropsByAnnotation = true;

  }

}
