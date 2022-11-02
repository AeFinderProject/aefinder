using System;
using System.Linq;
using System.Linq.Expressions;
using AElf.Indexing.Elasticsearch.Provider;
using AElf.Indexing.Elasticsearch.Translator;
using Nest;
using Volo.Abp.Domain.Entities;

namespace AElf.Indexing.Elasticsearch
{
    public class NESTBaseRepository<TEntity, TKey> 
        where TEntity : class, IEntity<TKey>, new()
    {
        private readonly IEsClientProvider _esClientProvider;
        protected virtual string Type { get; private set; }
        protected virtual string IndexName { get; private set; }

        protected NESTBaseRepository(IEsClientProvider esClientProvider, string index = null, string type = null)
        {
            _esClientProvider = esClientProvider;
            Type = type ?? typeof(TEntity).Name.ToLower();
            IndexName = index ?? typeof(TEntity).Name.ToLower();
        }

        protected Func<SourceFilterDescriptor<TEntity>, ISourceFilter> IncludeFields(Expression<Func<TEntity, object>> fieldsExp)
        {
            var builder = Builders<TEntity>.Projection;

            var body = fieldsExp.Body as NewExpression;
            if (body?.Members == null)
            {
                throw new Exception("fieldsExp is invalid expression format， eg: x => new { x.Field1, x.Field2 }");
            }

            builder = body.Members.Aggregate(builder, (current, m) => current.Include(m.Name));

            return x => builder.Project;
        }

        protected ElasticClient GetElasticClient()
        {
            return _esClientProvider.GetClient();
        }
    }
}