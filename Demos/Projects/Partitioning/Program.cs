using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Partitioning;
using Newtonsoft.Json;
using SharedCode;
using SharedCode.Models;

namespace Partitioning
{
    class Program
    {
        private static DocumentClient client;

        static void Main(string[] args)
        {
            try
            {
                using (client = new DocumentClient(new Uri(ConfigurationHelper.EndpointUrl), ConfigurationHelper.AuthorizationKey))
                {
                    client.OpenAsync().Wait();
                    DoAsync().Wait();
                }
            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        private static async Task DoAsync()
        {
            var database = await DocumentClientHelper.GetDatabaseAsync(client, ConfigurationHelper.DatabaseId);

            var resolver = await InitializeHashResolver(database);

            await RunImport(database);

            await WriteCollectionSizes(resolver);
        }

        private static async Task WriteCollectionSizes(HashPartitionResolver resolver)
        {
            foreach (var collectionLink in resolver.CollectionLinks)
            {
                var collection = await client.ReadDocumentCollectionAsync(collectionLink);
                var feed = await client.ReadDocumentFeedAsync(collectionLink, new FeedOptions
                {
                    MaxItemCount = 1000
                });

                Console.WriteLine("Collection '{0}', documents: {1}", collection.Resource.Id, feed.Count);
            }
        }

        private static async Task RunImport(Database database)
        {
            var di = new DirectoryInfo(@".\Data\");
            var fileInfos = di.GetFiles("*.json");

            foreach (var info in fileInfos)
            {
                var fileContent = File.ReadAllText(info.FullName);
                var status = JsonConvert.DeserializeObject<TweeterStatus>(fileContent);
                try
                {
                    await client.CreateDocumentAsync(database.SelfLink, status);
                }
                catch (Exception e)
                {
                    Exception baseException = e.GetBaseException();
                    Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
                }
            }
        }

        private static async Task<HashPartitionResolver> InitializeHashResolver(Database database)
        {
            var collection1 = await DocumentClientHelper.GetCollectionAsync(client, database, "TweeterStatus.HashBucket0");
            var collection2 = await DocumentClientHelper.GetCollectionAsync(client, database, "TweeterStatus.HashBucket1");

            HashPartitionResolver hashResolver = new DisplayableHashPartitionResolver(new [] { collection1, collection2 }, PartitionKeyExtractor);
            client.PartitionResolvers[database.SelfLink] = hashResolver;
            
            return hashResolver;
        }

        private static string PartitionKeyExtractor(object u)
        {
            var id = ((TweeterStatus)u).User.UserId.ToString();
            return id;
        }
    }
}