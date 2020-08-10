using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace HX711DotNet
{
    public class HX711Enumerator
    {
        private IHX711 _hx711;
        private bool _read;

        public HX711Enumerator(IHX711Factory hX711Factory, int dout, int pdSck, int delay = 100)
        {
            _hx711 = hX711Factory.GetHX711(dout, pdSck);
            Delay = delay;
        }

        public async IAsyncEnumerable<int> GetValues()
        {
            _read = true;
            _hx711.SetReferenceUnit(1);
            _hx711.Reset();
            _hx711.Tare();
            while (_read)
            {
                var value = await Task<int>.Factory.StartNew(() =>
                {
                    var val = _hx711.GetWeight(5);
                    _hx711.Reset();
                    return val;
                });
                yield return value;
                Thread.Sleep(Delay);
            }            
        }

        public int Delay { get; set; }
       
        public void StopReading()
        {
            _read = false;
        }

        public bool IsStreaming => _read;
    }
}
