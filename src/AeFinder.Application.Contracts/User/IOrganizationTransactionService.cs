using System.Threading.Tasks;
using AeFinder.User.Dto;
using Volo.Abp.Application.Dtos;

namespace AeFinder.User;

public interface IOrganizationTransactionService
{
    Task<PagedResultDto<TransactionHistoryDto>>
        GetOrganizationTransactionHistoryAsync(GetTransactionHistoryInput input);
}