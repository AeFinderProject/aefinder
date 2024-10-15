using System.Threading.Tasks;
using AeFinder.CodeOps;
using AElf.ExceptionHandler;
using Volo.Abp;

namespace AeFinder.Subscriptions;

public partial class SubscriptionAppService
{
    public virtual Task<FlowBehavior> HandleCodeCheckExceptionAsync(CodeCheckException exception)
    {
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(exception.Message)
        });
    }
}