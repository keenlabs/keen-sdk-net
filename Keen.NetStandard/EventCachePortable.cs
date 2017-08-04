using Keen.NetStandard.EventCache;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace Keen.NetStandard
{
    /// <summary>
    /// <para>EventCachePortable implements the IEventCache interface using
    /// file-based storage. It has no cache-expiration policy.</para>
    /// <para>To use, pass an instance of this class when constructing KeenClient.
    /// To construct a new instance, call the static New() method.</para>
    /// </summary>
    public class EventCachePortable : IEventCache
    {
        // An asynchronous, lazy loaded instance of this class.
        private static AsyncLazy<EventCachePortable> _instanceTask =
            new AsyncLazy<EventCachePortable>(
                taskFactory: CreateInstanceAsync);

        // A list of events in the cache
        protected List<string> events = new List<string>();

        protected EventCachePortable() { }

        /// <summary>
        /// Get the singleton instance of EventCachePortable.
        /// Since initialization is async, this wraps the instance
        /// with a Task.
        /// </summary>
        /// <para>To get the EventCachePortable instance, await this property.</para>
        /// <para>The Task<EventCachePortable> is available via Value on this property.</para>
        /// <returns></returns>
        public static AsyncLazy<EventCachePortable> InstanceTask => _instanceTask;

        /// <summary>
        /// Create, initialize and return an instance of EventCachePortable.
        /// </summary>
        /// <returns></returns>
        private static async Task<EventCachePortable> CreateInstanceAsync()
        {
            var cache = new EventCachePortable();

            await cache.InitializeAsync();

            return cache;
        }

        /// <summary>
        /// Initialize an instance by ensuring the cache path exists,
        /// then read all existing cached events into the in-memory queue.
        /// </summary>
        /// <returns></returns>
        protected async Task InitializeAsync()
        {
            // Get/create the cache directory
            DirectoryInfo keenFolder = await GetOrCreateKeenDirectoryAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            // Read all files from the cache. Each one contains a json
            // serialized CachedEvent
            var files = await Task.Run(() => keenFolder.GetFiles())
                .ConfigureAwait(continueOnCapturedContext: false);

            // Only bother to lock if there are files
            if (files.Count() > 0)
            {
                // No need to lock since this is a singleton initializer
                // Add each file as an event to the pending queue
                foreach (var file in files)
                {
                    events.Add(file.Name);
                }
            }
        }

        /// <summary>
        /// Clears all events from the cache.
        /// </summary>
        /// <returns></returns>
        public async Task ClearAsync()
        {
            lock (events)
            {
                events.Clear();
            }

            string cachePath = GetKeenFolderPath();

            if (Directory.Exists(cachePath))
            {
                await Task.Run(() => Directory.Delete(GetKeenFolderPath(), recursive: true))
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        /// <summary>
        /// Adds an event to the cache.
        /// </summary>
        /// <returns></returns>
        /// <param name="e">The CachedEvent to add to the cache.</param>
        public async Task AddAsync(CachedEvent e)
        {
            if (null == e)
                throw new KeenException("Cached events may not be null");

            // Get/create the cache directory
            DirectoryInfo keenFolder = await GetOrCreateKeenDirectoryAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            // Come up with a name to use for the file on disk.
            // This is sufficiently random such that name collisions
            // won't happen.
            string fileName = Path.GetRandomFileName();

            lock (events)
            {
                events.Add(fileName);
            }

            try
            {
                var content = JObject.FromObject(e).ToString();

                using (FileStream stream = File.Open(Path.Combine(keenFolder.FullName, fileName),
                                                     FileMode.CreateNew))
                {
                    byte[] fileBytes = Encoding.UTF8.GetBytes(content);
                    await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
            }
            catch (Exception ex)
            {
                throw new KeenException("Failure while saving file", ex);
            }
        }

        public async Task<CachedEvent> TryTakeAsync()
        {
            if (!events.Any())
                return null;

            string fileName;
            lock (events)
            {
                fileName = events.First();
            }

            string fullFileName = Path.Combine(GetKeenFolderPath(), fileName);

            CachedEvent item;
            try
            {
                string content;
                using (FileStream stream = File.Open(fullFileName, FileMode.Open))
                {
                    byte[] fileBytes = new byte[stream.Length];
                    await stream.ReadAsync(fileBytes, 0, (int)stream.Length);
                    content = Encoding.UTF8.GetString(fileBytes);
                }

                var ce = JObject.Parse(content);

                item = new CachedEvent(
                    (string)ce.SelectToken("Collection"),
                    (JObject)ce.SelectToken("Event"),
                    ce.SelectToken("Error").ToObject<Exception>());

                await Task.Run(() => File.Delete(fullFileName))
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            finally
            {
                lock (events)
                {
                    events.Remove(fileName);
                }
            }

            return item;
        }

        protected static string GetKeenFolderPath()
        {
            string localStoragePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var appGuid = entryAssembly.GetType().GUID.ToString();
            return Path.Combine(localStoragePath, "KeenCache", appGuid);
        }

        protected static Task<DirectoryInfo> GetOrCreateKeenDirectoryAsync()
        {
            return Task.Run(() => Directory.CreateDirectory(GetKeenFolderPath()));
        }
    }
}
