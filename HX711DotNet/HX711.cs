using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
namespace HX711DotNet
{
    public class HX711 : IHX711
    {
        #region Properties
        public byte PdSck { get; }
        public byte Dout { get; }
        public int Gain { get; private set; }
        public int ReferenceUnit { get; private set; }
        public int ReferenceUnitB { get; private set; }
        public int Offet { get; private set; }
        public int OffsetB { get; private set; }
        public bool DebugPrinting { get; }
        #endregion

        #region Private fields
        private ByteFormat _byteFormat;
        private ByteFormat _bitFormat;
        private object _readLock;
        private int _lastVal;
        #endregion

        #region Public API
        public HX711(byte dout, byte pdSck, byte gain = 128)
        {
            PdSck = pdSck;
            Dout = dout;

            // Mutex for reading from the HX711, in case multiple threads in client
            // software try to access get values from the class at the same time.
            _readLock = new object();
            //GPIO.setmode(GPIO.BCM)
            Pi.Gpio[PdSck].PinMode = GpioPinDriveMode.Output;
            Pi.Gpio[Dout].PinMode = GpioPinDriveMode.Input;
            Gain = 0;
            // The value returned by the hx711 that corresponds to your reference
            // unit AFTER dividing by the SCALE.
            ReferenceUnit = 1;
            ReferenceUnitB = 1;

            Offet = 1;
            OffsetB = 1;
            _lastVal = 0;

            DebugPrinting = false;

            _byteFormat = ByteFormat.MSB;
            _bitFormat = ByteFormat.MSB;
            SetGain(gain);
            //# Think about whether this is necessary.
            Thread.Sleep(10);
        }


        //# Compatibility function, uses channel A version
        public int GetWeight(int times = 3) => GetWeightA(times);



        //# Sets tare for channel A for compatibility purposes
        public void Tare(int times = 15)
        {
            lock (_readLock)
            {
                TareA(times);
            }
        }


        public void SetReferenceUnit(int reference_unit) => this.SetReferenceUnitA(reference_unit);

        public void PowerDown()
        {
            // Wait for and get the Read Lock, incase another thread is already
            // driving the HX711 serial interface.
            lock (_readLock)
            {
                //Cause a rising edge on HX711 Digital Serial Clock (PD_SCK).  We then
                //leave it held up and wait 100 us.  After 60us the HX711 should be
                //powered down.
                Pi.Gpio[PdSck].Write(false);
                Pi.Gpio[PdSck].Write(true);
                Thread.Sleep(100);

                //Release the Read Lock, now that we've finished driving the HX711
                //serial interface.
            }
        }

        public void PowerUp()
        {
            //Wait for and get the Read Lock, incase another thread is already
            //driving the HX711 serial interface.
            lock (_readLock)
            {
                // Lower the HX711 Digital Serial Clock (PD_SCK) line.
                Pi.Gpio[PdSck].Write(false);
                // Wait 100 us for the HX711 to power back up.
                Thread.Sleep(100);
                //Release the Read Lock, now that we've finished driving the HX711
                //# serial interface.
            }

            //HX711 will now be defaulted to Channel A with gain of 128.  If this
            //isn't what client software has requested from us, take a sample and
            //throw it away, so that next sample from the HX711 will be from the
            //correct channel/gain.
            if (GetGain() != 128)
                ReadRawBytes();
        }

        public void Reset()
        {
            PowerDown();
            PowerUp();
        }

        #endregion

        #region Private stuff
        private byte GetGain()
        {
            switch (Gain)
            {
                case 1: return 128;
                case 3: return 64;
                case 2: return 32;
            }
            //Shouldn't get here.
            return 0;
        }
        private void SetGain(byte gain)
        {
            switch (gain)
            {
                case 128: Gain = 1; break;
                case 64: Gain = 3; break;
                case 32: Gain = 2; break;
            }
            Pi.Gpio[PdSck].Write(false);
            //# Read out a set of raw bytes and throw it away.
            ReadRawBytes();
        }

        private int convertFromTwosComplement24bit(int inputValue) => -(inputValue & 0x800000) + (inputValue & 0x7fffff);

        private bool IsReady()
        {
            var valueRead = Pi.Gpio[Dout].Read();
            return !valueRead;
        }
        private byte ReadNextBit()
        {
            // Clock HX711 Digital Serial Clock (PD_SCK).  DOUT will be
            // ready 1us after PD_SCK rising edge, so we sample after
            // lowering PD_SCL, when we know DOUT will be stable.
            Pi.Gpio[PdSck].Write(true);
            Pi.Gpio[PdSck].Write(false);
            var value = Pi.Gpio[Dout].Read();
            return value ? (byte)1 : (byte)0;
        }

        private byte ReadNextByte()
        {
            byte byteValue = 0;

            // Read bits and build the byte from top, or bottom, depending
            // on whether we are in MSB or LSB bit mode.
            for (int x = 0; x < 8; x++)
            {
                if (_bitFormat == ByteFormat.MSB)
                {
                    byteValue <<= 1;
                    byteValue |= ReadNextBit();
                }
                else
                {
                    byteValue >>= 1;
                    byteValue |= (byte)(ReadNextBit() * 0x80);
                }
            }
            return byteValue;
        }

        private byte[] ReadRawBytes()
        {
            // Wait for and get the Read Lock, incase another thread is already
            // driving the HX711 serial interface.
            lock (_readLock)
            {
                //Wait until HX711 is ready for us to read a sample.
                while (!IsReady())
                {
                    //pass
                    Thread.Yield();
                }
                // Read three bytes of data from the HX711.
                var firstByte = ReadNextByte();
                var secondByte = ReadNextByte();
                var thirdByte = ReadNextByte();

                // HX711 Channel and gain factor are set by number of bits read
                // after 24 data bits.
                for (int i = 0; i < Gain; i++)
                {
                    // Clock a bit out of the HX711 and throw it away.
                    ReadNextBit();
                }

                // Depending on how we're configured, return an orderd list of raw byte
                // values.
                if (_byteFormat == ByteFormat.LSB)
                {
                    return new[] { thirdByte, secondByte, firstByte };
                }
                else
                {
                    return new[] { firstByte, secondByte, thirdByte };
                }
                // Release the Read Lock, now that we've finished driving the HX711
                // serial interface.
            }
        }


        private int ReadInt()
        {
            //Get a sample from the HX711 in the form of raw bytes.
            var dataBytes = ReadRawBytes();
            if (DebugPrinting)
            {
                Console.Write(dataBytes);
            }

            //Join the raw bytes into a single 24bit 2s complement value.
            var twosComplementValue = ((dataBytes[0] << 16) |
                               (dataBytes[1] << 8) |
                               dataBytes[2]);

            if (DebugPrinting)
            {
                Console.WriteLine($"Twos: {twosComplementValue}");
            }
            //Convert from 24bit twos-complement to a signed value.
            int signedIntValue = this.convertFromTwosComplement24bit(twosComplementValue);

            //Record the latest sample value we've read.
            _lastVal = signedIntValue;
            //Return the sample value we've read from the HX711.
            return signedIntValue;
        }

        private int ReadAverage(int times = 3)
        {
            //Make sure we've been asked to take a rational amount of samples.
            if (times <= 0)
                throw new ArgumentException("HX711()::read_average(): times must >= 1!!");

            //If we're only average across one value, just read it and return it.
            if (times == 1)
                return ReadInt();

            //If we're averaging across a low amount of values, just take the
            //median.
            if (times < 5)
                return ReadMedian(times);

            //If we're taking a lot of samples, we'll collect them in a list, remove
            //the outliers, then take the mean of the remaining set.
            var valueList = new List<int>(times);

            for (int x = 0; x < times; x++)
            {
                valueList.Add(ReadInt());
            }
            valueList.Sort();

            //# We'll be trimming 20% of outlier samples from top and bottom of collected set.
            int trimAmount = Convert.ToInt32(Math.Round(valueList.Count * 0.2));

            //Trim the edge case values.
            valueList = valueList.Skip(trimAmount).Take(valueList.Count - trimAmount * 2).ToList();

            //Return the mean of remaining samples.
            return Convert.ToInt32(Math.Round(valueList.Average()));
        }

        private int GetValueA(int times = 3) => ReadMedian(times) - GetOffsetA();

        //Compatibility function, uses channel A version
        private int GetValue(int times = 3) => GetValueA(times);


        private int GetValueB(int times = 3)
        {
            //for channel B, we need to set_gain(32)
            var g = GetGain();
            this.SetGain(32);
            var value = ReadMedian(times) - GetOffsetB();
            this.SetGain(g);
            return value;
        }
        //A median-based read method, might help when getting random value spikes
        //for unknown or CPU-related reasons
        private int ReadMedian(int times = 3)
        {
            if (times <= 0)
                throw new ArgumentException("HX711::read_median(): times must be greater than zero!");

            //# If times == 1, just return a single reading.
            if (times == 1)
                return ReadInt();

            var valueList = new List<int>(times);

            for (int x = 0; x < times; x++)
            {
                valueList.Add(ReadInt());
            }
            valueList.Sort();

            //If times is odd we can just take the centre value.
            if ((times & 0x1) == 0x1)
                return valueList[valueList.Count / 2];
            else
            {
                //# If times is even we have to take the arithmetic mean of
                //# the two middle values.
                var midpoint = valueList.Count / 2;
                return (valueList[midpoint] + valueList[midpoint + 1]) / 2;
            }
        }

        private int GetWeightA(int times = 3)
        {
            var value = GetValueA(times);
            value = value / ReferenceUnit;
            return value;
        }

        private int GetWeightB(int times = 3)
        {
            var value = GetValueB(times);
            value = value / ReferenceUnitB;
            return value;
        }

        private int TareA(int times = 15)
        {
                //# Backup REFERENCE_UNIT value
                var backupReferenceUnit = this.GetReferenceUnitA();
                SetReferenceUnitA(1);
                var value = ReadAverage(times);

                if (DebugPrinting)
                    Console.WriteLine($"Tare A value: {value}");

                SetOffsetA(value);

                //# Restore the reference unit, now that we've got our offset.
                SetReferenceUnitA(backupReferenceUnit);

                return value;
        }

        private int TareB(int times = 15)
        {
            //# Backup REFERENCE_UNIT value
            var backupReferenceUnit = GetReferenceUnitB();
            SetReferenceUnitB(1);

            // for channel B, we need to set_gain(32)
            var backupGain = GetGain();
            SetGain(32);

            var value = ReadAverage(times);

            if (DebugPrinting)
                Console.WriteLine($"Tare B value: {value}");

            SetOffsetB(value);

            //Restore gain/channel/reference unit settings.
            SetGain(backupGain);
            SetReferenceUnitB(backupReferenceUnit);
            return value;
        }


        private void SetReadingFormat(ByteFormat byte_format = ByteFormat.LSB, ByteFormat bit_format = ByteFormat.MSB)
        {
            if (byte_format == ByteFormat.LSB)
                _byteFormat = byte_format;
            else if (byte_format == ByteFormat.MSB)
                _byteFormat = byte_format;
            else
                throw new ArgumentException($"Unrecognised byte_format: {byte_format}");

            if (bit_format == ByteFormat.LSB)
                _bitFormat = bit_format;
            else if (bit_format == ByteFormat.MSB)
                _bitFormat = bit_format;
            else
                throw new ArgumentException($"Unrecognised bitformat: {bit_format}");
        }


        //sets offset for channel A for compatibility reasons
        private void SetOffset(int offset) => this.SetOffsetA(offset);

        private void SetOffsetA(int offset) => this.Offet = offset;

        private void SetOffsetB(int offset) => this.OffsetB = offset;

        private int GetOffset() => GetOffsetA();

        private int GetOffsetA() => this.Offet;

        private int GetOffsetB() => this.OffsetB;
        private void SetReferenceUnitA(int reference_unit)
        {
            //# Make sure we aren't asked to use an invalid reference unit.
            if (reference_unit == 0)
            {
                throw new ArgumentException("HX711::set_reference_unit_A() can't accept 0 as a reference unit!");
            }
            ReferenceUnit = reference_unit;
        }

        private void SetReferenceUnitB(int reference_unit)
        {
            //Make sure we aren't asked to use an invalid reference unit.
            if (reference_unit == 0)
            {
                throw new ArgumentException("HX711::set_reference_unit_A() can't accept 0 as a reference unit!");
            }
            ReferenceUnitB = reference_unit;
        }

        private int GetReferenceUnit() => GetReferenceUnitA();

        private int GetReferenceUnitA() => ReferenceUnit;

        private int GetReferenceUnitB() => ReferenceUnitB;

        #endregion

    }
}
