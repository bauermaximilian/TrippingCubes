/*
 * TrippingCubes
 * A toolkit for creating games in a voxel-based environment.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

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
