using AeFinder.User.Dto;

namespace AeFinder.Grains.Grain.Users;

public interface IRegisterVerificationCodeGrain : IGrainWithStringKey
{
    Task<string> GetCodeAsync();
    Task<string> GenerateCodeAsync();
    Task VerifyAsync(string code);
    Task RemoveAsync();
}