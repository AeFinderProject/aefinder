using System.Reflection;
using AElfIndexer.CodeOps.Policies;
using AElfIndexer.CodeOps.Validators;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Mono.Cecil;

namespace AElfIndexer.CodeOps;

public interface ICodeAuditor
{
    void Audit(byte[] code);
}

public class CodeAuditor : ICodeAuditor, ITransientDependency
{
    private readonly IPolicy _policy;
    private readonly CodeOpsOptions _codeOpsOptions;

    public CodeAuditor(IPolicy policy, IOptionsSnapshot<CodeOpsOptions> codeOpsOptions)
    {
        _policy = policy;
        _codeOpsOptions = codeOpsOptions.Value;
    }

    public void Audit(byte[] code)
    {
        var findings = new List<ValidationResult>();
        var asm = Assembly.Load(code);
        var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
        var cts = new CancellationTokenSource(_codeOpsOptions.AuditTimeoutDuration);
        // Run module validators
        findings.AddRange(Validate(modDef, cts.Token));

        // Run assembly validators
        findings.AddRange(Validate(asm, cts.Token));

        // Run method validators
        foreach (var type in modDef.Types)
        {
            findings.AddRange(ValidateMethodsInType(type, cts.Token));
        }

        if (findings.Count > 0)
        {
            throw new CodeCheckException(
                $"Code did not pass audit. Audit failed for module: {modDef.Assembly.MainModule.Name}\n" +
                string.Join("\n", findings), findings);
        }
    }
    
    private IEnumerable<ValidationResult> Validate<T>(T t, CancellationToken ct)
    {
        return _policy.GetValidators<T>().SelectMany(v => v.Validate(t, ct));
    }
        
    private IEnumerable<ValidationResult> ValidateMethodsInType(TypeDefinition type,
        CancellationToken ct)
    {
        var findings = new List<ValidationResult>();

        foreach (var method in type.Methods)
        {
            findings.AddRange(Validate(method, ct));
        }

        foreach (var nestedType in type.NestedTypes)
        {
            findings.AddRange(ValidateMethodsInType(nestedType, ct));
        }

        return findings;
    }
}