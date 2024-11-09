using System;
using System.Linq;
using AeFinder.App.Es;
using AeFinder.AppResources;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Apps.Eto;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Entities.Es;
using AeFinder.Etos;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Grains.State.Apps;
using AeFinder.Grains.State.Subscriptions;
using AeFinder.Logger.Entities;
using AeFinder.Subscriptions;
using AeFinder.Subscriptions.Dto;
using AeFinder.User;
using AeFinder.User.Dto;
using AutoMapper;
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
        CreateMap<AppResourceLimitState, AppResourceLimitDto>();
        
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
        CreateMap<AppPodInfoIndex, AppPodResourceInfoIndexDto>();
        CreateMap<PodContainerInfo, PodContainerInfoDto>();

        CreateMap<UserExtensionDto, IdentityUserExtension>();
        CreateMap<UserChainAddressDto, UserChainAddressInfo>();
        CreateMap<IdentityUserExtension, UserExtensionDto>();
        CreateMap<IdentityUser, IdentityUserExtensionDto>();
    }
}