using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private string keenEventCollectionUriTemplate;

        /// <summary>
        /// Construct the Keen Events Collection API URL for the given collection name and API key.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        private string keenUrl(string collection, string apiKey)
        {
            return string.Format(keenEventCollectionUriTemplate, collection, apiKey);
        }


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

            keenEventCollectionUriTemplate = string.Format("{0}/{1}/projects/{2}/events/{{0}}?api_key={{1}}",
                KeenConstants.ServerAddress, KeenConstants.ApiVersion, _prjSettings.ProjectId);
        }

		/// <summary>
		/// Retrieve the schema for the specified collection. This requires
        /// a value for the project settings Master API key.
		/// </summary>
		/// <param name="collection"></param>
        public void GetSchema(string collection)
        {
            // Preconditions
            validateEventCollectionName(collection);

            using (var client = new HttpClient())
            {
                var task = client.GetAsync(keenUrl(collection, _prjSettings.MasterKey));
                var responseMsg = task.Result;

                var responseTask = responseMsg.Content.ReadAsStringAsync();
                var responseString = responseTask.Result;
                System.Diagnostics.Debug.WriteLine("GetSchema response:" + responseString);

                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("GetSchema failed with status: " + responseMsg.StatusCode);
            }
        }

        /// <summary>
        /// Add a an event to the specified collection.
        /// </summary>
        /// <param name="collection">Collection name</param>
        /// <param name="eventProperties">The event to add</param>
        public void AddEvent(string collection, dynamic eventInfo)
        {
            // Preconditions
            validateEventCollectionName(collection);
            if (null == eventInfo)
                throw new KeenException("An eventInfo object is required.");

            string content = JsonConvert.SerializeObject(eventInfo);
            using (var client = new HttpClient())
            using (var contentStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))))
            {
                contentStream.Headers.Add("content-type", "application/json");
                var task = client.PostAsync(keenUrl(collection, _prjSettings.WriteKey), contentStream);
                var responseMsg = task.Result;

                var responseTask = responseMsg.Content.ReadAsStringAsync();
                var responseString = responseTask.Result;
                System.Diagnostics.Debug.WriteLine("AddEvent response content:" + responseString);

                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("AddEvent failed with status: " + responseMsg.StatusCode);
            }
        }
    }
}
