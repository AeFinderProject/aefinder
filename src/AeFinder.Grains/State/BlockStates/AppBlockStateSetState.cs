using AeFinder.Grains.Grain.BlockStates;

namespace AeFinder.Grains.State.BlockStates;

[GenerateSerializer]
public class AppBlockStateSetState
{
    [Id(0)] public BlockStateSet BlockStateSet { get; set; }
}