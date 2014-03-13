using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Keen.Core
{
    public static class KeenUtil
    {
        private static HashSet<string> validCollectionNames = new HashSet<string>();

        public static void ValidatePropertyName(string property)
        {
            if (string.IsNullOrWhiteSpace(property))
                throw new KeenException("Property name may not be null or whitespace");

            if (property.Length >= 256)
                throw new KeenException("Property name must be less than 256 characters");

            if (property.StartsWith("$"))
                throw new KeenException("Property name may not start with \"$\"");

            if (property.Contains("."))
                throw new KeenException("Property name may not contain \".\"");
        }

        /// <summary>
        /// Apply the collection name restrictions.
        /// </summary>
        /// <param name="collection"></param>
        public static void ValidateEventCollectionName(string collection)
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

        public static JObject PostEvent(string keenUrl, JObject jEvent, string authKey, out HttpResponseMessage httpResponse)
        {
            var content = jEvent.ToString();
            Debug.WriteLine("AddEvent json:" + content);
            using (var client = new HttpClient())
            using (var contentStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))))
            {
                contentStream.Headers.Add("content-type", "application/json");

                client.DefaultRequestHeaders.Add("Authorization", authKey);
                httpResponse = client.PostAsync(keenUrl, contentStream).Result;
                var responseString = httpResponse.Content.ReadAsStringAsync().Result;
                return JObject.Parse(responseString);
            }
        }

        /// <summary>
        /// Check the 'error_code' field and throw the appropriate exception if non-null.
        /// </summary>
        /// <param name="apiResponse">Deserialized json response from a Keen API call.</param>
        public static void CheckApiErrorCode(dynamic apiResponse)
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

                    case "InvalidBatchError":
                        throw new KeenInvalidBatchException((string)apiResponse.message);

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
