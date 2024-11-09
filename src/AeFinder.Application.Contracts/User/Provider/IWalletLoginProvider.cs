using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.User.Dto;

namespace AeFinder.User.Provider;

public interface IWalletLoginProvider
{
    List<string> CheckParams(string publicKeyVal, string signatureVal, string chainId, string address,
        string timestamp);
    string GetErrorMessage(List<string> errors);
    Task<string> VerifySignatureAndParseWalletAddressAsync(string publicKeyVal, string signatureVal, string timestampVal,
        string caHash, string address, string chainId);
}