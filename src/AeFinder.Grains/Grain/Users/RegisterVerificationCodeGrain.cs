using AeFinder.Grains.State.Users;
using AeFinder.User.Dto;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Timing;

namespace AeFinder.Grains.Grain.Users;

public class RegisterVerificationCodeGrain : AeFinderGrain<RegisterVerificationCodeState>,
    IRegisterVerificationCodeGrain
{
    private readonly IObjectMapper _objectMapper;

    public RegisterVerificationCodeGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task SetCodeAsync(string code, DateTime sendingTime)
    {
        await WriteStateAsync();
        State.Code = code;
        State.SendingTime = sendingTime;
        await WriteStateAsync();
    }

    public async Task<RegisterVerificationCodeInfo> GetCodeAsync()
    {
        await ReadStateAsync();

        return _objectMapper.Map<RegisterVerificationCodeState, RegisterVerificationCodeInfo>(State);
    }

    public async Task RemoveAsync()
    {
        await ClearStateAsync();
        DeactivateOnIdle();
    }
}