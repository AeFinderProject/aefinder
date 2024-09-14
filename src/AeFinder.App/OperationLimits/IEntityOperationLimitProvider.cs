using AeFinder.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Serialization;

namespace AeFinder.App.OperationLimits;

public interface IEntityOperationLimitProvider: IOperationLimitProvider
{
    void Check<TEntity>(TEntity entity);
}

public class EntityOperationLimitProvider : CallCountOperationLimitProvider, IEntityOperationLimitProvider,
    ISingletonDependency
{
    private readonly IObjectSerializer _objectSerializer;
    private readonly OperationLimitOptions _options;

    public EntityOperationLimitProvider(IObjectSerializer objectSerializer,
        IOptionsSnapshot<OperationLimitOptions> options)
    {
        _objectSerializer = objectSerializer;
        _options = options.Value;
    }

    public void Check<TEntity>(TEntity entity)
    {
        CallCount++;

        if (CallCount > _options.MaxEntityCallCount)
        {
            throw new OperationLimitException(
                $"Too many entity calls. The maximum of calls allowed is {_options.MaxEntityCallCount} per block.");
        }

        var length = _objectSerializer.Serialize(entity).Length;
        if (length > _options.MaxEntitySize)
        {
            throw new OperationLimitException(
                $"Too large entity. The entity {length} exceeds the maximum value {_options.MaxEntitySize}");
        }
    }
}