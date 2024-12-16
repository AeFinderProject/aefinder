using AeFinder.User.Dto;

namespace AeFinder.Grains.Grain.Users;

public interface IRegisterVerificationCodeGrain : IGrainWithStringKey
{
    Task SetCodeAsync(string code, DateTime sendingTime);
    Task<RegisterVerificationCodeInfo> GetCodeAsync();
    Task RemoveAsync();
}