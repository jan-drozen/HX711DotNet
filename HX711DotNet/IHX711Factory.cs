using System;
using System.Collections.Generic;
using System.Text;

namespace HX711DotNet
{
    public interface IHX711Factory
    {
        IHX711 GetHX711(int dout, int pdSck);        
    }
}
