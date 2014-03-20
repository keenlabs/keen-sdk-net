using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Keen.Core
{
    public static class KeenUtil
    {
        private static HashSet<string> validCollectionNames = new HashSet<string>();

        /// <summary>
        /// Apply property name restrictions. Throws KeenException with an 
        /// explanation if a collection name is unacceptable.
        /// </summary>
        /// <param name="property"></param>
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
        /// Apply the collection name restrictions. Throws KeenException with an 
        /// explanation if a collection name is unacceptable.
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
            if (collection.Length > 64)
                throw new KeenException("Event collection name may not be longer than 64 characters.");
            if (new Regex("[^\x00-\x7F]").Match(collection).Success)
                throw new KeenException("Event collection name must contain only Ascii characters.");
            if (collection.Contains("$"))
                throw new KeenException("Event collection name may not contain \"$\".");
            if (collection.StartsWith("_"))
                throw new KeenException("Event collection name may not begin with \"_\".");

            validCollectionNames.Add(collection);
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

        /// <summary>
        /// Flatten an AggregateException and if only one exception instance is found 
        /// in the innerexceptions, return it, otherwise return the original 
        /// AggregateException unchanged.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        internal static Exception TryUnwrap(this AggregateException ex)
        {
            if (ex.Flatten().InnerExceptions.Count == 1)
                return ex.Flatten().InnerExceptions[0];
            else
                return ex;
        }
    }
}
