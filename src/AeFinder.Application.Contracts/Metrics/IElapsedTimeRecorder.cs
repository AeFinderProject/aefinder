namespace AeFinder.Metrics;

public interface IElapsedTimeRecorder
{
    void Record(string recordName, long elapsedMilliseconds);
}