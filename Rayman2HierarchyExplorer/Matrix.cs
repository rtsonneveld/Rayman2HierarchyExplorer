using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rayman2HierarchyExplorer {
  struct Matrix {
    public int transformationType;
    public float mat_0_0_x;
    public float mat_1_0_y;
    public float mat_2_0_z;
    public float mat_3_0;
    public float mat_0_1;
    public float mat_1_1;
    public float mat_2_1;
    public float mat_3_1;
    public float mat_0_2;
    public float mat_1_2;
    public float mat_2_2;
    public float mat_3_2;
    public float mat_0_3;
    public float mat_1_3;
    public float mat_2_3;
    public float mat_3_3;


    public string String() {
      return "Matrix (" + this.mat_0_0_x + "," + this.mat_1_0_y + "," + this.mat_2_0_z + ")";
    }
  }

}
