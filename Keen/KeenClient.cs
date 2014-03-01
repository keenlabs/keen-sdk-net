using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Keen.Core
{
    public class KeenClient
    {
        private IProjectSettings _prjSettings;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="prjSettings">A ProjectSettings instance containing the ProjectId and API keys</param>
		public KeenClient(IProjectSettings prjSettings)
        {
			// Preconditions
            if (null==prjSettings)
                throw new KeenException("An ProjectSettings instance is required.");
            if (string.IsNullOrWhiteSpace(prjSettings.ProjectId))
                throw new KeenException("A Project ID is required.");
            if ((string.IsNullOrWhiteSpace(prjSettings.MasterKey)
                && string.IsNullOrWhiteSpace(prjSettings.WriteKey)))
                throw new KeenException("A Master or Write API key is required.");

            _prjSettings = prjSettings;
        }

		/// <summary>
		/// Retrieve the schema for the specified collection. This requires
        /// a value for the project settings Master API key.
		/// </summary>
		/// <param name="collection"></param>
        public void GetSchema(string collection)
        {
            throw new KeenException();
        }

        /// <summary>
        /// Add a an event to the specified collection.
        /// </summary>
        /// <param name="collection">Collection name</param>
        /// <param name="eventProperties">The event to add</param>
        public void AddEvent(string collection, dynamic eventInfo)
        {
            string content = JsonConvert.SerializeObject(eventInfo);
            using (var client = new HttpClient())
            using (var contentStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))))
            {
                contentStream.Headers.Add("content-type", "application/json");
                var task = client.PostAsync(string.Format("http://api.keen.io/3.0/projects/{0}/events/{1}?api_key={2}", _prjSettings.ProjectId, collection, _prjSettings.WriteKey), contentStream);
                var response = task.Result;

                if (!response.IsSuccessStatusCode)
                    throw new KeenException("AddEvent failed with status: " + response.StatusCode);
            }
        }
    }
}
