using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.MvcControllers {

  public class Cfg: RootCfg {

    public bool generateSwashbuckleAttributesForControllers = true;
    public string generateGroupName = "";
    public string customAttributesPerControllerMethod = null;
    public bool fillFaultPropertyOnException = false;
    public string exceptionDisplay = "ex.Message";
    public string routePrefix = "";

    public bool useInterfaceTypeNameToGenerateSubNamespace = false;
    public int removeLeadingCharCountForSubNamespace = 0;
    public int removeTrailingCharCountForSubNamespace = 0;

  }

}
