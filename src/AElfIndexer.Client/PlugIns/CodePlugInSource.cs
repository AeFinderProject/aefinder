using System.Runtime.Loader;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Modularity.PlugIns;

namespace AElfIndexer.Client.PlugIns;

public class CodePlugInSource : IPlugInSource
{
    private byte[] Code { get; }

    public CodePlugInSource(byte[] code)
    {
        Code = code;
    }

    public Type[] GetModules()
    {
        var source = new List<Type>();
        var assembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(Code));
        try
        {
            foreach (var type in assembly.GetTypes())
            {
                if (AbpModule.IsAbpModule(type))
                {
                    source.AddIfNotContains<Type>(type);
                }
            }
        }
        catch (Exception ex)
        {
            throw new AbpException("Could not get module types from assembly: " + assembly.FullName, ex);
        }

        return source.ToArray();
    }
}