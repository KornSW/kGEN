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
    


  }

}
