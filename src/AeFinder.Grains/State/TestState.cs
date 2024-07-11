namespace AeFinder.Grains.State;

[GenerateSerializer]
public class TestState
{
    [Id(0)]public int Count { get; set; }
}