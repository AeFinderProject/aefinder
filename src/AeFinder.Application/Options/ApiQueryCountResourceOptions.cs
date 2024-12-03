using System.Collections.Generic;

namespace AeFinder.Options;

public class ApiQueryCountResourceOptions
{
    public List<QueryCountResourceInfo> GraphQLQueryPackages { get; set; }
}

public class QueryCountResourceInfo
{
    public string ResourceName { get; set; }
    public string Description { get; set; }
    public int QueryCount { get; set; }
    public decimal MonthlyUnitPrice { get; set; }
}