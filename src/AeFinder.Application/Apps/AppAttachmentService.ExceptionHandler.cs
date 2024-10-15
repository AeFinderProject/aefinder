using System.Text.Json;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AeFinder.Apps;

public partial class AppAttachmentService
{
    public virtual Task<FlowBehavior> HandleJsonExceptionAsync(JsonException exception)
    {
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        });
    }
}