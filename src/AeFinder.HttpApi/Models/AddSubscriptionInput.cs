using System.Collections.Generic;
using AeFinder.BlockScan;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeFinder.Models;

public class AddSubscriptionInput
{
    [ModelBinder(BinderType = typeof(JsonModelBinder))]
    public SubscriptionManifestDto Manifest { get; set; }
    public IFormFile Code { get; set; }
    public List<IFormFile> AttachmentList { get; set; }
}