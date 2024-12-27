using System.Threading.Tasks;
using AeFinder.User.Dto;

namespace AeFinder.User.Provider;

public interface IOrganizationInformationProvider
{
    Task<string> GetUserOrganizationWalletAddressAsync(string organizationId, string userWalletAddress);
    Task<bool> SaveOrganizationExtensionInfoAsync(OrganizationExtensionDto organizationExtensionDto);
    Task<string> GetOrganizationWalletAddressAsync(string organizationId);
}