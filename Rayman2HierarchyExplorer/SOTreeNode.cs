using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rayman2HierarchyExplorer {
  class SOTreeNode {
    public List<SOTreeNode> subitems = new List<SOTreeNode>();
    public string name;
    public int superObjectAddress;
    public bool isMainCharacter;
    public SuperObject superObject;
  }
}
