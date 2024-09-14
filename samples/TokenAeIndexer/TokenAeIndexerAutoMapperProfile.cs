using AutoMapper;
using TokenAeIndexer.Entities;
using TokenAeIndexer.GraphQL;

namespace TokenAeIndexer;

public class TokenAeIndexerAutoMapperProfile : Profile
{
    public TokenAeIndexerAutoMapperProfile()
    {
        CreateMap<Account, AccountDto>();
        CreateMap<TransferRecord, TransferRecordDto>();
    }
}