using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Keen.NET_35
{
    public static class KeenUtil
    {
        private static readonly string SdkVersion;

        static KeenUtil()
        {
            string version = GetAssemblyInformationalVersion();

            // TODO : What will be the proper string to represent unknown version numbers? Is
            // something like ".net35-*" or ".net35-*.*.*" OK?
            version = (version.IsNullOrWhiteSpace() ? "*" : version);
            SdkVersion = string.Format(".net35-{0}", version);
        }

        /// <summary>
        /// Retrieve a string representing the current version of the Keen IO SDK, as defined by
        /// the AssemblyInformationVersion.
        /// </summary>
        /// <returns>The SDK version string.</returns>
        public static string GetSdkVersion()
        {
            return SdkVersion;
        }

        private static string GetAssemblyInformationalVersion()
        {
            string assemblyInformationalVersion = string.Empty;

            var attributes =
                Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                as AssemblyInformationalVersionAttribute[];

            if (null != attributes && 0 < attributes.Length)
            {
                assemblyInformationalVersion = attributes[0].InformationalVersion;
            }

            return assemblyInformationalVersion;
        }

        public static bool IsNullOrWhiteSpace(this string s)
        {
            return s == null || string.IsNullOrEmpty(s.Trim());
        }

        private static readonly HashSet<string> ValidCollectionNames = new HashSet<string>();

        public static string ToSafeString(this object obj)
        {
            return (obj ?? string.Empty).ToString();
        }

        public static int? TryGetInt(this string s)
        {
            int i;
            return int.TryParse(s, out i) ? (int?) i : null;
        }

        public static double? TryGetDouble(this string s)
        {
            double i;
            return double.TryParse(s, out i) ? (double?) i : null;
        }

        /// <summary>
        /// Apply property name restrictions. Throws KeenException with an 
        /// explanation if a collection name is unacceptable.
        /// </summary>
        /// <param name="property"></param>
        public static void ValidatePropertyName(string property)
        {
            if (property.IsNullOrWhiteSpace())
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
            if (ValidCollectionNames.Contains(collection))
                return;

            if (null == collection)
                throw new KeenException("Event collection name may not be null.");
            if (collection.IsNullOrWhiteSpace())
                throw new KeenException("Event collection name may not be blank.");
            if (collection.Length > 64)
                throw new KeenException("Event collection name may not be longer than 64 characters.");
            if (new Regex("[^\x00-\x7F]").Match(collection).Success)
                throw new KeenException("Event collection name must contain only Ascii characters.");
            if (collection.Contains("$"))
                throw new KeenException("Event collection name may not contain \"$\".");
            if (collection.StartsWith("_"))
                throw new KeenException("Event collection name may not begin with \"_\".");

            ValidCollectionNames.Add(collection);
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
                    Debug.WriteLine(string.Format("Unhandled error_code \"{0}\" : \"{1}\"", errCode, message));
                    return new KeenException(errCode + " : " + message);
            }
        }


        /// <summary>
        /// Check the 'error_code' field and throw the appropriate exception if non-null.
        /// </summary>
        /// <param name="apiResponse">Deserialized json response from a Keen API call.</param>
        public static void CheckApiErrorCode(JObject apiResponse)
        {
            if (apiResponse == null) return;
            if (apiResponse["error_code"] == null) return;

            var err = apiResponse["error_code"].Value<string>();
            var msg = apiResponse["message"].Value<string>();

            switch (err)
            {
                case "InvalidApiKeyError":
                    throw new KeenInvalidApiKeyException(msg);

                case "ResourceNotFoundError":
                    throw new KeenResourceNotFoundException(msg);

                case "NamespaceTypeError":
                    throw new KeenNamespaceTypeException(msg);

                case "InvalidEventError":
                    throw new KeenInvalidEventException(msg);

                case "ListsOfNonPrimitivesNotAllowedError":
                    throw new KeenListsOfNonPrimitivesNotAllowedException(msg);

                case "InvalidBatchError":
                    throw new KeenInvalidBatchException(msg);

                case "InternalServerError":
                    throw new KeenInternalServerErrorException(msg);

                case "InvalidKeenNamespaceProperty":
                    throw new KeenInvalidKeenNamespacePropertyException(msg);

                default:
                    Debug.WriteLine(string.Format("Unhandled error_code \"{0}\" : \"{1}\"", err, msg));
                    throw new KeenException(err + " : " + msg);
            }
        }

        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>
            (this IEnumerable<TFirst> first,
                IEnumerable<TSecond> second,
                Func<TFirst, TSecond, TResult> resultSelector)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (second == null) throw new ArgumentNullException("second");
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            return ZipIterator(first, second, resultSelector);
        }

        private static IEnumerable<TResult> ZipIterator<TFirst, TSecond, TResult>
            (IEnumerable<TFirst> first,
                IEnumerable<TSecond> second,
                Func<TFirst, TSecond, TResult> resultSelector)
        {
            using (var e1 = first.GetEnumerator())
            using (var e2 = second.GetEnumerator())
                while (e1.MoveNext() && e2.MoveNext())
                    yield return resultSelector(e1.Current, e2.Current);
        }
    }
}
