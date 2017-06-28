using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Rayman2HierarchyExplorer {
  struct SuperObject {
    public int type;
    public int info; // SuperObjectInfo
    public int firstChild; // SuperObject
    public int nextChild; // SuperObject
    public int field_10;
    public int nextBrother; // SuperObject
    public int prevBrother; // SuperObject
    public int father; // SuperObject
    public int mainMatrix; // Matrix
    public int otherMatrix; // Matrix
    public int field_28;
    public int someRenderField;
    public int renderBits;

    public static bool getVisibleBit(int renderBits) {
      return (renderBits & 0x20) == 0;
    }

    public static int setVisibleBit(int renderBits, bool visible) {
      if (visible) {
        return renderBits & ~(0x20);
      } else {
        return renderBits | 0x20;
      }
    }
  }
}
