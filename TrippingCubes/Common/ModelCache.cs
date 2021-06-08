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

using ShamanTK;
using ShamanTK.Common;
using ShamanTK.Graphics;
using ShamanTK.IO;
using System;

namespace TrippingCubes.Common
{
    class ModelCache
    {
        public const string ModelNodeQueryParameterKey = "modelNode";

        public int CachedScenes => sceneCache.CachedResources;

        public int CachedMeshes => meshCache.CachedResources;

        public int CachedTextures => textureCache.CachedResources;

        private readonly ResourceCache<ResourcePath, Scene>
            sceneCache;
        private readonly ResourceCache<MeshData, MeshBuffer> 
            meshCache;
        private readonly ResourceCache<TextureData, TextureBuffer> 
            textureCache;
        private readonly ResourceManager resourceManager;

        public ModelCache(ResourceManager resourceManager)
        {
            this.resourceManager = resourceManager;

            sceneCache = new ResourceCache<ResourcePath, Scene>(
                resourceManager);
            meshCache = new ResourceCache<MeshData, MeshBuffer>(
                resourceManager);
            textureCache = new ResourceCache<TextureData, TextureBuffer>(
                resourceManager);
        }

        public void LoadModel(ResourcePath sceneModelResourcePath, 
            SyncTaskCompleted<Model> onCompleted)
        {
            if (TryStripModelNodeName(ref sceneModelResourcePath,
                out string modelNodeName))
            {
                LoadModel(sceneModelResourcePath, modelNodeName, onCompleted);
            }
            else onCompleted(false, null, new Exception("The resource path " +
                "didn't contain a value for the query parameter " +
                $"'{ModelNodeQueryParameterKey}', which would've specified " +
                $"the model node to load."));
        }

        public void LoadModel(ResourcePath sceneResourcePath, string modelName,
            SyncTaskCompleted<Model> onCompleted)
        {
            sceneCache.LoadResource(sceneResourcePath,
                () => resourceManager.LoadScene(sceneResourcePath),
                (success, result, error) =>
                {
                    if (!success) onCompleted(false, null, error);
                    else LoadModelFromScene(modelName, result, onCompleted);
                });
        }

        private void LoadModelFromScene(string modelName, Scene scene,
            SyncTaskCompleted<Model> onCompleted)
        {
            if (TryFindModelNode(scene, modelName, out var node,
                out MeshData meshData))
            {
                Model model = new Model();

                if (node.Value.TryGetValue(ParameterIdentifier.Timeline,
                    out Timeline timeline) && meshData.HasSkeleton)
                {
                    model.Animation = new DeformerAnimation(timeline,
                        meshData.Skeleton);
                }

                bool meshSubtaskCompleted = false,
                    baseColorMapSubtaskCompleted = false,
                    specularMapSubtaskCompleted = false,
                    normalMapSubtaskCompleted = false,
                    emissiveMapTaskCompleted = false,
                    metallicMapTaskCompleted = false,
                    occlusionMapTaskCompleted = false;

                void FinishSubtask<ResultT>(
                    bool success, ResultT result, Exception error,
                    Action onSubtaskCompleted, Action<ResultT> valueAssigner)
                {
                    if (success) valueAssigner(result);
                    else Log.Warning("Subtask for loading model " +
                        $"\"{modelName}\" failed.", error);

                    onSubtaskCompleted();

                    if (meshSubtaskCompleted &&
                        baseColorMapSubtaskCompleted &&
                        specularMapSubtaskCompleted &&
                        normalMapSubtaskCompleted &&
                        emissiveMapTaskCompleted &&
                        metallicMapTaskCompleted &&
                        occlusionMapTaskCompleted)
                    {
                        onCompleted(true, model, null);
                    }
                }

                void StartMeshSubtask(
                    ParameterIdentifier sourceParameterIdentifier,
                    Action onSubtaskCompleted,
                    Action<MeshBuffer> valueAssigner)
                {
                    if (node.Value.TryGetValue(sourceParameterIdentifier,
                        out MeshData meshData))
                    {
                        meshCache.LoadResource(meshData,
                            () => resourceManager.LoadMesh(meshData),
                            (success, result, error) =>
                                FinishSubtask(success, result, error,
                                onSubtaskCompleted, valueAssigner));
                    }
                    else onSubtaskCompleted();
                }

                void StartTextureSubtask(
                    ParameterIdentifier sourceParameterIdentifier,
                    Action onSubtaskCompleted,
                    Action<TextureBuffer> valueAssigner)
                {
                    if (node.Value.TryGetValue(sourceParameterIdentifier,
                        out TextureData textureData))
                    {
                        textureCache.LoadResource(textureData,
                            () => resourceManager.LoadTexture(textureData),
                            (success, result, error) =>
                                FinishSubtask(success, result, error,
                                onSubtaskCompleted, valueAssigner));
                    }
                    else onSubtaskCompleted();
                }

                StartMeshSubtask(ParameterIdentifier.MeshData,
                    () => meshSubtaskCompleted = true,
                    mesh => model.Mesh = mesh);
                StartTextureSubtask(ParameterIdentifier.BaseColorMap,
                    () => baseColorMapSubtaskCompleted = true,
                    texture => model.BaseColorMap = texture);
                StartTextureSubtask(ParameterIdentifier.SpecularMap,
                    () => specularMapSubtaskCompleted = true,
                    texture => model.SpecularMap = texture);
                StartTextureSubtask(ParameterIdentifier.NormalMap,
                    () => normalMapSubtaskCompleted = true,
                    texture => model.NormalMap = texture);
                StartTextureSubtask(ParameterIdentifier.EmissiveMap,
                    () => emissiveMapTaskCompleted = true,
                    texture => model.EmissiveMap = texture);
                StartTextureSubtask(ParameterIdentifier.MetallicMap,
                    () => metallicMapTaskCompleted = true,
                    texture => model.MetallicMap = texture);
                StartTextureSubtask(ParameterIdentifier.OcclusionMap,
                    () => occlusionMapTaskCompleted = true,
                    texture => model.OcclusionMap = texture);
            }
            else onCompleted(false, null, new Exception("No model node " +
              "with the specified name was found in the scene hierarchy."));
        }

        private bool TryFindModelNode(Node<ParameterCollection> searchRoot,
            string modelNodeName, out Node<ParameterCollection> modelNode,
            out MeshData modelMeshData)
        {
            if (searchRoot.Value.Name == modelNodeName &&
                searchRoot.Value.TryGetValue(ParameterIdentifier.MeshData,
                out modelMeshData))
            {
                modelNode = searchRoot;
                return true;
            }

            if (searchRoot.Children.Count > 0)
            {
                foreach (Node<ParameterCollection> childNode in 
                    searchRoot.Children)
                {
                    if (TryFindModelNode(childNode, modelNodeName, 
                        out modelNode, out modelMeshData))
                        return true;
                }
            }

            modelNode = null;
            modelMeshData = null;
            return false;
        }        

        private static bool TryStripModelNodeName(
            ref ResourcePath resourcePath, out string modelNodeName)
        {
            var queryParameters =
                resourcePath.Query.ToNameValueCollection();
            modelNodeName =
                queryParameters.Get(ModelNodeQueryParameterKey);
            queryParameters.Remove(ModelNodeQueryParameterKey);

            if (modelNodeName != null)
                resourcePath = new ResourcePath(resourcePath.Path,
                    new ResourceQuery(queryParameters));

            return modelNodeName != null;
        }
    }
}
