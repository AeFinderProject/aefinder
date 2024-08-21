using Orleans;

namespace AeFinder.Subscriptions.Dto;

[GenerateSerializer]
public class AttachmentInfoDto
{
    [Id(0)] public string FileKey { get; set; }
    [Id(1)] public string AppId { get; set; }
    [Id(2)] public string Version { get; set; }
    [Id(3)] public string FileName { get; set; }
}