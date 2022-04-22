using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Models {

  public class Cfg: RootCfg {

    public string[] modelTypeNameIncludePatterns = new string[] {
      "Foo.*"
    };

    public bool generateNavigationAnnotationsForLocalModels = true;
    
    /// <summary>
    /// all generated props are optional/nullable, if there is
    /// no required-attribute in the source assembly
    /// </summary>
    public bool requiredPropsByAnnotation = true;

  }

}
