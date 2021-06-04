using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;

namespace RimThreaded.Mod_Patches
{
    class ZombieLand_Patch
    {
		public static void Patch()
		{
			Type type = TypeByName("ZombieLand.ZombieStateHandler");
			if (type != null)
			{
				foreach (MethodInfo method in type.GetMethods())
				{
					if (method.IsDeclaredMember())
					{
						try
						{
							IEnumerable<KeyValuePair<OpCode, object>> f = PatchProcessor.ReadMethodBody(method);
							foreach (KeyValuePair<OpCode, object> e in f)
							{
								if (e.Value is FieldInfo fieldInfo && RimThreadedHarmony.replaceFields.ContainsKey(fieldInfo))
								{
									RimThreadedHarmony.TranspileFieldReplacements(method);
									break;
								}
								if (e.Value is MethodInfo methodInfo && RimThreadedHarmony.replaceFields.ContainsKey(methodInfo))
								{
									RimThreadedHarmony.TranspileFieldReplacements(method);
									break;
								}
							}
						}
						catch (NotSupportedException) { }
					}
				}
			}
				
		}
	}
}
