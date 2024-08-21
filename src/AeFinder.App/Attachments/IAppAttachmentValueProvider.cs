namespace AeFinder.App.Attachments;

public interface IAppAttachmentValueProvider
{
    string Key { get; }
    Task InitValueAsync();
}

public interface IAppAttachmentValueProvider<T> : IAppAttachmentValueProvider
{
    T GetValue();
}