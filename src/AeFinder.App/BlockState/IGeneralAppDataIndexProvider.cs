namespace AeFinder.App.BlockState;

public interface IGeneralAppDataIndexProvider
{
    Task AddOrUpdateAsync(object entity, Type type);
    Task DeleteAsync(object entity, Type type);
}
