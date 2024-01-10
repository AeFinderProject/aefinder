using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Entities.Es;
using AElfIndexer.Etos;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfIndexer;

public class AElfIndexerApplicationAutoMapperProfile:Profile
{
    public AElfIndexerApplicationAutoMapperProfile()
    {
        CreateMap<BlockIndex,BlockDto>();
        CreateMap<Transaction, TransactionDto>();
        CreateMap<LogEvent, LogEventDto>();

        CreateMap<TransactionIndex, TransactionDto>();
        CreateMap<LogEventIndex, LogEventDto>();
        
        CreateMap<BlockDto,BlockWithTransactionDto>();
        CreateMap<NewBlockEto,BlockWithTransactionDto>();
        CreateMap<ConfirmBlockEto,BlockWithTransactionDto>();
        
        CreateMap<TransactionFilter,FilterTransactionInput>();
        CreateMap<LogEventFilter,FilterContractEventInput>();
    }
    
}