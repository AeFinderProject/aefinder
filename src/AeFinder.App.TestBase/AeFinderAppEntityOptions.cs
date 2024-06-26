using System.Reflection;
using AeFinder.Sdk.Entities;

namespace AeFinder.App.TestBase;

public class AeFinderAppEntityOptions
{
    public List<Type> EntityTypes { get; set; }

    public void AddTypes<TModule>()
    {
        var types = GetTypesAssignableFrom<IAeFinderEntity>(typeof(TModule).Assembly);
        EntityTypes = types;
    }
    
    private List<Type> GetTypesAssignableFrom<T>(Assembly assembly)
    {
        var compareType = typeof(T);
        return assembly.DefinedTypes
            .Where(type => compareType.IsAssignableFrom(type) && !compareType.IsAssignableFrom(type.BaseType) &&
                           !type.IsAbstract && type.IsClass && compareType != type)
            .Cast<Type>().ToList();
    }
}