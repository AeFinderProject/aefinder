using AElfIndexer.CodeOps.Patchers;
using AElfIndexer.CodeOps.Validators;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.CodeOps.Policies;

public interface IPolicy
{
    List<IValidator<T>> GetValidators<T>();
    List<IPatcher<T>> GetPatchers<T>();
}

public class DefaultPolicy : IPolicy, ISingletonDependency
{
    private readonly List<IPatcher> _patchers;

    private readonly List<IValidator> _validators;

    public DefaultPolicy(IEnumerable<IValidator> validators, IEnumerable<IPatcher> patchers)
    {
        _validators = validators.ToList();
        _patchers = patchers.ToList();
    }

    public List<IValidator<T>> GetValidators<T>()
    {
        return _validators.OfType<IValidator<T>>().ToList();
    }

    public List<IPatcher<T>> GetPatchers<T>()
    {
        return _patchers.OfType<IPatcher<T>>().ToList();
    }
}