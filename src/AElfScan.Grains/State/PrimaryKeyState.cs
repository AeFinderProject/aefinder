using System.Numerics;

namespace AElfScan.Grains.State;

public class PrimaryKeyState
{
    public string GrainPrimaryKey;

    public BigInteger SerialNumber;

    public int Counter;

    public int SwitchInterval;
}