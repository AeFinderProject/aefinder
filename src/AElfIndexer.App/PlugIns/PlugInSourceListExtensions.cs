using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Modularity.PlugIns;

namespace AElfIndexer.App.PlugIns;

public static class PlugInSourceListExtensions
{
    public static void AddCode(
        [NotNull] this PlugInSourceList list,
        [NotNull] byte[] code)
    {
        Check.NotNull(list, nameof(list));

        list.Add(new CodePlugInSource(code));
    }
}