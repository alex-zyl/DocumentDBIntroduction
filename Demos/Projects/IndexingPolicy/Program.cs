using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using SharedCode;

namespace IndexingPolicy
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
                    UpdatePolicy().Wait();
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

        private static async Task UpdatePolicy()
        {
            await client.OpenAsync();

            var collectionUri = UriFactory.CreateCollectionUri(ConfigurationHelper.DatabaseId, ConfigurationHelper.CollectionId);

            var collection = await client.ReadDocumentCollectionAsync(collectionUri);
            collection.Resource.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
            collection.Resource.IndexingPolicy.Automatic = true;
            collection.Resource.IndexingPolicy.IncludedPaths.Clear();

            collection.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath
            {
                Path = "/user/name/?",
                Indexes = new Collection<Index>
                {
                    new RangeIndex(DataType.String) { Precision = -1 }
                }
            });

            collection.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath
            {
                Path = "/user/screen_name/?",
                Indexes = new Collection<Index>
                {
                    new RangeIndex(DataType.String) { Precision = -1 }
                }
            });

            collection.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath
            {
                Path = "/*",
                Indexes = new Collection<Index>
                {
                    new RangeIndex(DataType.Number) { Precision = -1 },
                    new HashIndex(DataType.String) { Precision = 3 }
                }
            });

            collection.Resource.IndexingPolicy.ExcludedPaths = new Collection<ExcludedPath>
            {
                new ExcludedPath
                {
                    Path = "/entities/*"
                },
                new ExcludedPath
                {
                    Path = "/user/favourites_count/?"
                },
                new ExcludedPath
                {
                    Path = "/in_reply_to_status_id/?"
                }
            };

            await UpdatePolicy(collection);
        }

        private static async Task UpdatePolicy(DocumentCollection collection)
        {
            await client.ReplaceDocumentCollectionAsync(collection);

            long delay = 1000;
            long progress = 0;

            Console.WriteLine("Index transformationProgress: {0}", progress);

            while (progress >= 0 && progress < 100)
            {
                var collectionReadResponse = await client.ReadDocumentCollectionAsync(collection.SelfLink);
                progress = collectionReadResponse.IndexTransformationProgress;
                Console.WriteLine("Index transformationProgress: {0}", progress);

                await Task.Delay(TimeSpan.FromMilliseconds(delay));
            }
        }
    }
}