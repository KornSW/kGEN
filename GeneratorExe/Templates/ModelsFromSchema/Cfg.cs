using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.ModelsFromSchema {

  public class Cfg: RootCfg {

    public string[] modelTypeNameIncludePatterns = new string[] {
      "Foo.*"
    };
 
    public bool generateNavigationAnnotationsForLocalModels = true;

    public string outputNamespace = "MedicalResearch.SubjectData.Model";
    public string entityClassNamePattern = "{E}";

    public bool generateNavPropsToPrincipal = false;
    public bool generateReverseNavPropsToDependents = false;

    public bool generateNavPropsToLookup = false;
    public bool generateReverseNavPropsToReferers = false;

    //public string navPropCollectionType           = "ObservableCollection<{T}>";
    public string navPropCollectionType = "List<{T}>";

    public bool generateEfAttributes = false;
    public string tablePrefix = "";

    public bool generateMappingMethods = false;
    public string mappingTargetClassNamePattern = "{E}";
    public string mappingMethodAccessLevel = "internal";





  }

}
