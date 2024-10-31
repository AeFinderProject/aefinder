using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.User.Dto;

namespace AeFinder.User.Provider;

public interface IWalletLoginProvider
{
    bool IsTimeStampOutRange(long timestamp, out int timeRange);
    
    bool RecoverPublicKey(string address, string timestampVal, byte[] signature, out byte[] managerPublicKey);

    Task<bool?> CheckManagerAddressAsync(string chainId, string caHash, string manager);

    Task<List<UserChainAddressDto>> GetAddressInfosAsync(string caHash);
}