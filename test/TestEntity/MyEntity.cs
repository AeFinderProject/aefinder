using AElfIndexer.Sdk;

namespace TestEntity;

public class MyEntity : IndexerEntity,IIndexerEntity
{
    public int IntValue { get; set; }
    public string StringValue { get; set; }
    public Detail Detail { get; set; }
    public List<string> ListString { get; set; }
    public List<Detail> Details { get; set; }

    [Fulltext(Index = true)]
    public string TextString { get; set; }
}

public class Detail
{
    public int DetailIntValue { get; set; }
    public string DetailStringValue { get; set; }
}

