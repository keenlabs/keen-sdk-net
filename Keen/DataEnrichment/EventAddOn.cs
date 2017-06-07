using Newtonsoft.Json;
using System.Collections.Generic;


namespace Keen.Core.DataEnrichment
{
    /// <summary>
    /// Represents a Data Enrichment add-on. 
    /// <remarks>
    /// https://keen.io/docs/data-collection/data-enrichment/
    /// </remarks>
    /// </summary>
    public sealed class AddOn
    {
        /// <summary>
        /// Name of the add-on 
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name{ get; protected set; }

        /// <summary>
        /// Parameters required by the add-on
        /// </summary>
        [JsonProperty(PropertyName = "input")]
        public Dictionary<string, string> Input { get; protected set; }

        /// <summary>
        /// Target property name where the enriched data should be stored.
        /// </summary>
        [JsonProperty(PropertyName = "output")]
        public string Output{ get; protected set; }

        /// <param name="name">Name of the data enhancement add-on.</param>
        /// <param name="input">Name-value pairs of input parameters required by the add-on.</param>
        /// <param name="output">Target property name for the enriched data.</param>
        public AddOn(string name, IDictionary<string, string> input, string output)
        {
            if (output.StartsWith("keen."))
                throw new KeenInvalidPropertyNameException(
                    "Add-on event output name may not be in the keen namespace:" + output);

            Name = name;
            Input = new Dictionary<string, string>(input);
            Output = output;
        }

        /// <summary>
        /// Build and return an IpToGeo Data Enhancement add-on. This add-on reads
        /// an IP address from the field identified by the input parameter and writes
        /// data about the geographical location to the field identified by the output parameter.
        /// </summary>
        /// <param name="outputField">Name of field to store the geographical information</param>
        /// <param name="ipField">Name of field containing an IP address</param>
        /// <returns></returns>
        public static AddOn IpToGeo(string ipField, string outputField)
        {
            return new AddOn("keen:ip_to_geo", 
                new Dictionary<string, string> {{"ip", ipField}}, 
                outputField);
        }

        /// <summary>
        /// Build and return a User-Agent Data Enhancement add-on. This add-on reads
        /// a user agent string from the field identified by the input parameter and parses it 
        /// into the device, browser, browser version, OS, and OS version fields and stores that
        /// data in the field identified by the output parameter.
        /// </summary>
        /// <param name="outputField">Name of field to store the parsed user agent field</param>
        /// <param name="userAgentString">Name of field containing the user agent string</param>
        /// <returns></returns>
        public static AddOn UserAgentParser(string userAgentString, string outputField)
        {
            return new AddOn("keen:ua_parser", 
                new Dictionary<string, string> {{"ua_string", userAgentString}}, 
                outputField);
        }

        /// <summary>
        /// Build and return a URL Parser Data Enhancement add-on. This add-on reads
        /// a well-formed URL from the field identified by the input parameter and parses
        /// it into it's components for easier filtering. The components are stored in the
        /// field identified by the output parameter.
        /// </summary>
        /// <param name="urlField">Name of field containing the URL to parse</param>
        /// <param name="outputField">Name of field to store the parsed url components</param>
        /// <returns></returns>
        public static AddOn UrlParser(string urlField, string outputField)
        {
            return new AddOn("keen:url_parser", 
                new Dictionary<string, string> { { "url", urlField } }, 
                outputField);
        }

        /// <summary>
        /// Build and return a Referrer Parser Data Enhancement add-on. This add-on reads
        /// a well-formed referrer URL from the field identified by the input parameter and 
        /// parses it into it's components. The components are stored in the field identified
        /// by the output parameter.
        /// </summary>
        /// <param name="pageUrlField">Name of field containing the URL of the current page</param>
        /// <param name="outputField">Name of field to store the parsed referrer data.</param>
        /// <param name="referrerUrlField">Name of field containing the referrer URL</param>
        /// <returns></returns>
        public static AddOn ReferrerParser(string referrerUrlField, string pageUrlField, string outputField)
        {
            return new AddOn("keen:referrer_parser",
                new Dictionary<string, string> {{"referrer_url", referrerUrlField}, {"page_url", pageUrlField}},
                outputField);
        }
    }
}