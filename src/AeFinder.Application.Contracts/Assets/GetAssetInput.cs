using AeFinder.Merchandises;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Assets;

public class GetAssetInput: PagedResultRequestDto
{
    public string AppId { get; set; }
    public bool? IsFree { get; set; }
    public bool IsDeploy { get; set; } = false;
    public MerchandiseType? Type { get; set; }
    public MerchandiseCategory? Category { get; set; }
}