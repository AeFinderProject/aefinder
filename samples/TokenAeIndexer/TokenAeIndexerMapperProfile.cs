using AutoMapper;
using TokenAeIndexer.Entities;
using TokenAeIndexer.GraphQL;

namespace TokenAeIndexer;

public class TokenAeIndexerMapperProfile : Profile
{
    public TokenAeIndexerMapperProfile()
    {
        CreateMap<Account, AccountDto>();
        CreateMap<TransferRecord, TransferRecordDto>();
    }
}