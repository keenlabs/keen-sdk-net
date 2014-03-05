using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Keen.Core
{
    public class KeenClient
    {
        private IProjectSettings _prjSettings;
        private string keenProjectUri;

        private HashSet<string> validCollectionNames = new HashSet<string>();

        /// <summary>
        /// Apply the collection name restrictions.
        /// </summary>
        /// <param name="collection"></param>
        public void validateEventCollectionName(string collection)
        {
            // avoid cost of re-checking collection names that have already been validated.
            if (validCollectionNames.Contains(collection))
                return;

            if (null == collection)
                throw new KeenException("Event collection name may not be null.");
            if (string.IsNullOrWhiteSpace(collection))
                throw new KeenException("Event collection name may not be blank.");
            if (collection.Length > KeenConstants.CollectionNameLengthLimit)
                throw new KeenException(string.Format("Event collection name may not be longer than {0} characters.", KeenConstants.CollectionNameLengthLimit));
            if (new Regex("[^\x00-\x7F]").Match(collection).Success)
                throw new KeenException("Event collection name must contain only Ascii characters.");
            if (collection.Contains("$"))
                throw new KeenException("Event collection name may not contain \"$\".");
            if (collection.StartsWith("_"))
                throw new KeenException("Event collection name may not begin with \"_\".");

            validCollectionNames.Add(collection);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="prjSettings">A ProjectSettings instance containing the ProjectId and API keys</param>
        public KeenClient(IProjectSettings prjSettings)
        {
            // Preconditions
            if (null == prjSettings)
                throw new KeenException("An ProjectSettings instance is required.");
            if (string.IsNullOrWhiteSpace(prjSettings.ProjectId))
                throw new KeenException("A Project ID is required.");
            if ((string.IsNullOrWhiteSpace(prjSettings.MasterKey)
                && string.IsNullOrWhiteSpace(prjSettings.WriteKey)))
                throw new KeenException("A Master or Write API key is required.");

            _prjSettings = prjSettings;

            keenProjectUri = string.Format("{0}/{1}/projects/{2}/", 
                KeenConstants.ServerAddress, KeenConstants.ApiVersion, _prjSettings.ProjectId);
        }

        /// <summary>
        /// Delete the specified collection. Deletion may be denied for collections with many events.
        /// Master API key is required.
        /// </summary>
        /// <param name="collection">Name of collection to delete.</param>
        public void DeleteCollection(string collection)
        {
            // Preconditions
            validateEventCollectionName(collection);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.MasterKey);
                var responseMsg = client.DeleteAsync(keenProjectUri + KeenConstants.EventsCollectionResource + "/" + collection).Result;
                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("DeleteCollection failed with status: " + responseMsg.StatusCode);
            }
            
        }

		/// <summary>
		/// Retrieve the schema for the specified collection. This requires
        /// a value for the project settings Master API key.
		/// </summary>
		/// <param name="collection"></param>
        public dynamic GetSchema(string collection)
        {
            // Preconditions
            validateEventCollectionName(collection);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.MasterKey);
                var responseMsg = client.GetAsync(keenProjectUri + KeenConstants.EventsCollectionResource + "/" + collection).Result;
                var responseString = responseMsg.Content.ReadAsStringAsync().Result;
                dynamic response = JObject.Parse(responseString);

                // error checking, throw an exception with information from the json 
                // response if available, then check the HTTP response.
                checkErrorCode(response);
                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("GetSchema failed with status: " + responseMsg.StatusCode);

                return response;
            }
        }

        /// <summary>
        /// Add a single event to the specified collection.
        /// </summary>
        /// <param name="collection">Collection name</param>
        /// <param name="eventProperties">The event to add. This should be a </param>
        public void AddEvent(string collection, object eventInfo)
        {
            // Preconditions
            validateEventCollectionName(collection);

            var jEvent = JObject.FromObject(eventInfo);
            AddEvent(keenProjectUri + KeenConstants.EventsCollectionResource + "/" + collection, jEvent);
        }

        /// <summary>
        /// Add one or more collections of events.
        /// </summary>
        /// <param name="eventCollections">The collections of events to send. Events are contained in 
        /// array properties, the names of which indicate what collection the events belong to.</param>
        public void AddEvents(object eventCollections)
        {
            JObject jEvent = JObject.FromObject(eventCollections);

            // Each property of eventCollections is a collection name, validate each one.
            foreach (var i in jEvent.Properties())
                validateEventCollectionName(i.Name);

            AddEvent(keenProjectUri + KeenConstants.EventsCollectionResource, jEvent);
        }

        /// <summary>
        /// Internal AddEvent. Used to send both single and bulk events.
        /// </summary>
        /// <param name="keenUrl">Keen resource URL for single or bulk operation</param>
        /// <param name="jEvent">A JObject containing either a single event for the collection 
        /// specified in the URL, or one or more arrays of events named for the target collections.</param>
        private void AddEvent(string keenUrl, JObject jEvent)
        {
            if (null == jEvent)
                throw new KeenException("Event data is required.");

            var content = jEvent.ToString();
            Debug.WriteLine("AddEvent json:" + content);
            using (var client = new HttpClient())
            using (var contentStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))))
            {
                contentStream.Headers.Add("content-type", "application/json");

                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.WriteKey);
                var responseMsg = client.PostAsync(keenUrl, contentStream).Result;
                var responseString = responseMsg.Content.ReadAsStringAsync().Result;
                dynamic response = JObject.Parse(responseString);

                // error checking, throw an exception with information from the json 
                // response if available, then check the HTTP response.
                checkErrorCode(response);
                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("AddEvent failed with status: " + responseMsg.StatusCode);
            }
        }

        /// <summary>
        /// Check the 'error_code' field and throw the appropriate exception if non-null.
        /// </summary>
        /// <param name="apiResponse">Deserialized json response from a Keen API call.</param>
        private static void checkErrorCode(dynamic apiResponse)
        {
            if (apiResponse.error_code != null)
            {
                switch ((string)apiResponse.error_code)
                {
                    case "InvalidApiKeyError":
                        throw new KeenInvalidApiKeyException((string)apiResponse.message);

                    case "ResourceNotFoundError":
                        throw new KeenResourceNotFoundException((string)apiResponse.message);

                    case "NamespaceTypeError":
                        throw new KeenNamespaceTypeException((string)apiResponse.message);

                    case "InvalidEventError":
                        throw new KeenInvalidEventException((string)apiResponse.message);

                    case "ListsOfNonPrimitivesNotAllowedError":
                        throw new KeenListsOfNonPrimitivesNotAllowedException((string)apiResponse.message);

                    case "InternalServerError":
                        throw new KeenInternalServerErrorException((string)apiResponse.message);

                    default:
                        Debug.WriteLine("Unhandled error_code \"{0}\" : \"{1}\"", (string)apiResponse.error_code, (string)apiResponse.message);
                        throw new KeenException((string)apiResponse.error_code + " : " + (string)apiResponse.message);
                }
            }
        }
    }
}
