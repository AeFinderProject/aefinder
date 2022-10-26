﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using Volo.Abp.Account;
using Volo.Abp.DependencyInjection;
using Volo.Abp.IdentityModel;
using Microsoft.AspNetCore.SignalR.Client;

namespace AElfScan.HttpApi.Client.ConsoleTestApp;

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
            var accessToken = await _authenticationService.GetAccessTokenAsync(new IdentityClientConfiguration
            {
                Authority = "http://localhost:8080",
                Scope = "AElfScan",
                GrantType = "client_credentials",
                ClientId = "AElfScan_App",
                ClientSecret = "1q2w3e*"
            });

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:8081/signalr-hubs/block",
                    options => { options.AccessTokenProvider = () => Task.FromResult(accessToken); })
                .Build();

            connection.On<List<Block>>("ReceiveBlock",
                s =>
                {
                    Console.WriteLine(
                        $"Receive Block From {s.First().BlockHeight} To {s.Last().BlockHeight}");
                });
            
            await connection.StartAsync().ConfigureAwait(false);
            await connection.InvokeAsync("Subscribe", new List<SubscribeInfo>
            {
                new SubscribeInfo
                {
                    ChainId = "AELF",
                    OnlyConfirmedBlock = true,
                    StartBlockNumber = 40
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }


        // var output = await _profileAppService.GetAsync();
        // Console.WriteLine($"UserName : {output.UserName}");
        // Console.WriteLine($"Email    : {output.Email}");
        // Console.WriteLine($"Name     : {output.Name}");
        // Console.WriteLine($"Surname  : {output.Surname}");
    }
}
