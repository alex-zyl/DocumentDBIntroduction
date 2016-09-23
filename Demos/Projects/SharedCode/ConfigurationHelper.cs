using System;
using System.Configuration;
using Microsoft.Azure.Documents.Client;

namespace SharedCode
{
    public static class ConfigurationHelper
    {
        public static Uri CollectionUrl
        {
            get { return UriFactory.CreateCollectionUri(DatabaseId, CollectionId);}
        }

        public static string EndpointUrl
        {
            get { return ConfigurationManager.AppSettings["EndPointUrl"]; }
        }

        public static string AuthorizationKey
        {
            get { return ConfigurationManager.AppSettings["AuthorizationKey"]; }
        }

        public static string DatabaseId
        {
            get { return ConfigurationManager.AppSettings["DatabaseId"]; }
        }

        public static string CollectionId
        {
            get { return ConfigurationManager.AppSettings["CollectionId"]; }
        }
    }
}
