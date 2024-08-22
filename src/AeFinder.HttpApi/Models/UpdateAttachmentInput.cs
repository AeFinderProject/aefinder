using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeFinder.Models;

public class UpdateAttachmentInput
{
    [ModelBinder(BinderType = typeof(JsonModelBinder))]
    public string AttachmentDeleteFileKeyList { get; set; }
    public IFormFile Attachment1 { get; set; }
    public IFormFile Attachment2 { get; set; }
    public IFormFile Attachment3 { get; set; }
    public IFormFile Attachment4 { get; set; }
    public IFormFile Attachment5 { get; set; }
}