using AeFinder.Apps;

namespace AeFinder.App.Attachments;

public class TestAppAttachmentValueProvider : AppAttachmentValueProviderBase<TestInfo>
{
    public override string Key => "TestKey";

    public TestAppAttachmentValueProvider(IAppInfoProvider appInfoProvider, IAppAttachmentService appAttachmentService)
        : base(appInfoProvider, appAttachmentService)
    {
    }
}

public class TestInfo
{
    public string Info { get; set; }
}