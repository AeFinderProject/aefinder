// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Protobuf/contract/billing_contract.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using System.Collections.Generic;
using aelf = global::AElf.CSharp.Core;

namespace AeFinder.Contracts {

  #region Events
  public partial class FeeSymbolSet : aelf::IEvent<FeeSymbolSet>
  {
    public global::System.Collections.Generic.IEnumerable<FeeSymbolSet> GetIndexed()
    {
      return new List<FeeSymbolSet>
      {
      };
    }

    public FeeSymbolSet GetNonIndexed()
    {
      return new FeeSymbolSet
      {
        Symbols = Symbols,
      };
    }
  }

  public partial class OrganizationCreated : aelf::IEvent<OrganizationCreated>
  {
    public global::System.Collections.Generic.IEnumerable<OrganizationCreated> GetIndexed()
    {
      return new List<OrganizationCreated>
      {
      };
    }

    public OrganizationCreated GetNonIndexed()
    {
      return new OrganizationCreated
      {
        Address = Address,
        Members = Members,
      };
    }
  }

  public partial class Deposited : aelf::IEvent<Deposited>
  {
    public global::System.Collections.Generic.IEnumerable<Deposited> GetIndexed()
    {
      return new List<Deposited>
      {
      };
    }

    public Deposited GetNonIndexed()
    {
      return new Deposited
      {
        Address = Address,
        Symbol = Symbol,
        Amount = Amount,
      };
    }
  }

  public partial class Withdrawn : aelf::IEvent<Withdrawn>
  {
    public global::System.Collections.Generic.IEnumerable<Withdrawn> GetIndexed()
    {
      return new List<Withdrawn>
      {
      };
    }

    public Withdrawn GetNonIndexed()
    {
      return new Withdrawn
      {
        Address = Address,
        Symbol = Symbol,
        Amount = Amount,
        ToAddress = ToAddress,
      };
    }
  }

  public partial class Locked : aelf::IEvent<Locked>
  {
    public global::System.Collections.Generic.IEnumerable<Locked> GetIndexed()
    {
      return new List<Locked>
      {
      };
    }

    public Locked GetNonIndexed()
    {
      return new Locked
      {
        Address = Address,
        Symbol = Symbol,
        Amount = Amount,
        BillingId = BillingId,
      };
    }
  }

  public partial class Charged : aelf::IEvent<Charged>
  {
    public global::System.Collections.Generic.IEnumerable<Charged> GetIndexed()
    {
      return new List<Charged>
      {
      };
    }

    public Charged GetNonIndexed()
    {
      return new Charged
      {
        Address = Address,
        Symbol = Symbol,
        ChargedAmount = ChargedAmount,
        UnlockedAmount = UnlockedAmount,
        BillingId = BillingId,
      };
    }
  }

  public partial class FeeReceived : aelf::IEvent<FeeReceived>
  {
    public global::System.Collections.Generic.IEnumerable<FeeReceived> GetIndexed()
    {
      return new List<FeeReceived>
      {
      };
    }

    public FeeReceived GetNonIndexed()
    {
      return new FeeReceived
      {
        UserAddress = UserAddress,
        FeeAddress = FeeAddress,
        Symbol = Symbol,
        Amount = Amount,
      };
    }
  }

  #endregion
  // public static partial class BillingContractContainer
  // {
  //   static readonly string __ServiceName = "BillingContract";
  //
  //   #region Marshallers
  //   static readonly aelf::Marshaller<global::AeFinder.Contracts.InitializeInput> __Marshaller_InitializeInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AeFinder.Contracts.InitializeInput.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.Empty> __Marshaller_google_protobuf_Empty = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.Empty.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::AElf.Types.Address> __Marshaller_aelf_Address = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AElf.Types.Address.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::AeFinder.Contracts.SymbolList> __Marshaller_SymbolList = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AeFinder.Contracts.SymbolList.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::AeFinder.Contracts.DepositInput> __Marshaller_DepositInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AeFinder.Contracts.DepositInput.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::AeFinder.Contracts.WithdrawInput> __Marshaller_WithdrawInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AeFinder.Contracts.WithdrawInput.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::AeFinder.Contracts.LockInput> __Marshaller_LockInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AeFinder.Contracts.LockInput.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::AeFinder.Contracts.LockFromInput> __Marshaller_LockFromInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AeFinder.Contracts.LockFromInput.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::AeFinder.Contracts.ChargeInput> __Marshaller_ChargeInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AeFinder.Contracts.ChargeInput.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::AeFinder.Contracts.GetBalanceInput> __Marshaller_GetBalanceInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AeFinder.Contracts.GetBalanceInput.Parser.ParseFrom);
  //   static readonly aelf::Marshaller<global::AeFinder.Contracts.GetBalanceOutput> __Marshaller_GetBalanceOutput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AeFinder.Contracts.GetBalanceOutput.Parser.ParseFrom);
  //   #endregion
  //
  //   #region Methods
  //   static readonly aelf::Method<global::AeFinder.Contracts.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Initialize = new aelf::Method<global::AeFinder.Contracts.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "Initialize",
  //       __Marshaller_InitializeInput,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetAdmin = new aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "SetAdmin",
  //       __Marshaller_aelf_Address,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetTreasurer = new aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "SetTreasurer",
  //       __Marshaller_aelf_Address,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetFeeAddress = new aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "SetFeeAddress",
  //       __Marshaller_aelf_Address,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::AeFinder.Contracts.SymbolList, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetFeeSymbol = new aelf::Method<global::AeFinder.Contracts.SymbolList, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "SetFeeSymbol",
  //       __Marshaller_SymbolList,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::AeFinder.Contracts.DepositInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Deposit = new aelf::Method<global::AeFinder.Contracts.DepositInput, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "Deposit",
  //       __Marshaller_DepositInput,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::AeFinder.Contracts.WithdrawInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Withdraw = new aelf::Method<global::AeFinder.Contracts.WithdrawInput, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "Withdraw",
  //       __Marshaller_WithdrawInput,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::AeFinder.Contracts.LockInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Lock = new aelf::Method<global::AeFinder.Contracts.LockInput, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "Lock",
  //       __Marshaller_LockInput,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::AeFinder.Contracts.LockFromInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_LockFrom = new aelf::Method<global::AeFinder.Contracts.LockFromInput, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "LockFrom",
  //       __Marshaller_LockFromInput,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::AeFinder.Contracts.ChargeInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Charge = new aelf::Method<global::AeFinder.Contracts.ChargeInput, global::Google.Protobuf.WellKnownTypes.Empty>(
  //       aelf::MethodType.Action,
  //       __ServiceName,
  //       "Charge",
  //       __Marshaller_ChargeInput,
  //       __Marshaller_google_protobuf_Empty);
  //
  //   static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address> __Method_GetAdmin = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address>(
  //       aelf::MethodType.View,
  //       __ServiceName,
  //       "GetAdmin",
  //       __Marshaller_google_protobuf_Empty,
  //       __Marshaller_aelf_Address);
  //
  //   static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address> __Method_GetTreasurer = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address>(
  //       aelf::MethodType.View,
  //       __ServiceName,
  //       "GetTreasurer",
  //       __Marshaller_google_protobuf_Empty,
  //       __Marshaller_aelf_Address);
  //
  //   static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address> __Method_GetFeeAddress = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address>(
  //       aelf::MethodType.View,
  //       __ServiceName,
  //       "GetFeeAddress",
  //       __Marshaller_google_protobuf_Empty,
  //       __Marshaller_aelf_Address);
  //
  //   static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AeFinder.Contracts.SymbolList> __Method_GetFeeSymbols = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AeFinder.Contracts.SymbolList>(
  //       aelf::MethodType.View,
  //       __ServiceName,
  //       "GetFeeSymbols",
  //       __Marshaller_google_protobuf_Empty,
  //       __Marshaller_SymbolList);
  //
  //   static readonly aelf::Method<global::AeFinder.Contracts.GetBalanceInput, global::AeFinder.Contracts.GetBalanceOutput> __Method_GetBalance = new aelf::Method<global::AeFinder.Contracts.GetBalanceInput, global::AeFinder.Contracts.GetBalanceOutput>(
  //       aelf::MethodType.View,
  //       __ServiceName,
  //       "GetBalance",
  //       __Marshaller_GetBalanceInput,
  //       __Marshaller_GetBalanceOutput);
  //
  //   #endregion
  //
  //   #region Descriptors
  //   public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
  //   {
  //     get { return global::AeFinder.Contracts.BillingContractReflection.Descriptor.Services[0]; }
  //   }
  //
  //   public static global::System.Collections.Generic.IReadOnlyList<global::Google.Protobuf.Reflection.ServiceDescriptor> Descriptors
  //   {
  //     get
  //     {
  //       return new global::System.Collections.Generic.List<global::Google.Protobuf.Reflection.ServiceDescriptor>()
  //       {
  //         global::AElf.Standards.ACS12.Acs12Reflection.Descriptor.Services[0],
  //         global::AeFinder.Contracts.BillingContractReflection.Descriptor.Services[0],
  //       };
  //     }
  //   }
  //   #endregion
  //
  //   /// <summary>Base class for the contract of BillingContract</summary>
  //   public abstract partial class BillingContractBase : AElf.Sdk.CSharp.CSharpSmartContract<AeFinder.Contracts.BillingContractState>
  //   {
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty Initialize(global::AeFinder.Contracts.InitializeInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty SetAdmin(global::AElf.Types.Address input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty SetTreasurer(global::AElf.Types.Address input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty SetFeeAddress(global::AElf.Types.Address input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty SetFeeSymbol(global::AeFinder.Contracts.SymbolList input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty Deposit(global::AeFinder.Contracts.DepositInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty Withdraw(global::AeFinder.Contracts.WithdrawInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty Lock(global::AeFinder.Contracts.LockInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty LockFrom(global::AeFinder.Contracts.LockFromInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty Charge(global::AeFinder.Contracts.ChargeInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::AElf.Types.Address GetAdmin(global::Google.Protobuf.WellKnownTypes.Empty input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::AElf.Types.Address GetTreasurer(global::Google.Protobuf.WellKnownTypes.Empty input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::AElf.Types.Address GetFeeAddress(global::Google.Protobuf.WellKnownTypes.Empty input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::AeFinder.Contracts.SymbolList GetFeeSymbols(global::Google.Protobuf.WellKnownTypes.Empty input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::AeFinder.Contracts.GetBalanceOutput GetBalance(global::AeFinder.Contracts.GetBalanceInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //   }
  //
  //   public static aelf::ServerServiceDefinition BindService(BillingContractBase serviceImpl)
  //   {
  //     return aelf::ServerServiceDefinition.CreateBuilder()
  //         .AddDescriptors(Descriptors)
  //         .AddMethod(__Method_Initialize, serviceImpl.Initialize)
  //         .AddMethod(__Method_SetAdmin, serviceImpl.SetAdmin)
  //         .AddMethod(__Method_SetTreasurer, serviceImpl.SetTreasurer)
  //         .AddMethod(__Method_SetFeeAddress, serviceImpl.SetFeeAddress)
  //         .AddMethod(__Method_SetFeeSymbol, serviceImpl.SetFeeSymbol)
  //         .AddMethod(__Method_Deposit, serviceImpl.Deposit)
  //         .AddMethod(__Method_Withdraw, serviceImpl.Withdraw)
  //         .AddMethod(__Method_Lock, serviceImpl.Lock)
  //         .AddMethod(__Method_LockFrom, serviceImpl.LockFrom)
  //         .AddMethod(__Method_Charge, serviceImpl.Charge)
  //         .AddMethod(__Method_GetAdmin, serviceImpl.GetAdmin)
  //         .AddMethod(__Method_GetTreasurer, serviceImpl.GetTreasurer)
  //         .AddMethod(__Method_GetFeeAddress, serviceImpl.GetFeeAddress)
  //         .AddMethod(__Method_GetFeeSymbols, serviceImpl.GetFeeSymbols)
  //         .AddMethod(__Method_GetBalance, serviceImpl.GetBalance).Build();
  //   }
  //
  // }
}
#endregion

