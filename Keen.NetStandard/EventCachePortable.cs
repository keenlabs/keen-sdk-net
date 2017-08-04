﻿using Keen.NetStandard.EventCache;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Keen.NetStandard
{
    /// <summary>
    /// <para>EventCachePortable implements the IEventCache interface using
    /// file-based storage via the PCLStorage library. It has no
    /// cache-expiration policy.</para>
    /// <para>To use, pass an instance of this class when constructing KeenClient.
    /// To construct a new instance, call the static New() method.</para>
    /// </summary>
    public class EventCachePortable : IEventCache
    {
        protected static List<string> events = new List<string>();

        protected EventCachePortable() { }

        /// <summary>
        /// Create, initialize and return an instance of EventCachePortable.
        /// </summary>
        /// <returns></returns>
        public static EventCachePortable New()
        {
            try
            {
                return NewAsync().Result;
            }
            catch (AggregateException ex)
            {
                Debug.WriteLine(ex.TryUnwrap());
                throw ex.TryUnwrap();
            }
        }

        /// <summary>
        /// Create, initialize and return an instance of EventCachePortable.
        /// </summary>
        /// <returns></returns>
        public static async Task<EventCachePortable> NewAsync()
        {
            var instance = new EventCachePortable();

            await instance.Initialize();

            return instance;
        }

        public async Task Initialize()
        {
            // Get/create the cache directory
            DirectoryInfo keenFolder = await GetOrCreateKeenDirectory()
                .ConfigureAwait(continueOnCapturedContext: false);

            // Read all files from the cache. Each one contains a json
            // serialized CachedEvent
            var files = await Task.Run(() => keenFolder.GetFiles()).ConfigureAwait(continueOnCapturedContext: false);

            // Only bother to lock if there are files
            if (files.Count() > 0)
            {
                lock (events)
                {
                    // Add each file as an event to the pending queue
                    foreach (var file in files)
                    {
                        events.Add(file.Name);
                    }
                }
            }
        }

        public async Task AddAsync(CachedEvent e)
        {
            if (null == e)
                throw new KeenException("Cached events may not be null");

            // Get/create the cache directory
            DirectoryInfo keenFolder = await GetOrCreateKeenDirectory()
                .ConfigureAwait(continueOnCapturedContext: false);

            var attempts = 0;
            var done = false;
            string name = null;
            do
            {
                attempts++;

                // Avoid race conditions in parallel environment by locking on the events queue
                // and generating and inserting a unique name within the lock. CreateFileAsync has
                // a CreateCollisionOption.GenerateUniqueName, but it will return the same name
                // multiple times when called from parallel tasks.
                // If creating and writing the file fails, loop around and generate a new name.
                if (string.IsNullOrEmpty(name))
                    lock (events)
                    {
                        var i = 0;
                        while (events.Contains(name = e.Collection + i++))
                            ;
                        events.Add(name);
                    }

                Exception lastErr = null;
                try
                {
                    var content = JObject.FromObject(e).ToString();

                    await Task.Run(() => File.WriteAllText(Path.Combine(GetKeenFolderPath(), name), content))
                        .ConfigureAwait(continueOnCapturedContext: false);

                    done = true;
                }
                catch (Exception ex)
                {
                    lastErr = ex;
                }

                // If the file was not created, not written, or partially written,
                // the events queue may be left with a file name that references a
                // file that is nonexistent, empty, or invalid. It's easier to handle
                // this when the queue is read than to try to dequeue the name.
                if (attempts > 100)
                    throw new KeenException("Persistent failure while saving file, aborting", lastErr);
            } while (!done);
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
                var content = await Task.Run(() => File.ReadAllText(fullFileName))
                    .ConfigureAwait(continueOnCapturedContext: false);

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

        public async Task ClearAsync()
        {
            var keenFolder = await GetOrCreateKeenDirectory()
                .ConfigureAwait(continueOnCapturedContext: false);
            lock (events)
                events.Clear();
            await Task.Run(() => keenFolder.Delete(recursive: true))
                .ConfigureAwait(continueOnCapturedContext: false);
            await GetOrCreateKeenDirectory()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected static string GetKeenFolderPath()
        {
            string localStoragePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var appGuid = entryAssembly.GetType().GUID.ToString();
            return Path.Combine(localStoragePath, "KeenCache", appGuid);
        }

        protected static Task<DirectoryInfo> GetOrCreateKeenDirectory()
        {
            return Task.Run(() => Directory.CreateDirectory(GetKeenFolderPath()));
        }
    }
}
