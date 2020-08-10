using System;
using System.Collections.Generic;
using System.Text;

namespace HX711DotNet
{
    public class HX711Factory : IHX711Factory
    {
        public IHX711 GetHX711(int dout, int pdSck) => new HX711((byte)dout, (byte)pdSck);
    }
}
