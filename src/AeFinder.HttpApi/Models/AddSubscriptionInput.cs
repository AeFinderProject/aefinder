using System.Collections.Generic;
using AeFinder.BlockScan;
using AeFinder.Subscriptions.Dto;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeFinder.Models;

public class AddSubscriptionInput
{
    [ModelBinder(BinderType = typeof(JsonModelBinder))]
    public SubscriptionManifestDto Manifest { get; set; }
    public IFormFile Code { get; set; }
    public IFormFile Attachment1 { get; set; }
    public IFormFile Attachment2 { get; set; }
    public IFormFile Attachment3 { get; set; }
    public IFormFile Attachment4 { get; set; }
    public IFormFile Attachment5 { get; set; }
    [ModelBinder(BinderType = typeof(JsonModelBinder))]
    public AddAttachmentInput Attachments { get; set; }
}