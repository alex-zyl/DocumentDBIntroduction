using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using SharedCode.Models;

namespace SharedCode
{
    /// <summary>
    /// Providers common helper methods for working with DocumentClient.
    /// </summary>
    public static class DocumentClientHelper
    {
        /// <summary>
        /// Get a Database by id, or create a new one if one with the id provided doesn't exist.
        /// </summary>
        /// <param name="client">The DocumentDB client instance.</param>
        /// <param name="id">The id of the Database to search for, or create.</param>
        /// <returns>The matched, or created, Database object</returns>
        public static async Task<Database> GetDatabaseAsync(DocumentClient client, string id)
        {
            return client.CreateDatabaseQuery().Where(db => db.Id == id).ToArray().FirstOrDefault() 
                ?? await client.CreateDatabaseAsync(new Database { Id = id });
        }

        /// <summary>
        /// Get a Database by id, or create a new one if one with the id provided doesn't exist.
        /// </summary>
        /// <param name="client">The DocumentDB client instance.</param>
        /// <param name="id">The id of the Database to search for, or create.</param>
        /// <returns>The matched, or created, Database object</returns>
        public static async Task<Database> GetNewDatabaseAsync(DocumentClient client, string id)
        {
            Database database = client.CreateDatabaseQuery().Where(db => db.Id == id).ToArray().FirstOrDefault();
            if (database != null)
            {
                await client.DeleteDatabaseAsync(database.SelfLink);
            }

            database = await client.CreateDatabaseAsync(new Database { Id = id });
            return database;
        }

        /// <summary>
        /// Get a DocumentCollection by id, or create a new one if one with the id provided doesn't exist.
        /// </summary>
        /// <param name="client">The DocumentDB client instance.</param>
        /// <param name="database">The Database where this DocumentCollection exists / will be created</param>
        /// <param name="collectionId">The id of the DocumentCollection to search for, or create.</param>
        /// <returns>The matched, or created, DocumentCollection object</returns>
        public static async Task<DocumentCollection> GetCollectionAsync(DocumentClient client, Database database, string collectionId)
        {
            return client.CreateDocumentCollectionQuery(database.SelfLink).Where(c => c.Id == collectionId).ToArray().FirstOrDefault() 
                ?? await CreateDocumentCollectionWithRetriesAsync(client, database, new DocumentCollection { Id = collectionId });
        }

        /// <summary>
        /// Get a DocumentCollection by id, or create a new one if one with the id provided doesn't exist.
        /// </summary>
        /// <param name="client">The DocumentDB client instance.</param>
        /// <param name="database">The Database where this DocumentCollection exists / will be created</param>
        /// <param name="collectionId">The id of the DocumentCollection to search for, or create.</param>
        /// <param name="collectionSpec">The spec/template to create collections from.</param>
        /// <returns>The matched, or created, DocumentCollection object</returns>
        public static async Task<DocumentCollection> GetCollectionAsync(DocumentClient client, Database database, string collectionId, DocumentCollectionSpec collectionSpec)
        {
            return client.CreateDocumentCollectionQuery(database.SelfLink).Where(c => c.Id == collectionId).ToArray().FirstOrDefault()
                ?? await CreateNewCollection(client, database, collectionId, collectionSpec);
        }

        /// <summary>
        /// Creates a new collection.
        /// </summary>
        /// <param name="client">The DocumentDB client instance.</param>
        /// <param name="database">The Database where this DocumentCollection exists / will be created</param>
        /// <param name="collectionId">The id of the DocumentCollection to search for, or create.</param>
        /// <param name="collectionSpec">The spec/template to create collections from.</param>
        /// <returns>The created DocumentCollection object</returns>
        public static async Task<DocumentCollection> CreateNewCollection(DocumentClient client, Database database, string collectionId, DocumentCollectionSpec collectionSpec)
        {
            DocumentCollection collectionDefinition = new DocumentCollection { Id = collectionId };
            if (collectionSpec != null)
            {
                CopyIndexingPolicy(collectionSpec, collectionDefinition);
            }

            DocumentCollection collection = await CreateDocumentCollectionWithRetriesAsync(client, database, collectionDefinition, (collectionSpec != null) ? collectionSpec.OfferType : null);

            if (collectionSpec != null)
            {
                await RegisterScripts(client, collectionSpec, collection);
            }

            return collection;
        }

        /// <summary>
        /// Registers the stored procedures, triggers and UDFs in the collection spec/template.
        /// </summary>
        /// <param name="client">The DocumentDB client.</param>
        /// <param name="collectionSpec">The collection spec/template.</param>
        /// <param name="collection">The collection.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        public static async Task RegisterScripts(DocumentClient client, DocumentCollectionSpec collectionSpec, DocumentCollection collection)
        {
            if (collectionSpec.StoredProcedures != null)
            {
                foreach (StoredProcedure sproc in collectionSpec.StoredProcedures)
                {
                    await client.CreateStoredProcedureAsync(collection.SelfLink, sproc);
                }
            }

            if (collectionSpec.Triggers != null)
            {
                foreach (Trigger trigger in collectionSpec.Triggers)
                {
                    await client.CreateTriggerAsync(collection.SelfLink, trigger);
                }
            }

            if (collectionSpec.UserDefinedFunctions != null)
            {
                foreach (UserDefinedFunction udf in collectionSpec.UserDefinedFunctions)
                {
                    await client.CreateUserDefinedFunctionAsync(collection.SelfLink, udf);
                }
            }
        }

        /// <summary>
        /// Copies the indexing policy from the collection spec.
        /// </summary>
        /// <param name="collectionSpec">The collection spec/template</param>
        /// <param name="collectionDefinition">The collection definition to create.</param>
        public static void CopyIndexingPolicy(DocumentCollectionSpec collectionSpec, DocumentCollection collectionDefinition)
        {
            if (collectionSpec.IndexingPolicy != null)
            {
                collectionDefinition.IndexingPolicy.Automatic = collectionSpec.IndexingPolicy.Automatic;
                collectionDefinition.IndexingPolicy.IndexingMode = collectionSpec.IndexingPolicy.IndexingMode;

                if (collectionSpec.IndexingPolicy.IncludedPaths != null)
                {
                    foreach (IncludedPath path in collectionSpec.IndexingPolicy.IncludedPaths)
                    {
                        collectionDefinition.IndexingPolicy.IncludedPaths.Add(path);
                    }
                }

                if (collectionSpec.IndexingPolicy.ExcludedPaths != null)
                {
                    foreach (ExcludedPath path in collectionSpec.IndexingPolicy.ExcludedPaths)
                    {
                        collectionDefinition.IndexingPolicy.ExcludedPaths.Add(path);
                    }
                }
            }
        }

        /// <summary>
        /// Create a DocumentCollection, and retry when throttled.
        /// </summary>
        /// <param name="client">The DocumentDB client instance.</param>
        /// <param name="database">The database to use.</param>
        /// <param name="collectionDefinition">The collection definition to use.</param>
        /// <param name="offerType">The offer type for the collection.</param>
        /// <returns>The created DocumentCollection.</returns>
        public static async Task<DocumentCollection> CreateDocumentCollectionWithRetriesAsync(DocumentClient client, Database database, DocumentCollection collectionDefinition, string offerType = "S1")
        {
            return await ExecuteWithRetries(
                client,
                () => client.CreateDocumentCollectionAsync(
                        database.SelfLink,
                        collectionDefinition,
                        new RequestOptions
                        {
                            OfferType = offerType
                        }));
        }

        /// <summary>
        /// Execute the function with retries on throttle.
        /// </summary>
        /// <typeparam name="V">The type of return value from the execution.</typeparam>
        /// <param name="client">The DocumentDB client instance.</param>
        /// <param name="function">The function to execute.</param>
        /// <returns>The response from the execution.</returns>
        public static async Task<V> ExecuteWithRetries<V>(DocumentClient client, Func<Task<V>> function)
        {
            TimeSpan sleepTime = TimeSpan.Zero;

            while (true)
            {
                try
                {
                    return await function();
                }
                catch (DocumentClientException de)
                {
                    if ((int)de.StatusCode != 429)
                    {
                        throw;
                    }

                    sleepTime = de.RetryAfter;
                }
                catch (AggregateException ae)
                {
                    if (!(ae.InnerException is DocumentClientException))
                    {
                        throw;
                    }

                    DocumentClientException de = (DocumentClientException)ae.InnerException;
                    if ((int)de.StatusCode != 429)
                    {
                        throw;
                    }

                    sleepTime = de.RetryAfter;
                }

                await Task.Delay(sleepTime);
            }
        }

        public static async Task TryDeleteStoredProcedure(this DocumentClient client, DocumentCollection collection, string sprocId)
        {
            StoredProcedure sproc = client.CreateStoredProcedureQuery(collection.SelfLink).Where(s => s.Id == sprocId).AsEnumerable().FirstOrDefault();
            if (sproc != null)
            {
                await ExecuteWithRetries(client, () => client.DeleteStoredProcedureAsync(sproc.SelfLink));
            }
        }

        public static async Task TryDeleteStoredProcedure(this DocumentClient client, Uri storedProcedureUri)
        {
            try
            {
                await client.DeleteStoredProcedureAsync(storedProcedureUri);
            }
            catch (DocumentClientException ex)
            {
                if (ex.Error.Code != "NotFound")
                    throw;
            }
        }
    }
}