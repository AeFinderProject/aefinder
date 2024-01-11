using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Entities.Es;
using AElfIndexer.Etos;
using AElfIndexer.Grains.Grain.ScanApps;
using AElfIndexer.Grains.State.Subscriptions;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfIndexer;

public class AElfIndexerApplicationAutoMapperProfile:Profile
{
    public AElfIndexerApplicationAutoMapperProfile()
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

        CreateMap<SubscriptionDto, Subscription>();
        CreateMap<Subscription, SubscriptionDto>();
        CreateMap<SubscriptionItemDto, SubscriptionItem>();
        CreateMap<TransactionConditionDto, TransactionCondition>();
        CreateMap<LogEventConditionDto, LogEventCondition>();

        CreateMap<SubscriptionItem, SubscriptionItemDto>();
        CreateMap<TransactionCondition, TransactionConditionDto>();
        CreateMap<LogEventCondition, LogEventConditionDto>();
        
        CreateMap<AllSubscription, AllSubscriptionDto>();
        CreateMap<SubscriptionDetail, AllSubscriptionDetailDto>();
    }
}