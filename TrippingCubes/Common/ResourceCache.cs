using ShamanTK.Common;
using ShamanTK.IO;
using System;
using System.Collections.Generic;

namespace TrippingCubes.Common
{
    class ResourceCache<KeyT, ResourceT> : IDisposable
        where ResourceT : class
    {
        protected ResourceManager ResourceManager { get; }

        public int RunningCacheTasks => taskCache.Count;

        public int CachedResources => resourceCache.Count;

        private readonly Dictionary<KeyT, SyncTask<ResourceT>>
            taskCache = new Dictionary<KeyT, SyncTask<ResourceT>>();
        private readonly Dictionary<KeyT, ResourceT>
            resourceCache = new Dictionary<KeyT, ResourceT>();

        public ResourceCache(ResourceManager resourceManager)
        {
            ResourceManager = resourceManager;
        }

        public void LoadResource(KeyT resourceKey,
            Func<SyncTask<ResourceT>> onStartLoadingResource,
            Action<bool, ResourceT, Exception> onCompleted)
        {
            if (resourceCache.TryGetValue(resourceKey, out ResourceT resource))
            {
                onCompleted(true, resource, null);
            }
            else if (taskCache.TryGetValue(resourceKey,
                out SyncTask<ResourceT> loader))
            {
                loader.Subscribe((e) =>
                {
                    if (e.HasValue) onCompleted(true, e.Value, null);
                    else onCompleted(false, null, e.Error);
                });
            }
            else
            {
                SyncTask<ResourceT> newLoader = onStartLoadingResource();

                newLoader.Subscribe((e) =>
                {
                    taskCache.Remove(resourceKey);

                    if (e.HasValue) onCompleted(true, e.Value, null);
                    else onCompleted(false, null, e.Error);
                });

                taskCache.Add(resourceKey, newLoader);
            }
        }

        public void Dispose()
        {
            foreach (ResourceT resource in resourceCache.Values)
            {
                if (resource is IDisposable disposableResource)
                    disposableResource.Dispose();
            }
            resourceCache.Clear();
        }
    }
}
