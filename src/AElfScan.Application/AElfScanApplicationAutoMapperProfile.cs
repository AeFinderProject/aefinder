using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfScan;

public class AElfScanApplicationAutoMapperProfile:Profile
{
    public AElfScanApplicationAutoMapperProfile()
    {
        CreateMap<BlockIndex,BlockDto>();
        CreateMap<Transaction, TransactionDto>();
        CreateMap<LogEvent, LogEventDto>();

        CreateMap<TransactionIndex, TransactionDto>();
        CreateMap<LogEventIndex, LogEventDto>();
    }
    
}