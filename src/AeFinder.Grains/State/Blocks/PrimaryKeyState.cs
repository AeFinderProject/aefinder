using System.Numerics;

namespace AeFinder.Grains.State.Blocks;

public class PrimaryKeyState
{
    public string GrainPrimaryKey;

    public BigInteger SerialNumber;

    public int Counter;

    public int SwitchInterval;
}