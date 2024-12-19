using AeFinder.ApiKeys;
using AeFinder.Orleans.TestBase;

namespace AeFinder;

public abstract class AeFinderApplicationTestBase : AeFinderTestBase<AeFinderApplicationTestModule>
{

}

public class AeFinderApplicationOrleansTestBase:AeFinderOrleansTestBase<AeFinderApplicationTestModule>
{
    
}

public abstract class AeFinderApplicationApiKeyTestBase : AeFinderTestBase<ApiKeyTestModule>
{

}
