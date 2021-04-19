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
using ShamanTK.IO;
using TrippingCubes.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using Rectangle = ShamanTK.Common.Rectangle;
using ShamanTK.Platforms.Common.IO;

namespace TrippingCubes.World
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// The idea of this class is to seperately add all block definitions,
    /// textures and models loosely connected with each other through 
    /// <see cref="BlockKey"/>s. It is possible, for example, to add textures 
    /// for a block which doesn't exist (yet), add meshes with an identifier
    /// that can be referenced by a block, and so on. It isn't until calling
    /// the <see cref="GenerateRegistry(IFileSystem)"/> method that all of the
    /// data is put together to an efficient, read-only data structure - the
    /// <see cref="BlockRegistry"/>. This approach also allows a dynamic 
    /// generation of a shared block texture map where each texture file - 
    /// even if referenced multiple times - only appears once. 
    /// </remarks>
    class BlockRegistryBuilder
    {
        public const int MaximumTextureClippingSizePx = 128;

        private class BlockDraft
        {
            public BlockKey Key { get; }

            public string Identifier { get; }

            public BlockProperties Properties { get; }

            public BlockDraft(BlockKey key, string identifier, 
                BlockProperties properties)
            {
                Key = key;
                Identifier = identifier ?? throw new ArgumentNullException(
                    nameof(identifier));
                Properties = properties ??
                    throw new ArgumentNullException(nameof(properties));
            }
        }

        private readonly Dictionary<string, AdjacentList<BlockMeshSegment>> 
            referenceableMeshes = 
            new Dictionary<string, AdjacentList<BlockMeshSegment>>();
        private readonly Dictionary<BlockKey, AdjacentList<BlockMeshSegment>> 
            meshes = 
            new Dictionary<BlockKey, AdjacentList<BlockMeshSegment>>();

        private readonly Dictionary<string, AdjacentList<FileSystemPath>>
            referenceableTextures = 
            new Dictionary<string, AdjacentList<FileSystemPath>>();
        private readonly Dictionary<BlockKey, AdjacentList<FileSystemPath>> 
            textures = 
            new Dictionary<BlockKey, AdjacentList<FileSystemPath>>();

        private readonly Dictionary<BlockKey, BlockDraft> blocks = 
            new Dictionary<BlockKey, BlockDraft>();
        private readonly Dictionary<string, BlockDraft> namedBlocks =
            new Dictionary<string, BlockDraft>();

        private readonly Dictionary<int, int> blockKeyIdVariations = 
            new Dictionary<int, int>();

        private readonly FileSystemPath rootDirectory;

        public int TextureClippingSizePx
        {
            get => textureClippingSizePx;
            set
            {
                if (value > 0 && value <= MaximumTextureClippingSizePx)
                    textureClippingSizePx = value;
                else throw new ArgumentOutOfRangeException();
            }
        }
        private int textureClippingSizePx = 32;

        public VertexPropertyDataFormat BlockMeshVertexPropertyDataFormat
        {
            get => blockMeshVertexPropertyDataFormat;
            set
            {
                if (Enum.IsDefined(typeof(VertexPropertyDataFormat), value))
                    blockMeshVertexPropertyDataFormat = value;
                else throw new ArgumentException("The specified value is " +
                    "no valid vertex property data format.");
            }
        }
        private VertexPropertyDataFormat blockMeshVertexPropertyDataFormat =
            VertexPropertyDataFormat.ColorLight;

        public BlockKey? ErrorBlockKey { get; set; }

        public BlockRegistryBuilder() : this(FileSystemPath.Root) { }

        public BlockRegistryBuilder(FileSystemPath rootDirectory)
        {
            if (!rootDirectory.IsAbsolute)
                throw new ArgumentException("The specified root path is " +
                    "not absolute.");
            if (!rootDirectory.IsDirectoryPath)
                throw new ArgumentException("The specified root path is " +
                    "no valid directory path.");

            this.rootDirectory = rootDirectory;
        }

        public static BlockRegistryBuilder FromXml(
            FileSystemPath configurationFilePath, IFileSystem fileSystem,
            bool throwOnConfigurationError)
        {
            XmlDocument document = new XmlDocument();

            try
            {
                using (Stream stream = fileSystem.OpenFile(
                    configurationFilePath, false)) 
                    document.Load(stream);
            }
            catch (ArgumentException exc)
            {
                throw new FileNotFoundException("The specified " +
                    "configuration file path is invalid.", exc);
            }
            catch (FileNotFoundException exc)
            {
                throw new FileNotFoundException("The specified " +
                    "configuration file wasn't found.", exc);
            }
            catch (IOException exc)
            {
                throw new IOException("The specified configuration file " +
                       "couldn't be accessed.", exc);
            }
            catch (XmlException exc)
            {
                throw new FormatException("The configuration file was " +
                    "no valid XML file.", exc);
            }

            return FromXml(document, 
                configurationFilePath.GetParentDirectory(), 
                throwOnConfigurationError);
        }

        public static BlockRegistryBuilder FromXml(XmlDocument document,
            bool throwOnConfigurationError)
        {
            return FromXml(document, FileSystemPath.Root, 
                throwOnConfigurationError);
        }

        public static BlockRegistryBuilder FromXml(XmlDocument document,
            FileSystemPath rootDirectory, bool throwOnConfigurationError)
        {
            BlockRegistryBuilder blockRegistryBuilder = 
                new BlockRegistryBuilder(rootDirectory);

            blockRegistryBuilder.ApplySettingsFromXml(
                document.SelectSingleNode("//settings"), 
                !throwOnConfigurationError);

            foreach (XmlNode meshNode in document.SelectNodes("//mesh"))
            {
                try { blockRegistryBuilder.AddMesh(meshNode); }
                catch (Exception exc)
                {
                    if (throwOnConfigurationError) throw exc;
                    else Log.Error("An invalid mesh definition was " +
                        "encountered, which will be ignored.", exc);
                }
            }

            foreach (XmlNode textureNode in document.SelectNodes("//texture"))
            {
                try { blockRegistryBuilder.AddTexture(textureNode); }
                catch (Exception exc)
                {
                    if (throwOnConfigurationError) throw exc;
                    else Log.Error("An invalid texture definition was " +
                        "encountered, which will be ignored.", exc);
                }
            }

            foreach (XmlNode blockNode in document.SelectNodes("//block"))
            {
                try { blockRegistryBuilder.AddBlock(blockNode); }
                catch (Exception exc)
                {
                    if (throwOnConfigurationError) throw exc;
                    else Log.Error("An invalid block definition was " +
                        "encountered, which will be ignored.", exc);
                }
            }

            return blockRegistryBuilder;
        }

        #region XML parser methods
        private void ApplySettingsFromXml(XmlNode settingsNode, bool tolerant)
        {
            if (settingsNode == null) return;

            XmlNode textureClippingSizePxNode =
                settingsNode["textureClippingSizePx"];
            XmlNode vertexPropertyDataFormatNode =
                settingsNode["vertexPropertyDataFormat"];
            XmlNode errorBlockKeyNode =
                settingsNode["errorBlockKey"];

            try
            {
                if (textureClippingSizePxNode != null)
                {
                    if (int.TryParse(textureClippingSizePxNode.InnerText,
                        out int textureClippingSizePx))
                    {
                        try { TextureClippingSizePx = textureClippingSizePx; }
                        catch (Exception exc)
                        {
                            throw new FormatException("The specified " +
                                "texture clipping size value is invalid.",
                                exc);
                        }
                    }
                    else throw new FormatException("The specified clipping " +
                        "size value is no valid integer number.");
                }
            }
            catch (Exception exc)
            {
                if (tolerant) Log.Error(exc);
                else throw exc;
            }

            try {
                if (vertexPropertyDataFormatNode != null)
                {
                    if (Enum.TryParse(vertexPropertyDataFormatNode.InnerText, 
                        true, out VertexPropertyDataFormat dataFormat))
                    {
                        BlockMeshVertexPropertyDataFormat = dataFormat;
                    }
                    else throw new FormatException("The specified vertex " +
                        "property data format is invalid.");
                }
            }
            catch (Exception exc)
            {
                if (tolerant) Log.Error(exc);
                else throw exc;
            }

            try {
                if (errorBlockKeyNode != null)
                {
                    if (BlockKey.TryParse(errorBlockKeyNode.InnerText,
                        out BlockKey errorBlockKey)) 
                        ErrorBlockKey = errorBlockKey;
                    else throw new FormatException("The specified error " +
                        "block key reference is invalid.");
                }
            }
            catch (Exception exc)
            {
                if (tolerant) Log.Error(exc);
                else throw exc;
            }
        }

        private static bool IsBlockNode(XmlNode blockNode, 
            out BlockKey blockKey, out string blockIdentifier)
        {
            blockKey = default;

            if (blockNode != null && blockNode.Name == "block")
            {
                blockIdentifier = blockNode.Attributes["identifier"]?.Value;

                return !string.IsNullOrWhiteSpace(blockIdentifier) &&
                    BlockKey.TryParse(blockNode.Attributes["key"]?.Value,
                    out blockKey);
            }

            blockIdentifier = null;
            return false;
        }

        public void AddBlock(XmlNode blockNode)
        {
            if (blockNode == null)
                throw new ArgumentNullException(nameof(blockNode));

            if (IsBlockNode(blockNode, out var key, out var identifier))
            {
                int luminance = BlockProperties.Solid.Luminance;
                bool isTranslucent = BlockProperties.Solid.IsTranslucent;
                BlockColliderType type = BlockProperties.Solid.Type;

                XmlNode propertiesNode = blockNode["properties"];
                if (propertiesNode != null)
                {
                    XmlNode luminanceNode = propertiesNode["luminance"];
                    XmlNode isTranslucentNode =
                        propertiesNode["isTranslucent"];
                    XmlNode typeNode = propertiesNode["type"];

                    if (luminanceNode != null)
                    {
                        if (!int.TryParse(luminanceNode.InnerText,
                            out luminance))
                            throw new FormatException("The specified " +
                              "luminance level is no valid integer.");
                    }

                    if (isTranslucentNode != null)
                    {
                        if (!bool.TryParse(isTranslucentNode.InnerText,
                            out isTranslucent))
                            throw new FormatException("The specified " +
                                "value for the isTranslucent node is " +
                                "no valid boolean.");
                    }

                    if (typeNode != null)
                    {
                        if (!Enum.TryParse(typeNode.InnerText, out type))
                            throw new FormatException("The specified " +
                                "value for the typeNode node is " +
                                "no valid BlockColliderType.");
                    }
                }

                BlockProperties blockProperties;
                try
                {
                    blockProperties = new BlockProperties(type, isTranslucent,
                        luminance);
                }
                catch (Exception exc)
                {
                    throw new FormatException("The block properties have " +
                        "invalid values.", exc);
                }

                AddBlock(key, identifier, blockProperties);
            }
            else throw new FormatException("The specified node " +
              "is no valid/complete block declaration.");
        }

        private static BlockMeshSegment ParseBlockMeshSegment(
            XmlNode meshSegmentNode)
        {
            if (meshSegmentNode == null) return null;

            XmlNode verticesNode = meshSegmentNode["vertices"];
            XmlNode facesNode = meshSegmentNode["faces"];

            if (verticesNode == null && facesNode != null) return null;
            else if (verticesNode == null)
                throw new FormatException("The vertices were missing.");
            else if (facesNode == null)
                throw new FormatException("The faces were missing.");

            string format = verticesNode.Attributes["format"]?.Value;

            Vertex[] vertices;
            Face[] faces;

            try
            {
                if (string.IsNullOrEmpty(format))
                    vertices = Vertex.Parse(verticesNode.InnerText);
                else vertices = Vertex.Parse(verticesNode.InnerText, format);
            }
            catch (Exception exc)
            {
                throw new FormatException("The vertex data was invalid.", exc);
            }

            try { faces = Face.Parse(facesNode.InnerText); }
            catch (Exception exc)
            {
                throw new FormatException("The face data was invalid.", exc);
            }

            return new BlockMeshSegment(vertices, faces);
        }

        private static AdjacentList<BlockMeshSegment> ParseBlockMesh(
            XmlNode blockMeshNode)
        {
            return new AdjacentList<BlockMeshSegment>
            {
                Base = ParseBlockMeshSegment(blockMeshNode["base"]),
                East = ParseBlockMeshSegment(blockMeshNode["east"]),
                West = ParseBlockMeshSegment(blockMeshNode["west"]),
                Above = ParseBlockMeshSegment(blockMeshNode["above"]),
                Below = ParseBlockMeshSegment(blockMeshNode["below"]),
                North = ParseBlockMeshSegment(blockMeshNode["north"]),
                South = ParseBlockMeshSegment(blockMeshNode["south"])
            };
        }

        private static AdjacentList<FileSystemPath> ParseTexture(
            XmlNode textureNode)
        {
            return new AdjacentList<FileSystemPath>
            {
                Base = textureNode["base"]?.InnerText ??
                    textureNode.SelectSingleNode("text()")?.InnerText,
                West = textureNode["west"]?.InnerText,
                East = textureNode["east"]?.InnerText,
                Above = textureNode["above"]?.InnerText,
                Below = textureNode["below"]?.InnerText,
                North = textureNode["north"]?.InnerText,
                South = textureNode["south"]?.InnerText
            };
        }

        public void AddMesh(XmlNode meshNode)
        {
            if (meshNode == null)
                throw new ArgumentNullException(nameof(meshNode));

            string identifier = meshNode.Attributes["identifier"]?.Value;
            string reference = meshNode.Attributes["reference"]?.Value;

            bool isChildOfBlockDefinition = IsBlockNode(meshNode.ParentNode, 
                out var parentBlockKey, out _);

            //Interpret the node as referencable mesh.
            if (identifier != null && reference == null)
            {
                if (identifier.Trim().Length == 0)
                    throw new FormatException("The mesh identifier was " +
                        "empty or whitespaces only.");

                try
                {
                    AddReferenceableMesh(identifier, ParseBlockMesh(meshNode));
                }
                catch (Exception exc)
                {
                    throw new FormatException("The definition of mesh\"" +
                        identifier.Clamp(32) + "\" is invalid.", exc);
                }
            }
            else if (identifier == null)
            {
                if (!isChildOfBlockDefinition)
                    throw new FormatException("A mesh node without " +
                        "identifier must be a child of a block node.");

                //Interpret the node as mesh reference inside a block
                if (reference != null)
                {
                    try { AddMesh(parentBlockKey, reference); }
                    catch (Exception exc)
                    {
                        throw new FormatException("The mesh reference \"" +
                            reference.Clamp(32) + "\" couldn't be " +
                            "dereferenced.", exc);
                    }
                }
                else
                //Interpret the node as mesh declaration inside a block
                {
                    try { AddMesh(parentBlockKey, ParseBlockMesh(meshNode)); }
                    catch (Exception exc)
                    {
                        throw new FormatException("The mesh definition in " +
                            "the block definition (with key '" +
                            parentBlockKey + "') was invalid.", exc);
                    }
                }
            }
            else throw new FormatException("A mesh node must not have both " +
                "an identifier and a reference to an identifier.");
        }

        public void AddTexture(XmlNode textureNode)
        {
            if (textureNode == null)
                throw new ArgumentNullException(nameof(textureNode));

            string identifier = textureNode.Attributes["identifier"]?.Value;
            string reference = textureNode.Attributes["reference"]?.Value;

            bool isChildOfBlockDefinition = IsBlockNode(textureNode.ParentNode,
                out var parentBlockKey, out _);

            //Interpret the node as referencable texture.
            if (identifier != null && reference == null)
            {
                if (identifier.Trim().Length == 0)
                    throw new FormatException("The texture identifier was " +
                        "empty or whitespaces only.");

                try
                {
                    AddReferenceableTexture(identifier, 
                        ParseTexture(textureNode));
                }
                catch (Exception exc)
                {
                    throw new FormatException("The definition of texture \"" +
                        identifier.Clamp(32) + "\" is invalid.", exc);
                }
            }
            //Interpret the node as texture reference inside a block.
            else if (identifier == null)
            {
                if (!isChildOfBlockDefinition)
                    throw new FormatException("A texture node without " +
                        "identifier must be a child of a block node.");

                if (reference != null)
                {
                    try { AddTexture(parentBlockKey, reference); }
                    catch (Exception exc)
                    {
                        throw new FormatException("The texture reference \"" +
                            reference.Clamp(32) + "\" couldn't be " +
                            "dereferenced.", exc);
                    }
                }
                else
                {
                    try
                    {
                        AddTexture(parentBlockKey, ParseTexture(textureNode));
                    }
                    catch (Exception exc)
                    {
                        throw new FormatException("The texture definition " +
                            "in the block definition (with key '" +
                            parentBlockKey + "') was invalid.", exc);
                    }
                }
            }
            else throw new FormatException("A texture node must not have " +
                "both an identifier and a reference to an identifier.");
        }
#endregion

        public void AddBlock(BlockKey blockKey, string blockIdentifier,
            BlockProperties blockProperties)
        {
            if (blockIdentifier == null)
                throw new ArgumentNullException(nameof(blockIdentifier));
            if (blockProperties == null)
                throw new ArgumentNullException(nameof(blockProperties));

            if (blockKeyIdVariations.TryGetValue(blockKey.Id,
                out int highestBlockIdVariation))
            {
                blockKeyIdVariations[blockKey.Id] =
                    Math.Max(highestBlockIdVariation, blockKey.Variation);
            }
            else blockKeyIdVariations[blockKey.Id] =
                    blockKey.Variation;

            if (blocks.ContainsKey(blockKey))
                throw new ArgumentException("A block with the key '" +
                    blockKey + "' was already defined.");
            else if (namedBlocks.ContainsKey(blockIdentifier))
                throw new ArgumentException("A block with the identifier \"" +
                    blockIdentifier.Clamp(32) + "\" was already defined.");
            else
            {
                BlockDraft newBlockDraft = new BlockDraft(blockKey,
                    blockIdentifier, blockProperties);
                blocks[blockKey] = newBlockDraft;
                namedBlocks[blockIdentifier] = newBlockDraft;
            }
        }

        public void AddReferenceableMesh(string meshIdentifier,
            AdjacentList<BlockMeshSegment> blockMesh)
        {
            if (meshIdentifier == null)
                throw new ArgumentNullException(nameof(meshIdentifier));
            if (blockMesh == null)
                throw new ArgumentNullException(nameof(blockMesh));

            if (referenceableMeshes.ContainsKey(meshIdentifier))
                throw new ArgumentException("The mesh reference " +
                    "identifier \"" + meshIdentifier.Clamp(32) + "\" was " +
                    "already defined.");
            else referenceableMeshes[meshIdentifier] = blockMesh;
        }

        public void AddMesh(BlockKey associatedBlockKey,
            string meshIdentifierReference)
        {
            if (meshIdentifierReference == null)
                throw new ArgumentNullException(
                    nameof(meshIdentifierReference));

            if (!referenceableMeshes.TryGetValue(meshIdentifierReference,
                out AdjacentList<BlockMeshSegment> blockMesh))
                throw new ArgumentException("The specified mesh " +
                    "reference identifier wasn't defined.");
            else AddMesh(associatedBlockKey, blockMesh);
        }

        public void AddMesh(BlockKey associatedBlockKey,
            AdjacentList<BlockMeshSegment> blockMesh)
        {
            if (blockMesh == null)
                throw new ArgumentNullException(nameof(blockMesh));

            if (meshes.ContainsKey(associatedBlockKey))
                throw new ArgumentException("The mesh for the block key '" +
                    associatedBlockKey + "' was already defined.");
            else meshes[associatedBlockKey] = blockMesh;
        }

        public void AddReferenceableTexture(string textureIdentifier,
            AdjacentList<FileSystemPath> blockSideTexturePaths)
        {
            if (textureIdentifier == null)
                throw new ArgumentNullException(nameof(textureIdentifier));
            if (blockSideTexturePaths == null)
                throw new ArgumentNullException(nameof(blockSideTexturePaths));

            if (referenceableTextures.ContainsKey(textureIdentifier))
                throw new ArgumentException("The texture reference " +
                    "identifier \"" + textureIdentifier.Clamp(32) + "\" was " +
                    "already defined.");
            else referenceableTextures[textureIdentifier] =
                    blockSideTexturePaths.Convert(MakeFilePathAbsolute);
        }

        public void AddTexture(BlockKey associatedBlockKey, 
            string textureIdentifierReference)
        {
            if (textureIdentifierReference == null)
                throw new ArgumentNullException(
                    nameof(textureIdentifierReference));

            if (!referenceableTextures.TryGetValue(textureIdentifierReference,
                out AdjacentList<FileSystemPath> blockSideTexturePaths))
                throw new ArgumentException("The specified texture " +
                    "reference identifier wasn't defined.");
            else AddTexture(associatedBlockKey, blockSideTexturePaths);
        }

        public void AddTexture(BlockKey associatedBlockKey,
                AdjacentList<FileSystemPath> blockSideTexturePaths)
        {
            if (blockSideTexturePaths == null)
                throw new ArgumentNullException(nameof(blockSideTexturePaths));

            if (textures.ContainsKey(associatedBlockKey))
                throw new ArgumentException("The texture for the block key '" +
                    associatedBlockKey + "' was already defined.");
            else
            {
                //If the base texture clipping is defined, it's also 
                //assigned to every undefined side - so that every side
                //without an explicit texture definition "inherits" the
                //base texture clipping, which saves lots of redundant
                //file references.
                if (!blockSideTexturePaths.Base.IsEmpty)
                    blockSideTexturePaths.SetAllUndefined(
                        blockSideTexturePaths.Base);

                //All texture sides should be defined now - either implicitely
                //by applying the base to every undefined slot or explicitely.
                //If not, the texture definition is not valid.
                if (blockSideTexturePaths.ContainsUndefinedElements())
                    throw new ArgumentException("The texture definition " +
                        "for the block key '" + associatedBlockKey + "' was " +
                        "incomplete - not all sides have a texture assigned " +
                        "or a base texture definition was missing.");

                try
                {
                    textures[associatedBlockKey] =
                        blockSideTexturePaths.Convert(MakeFilePathAbsolute);
                }
                catch (Exception exc)
                {
                    throw new ArgumentException("The texture for the block " +
                        "key '" + associatedBlockKey + "' was invalid.", exc);
                }
            }
        }        

        private FileSystemPath MakeFilePathAbsolute(
            FileSystemPath relativeFilePath)
        {
            if (relativeFilePath.IsEmpty)
                throw new ArgumentException("The specified path is empty.");
            if (relativeFilePath.IsDirectoryPath)
                throw new ArgumentException("The specified path is no valid " +
                    "file path.");

            if (relativeFilePath.IsAbsolute) return relativeFilePath;
            else
            {
                try { return relativeFilePath.ToAbsolute(rootDirectory); }
                catch (Exception exc)
                {
                    throw new ArgumentException("The specified relative " +
                        "file path couldn't be converted into an absolute " +
                        "file path.", exc);
                }
            }
        }

        private static bool TryImportBitmap(IFileSystem fileSystem,
            FileSystemPath path, out Bitmap bitmap)
        {
            string fileName = "?";
            try
            {
                fileName = path.GetFileName(false);
                using (Stream bitmapStream =
                    fileSystem.OpenFile(path, false))
                    bitmap = new Bitmap(bitmapStream);
                return true;
            }
            catch (Exception exc)
            {
                Log.Error("The block texture file \"" + fileName + "\" " +
                    "couldn't be loaded.", exc);

                bitmap = null;
                return false;
            }
        }

        private bool GenerateRegistryTextureCollection(
            IFileSystem fileSystem, out TextureData texture,
            out Dictionary<BlockKey, AdjacentList<Rectangle>> textureClippings)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(
                    nameof(fileSystem));

            DateTime start = DateTime.Now;

            //Create a set with all texture file paths to determine how many
            //textures are used (and how many texture slots will be required).
            HashSet<FileSystemPath> texturePaths = 
                new HashSet<FileSystemPath>();
            foreach (var blockTexture in textures)
                blockTexture.Value.ForEachSlot(p => texturePaths.Add(p));

            //If no textures are referenced (for example when all the coloring
            //is provided by using vertex colors), the method can stop here.
            if (texturePaths.Count == 0)
            {
                texture = null;
                textureClippings = 
                    new Dictionary<BlockKey, AdjacentList<Rectangle>>();
                return false;
            }

            //Calculate the amount of "texture slots" per side as a potency
            //of two - while this might require more texture space than 
            //absolutely necessary, it will prevent graphic glitches on the
            //edges of the voxel blocks.
            int slotCount = texturePaths.Count;
            int slotsPerSideMin = (int)Math.Ceiling(Math.Sqrt(slotCount));
            int slotsPerSide = 2;
            while (slotsPerSide < slotsPerSideMin) slotsPerSide *= 2;

            //Create a dictionary where the texture file path is the key and
            //the location of the target slot (the texture clipping for the 
            //block which uses it) as value.
            Dictionary<FileSystemPath, Rectangle> textureTargetClippings =
                new Dictionary<FileSystemPath, Rectangle>();
            int currentSlot = 0;

            //Create a new bitmap and copy each referenced texture file into
            //the correct target location into the bitmap.
            int textureSizePx = TextureClippingSizePx * slotsPerSide;
            Bitmap textureBitmap = new Bitmap(textureSizePx, textureSizePx);

            using (Graphics textureCanvas = Graphics.FromImage(textureBitmap))
            {
                textureCanvas.InterpolationMode =
                    System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                textureCanvas.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.None;

                //Go through every file in the previously created set...
                foreach (FileSystemPath filePath in texturePaths)
                {
                    //...and calculate the correct slot row/column.
                    int row = currentSlot / slotsPerSide;
                    int column = currentSlot % slotsPerSide;

                    //That row/column is converted into the final clipping
                    //rectangle used by the blocks later and into the "Bitmap
                    //target" rectangle used to draw the texture into the
                    //correct position in the texture bitmap.
                    Rectangle clippingRectangle = new Rectangle(
                        (float)column / slotsPerSide,
                        1 - (float)(row + 1) / slotsPerSide,
                        1.0f / slotsPerSide, 1.0f / slotsPerSide);
                    var bitmapTargetRectangle = new System.Drawing.Rectangle(
                        (int)((float)column / slotsPerSide * textureSizePx),
                        (int)((float)row / slotsPerSide * textureSizePx),
                        TextureClippingSizePx, TextureClippingSizePx);

                    //Then, attempt to load the referenced file as bitmap.
                    //If that fails, skip the current texture and continue
                    //with the next one.
                    if (!TryImportBitmap(fileSystem, filePath,
                        out Bitmap singleTextureBitmap)) continue;

                    //If the bitmap was loaded successfully, draw it into 
                    //the block collection texture at the correct position...
                    textureCanvas.DrawImage(singleTextureBitmap, 
                        bitmapTargetRectangle);

                    //..and add an entry into the clippings dictionary.
                    textureTargetClippings.Add(filePath, clippingRectangle);

                    currentSlot++;
                    singleTextureBitmap.Dispose();
                }
            }

            //Convert the generated bitmap into a TextureData instance and 
            //put it into the associated "out" parameter.
            texture = new BitmapTextureData(textureBitmap);

            //Create a dictionary with the block key as key and the clippings
            //of the block sides textures. If a texture file couldn't be loaded
            //before and it's encountered again here, "null" is added into the
            //dictionary for that block key instead (so faulty blocks won't be
            //displayed at all but replaced by air or an error block later).
            textureClippings = 
                new Dictionary<BlockKey, AdjacentList<Rectangle>>();
            foreach (var blockTexture in textures)
            {
                try
                {
                    textureClippings.Add(blockTexture.Key,
                        blockTexture.Value.Convert(
                            path => textureTargetClippings[path]));
                }
                catch { continue; }
            }

#if VERBOSE_LOGGING
            //Log the time used to generate the texture - and done!
            Log.Trace("Block texture collection generated in " +
                (DateTime.Now - start).TotalMilliseconds + "ms.");
#endif

            return true;
        }

        public BlockRegistry GenerateRegistry(IFileSystem fileSystem)
        {
            Block[][] blocks = 
                new Block[blockKeyIdVariations.Keys.Max() + 1][];

            foreach (var idMaxVariation in blockKeyIdVariations)
                blocks[idMaxVariation.Key] = 
                    new Block[idMaxVariation.Value + 1];

            if (!GenerateRegistryTextureCollection(fileSystem, out var texture,
                out var blocksTextureClippings))
                Log.Trace("No block texture collection generated for block " +
                    "registry, as no textures were defined.");

            foreach (BlockDraft blockDraft in this.blocks.Values)
            {
                bool hasMesh = meshes.TryGetValue(blockDraft.Key, 
                    out AdjacentList<BlockMeshSegment> mesh);

                if (hasMesh && mesh.ContainsOnlyUndefinedElements())
                    Log.Warning("The mesh for the block with the key '" +
                        blockDraft.Key + "' was defined, but neither " +
                        "contained a base or any sides.");

                AdjacentList<Rectangle> blockTextureClippings = null;

                bool hasTexture = blocksTextureClippings?.TryGetValue(
                    blockDraft.Key, out blockTextureClippings) ?? false;

                //The block should only be added if:
                //a) There are no textures defined at all (for voxels that are 
                //   colored by their vertices)
                //b) There are textures defined, but not for the current block.
                //   Which would be a problem if the block has a mesh. But it
                //   doesn't. So the missing texture isn't a problem, as there
                //   is no mesh that needs textures anyways. This case actually
                //   is only relevant for non-visible blocks (like air).
                //c) There are textures and the clippings for the current block
                //   were successfully retrieved, just like its mesh. That 
                //   should be the normal case for most blocks actually.
                //If the block has a mesh and also would've had texture 
                //assignments, but these couldn't be loaded, clippingRetrieved 
                //is "false". In that case, the block should not be created 
                //and rendered as error block (or "air").
                if (blocksTextureClippings == null || 
                    (!hasMesh && !hasTexture) || (hasTexture && hasMesh))
                {
                    blocks[blockDraft.Key.Id][blockDraft.Key.Variation] =
                        new Block(blockDraft.Key, blockDraft.Identifier,
                        blockDraft.Properties, mesh, blockTextureClippings);
                }
            }

            if (ErrorBlockKey.HasValue)
            {
                try
                {
                    return new BlockRegistry(blocks, ErrorBlockKey, texture);
                }
                catch (ArgumentException)
                {
                    Log.Error("The specified error block key '" +
                        ErrorBlockKey + " doesn't exist in the block " +
                        "registry - reverting to default first block.");
                }
            }

            return new BlockRegistry(blocks, null, texture);
        }
    }
}
