using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class QueryAeFinderAppInput
{
    [Required] public string AppId { get; set; }
    [Required] public string GraphQl { get; set; }
}