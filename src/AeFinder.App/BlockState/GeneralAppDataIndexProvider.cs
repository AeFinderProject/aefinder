using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockState;

public class GeneralAppDataIndexProvider : IGeneralAppDataIndexProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, object> _appDataIndexProviders = new();
    private readonly ConcurrentDictionary<string, MethodInfo> _addOrUpdateMethods = new();
    private readonly ConcurrentDictionary<string, MethodInfo> _deleteMethods = new();

    private const string AddOrUpdateMethodName = nameof(IAppDataIndexProvider<EmptyAeFinderEntity>.AddOrUpdateAsync);
    private const string DeleteMethodName = nameof(IAppDataIndexProvider<EmptyAeFinderEntity>.DeleteAsync);

    private readonly IServiceProvider _serviceProvider;
    private readonly IAppInfoProvider _appInfoProvider;

    public GeneralAppDataIndexProvider(IServiceProvider serviceProvider, IAppInfoProvider appInfoProvider)
    {
        _serviceProvider = serviceProvider;
        _appInfoProvider = appInfoProvider;
    }

    public Task AddOrUpdateAsync(object entity, Type type)
    {
        var provider = GetProvider(type);
        var method = GetMethod(type, AddOrUpdateMethodName, _addOrUpdateMethods);
        method.Invoke(provider, new[] { entity, GetIndexName(type) });
        return Task.CompletedTask;
    }

    public Task DeleteAsync(object entity, Type type)
    {
        var provider = GetProvider(type);
        var method = GetMethod(type, DeleteMethodName, _deleteMethods);
        method.Invoke(provider, new[] { entity, GetIndexName(type) });
        return Task.CompletedTask;
    }

    private object GetProvider(Type type)
    {
        if (_appDataIndexProviders.TryGetValue(type.FullName, out var provider))
        {
            return provider;
        }

        var interfaceType = GetInterfaceType(type);
        provider = _serviceProvider.GetRequiredService(interfaceType);
        _appDataIndexProviders[type.FullName] = provider;

        return provider;
    }

    private MethodInfo GetMethod(Type type, string methodName, ConcurrentDictionary<string,MethodInfo> methodCache)
    {
        if (methodCache.TryGetValue(type.FullName, out var method))
        {
            return method;
        }

        var interfaceType = GetInterfaceType(type);
        method = interfaceType.GetMethod(methodName);
        if (method == null)
        {
            throw new Exception($"Cannot find method: {methodName} from type: {type.FullName}");
        }

        methodCache[type.FullName] = method;

        return method;
    }

    private Type GetInterfaceType(Type type)
    {
        var appDataIndexProviderInterfaceType = typeof(IAppDataIndexProvider<>);
        return appDataIndexProviderInterfaceType.MakeGenericType(type);
    }

    private string GetIndexName(Type type)
    {
        return $"{_appInfoProvider.AppId}-{_appInfoProvider.Version}.{type.Name}".ToLower();
    }
}