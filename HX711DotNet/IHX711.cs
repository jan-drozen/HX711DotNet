namespace HX711DotNet
{
    public interface IHX711
    {
        bool DebugPrinting { get; }
        byte Dout { get; }
        int Gain { get; }
        int Offet { get; }
        int OffsetB { get; }
        byte PdSck { get; }
        int ReferenceUnit { get; }
        int ReferenceUnitB { get; }

        int GetWeight(int times = 3);
        void PowerDown();
        void PowerUp();
        void Reset();
        void SetReferenceUnit(int reference_unit);
        void Tare(int times = 15);
    }
}