using AElfIndexer.Sdk;
using Mono.Cecil;
using Nest;

namespace AElfIndexer.CodeOps.Patchers.Module;

public class EntityIndexingPatcher : IPatcher<ModuleDefinition>
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
        // (type.Properties[3].PropertyType as Mono.Cecil.GenericInstanceType).GenericArguments[0].Resolve().Properties
        // type.Properties[0].PropertyType.Resolve().Properties

        // TODO: Be careful depth
        if (type == null || type.Module.Name != module.Name)
        {
            return;
        }

        if(type.BaseType != null)
        {
            PatchType(module, type.BaseType.Resolve());
        }
        
        foreach (var property in type.Properties)
        {
            var propertyType = property.PropertyType;
            if (propertyType.FullName == typeof(string).FullName)
            {
                var fulltextAttributes = property.CustomAttributes
                    .Where(ca => ca.AttributeType.FullName == typeof(FulltextAttribute).FullName).ToList();
                if (fulltextAttributes.Count > 0)
                {
                    property.CustomAttributes.Remove(fulltextAttributes[0]);

                    var indexArgumentValue = fulltextAttributes[0].Properties
                        .FirstOrDefault(p => p.Name == nameof(FulltextAttribute.Index)).Argument;
                    var isIndex = indexArgumentValue.Value != null && (bool) indexArgumentValue.Value;
                    var attributeCtor =
                        module.ImportReference(typeof(TextAttribute).GetConstructor(new Type[] { }));
                    
                    var newAttribute = new CustomAttribute(attributeCtor);
                    newAttribute.Properties.Add(new CustomAttributeNamedArgument(nameof(TextAttribute.Index),
                        new CustomAttributeArgument(module.ImportReference(typeof(bool)), isIndex)));
                    property.CustomAttributes.Add(newAttribute);
                }
                else
                {
                    var attributeCtor = module.ImportReference(typeof(KeywordAttribute).GetConstructor(new Type[] { }));
                    var newAttribute = new CustomAttribute(attributeCtor);
                    property.CustomAttributes.Add(newAttribute);
                }
            }
            else if (propertyType is GenericInstanceType genericType)
            {
                foreach (var typeReference in genericType.GenericArguments)
                {
                    PatchType(module, typeReference.Resolve());
                }
            }
            else
            {
                PatchType(module, propertyType.Resolve());
            }
        }
    }
}