using AeFinder.Sdk.Processor;
using AppTemplate.Entities;
using Volo.Abp.DependencyInjection;

namespace AppTemplate.Processors;

// public class MyLogEventProcessor : LogEventProcessorBase<MyLogEvent>
// {
//     public override string GetContractAddress(string chainId)
//     {
//         return chainId switch
//         {
//             "AELF" => "MainChainContractAddress",
//             "tDVV" => "SideChainContractAddress",
//             _ => throw new Exception("Unknown chain id")
//         };
//     }
//
//     public override async Task ProcessAsync(MyLogEvent logEvent, LogEventContext context)
//     {
//         var id = context.ChainId + logEvent.Symbol;
//         var entity = await GetEntityAsync<MyEntity>(id);
//         if (entity == null)
//         {
//             entity = new MyEntity
//             {
//                 Id = id,
//                 Symbol = logEvent.Symbol,
//                 Amount = logEvent.Amount
//             };
//         }
//         else
//         {
//             entity.Amount += logEvent.Amount;
//         }
//         await SaveEntityAsync(entity);
//     }
// }