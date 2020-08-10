using HX711DotNet;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.WiringPi;

namespace TestApplication
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Pi.Init<BootstrapWiringPi>();
            Console.WriteLine(Pi.Info.ToString());
            byte dout = byte.Parse(args[0]);
            byte pd_sck = byte.Parse(args[1]);
            //var example = new Example(dout, pd_sck);
            //example.Run();
            //TestStreamer();
            TestBackgroundReader(dout, pd_sck);
            //Console.ReadLine();

        }

        private static async Task TestStreamer()
        {
            var streamer = new HX711Enumerator(new FakeHX711Factory(),5,6);
            await foreach (var value in streamer.GetValues())
            {
                Console.WriteLine(value);
            }
        }

        private static void TestBackgroundReader(byte dout, byte pdsck)
        {
            var backgroundReader = new BackgroundHX711(new HX711Factory(), dout, pdsck);
            backgroundReader.Start();
            while (true)
            {
                var currentValue = backgroundReader.CurrentValue;
                Console.WriteLine(currentValue);
            }
        }
    }
}
