using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Interfaces {

  public class Cfg: RootCfg {

    /// <summary> especially for typescript, because there is no way to have a wildcard-import </summary>
    public string nsPrefixForModelTypesUsage = "";

    /// <summary> especially for typescript, because there are by default no out-args... </summary>
    public bool transformRefAndOutArgsToReturnPropertyBag = false;

    public bool generateAsyncMethods = false;

  }

}
