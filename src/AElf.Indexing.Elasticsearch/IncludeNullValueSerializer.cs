using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;

namespace AElf.Indexing.Elasticsearch
{
    public class IncludeNullValueSerializer : ConnectionSettingsAwareSerializerBase
    {
        public IncludeNullValueSerializer(IElasticsearchSerializer builtinSerializer,
            IConnectionSettingsValues connectionSettings) : base(builtinSerializer, connectionSettings)
        {
        }

        protected override JsonSerializerSettings CreateJsonSerializerSettings() => new JsonSerializerSettings
            {NullValueHandling = NullValueHandling.Include};
    }
}