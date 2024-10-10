using System.Text.Json;
using AElf.ExceptionHandler;

namespace AeFinder.Apps;

public partial class AppAttachmentService
{
    private FlowBehavior HandleJsonExceptionException(JsonException exception)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}