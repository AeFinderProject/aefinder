using AeFinder.Sdk.Attachments;

namespace AeFinder.App.Attachments;

public class TestAppAttachmentValueProvider : AppAttachmentValueProviderBase<TestInfo>
{
    public override string Key => "TestKey";
}

public class TestInfo
{
    public string Info { get; set; }
}