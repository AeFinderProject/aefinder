using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Sdk.Attachments;

public abstract class AppAttachmentValueProviderBase<T> : IAppAttachmentValueProvider<T>, ISingletonDependency
{
    private T _value;

    public abstract string Key { get; }

    public void InitValue(string value)
    {
        _value = JsonConvert.DeserializeObject<T>(value);
    }

    public T GetValue()
    {
        return _value;
    }
}