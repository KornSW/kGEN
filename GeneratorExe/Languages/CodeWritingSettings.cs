using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGeneration.Languages {

  public class CodeWritingSettings {

    /// <summary> number of spaces per level </summary>
    public int indentDepthPerLevel { get; set; } = 4;

    #region " PHP Specific " 
    
    public bool generateTypeNamesInPhp { get; set; } = true;

    #endregion


  }

}
