using System.Collections.Generic;

namespace AeFinder.Apps.Dto;

public class AppPodsPageResultDto
{
    public string ContinueToken { get; set; }
    
    public List<AppPodInfoDto> PodInfos { get; set; }
}