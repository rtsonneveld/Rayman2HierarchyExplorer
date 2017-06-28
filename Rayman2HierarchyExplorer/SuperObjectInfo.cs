using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rayman2HierarchyExplorer {
  struct SuperObjectInfo { // (tdst_EngineObject probably?)
    public int renderInfo; // RenderInfo
    public int standardGameStruct;
    public int sectorInfo;
    public int aiPointer;
    public int isCamera;
    public int platformInfo;
    public int field_18;
    public int lightData;
  }
}
