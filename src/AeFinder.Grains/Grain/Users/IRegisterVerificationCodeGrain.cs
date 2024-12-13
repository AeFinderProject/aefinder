namespace AeFinder.Grains.Grain.Users;

public interface IRegisterVerificationCodeGrain : IGrainWithStringKey
{
    Task<string> GenerationCodeAsync();
    Task<string> GetCodeAsync();
}