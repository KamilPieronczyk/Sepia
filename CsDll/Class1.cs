/**
 * @author Kamil Pierończyk Gr.2 AEI, Inf, rok 3
 * Sepia
 */
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
                int gray = (int)Math.Round((double)(bytes[i] + bytes[i + 1] + bytes[i + 2]) / 3, MidpointRounding.AwayFromZero);

                int newR = gray + (2 * deepth);
                int newG = gray + deepth;
                int newB = gray;

                byte R = newR > 255 ? (byte)255 : (byte)newR;
                byte G = newG > 255 ? (byte)255 : (byte)newG;
                byte B = newB > 255 ? (byte)255 : (byte)newB;

                bytes[i] = B;
                bytes[i + 1] = G;
                bytes[i + 2] = R;
            }
        }
    }
}
