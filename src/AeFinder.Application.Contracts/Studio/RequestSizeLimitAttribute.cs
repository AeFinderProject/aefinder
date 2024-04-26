using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AeFinder.Studio;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequestSizeLimitAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
{
    private readonly FormOptions _formOptions;

    public RequestSizeLimitAttribute(int valueCountLimit)
    {
        _formOptions = new FormOptions()
        {
            // tip: you can use different arguments to set each properties instead of single argument
            KeyLengthLimit = valueCountLimit,
            ValueCountLimit = valueCountLimit,
            ValueLengthLimit = valueCountLimit

            // uncomment this line below if you want to set multipart body limit too
            // MultipartBodyLengthLimit = valueCountLimit
        };
    }

    public int Order { get; set; }

    // taken from /a/38396065
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var contextFeatures = context.HttpContext.Features;
        var formFeature = contextFeatures.Get<IFormFeature>();

        if (formFeature == null || formFeature.Form == null)
        {
            // Setting length limit when the form request is not yet being read
            contextFeatures.Set<IFormFeature>(new FormFeature(context.HttpContext.Request, _formOptions));
        }
    }
}