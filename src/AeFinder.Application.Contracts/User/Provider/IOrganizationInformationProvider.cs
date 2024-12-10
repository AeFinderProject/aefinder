using System.Threading.Tasks;

namespace AeFinder.User.Provider;

public interface IOrganizationInformationProvider
{
    Task<string> GetUserOrganizationWalletAddressAsync(string organizationId);
}