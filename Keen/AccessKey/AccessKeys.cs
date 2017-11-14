using System;
using System.Threading.Tasks;
using Keen.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;


namespace Keen.AccessKey
{
    /// <summary>
    /// AccessKeys implements the IAccessKeys interface which represents the Keen.IO Access
    /// Key API methods.
    /// </summary>
    public class AccessKeys : IAccessKeys
    {
        private static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                Formatting = Formatting.None
            };

        private readonly IKeenHttpClient _keenHttpClient;
        private readonly string _accessKeyRelativeUrl;
        private readonly string _readKey;
        private readonly string _masterKey;


        internal AccessKeys(IProjectSettings prjSettings,
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
            _accessKeyRelativeUrl = KeenHttpClient.GetRelativeUrl(prjSettings.ProjectId,
                                                                  KeenConstants.AccessKeyResource);

            _readKey = prjSettings.ReadKey;
            _masterKey = prjSettings.MasterKey;
        }

        public async Task<JObject> CreateAccessKey(AccessKeyDefinition accesskey)
        {
            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API WriteKey is required to add events.");
            }

            if (null == accesskey)
            {
                throw new KeenException("An instance of AccessKeyDefinition must be provided");
            }

            var content = JsonConvert.SerializeObject(accesskey, SerializerSettings);

            var responseMsg = await _keenHttpClient
                .PostAsync(_accessKeyRelativeUrl, _masterKey, content)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            JObject jsonResponse = null;

            try
            {
                jsonResponse = JObject.Parse(responseString);
            }
            catch (Exception)
            {
                // To avoid any flow stoppers
            }

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException("Creating Access Key failed with status: " +
                                        responseMsg.StatusCode);
            }

            if (null == jsonResponse)
            {
                throw new KeenException("Creating Access Key failed with empty JSON response.");
            }

            return jsonResponse;
        }
    }
}
