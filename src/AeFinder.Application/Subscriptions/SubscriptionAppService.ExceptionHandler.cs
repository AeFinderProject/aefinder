using AeFinder.CodeOps;
using AElf.ExceptionHandler;
using Volo.Abp;

namespace AeFinder.Subscriptions;

public partial class SubscriptionAppService
{
    private FlowBehavior HandleCodeCheckException(CodeCheckException exception)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(exception.Message)
        };
    }
}