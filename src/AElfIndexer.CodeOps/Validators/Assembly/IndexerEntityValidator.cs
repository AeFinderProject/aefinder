using AElfIndexer.Sdk;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.CodeOps.Validators.Assembly;

public interface IIndexerEntityValidator
{
    IEnumerable<ValidationResult> Validate(System.Reflection.Assembly assembly);   
}

public class IndexerEntityValidator : IValidator<System.Reflection.Assembly>, ITransientDependency
{
    private readonly CodeOpsOptions _codeOpsOptions;

    public IndexerEntityValidator(IOptionsSnapshot<CodeOpsOptions> codeOpsOptions)
    {
        _codeOpsOptions = codeOpsOptions.Value;
    }

    public IEnumerable<ValidationResult> Validate(System.Reflection.Assembly item, CancellationToken ct)
    {
        var compareType = typeof(IIndexerEntity);
        var count = item.DefinedTypes.Count(type =>
            compareType.IsAssignableFrom(type) && !compareType.IsAssignableFrom(type.BaseType) && !type.IsAbstract &&
            type.IsClass && compareType != type);

        if (count > _codeOpsOptions.MaxEntityCount)
        {
            return new List<ValidationResult>
            {
                new IndexerEntityValidationResult(
                    $"Entity count {count} exceeds the limit {_codeOpsOptions.MaxEntityCount}.")
            };
        }

        return Enumerable.Empty<ValidationResult>();
    }
}

public class IndexerEntityValidationResult : ValidationResult
{
    public IndexerEntityValidationResult(string message) : base(message)
    {
    }
}