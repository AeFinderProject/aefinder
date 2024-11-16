using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace AeFinder.Metrics;

public class KubernetesAppMonitorTests:AeFinderApplicationTestBase
{
    private readonly IAppService _appService;
    private readonly IKubernetesAppMonitor _kubernetesAppMonitor;
    
    public KubernetesAppMonitorTests()
    {
        _appService = GetRequiredService<IAppService>();
        _kubernetesAppMonitor = GetRequiredService<IKubernetesAppMonitor>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildKubernetesAppMonitor());
    }
    
    private static IKubernetesAppMonitor BuildKubernetesAppMonitor()
    {
        var mockKubernetesAppMonitor = new Mock<IKubernetesAppMonitor>();
        mockKubernetesAppMonitor
            .Setup(service => service.GetAppPodsResourceInfoFromPrometheusAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<AppPodResourceInfoDto> { 
                new AppPodResourceInfoDto
                {
                    // PodUid="7282799a-b15c-4919-bc78-843410fcfb2e",
                    PodName="deployment-test-ae8ec5b7361e4e1381082f5570283214-fulmhzcw",
                    // CurrentTime=DateTime.Now,
                    Timestamp = 1731044470,
                    Containers=new List<PodContainerResourceDto>()
                    {
                        new PodContainerResourceDto()
                        {
                            ContainerName = "container-ae8ec5b7361e4e1381082f5570283214-full",
                            CpuUsage = "0.0015564684208729614",
                            MemoryUsage = "676065280"
                        },
                        new PodContainerResourceDto()
                        {
                            ContainerName = "filebeat-sidecar",
                            CpuUsage = "0.00039660100332426063",
                            MemoryUsage = "76947456"
                        }
                    }
                } 
            });
        return mockKubernetesAppMonitor.Object;
    }

    [Fact]
    public async Task GetAppPodsResourceInfoFromPrometheus_Test()
    {
        var podsName = new List<string>()
        {
            "deployment-awaken-e55a9e430bd14ebb95ef81378906fd5f-full-ff7p6g2",
            "deployment-beangogame-ae8ec5b7361e4e1381082f5570283214-fulmhzcw"
        };
        var podResourceInfoDtos=await _kubernetesAppMonitor.GetAppPodsResourceInfoFromPrometheusAsync(podsName);
        podResourceInfoDtos.Count.ShouldBe(1);
    }
}