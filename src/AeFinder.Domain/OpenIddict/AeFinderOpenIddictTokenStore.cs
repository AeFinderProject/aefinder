using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.Guids;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Authorizations;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Uow;

namespace AeFinder.OpenIddict;

public class AeFinderOpenIddictTokenStore:AbpOpenIddictTokenStore
{
    public AeFinderOpenIddictTokenStore(IOpenIddictTokenRepository repository,
        IUnitOfWorkManager unitOfWorkManager,
        IGuidGenerator guidGenerator,
        IOpenIddictApplicationRepository applicationRepository,
        IOpenIddictAuthorizationRepository authorizationRepository,
        AbpOpenIddictIdentifierConverter identifierConverter,
        IOpenIddictDbConcurrencyExceptionHandler concurrencyExceptionHandler,
        IOptions<AbpOpenIddictStoreOptions> storeOptions)
        : base(repository, unitOfWorkManager, guidGenerator, applicationRepository, authorizationRepository,
            identifierConverter, concurrencyExceptionHandler, storeOptions)
    {

    }

    /// <summary>
    /// Override PruneAsync and set isTransactional as false
    /// </summary>
    /// <param name="threshold"></param>
    /// <param name="cancellationToken"></param>
    // public override async ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    // {
    //     for (var index = 0; index < 1_000; index++)
    //     {
    //         cancellationToken.ThrowIfCancellationRequested();
    //
    //         using (var uow = UnitOfWorkManager.Begin(requiresNew: true, isTransactional: false, isolationLevel: IsolationLevel.RepeatableRead))
    //         {
    //             var date = threshold.UtcDateTime;
    //
    //             var tokens = await Repository.GetPruneListAsync(date, 1_000, cancellationToken);
    //             if (!tokens.Any())
    //             {
    //                 break;
    //             }
    //
    //             await Repository.DeleteManyAsync(tokens, autoSave: true, cancellationToken: cancellationToken);
    //             await uow.CompleteAsync(cancellationToken);
    //         }
    //     }
    // }
    public override async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        //Set isTransactional false
        using (var uow = UnitOfWorkManager.Begin(requiresNew: true, isTransactional: false, isolationLevel: IsolationLevel.RepeatableRead))
        {
            var date = threshold.UtcDateTime;
            var count = await Repository.PruneAsync(date, cancellationToken: cancellationToken);
            await uow.CompleteAsync(cancellationToken);
            return count;
        }
    }
}