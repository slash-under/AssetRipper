﻿using System;
using System.Collections.Generic;
using System.IO;

namespace UtinyRipper.Exporters.Scripts
{
	public abstract class ScriptExportField
	{
		public abstract void Init(IScriptExportManager manager);

		public void Export(TextWriter writer, int intent)
		{
			if (Attribute != null)
			{
				Attribute.Export(writer, intent);
			}

			writer.WriteIntent(intent);
			writer.Write("{0} ", Keyword);
			if(IsNew)
			{
				writer.Write("new ");
			}
			writer.WriteLine("{0} {1};", Type.Name, Name);
		}

		public void ExportEnum(TextWriter writer, int intent)
		{
			if (Type.IsEnum)
			{
				writer.WriteIntent(intent);
				writer.WriteLine("{0} = {1},", Name, Constant);
			}
			else
			{
				throw new NotSupportedException();
			}
		}
		
		public void GetUsedNamespaces(ICollection<string> namespaces)
		{
			Type.GetTypeNamespaces(namespaces);
			if(Attribute != null)
			{
				Attribute.GetUsedNamespaces(namespaces);
			}
		}

		public abstract ScriptExportType Type { get; }
		public abstract ScriptExportAttribute Attribute { get; }

		protected abstract string Keyword { get; }
		protected abstract bool IsNew { get; }
		protected abstract string Name { get; }
		protected abstract string Constant { get; }

		protected const string PublicKeyWord = "public";
		protected const string InternalKeyWord = "internal";
		protected const string ProtectedKeyWord = "protected";
		protected const string PrivateKeyWord = "private";
	}
}