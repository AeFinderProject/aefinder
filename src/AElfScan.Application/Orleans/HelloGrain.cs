using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElfScan.Orleans
{
    public class HelloGrain : Grain<PersistentData>, IHello
    {
        private readonly ILogger logger;

        public override Task OnActivateAsync()
        {
            this.ReadStateAsync();
            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            this.WriteStateAsync();
            return base.OnDeactivateAsync();
        }

        public HelloGrain(ILogger<HelloGrain> logger)
        {
            this.logger = logger;
            
        }

        public async Task AddCount()
        {
            Console.WriteLine(this.GetPrimaryKeyLong());
            this.State.Count ++;
            Console.WriteLine("add count2");
            await this.WriteStateAsync();
        }

        public Task<int> GetCount()
        {
            return Task.FromResult(this.State.Count);
        }

        Task<string> IHello.SayHello(string greeting)
        {
            logger.LogInformation($"\n SayHello message received: greeting = '{greeting}'");
            return Task.FromResult($"\n Client said: '{greeting}', so HelloGrain says: Hello!");
        }
    }
}