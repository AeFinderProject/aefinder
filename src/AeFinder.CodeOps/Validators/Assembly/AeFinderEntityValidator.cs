using AeFinder.Sdk.Entities;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AeFinder.CodeOps.Validators.Assembly;

public class AeFinderEntityValidator : IValidator<System.Reflection.Assembly>, ITransientDependency
{
    private readonly CodeOpsOptions _codeOpsOptions;

    public AeFinderEntityValidator(IOptionsSnapshot<CodeOpsOptions> codeOpsOptions)
    {
        _codeOpsOptions = codeOpsOptions.Value;
    }

    public IEnumerable<ValidationResult> Validate(System.Reflection.Assembly assembly, CancellationToken ct)
     {
        var count = assembly.DefinedTypes.Count(type =>
            typeof(IAeFinderEntity).IsAssignableFrom(type) && typeof(AeFinderEntity).IsAssignableFrom(type) && !typeof(IAeFinderEntity).IsAssignableFrom(type.BaseType) && !type.IsAbstract && type.IsClass);

        if (count > _codeOpsOptions.MaxEntityCount)
        {
            return new List<ValidationResult>
            {
                new AeFinderEntityValidationResult(
                    $"Entity count {count} exceeds the limit {_codeOpsOptions.MaxEntityCount}.")
            };
        }

        return Enumerable.Empty<ValidationResult>();
    }
}

public class AeFinderEntityValidationResult : ValidationResult
{
    public AeFinderEntityValidationResult(string message) : base(message)
    {
    }
}