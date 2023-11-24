using AElfIndexer.Sdk;
using Microsoft.CodeAnalysis;
using Mono.Cecil;
using Nest;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.CodeOps.Patchers.Module.Entity;

public class EntityIndexingPatcher : IPatcher<ModuleDefinition>, ITransientDependency
{
    public void Patch(ModuleDefinition module)
    {
        var compareType = typeof(IIndexerEntity);
        var types = module.Types.Where(t =>
            t.IsClass && !t.IsAbstract &&
            t.Interfaces.Any(i => i.InterfaceType.FullName == compareType.FullName)).ToList();

        foreach (var type in types)
        {
            PatchType(module, type);
        }
    }

    private void PatchType(ModuleDefinition module, TypeDefinition type)
    {
        foreach (var property in type.Properties)
        {
            var propertyType = property.PropertyType;
            if (propertyType.FullName == typeof(string).FullName)
            {
                var fulltextAttributes = property.CustomAttributes
                    .Where(ca => ca.AttributeType.FullName == typeof(FulltextAttribute).FullName).ToList();
                if (fulltextAttributes.Count > 0)
                {
                    foreach (var attribute in fulltextAttributes)
                    {
                        property.CustomAttributes.Remove(attribute);
                    }

                    var isIndex = fulltextAttributes[0].Properties
                        .First(p => p.Name == nameof(FulltextAttribute.Index)).Argument.Value;
                    var attrCtor =
                        module.ImportReference(typeof(TextAttribute).GetConstructor(new Type[] { }));
                    var newAttr = new CustomAttribute(attrCtor)
                    {
                        ConstructorArguments = {
                        new CustomAttributeArgument(fulltextAttributes[0].ConstructorArguments[0].Type, 
                            isIndex)
                        }
                    };
                    
                    // newAttr.Properties.Add(new CustomAttributeNamedArgument(nameof(TextAttribute.Index),
                    //     new CustomAttributeArgument(fulltextAttributes[0].ConstructorArguments[0].Type, isIndex)));
                    property.CustomAttributes.Add(newAttr);
                }
                else
                {
                    var attrCtor = module.ImportReference(typeof(KeywordAttribute).GetConstructor(new Type[] {}));
                    var newAttr = new CustomAttribute(attrCtor);
                    type.CustomAttributes.Add(newAttr);
                }
            }

            // foreach (var nestedType in property.)
            // {
            //     PatchType(nestedType);
            // }
        }
    }
}