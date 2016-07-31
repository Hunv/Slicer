using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Slicer
{
    public class ShapeDirectory
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public WriteableBitmap Icon { get; set; }
        public List<ShapeDirectory> Shapes { get; set; }

        public bool IsImage
        {
            get
            {
                if (Path.EndsWith("T"))
                    return true;

                return false;
            }
        }
    }
}
