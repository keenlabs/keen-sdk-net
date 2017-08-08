keen-sdk-net
============

[![Build status](https://ci.appveyor.com/api/projects/status/sxkqpvmlxto07y4r/branch/master?svg=true)](https://ci.appveyor.com/project/masojus/keen-sdk-net/branch/master) [![Coverage Status](https://coveralls.io/repos/github/keenlabs/keen-sdk-net/badge.svg?branch=master)](https://coveralls.io/github/keenlabs/keen-sdk-net?branch=master) [![NuGet](http://img.shields.io/nuget/v/KeenClient.svg)](https://www.nuget.org/packages/KeenClient/)

Overview
-----

The Keen IO .NET SDK can be used to do custom analytics and event tracking for .NET applications. Use this SDK to capture large volumes of event data such as user actions, errors, server interactions, or any arbitrary event you specify. The SDK posts your events to Keen IO, a highly available, scalable cloud datastore. See [Keen IO docs](https://keen.io/docs) for instructions on extracting, querying, and building custom analytics with your data.

.NET Version Support
------------

There are three versions of the .NET SDK, which vary based on the target platform.

A portable class library targets .NET 4.5, Windows and Windows Phone 8+, and Xamarin for iOS and Android.

A .NET 4.5 specific library makes use of the portable class library and adds a few features including scoped key generation and project settings providers which read settings from environment variables or a file.

For older projects and Unity, a separate .NET 3.5 library exists, though it lacks query support.

Installation
------------

The easiest way to get started with the Keen IO .NET SDK is to use the [KeenClient NuGet package](http://www.nuget.org/packages/KeenClient/).

Install the NuGet package by running the following command from the NuGet Package Manager Console:

```
PM> Install-Package KeenClient
```

The most up-to-date code is available in the following repository:

```
https://github.com/keenlabs/keen-sdk-net
```

Initializing the Library
------------------------

The core object you'll interact with to add events to a collection is the `KeenClient` object. When creating a `KeenClient` instance, you'll want to provide it with a `ProjectSettingsProvider` instance that contains details about your project id, keys, and optionally a different root URL for Keen.IO's API.

```
using Keen.Core; // Replace this with Keen.NET_35 for projects targeting .NET 3.5
...
var projectSettings = new ProjectSettingsProvider("YourProjectID", writeKey: "YourWriteKey");
var keenClient = new KeenClient(projectSettings);
```

Recording Events
----------------

Event data is provided to the client as an object. A simple way to do this is with an anonymous object:

```
var purchase = new
{
    category = "magical animals",
    username = "hagrid",
    price = 7.13,
    payment_type = "information",
    animal_type = "norwegian ridgeback dragon"
};

keenClient.AddEvent("purchases", purchase);
```

Recording Events Asynchronously
-------------------------------

Sometimes you want to record events in a non-blocking manner. This is pretty simple:

```
keenClient.AddEventAsync("purchases", purchase);
```

Using Global Properties
-----------------------

Static global properties are added with the `KeenClient`'s `AddGlobalProperty` method:

```
keenClient.AddGlobalProperty("client_type", "mobile");
```

Static global properties are added at the root level of all events just before they are sent or cached.

Dynamic global properties are an SDK concept that can be added in the same way, but rather than a static object, an object implementing `IDynamicPropertyValue` is added. The class `DynamicPropertyValue` implements this interface and may be used to provide dynamic properties with a `Func<object>` delegate:

```
var dynProp = new DynamicPropertyValue(() => new Random().Next(9999));
keenClient.AddGlobalProperty("bonus_field", dynProp);
```

The delegate is executed each time event data is added as well as during the `AddGlobalProperty` call.

Using Data Enrichment Add-ons
------------------------------

Keen IO can enrich event data by parsing or joining it with other data sets. This is done through the concept of “add-ons”. See the [Keen IO API documentation](https://keen.io/docs/api/#data-enrichment) for more on this. The .NET SDK enables add-ons with the `Keen.Core.DataEnrichment.AddOn` class.

```
// Build an event object
var purchase = new
{
    category = "magical animals",
    username = "hagrid",
    price = 7.13,
    payment_type = "information",
    animal_type = "norwegian ridgeback dragon",
    user_ip = "8.8.8.8",
    ua = "Mozilla/4.0 (compatible; MSIE 5.0; Windows NT; DigExt; .NET CLR 1.0.3705)"
};

var addOns = new[]
{
    AddOn.IpToGeo("user_ip", "user_geo"),
    AddOn.UserAgentParser("ua", "user_agent")
};

// send the event
keenClient.AddEvent("purchases", purchase, addOns);
```

When the event is recorded the "user_geo" and "user_agent" fields will be populated automatically by the Keen IO API.

Complete Event Recording Example
------------

```
static void Main(string[] args)
{
    // Set up the client
    var projectSettings = new ProjectSettingsProvider("YourProjectID", writeKey: "YourWriteKey");
    var keenClient = new KeenClient(projectSettings);

    keenClient.AddGlobalProperty("client_type", "mobile");

    var dynProp = new DynamicPropertyValue(() => new Random().Next(9999));
    keenClient.AddGlobalProperty("bonus_field", dynProp );

    // Build an event object
    var purchase = new
    {
        category = "magical animals",
        username = "hagrid",
        price = 7.13,
        payment_type = "information",
        animal_type = "norwegian ridgeback dragon",
        user_ip = "8.8.8.8",
        ua = "Mozilla/4.0 (compatible; MSIE 5.0; Windows NT; DigExt; .NET CLR 1.0.3705)"
    };

    var addOns = new[]
    {
        AddOn.IpToGeo("user_ip", "user_geo"),
        AddOn.UserAgentParser("ua", "user_agent")
    };

    // send the event
    keenClient.AddEvent("purchases", purchase, addOns);
}
```

Caching
-------

KeenClient supports an event data cache interface that allows transmission of event data to the Keen IO server to be deferred until you call SendCachedEvents(). You may implement your own cache by supporting the IEventCache interface or you may use one of the two cache classes included, EventCacheMemory and EventCachePortable which store event data in memory and in portable storage, respectively.

To enable caching provide an instance supporting IEventCache when constructing KeenClient:

```
var client = new KeenClient(new ProjectSettingsProviderEnv(), new EventCacheMemory());
```

Or:

```
var client = new KeenClient(new ProjectSettingsProviderEnv(), EventCachePortable.New());
```

Events are added as usual, and at any time you may transmit the cached events to the server:

```
client.SendCachedEvents();
```

The server may reject one or more events included in the cache. If this happens the item that was rejected will be recorded and transmission of the remaining cached events will continue. After all events in the cache have been transmitted, if any events were rejected they will be attached as instances of CachedEvent to an instance of KeenBulkException which will then be thrown. The KeenBulkException FailedEvents property may be accessed to review the failures.

Global properties are evaluated and added when AddEvent() is called, so dynamic properties will not be evaluated when SendCachedEvents() is called.

Analysis
------------

To run [analyses](https://keen.io/docs/api/#analyses) on your data, use the provided `KeenClient.Query` family of methods. For example:

```
var itemCount = keenClient.Query(QueryType.Count(), "target_collection", null);
```

An `async` version of this analysis could be run as follows:

```
var itemCount = await keenClient.QueryAsync(QueryType.Count(), "target_collection", null);
```

Additional qualifiers can be added to the analysis, such as the target property to use for analyses that require it. A timeframe and/or list of filters to use for the analysis can also be provided. If you'd like to get results in a grouped or time interval format, the `KeenClient.QueryGroup`, `KeenClient.QueryInterval`, and `KeenClient.QueryIntervalGroup` synchronous and asynchronous methods can be used. See the Grouped and Interval Query Results and Filters sections below for more detail.


Multi-Analysis
------------

Multi-analysis is a way to run multiple analyses over the same dataset. For more information about multi-analysis, see the [API documentation](https://keen.io/docs/api/#multi-analysis).

To perform multi-analysis, use the `KeenClient.QueryMultiAnalysis` family of methods.

```
IEnumerable<MultiAnalysisParam> analyses = new List<MultiAnalysisParam>()
{
    new MultiAnalysisParam("purchases", MultiAnalysisParam.Metric.Count()),
    new MultiAnalysisParam("max_price", MultiAnalysisParam.Metric.Maximum("price")),
    new MultiAnalysisParam("min_price", MultiAnalysisParam.Metric.Minimum("price"))
};

var result = keenClient.QueryMultiAnalysis("purchases", analyses);

var purchases = int.Parse(result["purchases"]);
var maxPrice = float.Parse(result["max_price"]);
```


Funnel Analysis
------------

Returns the number of unique actors that successfully (or unsuccessfully) make it through a series of steps. “Actors” could mean users, devices, or any other identifiers that are meaningful to you. For more information about Funnels, see the [Funnel API documentation](https://keen.io/docs/api/#funnels).

To perform funnel analysis, `KeenClient` exposes the methods `QueryFunnel` and `QueryFunnelAsync`, which are used as follows:

```
IEnumerable<FunnelStep> funnelSteps = new List<FunnelStep>
{
    new FunnelStep
    {
        EventCollection = "registered_users",
        ActorProperty = "id"
    },
    new FunnelStep
    {
        EventCollection = "subscribed_users",
        ActorProperty = "user_id"
    },
};

var result = keenClient.QueryFunnel(funnelSteps);

var registeredUsers = result.ElementAt(0);
var registeredAndSubscribedUserCount = result.ElementAt(1);
```

Timeframes
------------

A timeframe can be specified for analysis using the `QueryRelativeTimeframe` and `QueryAbsoluteTimeframe` classes, along with an optional `timezone` parameter passed to the `KeenClient.Query` method when using `QueryRelativeTimeframe`. The `timezone` parameter must be one of the timezones supported by the Keen IO API as specified [here](https://keen.io/docs/api/#timezone).

For example:
```
var relativeTimeframe = QueryRelativeTimeframe.ThisWeek();
var timezone = "US/Pacific"; // If not specified, timezone defaults to "UTC"

var countUnique = keenClient.Query(QueryType.CountUnique(), "target_collection", "target_property", relativeTimeframe, timezone: timezone);
```

Here's an example using an absolute timeframe. Note that timezone information is included in the DateTime struct, and therefore shouldn't be provided as an additional parameter.
```
var absoluteTimeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddMonths(-1), DateTime.Now));

var countUnique = keenClient.Query(QueryType.CountUnique(), "target_collection", "target_property", absoluteTimeframe);
```


Filters
------------

Analyses, multi-analysis, and funnel steps all support using filters to be more specific about the dataset being worked on. For simple analyses and multi-analyses, provide an `IEnumerable<QueryFilter>` to the `KeenClient.Query` or `KeenClient.QueryMultiAnalysis` method of choice. For funnel analysis, filters can be specified on each `FunnelStep` through the `FunnelStep.Filters` property.

```
var filters = new List<QueryFilter>()
{
    new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1")
};

var result = keenClient.Query(QueryType.Count(), "user_registrations", null, filters: filters);
```

Grouped and Interval Query Results
------------

To perform analysis or multi-analysis with results grouped by a column value, separated by a timeframe, or a combination of both, there are versions of the `KeenClient.Query` and `KeenClient.QueryMultiAnalysis` methods available. These include `KeenClient.QueryInterval`, `KeenClient.QueryGroup`, `KeenClient.QueryIntervalGroup` and their corresponding asynchronous methods for single-analysis. For multi-analysis, similar methods exist including `KeenClient.QueryMultiAnalysisGroup`, `KeenClient.QueryMultiAnalysisInterval`, `KeenClient.QueryMultiAnalysisIntervalGroup`, and the asynchronous versions of those methods. See the Keen IO [group by](https://keen.io/docs/api/#group-by) and [interval](https://keen.io/docs/api/#interval) API documentation for more about these types of analyses.


Scoped Keys
------------

Scoped keys are customized API keys you can generate yourself. Each key has a defined scope of allowed operations (read/write), along with a set of predetermined filters that are applied to every request. See the [Keen IO API reference](https://keen.io/docs/api/#scoped-keys) for more information on scoped keys.

The .NET 4.0 SDK includes methods for generating scoped keys. These methods aren't available in the .NET 4.5 portable library or the .NET 3.5 library. You'll find them under the `Keen.Core` namespace as `ScopedKey.Encrypt`, `ScopedKey.EncryptString`, and `ScopedKey.Decrypt`.


```
// Create a filter to apply when using the scoped key
IDictionary<string, object> filter = new ExpandoObject();
filter.Add("property_name", "account_id");
filter.Add("operator", "eq");
filter.Add("property_value", 123);

dynamic options = new ExpandoObject();
// Set filters for the key
options.filters = new List<object>() { filter };
// Set read/write permissions for the key
options.allowed_operations = new List<string>() { "read" };

// Generate the key using the given master key and options
var scopedKey = ScopedKey.Encrypt(masterKey, (object)options);

// Decrypt the key to get the key's filters and permissions
var decrypted = ScopedKey.Decrypt(masterKey, scopedKey);
var decryptedOptions = JObject.Parse(decrypted);
```
