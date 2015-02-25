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

        public static string ToSafeString(this object obj)
        {
            return (obj ?? string.Empty).ToString();
        }

        public static int? TryGetInt(this string s)
        {
            int i;
            return int.TryParse(s, out i) ? (int?)i : null;
        }

        public static double? TryGetDouble(this string s)
        {
            double i;
            return double.TryParse(s, out i) ? (double?)i : null;
        }

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
            // Avoid cost of re-checking collection names that have already been validated.
            // There is a race condition here, but it's harmless and does not justify the
            // overhead of synchronization.
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
        /// Check the 'error' field on a bulk insert operation response and return 
        /// the appropriate exception.
        /// </summary>
        /// <param name="apiResponse">Deserialized json response from a Keen API call.</param>
        public static Exception GetBulkApiError(JObject apiResponse)
        {
            var error = apiResponse.SelectToken("$.error");
            if (null == error)
                return null;

            var errCode = error.SelectToken("$.name").ToString();
            var message = error.SelectToken("$.description").ToString();
            switch (errCode)
            {
                case "InvalidApiKeyError":
                    return new KeenInvalidApiKeyException(message);

                case "ResourceNotFoundError":
                    return new KeenResourceNotFoundException(message);

                case "NamespaceTypeError":
                    return new KeenNamespaceTypeException(message);

                case "InvalidEventError":
                    return new KeenInvalidEventException(message);

                case "ListsOfNonPrimitivesNotAllowedError":
                    return new KeenListsOfNonPrimitivesNotAllowedException(message);

                case "InvalidBatchError":
                    return new KeenInvalidBatchException(message);

                case "InternalServerError":
                    return new KeenInternalServerErrorException(message);

                case "InvalidKeenNamespaceProperty":
                    return new KeenInvalidKeenNamespacePropertyException(message);

                case "InvalidPropertyNameError":
                    return new KeenInvalidPropertyNameException(message);

                default:
                    Debug.WriteLine("Unhandled error_code \"{0}\" : \"{1}\"", errCode, message);
                    return new KeenException(errCode + " : " + message);
            }
        }


        /// <summary>
        /// Check the 'error_code' field and throw the appropriate exception if non-null.
        /// </summary>
        /// <param name="apiResponse">Deserialized json response from a Keen API call.</param>
        public static void CheckApiErrorCode(dynamic apiResponse)
        {
            if (apiResponse is JArray) return;
            
            var errorCode = (string) apiResponse.SelectToken("$.error_code");

            if (errorCode != null)
            {
                var message = (string)apiResponse.SelectToken("$.message");
                switch (errorCode)
                {
                    case "InvalidApiKeyError":
                        throw new KeenInvalidApiKeyException(message);

                    case "ResourceNotFoundError":
                        throw new KeenResourceNotFoundException(message);

                    case "NamespaceTypeError":
                        throw new KeenNamespaceTypeException(message);

                    case "InvalidEventError":
                        throw new KeenInvalidEventException(message);

                    case "ListsOfNonPrimitivesNotAllowedError":
                        throw new KeenListsOfNonPrimitivesNotAllowedException(message);

                    case "InvalidBatchError":
                        throw new KeenInvalidBatchException(message);

                    case "InternalServerError":
                        throw new KeenInternalServerErrorException(message);

                    case "InvalidKeenNamespaceProperty":
                        throw new KeenInvalidKeenNamespacePropertyException(message);

                    default:
                        Debug.WriteLine("Unhandled error_code \"{0}\" : \"{1}\"", errorCode, message);
                        throw new KeenException(errorCode + " : " + message);
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
