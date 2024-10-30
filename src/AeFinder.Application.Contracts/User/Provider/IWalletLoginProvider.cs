using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.User.Dto;

namespace AeFinder.User.Provider;

public interface IWalletLoginProvider
{
    bool RecoverPublicKey(string address, string timestampVal, byte[] signature, out byte[] managerPublicKey);
    
    bool RecoverPublicKeyOld(string address, string timestampVal, byte[] signature, out byte[] managerPublicKeyOld);

    bool CheckPublicKey(byte[] managerPublicKey, byte[] managerPublicKeyOld, string publicKeyVal);

    Task<bool?> CheckAddressAsync(string chainId, string graphQlUrl, string caHash, string manager);

    Task<List<UserChainAddressDto>> GetAddressInfosAsync(string caHash);
}