using System.Collections.Generic;

namespace AeFinder.GraphQL.Dto;

public class SyncStateVersionDto
{
    public string Version { get; set; }
    public List<SyncStateVersionItemDto> Items { get; set; }
}