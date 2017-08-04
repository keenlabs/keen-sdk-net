using Keen.NetStandard.EventCache;
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
        protected static Queue<string> events = new Queue<string>();

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
            var keenFolder = await GetKeenFolder()
                .ConfigureAwait(continueOnCapturedContext: false);
            var files = await Task.Run(() => keenFolder.GetFiles()).ConfigureAwait(continueOnCapturedContext: false);

            lock (events)
                if (events.Any())
                    foreach (var f in files)
                        events.Enqueue(f.Name);
        }

        public async Task AddAsync(CachedEvent e)
        {
            if (null == e)
                throw new KeenException("Cached events may not be null");

            var keenFolder = await GetKeenFolder()
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
                        events.Enqueue(name);
                    }

                Exception lastErr = null;
                try
                {
                    var content = JObject.FromObject(e).ToString();

                    await Task.Run(() => File.WriteAllText(Path.Combine(keenFolder.FullName, name), content))
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
            var keenFolder = await GetKeenFolder()
                .ConfigureAwait(continueOnCapturedContext: false);
            if (!events.Any())
                return null;

            string fileName;
            lock (events)
                fileName = events.Dequeue();
            string fullFileName = Path.Combine(keenFolder.FullName, fileName);

            var content = await Task.Run(() => File.ReadAllText(fullFileName))
                .ConfigureAwait(continueOnCapturedContext: false);
            var ce = JObject.Parse(content);

            var item = new CachedEvent((string)ce.SelectToken("Collection"), (JObject)ce.SelectToken("Event"), ce.SelectToken("Error").ToObject<Exception>());
            await Task.Run(() => File.Delete(fullFileName))
                .ConfigureAwait(continueOnCapturedContext: false);
            return item;
        }

        public async Task ClearAsync()
        {
            var keenFolder = await GetKeenFolder()
                .ConfigureAwait(continueOnCapturedContext: false);
            lock (events)
                events.Clear();
            await Task.Run(() => keenFolder.Delete(recursive: true))
                .ConfigureAwait(continueOnCapturedContext: false);
            await GetKeenFolder()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        private static Task<DirectoryInfo> GetKeenFolder()
        {
            string localStoragePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var appGuid = entryAssembly.GetType().GUID.ToString();

            return Task.Run(() => Directory.CreateDirectory(Path.Combine(localStoragePath, "KeenCache", appGuid)));
        }
    }
}
