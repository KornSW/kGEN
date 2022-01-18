using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Wrappers {

  public class Cfg: RootCfg {

    public bool generateFaultProperty = false;

    public bool useInterfaceTypeNameToGenerateSubNamespace = false;
    public int removeLeadingCharCountForSubNamespace = 0;
    public int removeTrailingCharCountForSubNamespace = 0;

    /// <summary> especially for typescript, because there is no way to have a wildcard-import </summary>
    public string nsPrefixForModelTypesUsage = "";

  }

}
