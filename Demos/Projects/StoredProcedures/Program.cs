using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using SharedCode;

namespace StoredProcedures
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
                    //ExecuteAsync().Wait();
                    ExecuteWithRollbackAsync().Wait();
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


        private static async Task ExecuteAsync()
        {
            var sproc = await CreateScript(@"Scripts\UpdateStatus.js");

            var response = await client.ExecuteStoredProcedureAsync<string>(sproc.SelfLink);

            Console.WriteLine("Result from script: {0}\r\n", response.Response);
        }

        private static async Task ExecuteWithRollbackAsync()
        {
            var sproc = await CreateScript(@"Scripts\UpdateStatusWithRollback.js");

            var response = await client.ExecuteStoredProcedureAsync<string>(sproc.SelfLink);

            Console.WriteLine("Result from script: {0}\r\n", response.Response);
        }

        private static async Task<StoredProcedure> CreateScript(string fileName)
        {
            var scriptId = Path.GetFileNameWithoutExtension(fileName);

            var sproc = new StoredProcedure
            {
                Id = scriptId,
                Body = File.ReadAllText(fileName)
            };

            await client.TryDeleteStoredProcedure(UriFactory.CreateStoredProcedureUri(ConfigurationHelper.DatabaseId, ConfigurationHelper.CollectionId, scriptId));

            var response = await client.CreateStoredProcedureAsync(ConfigurationHelper.CollectionUrl, sproc);
            return response.Resource;
        }
    }
}