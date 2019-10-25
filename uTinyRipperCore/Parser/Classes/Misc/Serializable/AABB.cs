using System.Collections.Generic;
using uTinyRipper.YAML;
using uTinyRipper.Converters;
using uTinyRipper.Game.Assembly;

namespace uTinyRipper.Classes
{
	public struct AABB : IAsset, ISerializableStructure
	{
		public AABB(Vector3f center, Vector3f extent)
		{
			Center = center;
			Extent = extent;
		}

		public void Read(AssetReader reader)
		{
			Center.Read(reader);
			Extent.Read(reader);
		}

		public void Write(AssetWriter writer)
		{
			Center.Write(writer);
			Extent.Write(writer);
		}

		public YAMLNode ExportYAML(IExportContainer container)
		{
			YAMLMappingNode node = new YAMLMappingNode();
			node.Add(CenterName, Center.ExportYAML(container));
			node.Add(ExtentName, Extent.ExportYAML(container));
			return node;
		}

		public IEnumerable<Object> FetchDependencies(ISerializedFile file, bool isLog = false)
		{
			yield break;
		}

		public ISerializableStructure CreateDuplicate()
		{
			return new AABB();
		}

		public override string ToString()
		{
			return $"C:{Center} E:{Extent}";
		}

		public const string CenterName = "m_Center";
		public const string ExtentName = "m_Extent";

		public Vector3f Center;
		public Vector3f Extent;
	}
}
