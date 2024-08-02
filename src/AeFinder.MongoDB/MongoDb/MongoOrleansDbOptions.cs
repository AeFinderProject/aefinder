namespace AeFinder.MongoDb;

public class MongoOrleansDbOptions
{
    public string MongoDBClient { get; set; }
    public string DataBase { get; set; }
    public string AppBlockStateChangeGrainIdPrefix { get; set; }
    public string AppStateGrainIdPrefix { get; set; }
    public int ClearTaskPeriodMilliSeconds { get; set; } = 180000;
    public int PeriodClearLimitCount { get; set; } = 1000;
}