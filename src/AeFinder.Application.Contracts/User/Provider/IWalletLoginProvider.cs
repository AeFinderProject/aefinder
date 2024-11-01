using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.User.Dto;

namespace AeFinder.User.Provider;

public interface IWalletLoginProvider
{
    List<string> CheckParams(string signatureVal, string chainId, string address,
        string timestamp);
    string GetErrorMessage(List<string> errors);
    Task<string> VerifySignatureAndParseWalletAddressAsync(string signatureVal, string timestampVal,
        string caHash, string address, string chainId);
    bool IsTimeStampOutRange(long timestamp, out int timeRange);
    
    bool RecoverPublicKey(string address, string timestampVal, byte[] signature, out byte[] managerPublicKey);

    Task<bool?> CheckManagerAddressAsync(string chainId, string caHash, string manager);

    Task<List<UserChainAddressDto>> GetAddressInfosAsync(string caHash);
}