using ShamanTK.Common;
using ShamanTK.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml;
using TrippingCubes.Common;
using TrippingCubes.Entities;

namespace TrippingCubes
{
    class GameWorldConfiguration
    {
        private const string XmlNodeRoot = "TrippingCubes";
        private const string XmlNodeStyle = "Style";
        private const string XmlNodeBlockRegistryPath = "BlockRegistryPath";
        private const string XmlNodeSkyboxPath = "SkyboxPath";
        private const string XmlNodePaths = "Paths";
        private const string XmlNodePathPoint = "Point";
        private const string XmlNodeEntityConfigurations = 
            "EntityConfigurations";
        private const string XmlNodeEntities = "Entities";
        private const string XmlAttributeEntityConfigurationType = "Type";
        private const string XmlAttributePathClose = "Close";

        public Dictionary<string, PathLinear> Paths { get; }
            = new Dictionary<string, PathLinear>();

        public Dictionary<string, EntityConfiguration> EntityConfigurations 
            { get; } =  new Dictionary<string, EntityConfiguration>();

        public List<EntityInstantiation> Entities { get; }
            = new List<EntityInstantiation>();

        public FileSystemPath BlockRegistryPath { get; set; } = 
            "/Styles/Vaporwave/registry.xml";

        public FileSystemPath SkyboxPath { get; set; } =
            "/Styles/Vaporwave/skybox.png";

        public static GameWorldConfiguration FromXml(IFileSystem fileSystem,
            FileSystemPath configurationFilePath)
        {
            string xmlString;
            try
            {
                using StreamReader streamReader = new StreamReader(
                    fileSystem.OpenFile(configurationFilePath, false));
                xmlString = streamReader.ReadToEnd();
            }
            catch (Exception exc)
            {
                throw new IOException("The configuration file couldn't be " +
                    "opened.", exc);
            }

            return FromXml(xmlString);
        }

        public static GameWorldConfiguration FromXml(string xmlString)
        {
            XmlDocument document = new XmlDocument();

            try { document.LoadXml(xmlString); }
            catch (XmlException) { throw; }

            XmlNode rootNode = document.DocumentElement;

            if (rootNode.Name != XmlNodeRoot)
                throw new XmlException("The document root element name " +
                    "is invalid for the current context.");

            GameWorldConfiguration configuration = 
                new GameWorldConfiguration();

            XmlNode styleNode = rootNode[XmlNodeStyle];
            XmlNode pathsNode = rootNode[XmlNodePaths];
            XmlNode configurationsNode = rootNode[XmlNodeEntityConfigurations];
            XmlNode entitiesNode = rootNode[XmlNodeEntities];

            if (styleNode != null)
                configuration.InitializeStyleParameters(styleNode);

            if (pathsNode != null)
                configuration.InitializePaths(pathsNode);

            if (configurationsNode != null)
                configuration.InitializeEntityConfigurations(
                    configurationsNode);

            if (entitiesNode != null)
                configuration.InitializeEntityInstantiations(entitiesNode);

            return configuration;
        }

        private IEnumerable<KeyValuePair<string, string>> GetNodeEnumerator(
            XmlNode node)
        {
            foreach (XmlNode parameterNode in node.ChildNodes)
            {
                if (parameterNode.NodeType == XmlNodeType.Element)
                    yield return new KeyValuePair<string, string>(
                        parameterNode.Name, parameterNode.InnerText);
            }
        }

        private void InitializeStyleParameters(XmlNode worldNode)
        {
            try
            {
                BlockRegistryPath = worldNode[XmlNodeBlockRegistryPath]?
                    .InnerText ?? BlockRegistryPath;
            }
            catch (Exception exc)
            {
                throw new XmlException("The specified value for " +
                    $"{nameof(XmlNodeBlockRegistryPath)} is invalid.", exc);
            }

            try
            {
                SkyboxPath = worldNode[XmlNodeSkyboxPath]?.InnerText ?? 
                    SkyboxPath;
            }
            catch (Exception exc)
            {
                throw new XmlException("The specified value for " +
                    $"{nameof(XmlNodeSkyboxPath)} is invalid.", exc);
            }
        }

        private void InitializeEntityInstantiations(
            XmlNode instantiationsNode)
        {
            Entities.Clear();

            foreach (XmlNode instantiationNode in instantiationsNode)
            {
                if (instantiationNode.NodeType != XmlNodeType.Element)
                    continue;

                string configurationKey = instantiationNode.Name;
                if (!EntityConfigurations.ContainsKey(configurationKey))
                    throw new XmlException("The entity configuration " +
                        $"{configurationKey} wasn't defined and can't be " +
                        "used to create a new entity.");

                var entityParameters =
                    GetNodeEnumerator(instantiationNode).ToList();

                Entities.Add(new EntityInstantiation()
                {
                    ConfigurationIdentifier = configurationKey,
                    InstanceParameters = entityParameters
                });
            }
        }

        private void InitializeEntityConfigurations(
            XmlNode configurationsNode)
        {
            EntityConfigurations.Clear();

            foreach (XmlNode configurationNode in configurationsNode)
            {
                if (configurationNode.NodeType != XmlNodeType.Element)
                    continue;

                string configurationKey = configurationNode.Name;
                if (Paths.ContainsKey(configurationKey))
                    throw new XmlException("The entity configuration key " +
                        $"{configurationKey} was defined more than once.");

                string configurationTypeName = configurationNode.Attributes[
                    XmlAttributeEntityConfigurationType]?.Value;
                if (string.IsNullOrWhiteSpace(configurationTypeName))
                    throw new XmlException("The " +
                        $"{nameof(XmlAttributeEntityConfigurationType)} " +
                        "attribute value for the entity configuration " +
                        $"{configurationKey} must not be undefined.");

                var configurationParameters =
                    GetNodeEnumerator(configurationNode).ToList();

                EntityConfigurations[configurationKey] =
                    new EntityConfiguration(configurationTypeName)
                    {
                        EntityParameters = configurationParameters
                    };
            }
        }

        private void InitializePaths(XmlNode pathsNode)
        {
            Paths.Clear();

            foreach (XmlNode pathNode in pathsNode.ChildNodes)
            {
                if (pathNode.NodeType != XmlNodeType.Element) continue;

                string pathKey = pathNode.Name;
                if (Paths.ContainsKey(pathKey))
                    throw new XmlException($"The path {pathKey} was defined " +
                        "more than once.");

                XmlAttribute closeAttibute = 
                    pathNode.Attributes[XmlAttributePathClose];
                if (closeAttibute == null)
                    throw new XmlException($"The {XmlAttributePathClose} " +
                        $"attribute was missing on path {pathKey}.");
                if (!bool.TryParse(closeAttibute.Value, out bool closePath))
                    throw new XmlException($"The {XmlAttributePathClose} " +
                        "attribute value was no valid boolean on " +
                        $"path {pathKey}.");

                List<Vector3> pathPoints = new List<Vector3>();

                foreach (XmlNode pointNode in pathNode.ChildNodes)
                {
                    if (pointNode.NodeType != XmlNodeType.Element ||
                        pointNode.Name != XmlNodePathPoint) continue;

                    if (!PrimitiveTypeParser.TryParse(pointNode.InnerText,
                        out Vector3 pathPoint))
                    {
                        throw new XmlException("The point definition with " +
                            $"index {pathPoints.Count} in path {pathKey} " +
                            "was no valid 3-dimensional vector.");
                    }

                    pathPoints.Add(pathPoint);
                }

                Paths[pathKey] = new PathLinear(pathPoints, closePath);
            }
        }
    }
}
