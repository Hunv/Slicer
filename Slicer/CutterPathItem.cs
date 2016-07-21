using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer
{
    public class CutterPathItem
    {
        public CutterPathItem(int deltaAngle, int deltaLength)
        {
            DeltaAngle = deltaAngle;
            DeltaLenght = deltaLength;
        }

        public int DeltaAngle { get; set; }
        public int DeltaLenght { get; set; }
    }
}
