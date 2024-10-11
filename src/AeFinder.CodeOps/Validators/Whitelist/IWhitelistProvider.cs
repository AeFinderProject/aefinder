using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AElf.Types;
using AeFinder.Sdk.Entities;
using AElf.Cryptography.SecretSharing;
using AElf.CSharp.Core;
using AElf.EntityMapping;
using AElf.EntityMapping.Elasticsearch;
using GraphQL;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.CodeOps.Validators.Whitelist;

public interface IWhitelistProvider
{
    Whitelist GetWhitelist();
}

public class WhitelistProvider : IWhitelistProvider, ISingletonDependency
{
    private Whitelist _whitelist;

    public Whitelist GetWhitelist()
    {
        if (_whitelist != null)
            return _whitelist;
        _whitelist = CreateWhitelist();
        return _whitelist;
    }

    private void WhitelistAssemblies(Whitelist whitelist)
    {
        whitelist
            .Assembly(System.Reflection.Assembly.Load("netstandard"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Runtime"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Runtime.Extensions"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Runtime.InteropServices"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Runtime.Numerics"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Private.CoreLib"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.ObjectModel"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Linq"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("System.Linq.Expressions"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("System.Linq.Queryable"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("System.Collections"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("System.ComponentModel"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("Microsoft.Extensions.DependencyInjection.Abstractions"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("Google.Protobuf"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("GraphQL"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("Nest"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("AutoMapper"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("Volo.Abp.ObjectMapping"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("Volo.Abp.AutoMapper"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("Volo.Abp.Core"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("AeFinder.Domain"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("Newtonsoft.Json"), Trust.Full)
            .Assembly(typeof(AeFinderEntity).Assembly, Trust.Full) // AeFinder.Sdk
            .Assembly(typeof(Address).Assembly, Trust.Full) // AElf.Types
            .Assembly(typeof(IMethod).Assembly, Trust.Full) // AElf.CSharp.Core
            .Assembly(typeof(SecretSharingHelper).Assembly, Trust.Full) // AElf.Cryptography
            .Assembly(typeof(AElfEntityMappingModule).Assembly, Trust.Partial)
            .Assembly(typeof(AElfEntityMappingElasticsearchModule).Assembly, Trust.Partial)
            ;
    }

    private void WhitelistSystemTypes(Whitelist whitelist)
    {
        whitelist
            // Selectively allowed types and members
            .Namespace("System", Permission.Denied, type => type
                .Type(typeof(Array), Permission.Allowed)
                .Type("Func`1", Permission.Allowed) // Required for protobuf generated code
                .Type("Func`2", Permission.Allowed) // Required for protobuf generated code
                .Type("Func`3", Permission.Allowed) // Required for protobuf generated code
                .Type("Action`1", Permission.Allowed) 
                .Type("Action`2", Permission.Allowed)
                .Type("Action`3", Permission.Allowed) 
                .Type("Nullable`1", Permission.Allowed) // Required for protobuf generated code
                .Type("Predicate`1",Permission.Allowed)
                .Type(typeof(BitConverter), Permission.Allowed)
                .Type(typeof(Uri), Permission.Allowed)
                .Type(typeof(NotImplementedException), Permission.Allowed) // Required for protobuf generated code
                .Type(typeof(NotSupportedException), Permission.Allowed) // Required for protobuf generated code
                .Type(typeof(ArgumentOutOfRangeException), Permission.Allowed) // From AEDPoS
                .Type(typeof(ArgumentException), Permission.Allowed)
                .Type(nameof(DateTime), Permission.Allowed)
                .Type(nameof(DateTimeOffset), Permission.Allowed)
                .Type(nameof(TimeSpan), Permission.Allowed)
                .Type(typeof(void).Name, Permission.Allowed)
                .Type(nameof(Object), Permission.Allowed)
                .Type(nameof(Type), Permission.Allowed)
                .Type(nameof(IDisposable), Permission.Allowed)
                .Type(nameof(Convert), Permission.Allowed)
                .Type(nameof(Math), Permission.Allowed)
                // Primitive types
                .Type(nameof(Boolean), Permission.Allowed)
                .Type(nameof(Byte), Permission.Allowed)
                .Type(nameof(SByte), Permission.Allowed)
                .Type(nameof(Char), Permission.Allowed)
                .Type(nameof(Int32), Permission.Allowed)
                .Type(nameof(UInt32), Permission.Allowed)
                .Type(nameof(Int64), Permission.Allowed)
                .Type(nameof(UInt64), Permission.Allowed)
                .Type(nameof(Decimal), Permission.Allowed)
                .Type(nameof(String), Permission.Allowed)
                .Type(typeof(byte[]).Name, Permission.Allowed)
                .Type(nameof(Double), Permission.Allowed)
                .Type(nameof(Single), Permission.Allowed)
                .Type(nameof(Exception), Permission.Allowed)
                .Type(nameof(Attribute), Permission.Allowed)
                .Type(nameof(Guid), Permission.Allowed)
                .Type(nameof(Random), Permission.Allowed)
                .Type(nameof(AbpStringExtensions), Permission.Allowed)
                .Type("Span`1",Permission.Allowed)
            );
    }

    private void WhitelistReflectionTypes(Whitelist whitelist)
    {
        whitelist
            // Used by protobuf generated code and GraphQL
            .Namespace("System.Reflection", Permission.Denied, type => type
                .Type(nameof(AssemblyCompanyAttribute), Permission.Allowed)
                .Type(nameof(AssemblyConfigurationAttribute), Permission.Allowed)
                .Type(nameof(AssemblyFileVersionAttribute), Permission.Allowed)
                .Type(nameof(AssemblyInformationalVersionAttribute), Permission.Allowed)
                .Type(nameof(AssemblyProductAttribute), Permission.Allowed)
                .Type(nameof(AssemblyTitleAttribute), Permission.Allowed)
                .Type(nameof(MethodBase), Permission.Allowed)
                .Type(nameof(MethodInfo), Permission.Allowed)
                .Type(nameof(FieldInfo), Permission.Allowed)
            );
    }

    private void WhitelistLinqAndCollections(Whitelist whitelist)
    {
        whitelist
            .Namespace("System.Linq", Permission.Allowed)
            .Namespace("System.Collections", Permission.Allowed)
            .Namespace("System.Collections.Generic", Permission.Allowed)
            .Namespace("System.Collections.ObjectModel", Permission.Allowed)
            ;
    }

    private void WhitelistOthers(Whitelist whitelist)
    {
        whitelist
            .Namespace("System.Globalization", Permission.Allowed)
            .Namespace("System.Runtime.CompilerServices", Permission.Denied, type => type
                .Type(nameof(RuntimeHelpers), Permission.Denied, member => member
                    .Member(nameof(RuntimeHelpers.InitializeArray), Permission.Allowed))
                .Type(nameof(DefaultInterpolatedStringHandler), Permission.Allowed)
                .Type("AsyncTaskMethodBuilder", Permission.Allowed)
                .Type("AsyncTaskMethodBuilder`1", Permission.Allowed)
                .Type("TaskAwaiter", Permission.Allowed)
                .Type("TaskAwaiter`1", Permission.Allowed)
                .Type(nameof(SwitchExpressionException), Permission.Allowed)
            )
            .Namespace("System.Runtime.InteropServices", Permission.Denied, type => type
                .Type(nameof(CollectionsMarshal), Permission.Allowed)
            )
            .Namespace("System.Text", Permission.Allowed)
            .Namespace("System.Numerics", Permission.Allowed)
            .Namespace("System.Guid", Permission.Allowed)
            .Namespace("System.HashCode", Permission.Allowed)
            .Namespace("System.Threading.Tasks", Permission.Denied, type => type
                .Type(nameof(Task), Permission.Allowed)
                .Type("Task`1", Permission.Allowed)
            )
            .Namespace("GraphQL", Permission.Denied, type => type
                        .Type(nameof(FromServicesAttribute), Permission.Allowed)
            )
            .Namespace("Volo.Abp.Modularity", Permission.Denied, type => type
                .Type(nameof(AbpModule), Permission.Allowed)
                .Type(nameof(ServiceConfigurationContext), Permission.Allowed)
            )
            .Namespace("Nest", Permission.Denied, type => type
                .Type(nameof(KeywordAttribute), Permission.Allowed)
                .Type(nameof(TextAttribute), Permission.Allowed)
            )
            .Namespace("AutoMapper", Permission.Allowed)
            .Namespace("Volo.Abp.AutoMapper", Permission.Allowed)
            .Namespace("AeFinder.Entities", Permission.Denied, type => type
                .Type("AeFinderEntity`1", Permission.Allowed)
            )
            .Namespace("Volo.Abp.DependencyInjection", Permission.Allowed)
            .Namespace("AElf.EntityMapping.Elasticsearch.Linq", Permission.Denied, type => type
                .Type("NestedAttributes", Permission.Allowed)
            )
            .Namespace("Newtonsoft.Json", Permission.Allowed)
            ;
    }

    private Whitelist CreateWhitelist()
    {
        var whitelist = new Whitelist();
        WhitelistAssemblies(whitelist);
        WhitelistSystemTypes(whitelist);
        WhitelistReflectionTypes(whitelist);
        WhitelistLinqAndCollections(whitelist);
        WhitelistOthers(whitelist);
        return whitelist;
    }
}