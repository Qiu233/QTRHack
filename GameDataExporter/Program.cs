using Microsoft.CSharp;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Implementation;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDataExporter
{
	class Program
	{
		static void WriteTypeInfo(string file, ClrType type)
		{
			StringBuilder sb = new StringBuilder();
			StringWriter sw = new StringWriter(sb);
			sw.WriteLine($"Type: {type.Name}");
			sw.Write($"\t");
			ClrType baseType = type.BaseType;
			while (baseType != null)
			{
				sw.Write($"->{baseType.Name}");
				baseType = baseType.BaseType;
			}
			sw.WriteLine();
			sw.WriteLine($"Methods:");
			type.Methods.ToList().ForEach(t =>
			{
				sw.WriteLine($"{t.Signature}");
			});
			sw.WriteLine();
			sw.WriteLine($"Fields:");
			var fields = type.Fields.ToList();
			fields.Sort((f1, f2) =>
			{
				int result = string.Compare(f1.Type.Name, f2.Type.Name);
				if (result == 0)
					result = string.Compare(f1.ContainingType.Name, f2.ContainingType.Name);
				if (result == 0)
					result = string.Compare(f1.Name, f2.Name);
				return result;
			});
			fields.ForEach(t =>
			{
				sw.WriteLine(string.Format("|Name: {0,-20}|Type: {1,-40}|From: {2}", t.Name, t.Type.Name, t.ContainingType));
			});
			sw.Close();
			File.WriteAllText(file, sb.ToString());
		}
		static void WriteTypeTT(string file, ClrType type)
		{
			StringBuilder sb = new StringBuilder();
			StringWriter sw = new StringWriter(sb);
			var fields = type.Fields.Where(t => t.ContainingType == type).ToList();
			fields.Sort((f1, f2) =>
			{
				int result = string.Compare(f1.Type.Name, f2.Type.Name);
				if (result == 0)
					result = string.Compare(f1.Name, f2.Name);
				return result;
			});
			fields.ForEach(t =>
			{
				string typeName;
#pragma warning disable CA1416
				using (var provider = new CSharpCodeProvider())
					typeName = provider.GetTypeOutput(new CodeTypeReference(t.Type.Name));
				sw.WriteLine(string.Format("\t\t<# PROPERTY_VIRTUAL(\"{0,-10}\", \"{1,-20}\"); #>", typeName, t.Name));
			});
			sw.Close();
			File.WriteAllText(file, sb.ToString());
		}
		static void WriteType(string file, ClrType type)
		{
			WriteTypeInfo(file + ".txt", type);
			WriteTypeTT(file + ".tt", type);
		}
		static void WriteTypes(ClrmdModule module)
		{
			foreach (var typeDef in module.EnumerateTypeDefToMethodTableMap())
			{
				ClrType type = module.ResolveToken(typeDef.Item2);
				string[] path = type.Name.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
				string cur = "./Types";
				bool flag = false;
				for (int i = 0; i < path.Length; i++)
				{
					string sec = path[i];
					if (!Directory.Exists(cur))
						Directory.CreateDirectory(cur);
					try
					{
						cur = Path.Combine(cur, sec);
					}
					catch
					{
						Console.WriteLine($"Skipped: {type.Name}");
						flag = true;
						break;
					}
				}
				if (!flag)
					WriteType(cur, type);
			}
		}
		static void Main(string[] args)
		{
			var id = Process.GetProcessesByName("Terraria")[0].Id;
			DataTarget dataTarget = DataTarget.AttachToProcess(id, false);
			ClrRuntime runtime = dataTarget.ClrVersions[0].CreateRuntime();
			ClrmdModule module = runtime.AppDomains[0].Modules.First(t => t.Name.EndsWith("Terraria.exe")) as ClrmdModule;
			WriteTypes(module);
		}
	}
}
