using AutoMapper;
using TokenApp.Entities;
using TokenApp.GraphQL;

namespace TokenApp;

public class TokenAppMapperProfile : Profile
{
    public TokenAppMapperProfile()
    {
        CreateMap<Account, AccountDto>();
        CreateMap<TransferRecord, TransferRecordDto>();
    }
}