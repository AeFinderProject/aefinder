using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Guids;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Authorizations;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Uow;

namespace AeFinder.OpenIddict;

public class AeFinderOpenIddictAuthorizationStore:AbpOpenIddictAuthorizationStore
{
    public AeFinderOpenIddictAuthorizationStore(IOpenIddictAuthorizationRepository repository,
        IUnitOfWorkManager unitOfWorkManager,
        IGuidGenerator guidGenerator,
        IOpenIddictApplicationRepository applicationRepository,
        IOpenIddictTokenRepository tokenRepository)
        : base(repository, unitOfWorkManager, guidGenerator,applicationRepository,tokenRepository)
    {
        
    }
    
    /// <summary>
    /// Override PruneAsync and set isTransactional as false
    /// </summary>
    /// <param name="threshold"></param>
    /// <param name="cancellationToken"></param>
    public override async ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        for (var index = 0; index < 1_000; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var uow = UnitOfWorkManager.Begin(requiresNew: true, isTransactional: false, isolationLevel: IsolationLevel.RepeatableRead))
            {
                var date = threshold.UtcDateTime;

                var authorizations = await Repository.GetPruneListAsync(date, 1_000, cancellationToken);
                if (!authorizations.Any())
                {
                    break;
                }

                await Repository.DeleteManyAsync(authorizations, autoSave: true, cancellationToken: cancellationToken);
                await uow.CompleteAsync(cancellationToken);
            }
        }
    }
}