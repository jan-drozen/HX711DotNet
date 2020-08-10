using System;
using System.Collections.Generic;
using System.Text;

namespace HX711DotNet
{
    public class FakeHX711 : IHX711
    {
        private Random _random = new Random();
        public bool DebugPrinting { get; set; }

        public byte Dout {get;set;}

        public int Gain { get; set; }

        public int Offet { get; set; }

        public int OffsetB { get; set; }

        public byte PdSck { get; set; }

        public int ReferenceUnit { get; set; }

        public int ReferenceUnitB { get; set; }

        public int GetWeight(int times = 3)
        {
            return _random.Next(-times,times);
        }

        public void PowerDown()
        {
            
        }

        public void PowerUp()
        {
            
        }

        public void Reset()
        {
            
        }

        public void SetReferenceUnit(int reference_unit)
        {
            
        }

        public void Tare(int times = 15)
        {
            
        }
    }
}
