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
        if (CallCount >= _options.MaxEntityCallCount)
        {
            throw new ApplicationException("Too many calls");
        }

        CallCount++;

        if (_objectSerializer.Serialize(entity).Length > _options.MaxEntitySize)
        {
            throw new ApplicationException("Too large entity");
        }
    }
}