using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace Keen.Core
{
    /// <summary>
    /// EventCollection implements the IEventCollection interface which represents the Keen.IO
    /// EventCollection API methods.
    /// </summary>
    internal class EventCollection : IEventCollection
    {
        private readonly IKeenHttpClient _keenHttpClient;
        private readonly string _eventsRelativeUrl;
        private readonly string _readKey;
        private readonly string _writeKey;
        private readonly string _masterKey;


        internal EventCollection(IProjectSettings prjSettings,
                                 IKeenHttpClientProvider keenHttpClientProvider)
        {
            if (null == prjSettings)
            {
                throw new ArgumentNullException(nameof(prjSettings),
                                                "Project Settings must be provided.");
            }

            if (null == keenHttpClientProvider)
            {
                throw new ArgumentNullException(nameof(keenHttpClientProvider),
                                                "A KeenHttpClient provider must be provided.");
            }

            if (string.IsNullOrWhiteSpace(prjSettings.KeenUrl) ||
                !Uri.IsWellFormedUriString(prjSettings.KeenUrl, UriKind.Absolute))
            {
                throw new KeenException(
                    "A properly formatted KeenUrl must be provided via Project Settings.");
            }

            var serverBaseUrl = new Uri(prjSettings.KeenUrl);
            _keenHttpClient = keenHttpClientProvider.GetForUrl(serverBaseUrl);
            _eventsRelativeUrl = KeenHttpClient.GetRelativeUrl(prjSettings.ProjectId,
                                                               KeenConstants.EventsResource);

            // TODO : It's possible we may want to change back to dynamically grabbing the keys
            // from a stored IProjectSettings so client code can lazily assign keys. It creates a
            // minor potential race condition, but will allow for scenarios like creating a
            // KeenClient instance with only a master key in order to generate/acquire access keys
            // to then set as the other keys. Otherwise a new KeenClient must be created or at
            // least a new instance of the IEventCollection/IEvent/IQueries implementations.

            _readKey = prjSettings.ReadKey;
            _writeKey = prjSettings.WriteKey;
            _masterKey = prjSettings.MasterKey;
        }


        public async Task<JObject> GetSchema(string collection)
        {
            // TODO : So much of this code, both in the constructor and in the actual message
            // dispatch, response parsing and error checking is copy/paste across Queries, Event
            // and EventCollection everywhere we use KeenHttpClient. We could shove some of that
            // into shared factory functionality (for the ctor stuff) and some of it into the
            // KeenHttpClient (for the dispatch/response portions).


            if (string.IsNullOrWhiteSpace(_readKey))
            {
                throw new KeenException("An API ReadKey is required to get collection schema.");
            }

            var responseMsg = await _keenHttpClient
                .GetAsync(GetCollectionUrl(collection), _readKey)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            dynamic response = JObject.Parse(responseString);

            // error checking, throw an exception with information from the json 
            // response if available, then check the HTTP response.
            KeenUtil.CheckApiErrorCode(response);

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException("GetSchema failed with status: " + responseMsg.StatusCode);
            }

            return response;
        }

        public async Task DeleteCollection(string collection)
        {
            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API MasterKey is required to delete a collection.");
            }

            var responseMsg = await _keenHttpClient
                .DeleteAsync(GetCollectionUrl(collection), _masterKey)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException("DeleteCollection failed with status: " + responseMsg.StatusCode);
            }
        }

        public async Task AddEvent(string collection, JObject anEvent)
        {
            if (string.IsNullOrWhiteSpace(_writeKey))
            {
                throw new KeenException("An API WriteKey is required to add events.");
            }

            var content = anEvent.ToString();

            var responseMsg = await _keenHttpClient
                .PostAsync(GetCollectionUrl(collection), _writeKey, content)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            JObject jsonResponse = null;

            try
            {
                // Normally the response content should be parsable JSON,
                // but if the server returned a 404 error page or something
                // like that, this will throw.
                jsonResponse = JObject.Parse(responseString);
            }
            catch (Exception)
            {
            }

            // error checking, throw an exception with information from the 
            // json response if available, then check the HTTP response.
            KeenUtil.CheckApiErrorCode(jsonResponse);

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException("AddEvent failed with status: " + responseMsg.StatusCode);
            }
        }

        private string GetCollectionUrl(string collection)
        {
            return $"{_eventsRelativeUrl}/{collection}";
        }
    }
}
