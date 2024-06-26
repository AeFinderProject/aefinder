﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using Volo.Abp.Account;
using Volo.Abp.DependencyInjection;
using Volo.Abp.IdentityModel;
using Microsoft.AspNetCore.SignalR.Client;

namespace AeFinder.HttpApi.Client.ConsoleTestApp;

public class ClientDemoService : ITransientDependency
{
    private readonly IProfileAppService _profileAppService;
    private readonly IIdentityModelAuthenticationService _authenticationService;

    public ClientDemoService(IProfileAppService profileAppService, IIdentityModelAuthenticationService authenticationService)
    {
        _profileAppService = profileAppService;
        _authenticationService = authenticationService;
    }

    public async Task RunAsync()
    {
        try
        {
            // var accessToken = await _authenticationService.GetAccessTokenAsync(new IdentityClientConfiguration
            // {
            //     Authority = "http://localhost:8080",
            //     Scope = "AeFinder",
            //     GrantType = "client_credentials",
            //     ClientId = "AeFinder_DApp",
            //     ClientSecret = "1q2w3e*",
            //     RequireHttps = false
            // });

            var connection = new HubConnectionBuilder()
                .WithUrl("http://172.25.181.75:8081/signalr-hubs/block")
                .Build();

            connection.On<SubscribedBlockDto>("ReceiveBlock",
                s =>
                {
                    Console.WriteLine(
                        $"Receive Block From {s.Blocks.First().BlockHeight} To {s.Blocks.Last().BlockHeight}");
                });
            
            await connection.StartAsync().ConfigureAwait(false);
            await connection.InvokeAsync("Start", "AeFinder_DApp", "04c0474b5ad148dd9ea5a676adca7dec");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
