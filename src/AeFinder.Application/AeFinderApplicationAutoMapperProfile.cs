using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Entities.Es;
using AeFinder.Etos;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Studio;
using AutoMapper;

namespace AeFinder;

public class AeFinderApplicationAutoMapperProfile : Profile
{
    public AeFinderApplicationAutoMapperProfile()
    {
        CreateMap<BlockIndex, BlockDto>();
        CreateMap<Transaction, TransactionDto>();
        CreateMap<LogEvent, LogEventDto>();

        CreateMap<TransactionIndex, TransactionDto>();
        CreateMap<LogEventIndex, LogEventDto>();

        CreateMap<BlockDto, BlockWithTransactionDto>();
        CreateMap<NewBlockEto, BlockWithTransactionDto>();
        CreateMap<ConfirmBlockEto, BlockWithTransactionDto>();

        CreateMap<TransactionCondition, FilterTransactionInput>();
        CreateMap<LogEventCondition, FilterContractEventInput>();

        CreateMap<SubscriptionManifestDto, SubscriptionManifest>();
        CreateMap<SubscriptionManifest, SubscriptionManifestDto>();
        CreateMap<SubscriptionDto, Subscription>();
        CreateMap<TransactionConditionDto, TransactionCondition>();
        CreateMap<LogEventConditionDto, LogEventCondition>();

        CreateMap<Subscription, SubscriptionDto>();
        CreateMap<TransactionCondition, TransactionConditionDto>();
        CreateMap<LogEventCondition, LogEventConditionDto>();

        CreateMap<AllSubscription, AllSubscriptionDto>();
        CreateMap<SubscriptionDetail, SubscriptionDetailDto>();
        CreateMap<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>();
        CreateMap<AeFinderAppInfo, AeFinderAppInfoDto>();
    }
}