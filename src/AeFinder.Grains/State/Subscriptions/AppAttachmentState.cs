namespace AeFinder.Grains.State.Subscriptions;

public class AppAttachmentState
{
    public Dictionary<string, AttachmentInfo> AttachmentInfos { get; set; }
}

public class AttachmentInfo
{
    public string FileKey { get; set; }
    public string AppId { get; set; }
    public string Version { get; set; }
    public string FileName { get; set; }
    public string AwsS3Key { get; set; }
}