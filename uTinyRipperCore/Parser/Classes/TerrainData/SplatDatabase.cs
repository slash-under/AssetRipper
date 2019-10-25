using System.Collections.Generic;
using uTinyRipper.Project;
using uTinyRipper.YAML;
using uTinyRipper.Converters.TerrainDatas;
using uTinyRipper.Converters;

namespace uTinyRipper.Classes.TerrainDatas
{
	public struct SplatDatabase : IAsset, IDependent
	{
		public static int ToSerializedVersion(Version version)
		{
			// SplatPrototype is replaced by TerrainLayer
			if (version.IsGreaterEqual(2018, 3))
			{
				return 2;
			}
			return 1;
		}

		/// <summary>
		/// 2018.3 and greater
		/// </summary>
		public static bool HasTerrainLayers(Version version) => version.IsGreaterEqual(2018, 3);
		/// <summary>
		/// 5.0.1 to 2018.3 exclusive
		/// </summary>
		public static bool HasColorSpace(Version version) => version.IsGreaterEqual(5, 0, 1) && version.IsLess(2018, 3);

		public SplatDatabase Convert(IExportContainer container)
		{
			return SplatDatabaseConverter.Convert(container, ref this);
		}

		public void Read(AssetReader reader)
		{
			if (HasTerrainLayers(reader.Version))
			{
				TerrainLayers = reader.ReadAssetArray<PPtr<TerrainLayer>>();
			}
			else
			{
				Splats = reader.ReadAssetArray<SplatPrototype>();
			}

			AlphaTextures = reader.ReadAssetArray<PPtr<Texture2D>>();
			AlphamapResolution = reader.ReadInt32();
			BaseMapResolution = reader.ReadInt32();
			if (HasColorSpace(reader.Version))
			{
				ColorSpace = reader.ReadInt32();
				MaterialRequiresMetallic = reader.ReadBoolean();
				MaterialRequiresSmoothness = reader.ReadBoolean();
				reader.AlignStream(AlignType.Align4);
			}
		}

		public void Write(AssetWriter writer)
		{
			if (HasTerrainLayers(writer.Version))
			{
				writer.WriteAssetArray(TerrainLayers);
			}
			else
			{
				writer.WriteAssetArray(Splats);
			}

			writer.WriteAssetArray(AlphaTextures);
			writer.Write(AlphamapResolution);
			writer.Write(BaseMapResolution);
			if (HasColorSpace(writer.Version))
			{
				writer.Write(ColorSpace);
				writer.Write(MaterialRequiresMetallic);
				writer.Write(MaterialRequiresSmoothness);
				writer.AlignStream(AlignType.Align4);
			}
		}

		public YAMLNode ExportYAML(IExportContainer container)
		{
			YAMLMappingNode node = new YAMLMappingNode();
			node.AddSerializedVersion(ToSerializedVersion(container.ExportVersion));
			if (HasTerrainLayers(container.ExportVersion))
			{
				node.Add(TerrainLayersName, TerrainLayers.ExportYAML(container));
			}
			else
			{
				node.Add(SplatsName, Splats.ExportYAML(container));
			}

			node.Add(AlphaTexturesName, AlphaTextures.ExportYAML(container));
			node.Add(AlphamapResolutionName, AlphamapResolution);
			node.Add(BaseMapResolutionName, BaseMapResolution);
			if (HasColorSpace(container.ExportVersion))
			{
				node.Add(ColorSpaceName, ColorSpace);
				node.Add(MaterialRequiresMetallicName, MaterialRequiresMetallic);
				node.Add(MaterialRequiresSmoothnessName, MaterialRequiresSmoothness);
			}
			return node;
		}

		public IEnumerable<Object> FetchDependencies(ISerializedFile file, bool isLog = false)
		{
			if (HasTerrainLayers(file.Version))
			{
				foreach (PPtr<TerrainLayer> terrainLayer in TerrainLayers)
				{
					yield return terrainLayer.FetchDependency(file, isLog, () => nameof(TerrainLayer), TerrainLayersName);
				}
			}
			else
			{
				foreach (SplatPrototype prototype in Splats)
				{
					foreach (Object asset in prototype.FetchDependencies(file, isLog))
					{
						yield return asset;
					}
				}
			}

			foreach (PPtr<Texture2D> alphaTexture in AlphaTextures)
			{
				yield return alphaTexture.FetchDependency(file, isLog, () => nameof(SplatDatabase), AlphaTexturesName);
			}
		}

		public SplatPrototype[] Splats { get; set; }
		public PPtr<TerrainLayer>[] TerrainLayers { get; set; }
		public PPtr<Texture2D>[] AlphaTextures { get; set; }
		public int AlphamapResolution { get; set; }
		public int BaseMapResolution { get; set; }
		public int ColorSpace { get; set; }
		public bool MaterialRequiresMetallic { get; set; }
		public bool MaterialRequiresSmoothness { get; set; }

		public const string TerrainLayersName = "m_TerrainLayers";
		public const string SplatsName = "m_Splats";
		public const string AlphaTexturesName = "m_AlphaTextures";
		public const string AlphamapResolutionName = "m_AlphamapResolution";
		public const string BaseMapResolutionName = "m_BaseMapResolution";
		public const string ColorSpaceName = "m_ColorSpace";
		public const string MaterialRequiresMetallicName = "m_MaterialRequiresMetallic";
		public const string MaterialRequiresSmoothnessName = "m_MaterialRequiresSmoothness";
	}
}
