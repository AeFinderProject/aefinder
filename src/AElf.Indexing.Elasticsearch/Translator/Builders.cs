using Nest;

namespace AElf.Indexing.Elasticsearch.Translator
{
    public class Builders<TEntity>
    {
        public QueryContainer Query { get; private set; }

        public SourceFilter Project { get; private set; }

        public Builders()
        {

        }

        public Builders(QueryContainer query)
        {
            Query = query;
        }

        public Builders(SourceFilter project)
        {
            Project = project;
        }

        public static Builders<TEntity> Filter
        {
            get
            {
                return new Builders<TEntity>();
            }
        }

        public static Builders<TEntity> Projection
        {
            get
            {
                return new Builders<TEntity>(new SourceFilter { });
            }
        }
    }
}
