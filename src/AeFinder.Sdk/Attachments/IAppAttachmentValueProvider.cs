namespace AeFinder.Sdk.Attachments;

public interface IAppAttachmentValueProvider
{
    string Key { get; }
    void InitValue(string value);
}

public interface IAppAttachmentValueProvider<T> : IAppAttachmentValueProvider
{
    T GetValue();
}