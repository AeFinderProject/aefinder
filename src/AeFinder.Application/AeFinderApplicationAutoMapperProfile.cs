using System;
using System.Linq;
using AeFinder.ApiKeys;
using AeFinder.App.Es;
using AeFinder.AppResources;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Apps.Eto;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Entities.Es;
using AeFinder.Etos;
using AeFinder.Grains.Grain.ApiKeys;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Grains.State.ApiKeys;
using AeFinder.Grains.State.Apps;
using AeFinder.Grains.State.Market;
using AeFinder.Grains.State.Subscriptions;
using AeFinder.Grains.State.Users;
using AeFinder.Logger.Entities;
using AeFinder.Market;
using AeFinder.Subscriptions;
using AeFinder.Subscriptions.Dto;
using AeFinder.User;
using AeFinder.User.Dto;
using AutoMapper;
using Volo.Abp.AutoMapper;
using Volo.Abp.Identity;
using SubscriptionInfo = AeFinder.App.Es.SubscriptionInfo;

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
        CreateMap<SummaryIndex, SummaryDto>();

        CreateMap<BlockDto, BlockWithTransactionDto>();
        CreateMap<NewBlockEto, BlockWithTransactionDto>();
        CreateMap<ConfirmBlockEto, BlockWithTransactionDto>();

        CreateMap<TransactionCondition, FilterTransactionInput>();
        CreateMap<LogEventCondition, FilterContractEventInput>();

        CreateMap<SubscriptionManifestDto, SubscriptionManifest>();
        CreateMap<SubscriptionManifest, SubscriptionManifestDto>();
        CreateMap<SubscriptionDto, Subscription>()
            .ForMember(destination => destination.TransactionConditions,
                opt => opt.MapFrom(source => source.Transactions))
            .ForMember(destination => destination.LogEventConditions,
                opt => opt.MapFrom(source => source.LogEvents));
        CreateMap<TransactionConditionDto, TransactionCondition>();
        CreateMap<LogEventConditionDto, LogEventCondition>();

        CreateMap<Subscription, SubscriptionDto>()
            .ForMember(destination => destination.Transactions,
                opt => opt.MapFrom(source => source.TransactionConditions))
            .ForMember(destination => destination.LogEvents,
                opt => opt.MapFrom(source => source.LogEventConditions));
        CreateMap<TransactionCondition, TransactionConditionDto>();
        CreateMap<LogEventCondition, LogEventConditionDto>();

        CreateMap<AppSubscriptionIndex, SubscriptionIndexDto>();
        CreateMap<SubscriptionManifestInfo, SubscriptionManifestDto>();
        CreateMap<SubscriptionInfo, SubscriptionDto>()
            .ForMember(destination => destination.Transactions,
                opt => opt.MapFrom(source => source.TransactionConditions))
            .ForMember(destination => destination.LogEvents,
                opt => opt.MapFrom(source => source.LogEventConditions));
        CreateMap<TransactionConditionInfo, TransactionConditionDto>();
        CreateMap<LogEventConditionInfo, LogEventConditionDto>();
        
        CreateMap<AllSubscription, AllSubscriptionDto>();
        CreateMap<SubscriptionDetail, SubscriptionDetailDto>();

        CreateMap<IdentityUser, IdentityUserDto>();

        CreateMap<AppState, AppDto>()
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.CreateTime)))
            .ForMember(destination => destination.UpdateTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.UpdateTime)));
        CreateMap<AppInfoIndex, AppIndexDto>()
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.CreateTime)))
            .ForMember(destination => destination.UpdateTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.UpdateTime)));
        CreateMap<CreateAppDto, AppState>();
        CreateMap<AppState, AppCreateEto>();
        CreateMap<AppState, AppUpdateEto>();
        CreateMap<OrganizationUnit, OrganizationUnitDto>();
        CreateMap<OrganizationIndex, OrganizationIndexDto>();
        CreateMap<AppVersionInfo, AppVersion>();

        CreateMap<BlockWithTransactionDto, AppSubscribedBlockDto>();
        CreateMap<TransactionDto, AppSubscribedTransactionDto>()
            .ForMember(destination => destination.ExtraProperties,
                opt => opt.MapFrom(source => source.ExtraProperties.Where(o =>
                    AeFinderApplicationConsts.AppInterestedExtraPropertiesKey.Contains(o.Key))));
        CreateMap<LogEventDto, AppSubscribedLogEventDto>();
        
        CreateMap<AppLimitInfoIndex, AppResourceLimitIndexDto>();
        CreateMap<ResourceLimitInfo, ResourceLimitDto>();
        CreateMap<OperationLimitInfo, OperationLimitDto>();
        CreateMap<DeployLimitInfo, DeployLimitInfoDto>();
        
        CreateMap<AppSubscriptionPodIndex, AppResourceDto>();
        
        CreateMap<AppLogIndex, AppLogRecordDto>();
        CreateMap<AppLogDetail, AppLogInfo>();
        CreateMap<AppResourceLimitState, AppResourceLimitDto>();
        CreateMap<AppResourceLimitDto, SetAppResourceLimitDto>();
        CreateMap<AppPodOperationSnapshotDto, AppPodOperationSnapshotState>();
        CreateMap<AppPodOperationSnapshotState, AppPodOperationSnapshotDto>();
        CreateMap<AppPodOperationSnapshotState, AppPodOperationSnapshotCreateEto>();
        CreateMap<AppPodOperationSnapshotCreateEto, AppPodOperationSnapshotIndex>();
        CreateMap<AppPodOperationSnapshotCreateEto, AppPodUsageDurationIndex>();
        CreateMap<AppPodUsageDurationIndex, AppPodUsageDurationDto>();
        
        CreateMap<AppCreateEto, AppInfoIndex>();
        CreateMap<AppDto, AppInfoIndex>()
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => DateTimeOffset.FromUnixTimeMilliseconds(source.CreateTime).UtcDateTime))
            .ForMember(destination => destination.UpdateTime,
                opt => opt.MapFrom(source => DateTimeOffset.FromUnixTimeMilliseconds(source.UpdateTime).UtcDateTime));
        CreateMap<AppVersion, AppVersionInfo>();
        CreateMap<SubscriptionManifest, SubscriptionManifestInfo>();
        CreateMap<Subscription, SubscriptionInfo>();
        CreateMap<TransactionCondition, TransactionConditionInfo>();
        CreateMap<LogEventCondition, LogEventConditionInfo>();
        CreateMap<AppResourceLimitDto, AppLimitUpdateEto>();
        
        CreateMap<AttachmentInfo, AttachmentInfoDto>();
        CreateMap<AppPodInfoDto, AppPodInfoIndex>();
        CreateMap<PodContainerDto, PodContainerInfo>();
        CreateMap<AppPodInfoIndex, AppPodInfoDto>();
        CreateMap<PodContainerInfo, PodContainerDto>();

        CreateMap<UserExtensionDto, IdentityUserExtension>();
        CreateMap<UserChainAddressDto, UserChainAddressInfo>();
        CreateMap<IdentityUserExtension, UserExtensionDto>();
        CreateMap<IdentityUser, IdentityUserExtensionDto>();

        CreateMap<AppInfoIndex, AppInfoImmutable>();
        
        // Api Key
        CreateMap<ApiKeyState, ApiKeyInfo>();
        CreateMap<ApiKeyChangedEto, ApiKeyInfo>();
        CreateMap<ApiKeyChangedEto, ApiKeyIndex>()
            .ForMember(destination => destination.AuthorisedAeIndexers,
                opt => opt.MapFrom(source => source.AuthorisedAeIndexers.Values.ToList()));
        CreateMap<ApiKeySnapshotChangedEto, ApiKeySnapshotIndex>();
        CreateMap<ApiKeyState, ApiKeyChangedEto>();
        CreateMap<ApiKeySnapshotState, ApiKeySnapshotChangedEto>();
        CreateMap<ApiKeyIndex, ApiKeyDto>();
        CreateMap<ApiKeyInfo, ApiKeyDto>()
            .ForMember(destination => destination.AuthorisedAeIndexers,
                opt => opt.MapFrom(source => source.AuthorisedAeIndexers.Values.ToList()));
        CreateMap<ApiKeySnapshotIndex, ApiKeySnapshotDto>();
        
        CreateMap<ApiKeySummaryState, ApiKeySummaryInfo>();
        CreateMap<ApiKeySummaryChangedEto, ApiKeySummaryIndex>();
        CreateMap<ApiKeySummarySnapshotChangedEto, ApiKeySummarySnapshotIndex>();
        CreateMap<ApiKeySummaryState, ApiKeySummaryChangedEto>();
        CreateMap<ApiKeySummarySnapshotState, ApiKeySummarySnapshotChangedEto>();
        CreateMap<ApiKeySummaryIndex, ApiKeySummaryDto>();
        CreateMap<ApiKeySummarySnapshotIndex, ApiKeySummarySnapshotDto>();
        
        CreateMap<ApiKeyQueryAeIndexerChangedEto, ApiKeyQueryAeIndexerIndex>();
        CreateMap<ApiKeyQueryAeIndexerSnapshotChangedEto, ApiKeyQueryAeIndexerSnapshotIndex>();
        CreateMap<ApiKeyQueryAeIndexerState, ApiKeyQueryAeIndexerChangedEto>();
        CreateMap<ApiKeyQueryAeIndexerSnapshotState, ApiKeyQueryAeIndexerSnapshotChangedEto>();
        CreateMap<ApiKeyQueryAeIndexerIndex, ApiKeyQueryAeIndexerDto>();
        CreateMap<ApiKeyQueryAeIndexerSnapshotIndex, ApiKeyQueryAeIndexerSnapshotDto>();
        CreateMap<ApiKeyQueryAeIndexerState, ApiKeyQueryAeIndexerInfo>();
        
        CreateMap<ApiKeyQueryBasicApiChangedEto, ApiKeyQueryBasicApiIndex>();
        CreateMap<ApiKeyQueryBasicApiSnapshotChangedEto, ApiKeyQueryBasicApiSnapshotIndex>();
        CreateMap<ApiKeyQueryBasicApiState, ApiKeyQueryBasicApiChangedEto>();
        CreateMap<ApiKeyQueryBasicApiSnapshotState, ApiKeyQueryBasicApiSnapshotChangedEto>();
        CreateMap<ApiKeyQueryBasicApiIndex, ApiKeyQueryApiDto>();
        CreateMap<ApiKeyQueryBasicApiSnapshotIndex, ApiKeyQueryBasicApiSnapshotDto>();
        CreateMap<ApiKeyQueryBasicApiState, ApiKeyQueryBasicApiInfo>();

        CreateMap<AppInfoImmutableIndex, AppInfoImmutable>();
        CreateMap<AppInfoImmutable, AppInfoImmutableIndex>();
        CreateMap<AppInfoImmutableEto, AppInfoImmutableIndex>();
        CreateMap<AppInfoImmutable, AppInfoImmutableEto>();
        CreateMap<AppInfoImmutableEto, AppInfoImmutable>();
        
        CreateMap<ProductDto, ProductState>();
        CreateMap<ProductState, ProductDto>();
        CreateMap<CreateOrderDto, OrderState>();
        CreateMap<OrderState, OrderDto>();
        CreateMap<CreateRenewalDto, RenewalState>();
        CreateMap<RenewalState, RenewalDto>();
        CreateMap<CreateOrderLockBillDto, BillState>();
        CreateMap<BillState, BillDto>();
        CreateMap<CreateSubscriptionBillDto, BillState>();
        CreateMap<CreateChargeBillDto, BillState>();
        CreateMap<BillDto, InvoiceInfoDto>();
        
        CreateMap<UserRegisterState, UserRegisterInfo>();
        CreateMap<RegisterVerificationCodeState, RegisterVerificationCodeInfo>();
    }
}