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
    private readonly UserRegisterOptions _userRegisterOptions;
    private readonly IClock _clock;

    public RegisterVerificationCodeGrain(IObjectMapper objectMapper,
        IOptionsSnapshot<UserRegisterOptions> userRegisterOptions, IClock clock)
    {
        _objectMapper = objectMapper;
        _clock = clock;
        _userRegisterOptions = userRegisterOptions.Value;
    }
    
    public async Task<string> GetCodeAsync()
    {
        await ReadStateAsync();
        return State.Code;
    }
    
    public async Task<string> GenerateCodeAsync()
    {
        await ReadStateAsync();

        if (!State.Code.IsNullOrWhiteSpace() && _clock.Now < State.GenerationTime.AddSeconds(_userRegisterOptions.EmailSendingInterval))
        {
            throw new UserFriendlyException("The operation is too frequent, please try again later.");
        }

        State.Code = Guid.NewGuid().ToString("N");
        State.GenerationTime = _clock.Now;

        await WriteStateAsync();

        return State.Code;
    }

    public async Task VerifyAsync(string code)
    {
        code = code.Trim().ToLower();
        await WriteStateAsync();
        
        if (State.Code.IsNullOrWhiteSpace() || code != State.Code)
        {
            throw new UserFriendlyException("The activation information is invalid.");
        }
        
        if (_clock.Now > State.GenerationTime.AddSeconds(_userRegisterOptions.CodeExpires))
        {
            throw new UserFriendlyException("The activation information has expired.");
        }
    }

    public async Task RemoveAsync()
    {
        await ClearStateAsync();
        DeactivateOnIdle();
    }
}