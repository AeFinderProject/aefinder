using System;
using System.Threading.Tasks;
using AeFinder.User;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AeFinder;

public partial class SignatureGrantHandler
{
    private Task<FlowBehavior> HandleSignatureVerifyExceptionAsync(SignatureVerifyException exception)
    {
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new Tuple<string, string>(string.Empty, exception.Message)
        });
    }

    private Task<FlowBehavior> HandleExceptionAsync(Exception exception)
    {
        _logger.LogError("[SignatureGrantHandler] Signature validation failed: {e}", exception.Message);
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        });
    }
}