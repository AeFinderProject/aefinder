using AeFinder.Apps;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.Attachments;

public abstract class AppAttachmentValueProviderBase<T> : IAppAttachmentValueProvider<T>, ISingletonDependency
{
    private T _value;
    private readonly IAppAttachmentService _appAttachmentService;
    private readonly IAppInfoProvider _appInfoProvider;

    protected AppAttachmentValueProviderBase(IAppInfoProvider appInfoProvider, IAppAttachmentService appAttachmentService)
    {
        _appInfoProvider = appInfoProvider;
        _appAttachmentService = appAttachmentService;
    }

    public abstract string Key { get; }

    public async Task InitValueAsync()
    {
        var content = await _appAttachmentService.GetAppAttachmentContentAsync(_appInfoProvider.AppId,
            _appInfoProvider.Version, Key);
        _value = JsonConvert.DeserializeObject<T>(content);
    }

    public T GetValue()
    {
        return _value;
    }
}