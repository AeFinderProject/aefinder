using System.Collections.Generic;

namespace AeFinder.Commons.Dto;

public class SyncStateVersionDto
{
    public string Version { get; set; }
    public List<SyncStateVersionItemDto> Items { get; set; }
}