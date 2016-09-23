using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Partitioning;

namespace Partitioning
{
    class DisplayableHashPartitionResolver : HashPartitionResolver
    {
        private readonly DocumentCollection[] collections;

        public DisplayableHashPartitionResolver(DocumentCollection[] collections,
            Func<object, string> partitionKeyExtractor, int numberOfVirtualNodesPerCollection = 128, IHashGenerator hashGenerator = null)
            : base(partitionKeyExtractor, collections.Select(x => x.SelfLink), numberOfVirtualNodesPerCollection, hashGenerator)
        {
            this.collections = collections;
        }

        public override object GetPartitionKey(object document)
        {
            var pk = base.GetPartitionKey(document);
            return pk;
        }

        public override string ResolveForCreate(object partitionKey)
        {
            var res = base.ResolveForCreate(partitionKey);
            var collection = collections.First(x => x.SelfLink.Equals(res));
            Console.WriteLine("Partition {0} goes to Bucket {1}", partitionKey, collection.Id);
            return res;
        }

        public override IEnumerable<string> ResolveForRead(object partitionKey)
        {
            var res = base.ResolveForRead(partitionKey);
            return res;
        }
    }
}