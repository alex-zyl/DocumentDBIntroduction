using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using SharedCode;
using SharedCode.Models;

namespace Queries
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
                    QueryAsync().Wait();
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

        private static async Task QueryAsync()
        {
            var collectionUri = UriFactory.CreateCollectionUri(ConfigurationHelper.DatabaseId, ConfigurationHelper.CollectionId);

            var query = client.CreateDocumentQuery<TweeterStatus>(collectionUri, new FeedOptions
            {
                MaxItemCount = 10
            })
            .Where(x => x.RetweetCount > 900)
            .OrderBy(x => x.User.ScreenName)
            .AsDocumentQuery();

            while (query.HasMoreResults)
            {
                var result = await query.ExecuteNextAsync<TweeterStatus>();

                Console.WriteLine("Request consumed {0} RU", result.RequestCharge);

                foreach (TweeterStatus status in result)
                {
                    Console.WriteLine("Id: {0}, User: {1}, Followers: {2}", status.StatusId, status.User.ScreenName, status.User.FollowersCount);
                }
            }
        }
    }
}