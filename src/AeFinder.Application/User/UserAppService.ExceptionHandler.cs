using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace AeFinder.User;

public partial class UserAppService
{
    private Task<FlowBehavior> HandleSignatureVerifyExceptionAsync(SignatureVerifyException exception)
    {
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(exception.Message)
        });
    }
    
    private Task<FlowBehavior> HandleExceptionAsync(Exception exception)
    {
        Logger.LogError("[BindUserWalletAsync] Signature validation failed: {e}", exception.Message);
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        });
    }
}