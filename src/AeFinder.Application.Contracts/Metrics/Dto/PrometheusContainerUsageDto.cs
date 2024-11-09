using System.Collections.Generic;

namespace AeFinder.Metrics.Dto;

public class PrometheusContainerUsageDto
{
    public PrometheusContainerMetric Metric { get; set; }
    public List<object> Value{get;set;}
}

