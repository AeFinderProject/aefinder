using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.ApiKeys;

public class SetAuthorisedAeIndexerInput
{
    [MinLength(1)] 
    public List<string> AppIds { get; set; } = new();
}