using Keen.Core.EventCache;
using Newtonsoft.Json.Linq;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Core
{
    public class EventCachePortable : IEventCache
    {
        private static Queue<string> events = new Queue<string>();

        private EventCachePortable()
        {}

        public static EventCachePortable Factory()
        {
            try
            {
                return FactoryAsync().Result;
            }
            catch (AggregateException ex)
            {
                Debug.WriteLine(ex.TryUnwrap());
                throw ex.TryUnwrap();
            } 
        }

        public static async Task<EventCachePortable> FactoryAsync()
        {
            var instance = new EventCachePortable();

            var keenFolder = await getKeenFolder()
                .ConfigureAwait(continueOnCapturedContext: false);
            var files = (await keenFolder.GetFilesAsync().ConfigureAwait(continueOnCapturedContext: false)).ToList();

            lock(events)
                if (events.Any())
                    foreach (var f in files)
                        events.Enqueue(f.Name);

            return instance;
        }

        public async Task Add(CachedEvent e)
        {
            if (null == e)
                throw new KeenException("Cached events may not be null");

            var keenFolder = await getKeenFolder()
                .ConfigureAwait(continueOnCapturedContext: false);

            IFile file;
            var attempts = 0;
            var done = false;
            do
            {
                attempts++;

                string name;
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
                    file = await keenFolder.CreateFileAsync(name, CreationCollisionOption.FailIfExists)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    var content = JObject.FromObject(e).ToString();

                    await file.WriteAllTextAsync(content)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    done = true;
                }
                catch (Exception ex)
                {
                    lastErr = ex;
                }

                if (attempts > 100)
                    throw new KeenException("Persistent failure while saving file, aborting", lastErr);
            } while (!done);           
        }

        public async Task<CachedEvent> TryTake()
        {
            var keenFolder = await getKeenFolder()
                .ConfigureAwait(continueOnCapturedContext: false);
            if (!events.Any())
                return null;

            string fileName;
            lock(events)
                fileName = events.Dequeue();

            var file = await keenFolder.GetFileAsync(fileName)
                .ConfigureAwait(continueOnCapturedContext: false);
            var content = await file.ReadAllTextAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
            dynamic ce = JObject.Parse(content);

            var item = new CachedEvent((string)ce.Collection, (JObject)ce.Event, (Exception)ce.Error );
            await file.DeleteAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
            return item;
        }

        public async Task Clear()
        {
            var keenFolder = await getKeenFolder()
                .ConfigureAwait(continueOnCapturedContext: false);
            lock(events)
                events.Clear();
            await keenFolder.DeleteAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
            await getKeenFolder()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        private static async Task<IFolder> getKeenFolder()
        {
            IFolder rootFolder = FileSystem.Current.LocalStorage;
            var keenFolder = await rootFolder.CreateFolderAsync("KeenCache", CreationCollisionOption.OpenIfExists)
                .ConfigureAwait(continueOnCapturedContext: false);
            return keenFolder;
        }

    }
}
