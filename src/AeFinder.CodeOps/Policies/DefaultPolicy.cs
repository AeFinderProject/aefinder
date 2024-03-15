using AeFinder.CodeOps.Validators;
using Volo.Abp.DependencyInjection;

namespace AeFinder.CodeOps.Policies;

public interface IPolicy
{
    List<IValidator<T>> GetValidators<T>();
}

public class DefaultPolicy : IPolicy, ITransientDependency
{
    private readonly List<IValidator> _validators;

    public DefaultPolicy(IEnumerable<IValidator> validators)
    {
        _validators = validators.ToList();
    }

    public List<IValidator<T>> GetValidators<T>()
    {
        return _validators.OfType<IValidator<T>>().ToList();
    }
}