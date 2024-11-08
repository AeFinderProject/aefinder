namespace AeFinder.Metrics.Dto;

public class PrometheusResultDto<T>
{
    public string Status { get; set; }
    public PrometheusResultData<T> Data { get; set; }
}

public class PrometheusResultData<T>
{
    public string ResultType { get; set; }
    public T Result { get; set; }
}