namespace AElfIndexer.CodeOps.Patchers;

public interface IPatcher
{
    
}

public interface IPatcher<T> : IPatcher
{
    void Patch(T item);
}