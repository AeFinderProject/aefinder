using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AeFinder.User.Dto;

public class BatchDeleteUsersInput : IValidatableObject
{
    [Required]
    [MinLength(1), MaxLength(200)]
    public List<Guid> UserIds { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UserIds != null && UserIds.Any(id => id == Guid.Empty))
        {
            yield return new ValidationResult("UserIds cannot contain an empty Guid.", new[] { nameof(UserIds) });
        }
    }
}
