using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.ApiKeys;

public class SetAuthorisedDomainInput
{
    [MinLength(1),MaxLength(50)]
    public List<string> Domains { get; set; }
}