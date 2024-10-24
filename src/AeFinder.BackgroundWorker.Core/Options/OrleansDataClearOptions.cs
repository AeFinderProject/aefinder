namespace AeFinder.BackgroundWorker.Options;

public class OrleansDataClearOptions
{
    public string MongoDBClient { get; set; }
    public string DataBase { get; set; }
    public string AppBlockStateChangeGrainIdPrefix { get; set; }
    public string AppStateGrainIdPrefix { get; set; }
    public int ClearTaskPeriodMilliSeconds { get; set; } = 180000;
    public int PeriodClearLimitCount { get; set; } = 100000;
    public double ExceedDeviationRatio { get; set; } = 0.01;
    public int MongoDbMaxDocumentSize { get; set; }  = 16793600;
}