using System;
using System.Collections.Generic;

namespace AeFinder.User.Dto;

public class BatchDeleteUsersResultDto
{
    public List<Guid> DeletedUserIds { get; set; } = new();
    public List<Guid> SkippedUserIds { get; set; } = new();

    // Deletion was not confirmed; nontransactional failures may leave partial changes.
    public List<Guid> FailedUserIds { get; set; } = new();
}
