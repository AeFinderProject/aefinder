using System.Reflection;
using System.Runtime.CompilerServices;
using AElf.Types;
using AElfIndexer.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.CodeOps.Validators.Whitelist;

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
            .Assembly(System.Reflection.Assembly.Load("System.Private.CoreLib"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.ObjectModel"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Linq"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("System.Linq.Expressions"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("System.Collections"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("Google.Protobuf"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("GraphQL"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("AutoMapper"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("Volo.Abp.ObjectMapping"), Trust.Partial)
            .Assembly(typeof(IndexerEntity).Assembly, Trust.Full) // AElfIndexer.Sdk
            .Assembly(typeof(Address).Assembly, Trust.Full) // AElf.Types
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
                .Type("Nullable`1", Permission.Allowed) // Required for protobuf generated code
                .Type(typeof(BitConverter), Permission.Allowed)
                .Type(typeof(Uri), Permission.Allowed)
                .Type(typeof(NotImplementedException), Permission.Allowed) // Required for protobuf generated code
                .Type(typeof(NotSupportedException), Permission.Allowed) // Required for protobuf generated code
                .Type(typeof(ArgumentOutOfRangeException), Permission.Allowed) // From AEDPoS
                .Type(nameof(DateTime), Permission.Allowed)
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
            );
    }

    private void WhitelistReflectionTypes(Whitelist whitelist)
    {
        whitelist
            // Used by protobuf generated code
            .Namespace("System.Reflection", Permission.Denied, type => type
                .Type(nameof(AssemblyCompanyAttribute), Permission.Allowed)
                .Type(nameof(AssemblyConfigurationAttribute), Permission.Allowed)
                .Type(nameof(AssemblyFileVersionAttribute), Permission.Allowed)
                .Type(nameof(AssemblyInformationalVersionAttribute), Permission.Allowed)
                .Type(nameof(AssemblyProductAttribute), Permission.Allowed)
                .Type(nameof(AssemblyTitleAttribute), Permission.Allowed))
            ;
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
            // Used for initializing large arrays hardcoded in the code, array validator will take care of the size
            .Namespace("System.Runtime.CompilerServices", Permission.Denied, type => type
                .Type(nameof(RuntimeHelpers), Permission.Denied, member => member
                    .Member(nameof(RuntimeHelpers.InitializeArray), Permission.Allowed))
                .Type(nameof(DefaultInterpolatedStringHandler), Permission.Allowed)
            )
            .Namespace("System.Text", Permission.Allowed)
            .Namespace("System.Numerics", Permission.Allowed)
            .Namespace("System.Guid", Permission.Allowed)
            .Namespace("System.HashCode", Permission.Allowed)
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