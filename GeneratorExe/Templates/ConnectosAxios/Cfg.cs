using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.ConnectorsAxiosJS {

  public class Cfg: RootCfg {

    public string connectorClassName = "Connector";
    public string authHeaderName = "Authorization";
    public bool throwClientExecptionsFromFaultProperty = false;

    public int removeLeadingCharCountForOwnerName = 0;
    public int removeTrailingCharCountForOwnerName = 0;
    public bool appendOwnerNameAsNamespace = false;

    public string ImportDtosFrom = "my-contract-module/dtos";
    public string ImportModelsFrom = "my-contract-module/models";
    public string ImportInterfacesFrom = "my-contract-module/interfaces";

  }

}
