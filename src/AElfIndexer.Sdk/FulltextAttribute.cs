namespace AElfIndexer.Sdk;

[AttributeUsage(AttributeTargets.Property)]
public sealed class FulltextAttribute : Attribute
{
    public bool Index { get; set; }

    public FulltextAttribute()
    {
    }
}