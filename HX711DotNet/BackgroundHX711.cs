using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HX711DotNet
{
    public class BackgroundHX711
    {
        private IHX711Factory _factory;
        private IHX711 _hx711;

        public BackgroundHX711(IHX711Factory factory, int dout, int pdSck, int delay = 100)
        {
            _factory = factory;
            _hx711 = _factory.GetHX711((byte)dout, (byte)pdSck);
            Delay = delay;
            _running = false;
        }
        public int Delay { get; set; }

        private bool _running;

        public void Start()
        {
            if (_running)
                return;
            _running = true;
            Task.Factory.StartNew(() => 
            {   
                _hx711.SetReferenceUnit(1);
                _hx711.Reset();
                _hx711.Tare();
                while (_running)
                {
                    var val = _hx711.GetWeight(5);
                    CurrentValue = val;
                    _hx711.Reset();
                    Thread.Sleep(Delay);
                }
            });
        }
        public int CurrentValue { get; private set; }
        public void Stop() => _running = false;
        public void Tare() => _hx711.Tare();
        
    }
}
