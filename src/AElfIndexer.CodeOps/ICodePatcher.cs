using Volo.Abp.DependencyInjection;

namespace AElfIndexer.CodeOps;

public interface ICodePatcher
{
    byte[] Patch(byte[] code);
}

public class CodePatcher : ICodePatcher, ISingletonDependency
{
    public byte[] Patch(byte[] code)
    {
        throw new NotImplementedException();
    }
}