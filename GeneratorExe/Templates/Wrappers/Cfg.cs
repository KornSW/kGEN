using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Wrappers {

  public class Cfg: RootCfg {

    public bool generateFaultProperty = false;

    public int removeLeadingCharCountForOwnerName = 0;
    public int removeTrailingCharCountForOwnerName = 0;
    public bool appendOwnerNameAsNamespace = false;

  }

}
