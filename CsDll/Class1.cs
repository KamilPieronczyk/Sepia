using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsDll
{
    public class Class1
    {
        public void CreateSepia(byte[] bytes, int length, int deepth)
        {
            for (int i = 0; i < length; i += 3)
            {
                double gray = (bytes[i] + bytes[i + 1] + bytes[i + 2]) / 3;

                double newR = gray + (2 * deepth);
                double newG = gray + deepth;
                double newB = gray;

                byte R = newR > 255.0 ? (byte)255 : (byte)((int)newR);
                byte G = newG > 255.0 ? (byte)255 : (byte)((int)newG);
                byte B = newB > 255.0 ? (byte)255 : (byte)((int)newB);

                bytes[i] = B;
                bytes[i + 1] = G;
                bytes[i + 2] = R;
            }
        }
    }
}
