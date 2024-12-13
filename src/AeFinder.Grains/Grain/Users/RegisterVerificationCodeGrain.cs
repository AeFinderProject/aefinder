using AeFinder.Grains.State.Users;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Timing;

namespace AeFinder.Grains.Grain.Users;

public class RegisterVerificationCodeGrain : AeFinderGrain<RegisterVerificationCodeState>,
    IRegisterVerificationCodeGrain
{
    private readonly IClock _clock;
    private readonly UserRegisterOptions _userRegisterOptions;

    public RegisterVerificationCodeGrain(IClock clock, IOptionsSnapshot<UserRegisterOptions> userRegisterOptions)
    {
        _clock = clock;
        _userRegisterOptions = userRegisterOptions.Value;
    }

    public async Task<string> GenerationCodeAsync()
    {
        await WriteStateAsync();
        if (!State.VerificationCode.IsNullOrWhiteSpace() &&
            _clock.Now < State.SendTime.AddSeconds(_userRegisterOptions.VerificationCodeRegeneratePeriod))
        {
            throw new UserFriendlyException("The operation is too frequent, please try again later.");
        }

        State.VerificationCode = Guid.NewGuid().ToString("N");
        State.SendTime = _clock.Now;
        await WriteStateAsync();

        return State.VerificationCode;
    }

    public async Task<string> GetCodeAsync()
    {
        await ReadStateAsync();

        if (_clock.Now > State.SendTime.AddSeconds(_userRegisterOptions.VerificationCodePeriod))
        {
            return null;
        }

        return State.VerificationCode;
    }
}