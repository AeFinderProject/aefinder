using AElfIndexer.CodeOps.Policies;
using Mono.Cecil;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.CodeOps;

public interface ICodePatcher
{
    byte[] Patch(byte[] code);
}

public class CodePatcher : ICodePatcher, ITransientDependency
{
    
    private readonly IPolicy _policy;

    public CodePatcher(IPolicy policy)
    {
        _policy = policy;
    }

    public byte[] Patch(byte[] code)
    {
        var assemblyDef = AssemblyDefinition.ReadAssembly(new MemoryStream(code));
        Patch(assemblyDef.MainModule);
        var newCode = new MemoryStream();
        assemblyDef.Write(newCode);
        return newCode.ToArray();
    }
    
    private void Patch<T>(T t)
    {
        _policy.GetPatchers<T>().ForEach(v => v.Patch(t));
    }
}