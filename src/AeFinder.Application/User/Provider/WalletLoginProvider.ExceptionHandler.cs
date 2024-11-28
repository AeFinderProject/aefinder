using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Options;
using AeFinder.User.Dto;
using AElf.ExceptionHandler;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AeFinder.User.Provider;

public partial class WalletLoginProvider
{
    private Task<FlowBehavior> HandleCallTransactionExceptionAsync(Exception exception, string chainId, string methodName, IMessage param,
        ChainOptions chainOptions)
    {
        _logger.LogError(exception, "CallTransaction error, chain id:{chainId}, methodName:{methodName}", chainId,
            methodName);
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        });
    }
    
    private Task<FlowBehavior> HandleGetAddressExceptionAsync(Exception exception, List<UserChainAddressDto> addressInfos, string chainId,
        string caHash)
    {
        _logger.LogError(exception, "get holder from chain error, caHash:{caHash}", caHash);
        
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        });
    }
}