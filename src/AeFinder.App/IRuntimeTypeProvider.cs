using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App;

public interface IRuntimeTypeProvider
{
    Type GetType(string typeName);
}

public class RuntimeTypeProvider : IRuntimeTypeProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, Type> _types = new();

    public Type GetType(string typeName)
    {
        if (_types.TryGetValue(typeName, out var type))
        {
            return type;
        }

        type = Type.GetType(typeName);

        _types[typeName] = type ?? throw new Exception($"Cannot find type: {typeName}.");

        return type;
    }
}