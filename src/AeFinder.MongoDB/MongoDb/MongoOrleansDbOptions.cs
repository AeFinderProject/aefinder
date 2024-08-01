namespace AeFinder.MongoDb;

public class MongoOrleansDbOptions
{
    public string MongoDBClient { get; set; }
    public string DataBase { get; set; }
    public string AppBlockStateChangeGrainIdPrefix { get; set; }
    public string AppStateGrainIdPrefix { get; set; }
}