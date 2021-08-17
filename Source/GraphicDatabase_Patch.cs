using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreadedHarmony;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class GraphicDatabase_Patch
	{
		public static void RunNonDestructivePatches()
		{
			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string) }, 
				new Type[] { typeof(Graphic_Single) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string) }, 
				new Type[] { typeof(Graphic_Single) }));

			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader) },
				new Type[] { typeof(Graphic_Single) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader) },
				new Type[] { typeof(Graphic_Single) }));
			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader) },
				new Type[] { typeof(Graphic_Multi) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader) },
				new Type[] { typeof(Graphic_Multi) }));

			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
				new Type[] { typeof(Graphic_Single) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
				new Type[] { typeof(Graphic_Single) }));
			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
				new Type[] { typeof(Graphic_Multi) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
				new Type[] { typeof(Graphic_Multi) }));
			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
				new Type[] { typeof(Graphic_Flicker) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
				new Type[] { typeof(Graphic_Flicker) }));

			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(int) },
				new Type[] { typeof(Graphic_Terrain) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(int) },
				new Type[] { typeof(Graphic_Terrain) }));

			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_Single) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_Single) }));
			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_Appearances) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_Appearances) }));
			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_Multi) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_Multi) }));
			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_StackCount) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_StackCount) }));
			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_Random) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(string) },
				new Type[] { typeof(Graphic_Random) }));

			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(Type), typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(string) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(Type), typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(string) }));

			replaceFields.Add(Method(typeof(GraphicDatabase), "Get", new Type[] { typeof(Type), typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(List<ShaderParameter>), typeof(string) }),
				Method(typeof(GraphicDatabase_Patch), "Get", new Type[] { typeof(Type), typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color), typeof(GraphicData), typeof(List<ShaderParameter>), typeof(string) }));

		}

		public static Graphic Get<T>(string path) where T : Graphic, new()
		{
			return GetInner<T>(new GraphicRequest(typeof(T), path, ShaderDatabase.Cutout, Vector2.one, Color.white, Color.white, null, 0, null, null));
		}

		public static Graphic Get<T>(string path, Shader shader) where T : Graphic, new()
		{
			return GetInner<T>(new GraphicRequest(typeof(T), path, shader, Vector2.one, Color.white, Color.white, null, 0, null, null));
		}

		public static Graphic Get<T>(string path, Shader shader, Vector2 drawSize, Color color) where T : Graphic, new()
		{
			return GetInner<T>(new GraphicRequest(typeof(T), path, shader, drawSize, color, Color.white, null, 0, null, null));
		}

		public static Graphic Get<T>(string path, Shader shader, Vector2 drawSize, Color color, int renderQueue) where T : Graphic, new()
		{
			return GetInner<T>(new GraphicRequest(typeof(T), path, shader, drawSize, color, Color.white, null, renderQueue, null, null));
		}

		public static Graphic Get<T>(string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo) where T : Graphic, new()
		{
			return GetInner<T>(new GraphicRequest(typeof(T), path, shader, drawSize, color, colorTwo, null, 0, null, null));
		}

		public static Graphic Get<T>(string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, GraphicData data, string maskPath = null) where T : Graphic, new()
		{
			return GetInner<T>(new GraphicRequest(typeof(T), path, shader, drawSize, color, colorTwo, data, 0, null, maskPath));
		}

		public static Graphic Get(Type graphicClass, string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, string maskPath = null)
		{
			return Get(graphicClass, path, shader, drawSize, color, colorTwo, null, null, maskPath);
		}

		public static Graphic Get(Type graphicClass, string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, GraphicData data, List<ShaderParameter> shaderParameters, string maskPath = null)
		{
			GraphicRequest graphicRequest = new GraphicRequest(graphicClass, path, shader, drawSize, color, colorTwo, data, 0, shaderParameters, maskPath);
			if (graphicRequest.graphicClass == typeof(Graphic_Single))
			{
				return GetInner<Graphic_Single>(graphicRequest);
			}
			if (graphicRequest.graphicClass == typeof(Graphic_Terrain))
			{
				return GetInner<Graphic_Terrain>(graphicRequest);
			}
			if (graphicRequest.graphicClass == typeof(Graphic_Multi))
			{
				return GetInner<Graphic_Multi>(graphicRequest);
			}
			if (graphicRequest.graphicClass == typeof(Graphic_Mote))
			{
				return GetInner<Graphic_Mote>(graphicRequest);
			}
			if (graphicRequest.graphicClass == typeof(Graphic_Random))
			{
				return GetInner<Graphic_Random>(graphicRequest);
			}
			if (graphicRequest.graphicClass == typeof(Graphic_Flicker))
			{
				return GetInner<Graphic_Flicker>(graphicRequest);
			}
			if (graphicRequest.graphicClass == typeof(Graphic_Appearances))
			{
				return GetInner<Graphic_Appearances>(graphicRequest);
			}
			if (graphicRequest.graphicClass == typeof(Graphic_StackCount))
			{
				return GetInner<Graphic_StackCount>(graphicRequest);
			}
			try
			{
				return (Graphic)GenGeneric.InvokeStaticGenericMethod(typeof(GraphicDatabase), graphicRequest.graphicClass, "GetInner", graphicRequest);
			}
			catch (Exception ex)
			{
				Log.Error(string.Concat("Exception getting ", graphicClass, " at ", path, ": ", ex.ToString()));
			}
			return BaseContent.BadGraphic;
		}


		public static T GetInner<T>(GraphicRequest req) where T : Graphic, new()
		{
			req.color = (Color32)req.color;
			req.colorTwo = (Color32)req.colorTwo;
			req.renderQueue = ((req.renderQueue == 0 && req.graphicData != null) ? req.graphicData.renderQueue : req.renderQueue);
			if (!GraphicDatabase.allGraphics.TryGetValue(req, out var value))
			{
				lock (GraphicDatabase.allGraphics)
				{
					if (!GraphicDatabase.allGraphics.TryGetValue(req, out var value2))
					{
						value = new T();
						value.Init(req);
						GraphicDatabase.allGraphics.Add(req, value);
					}
					else
					{
						value = value2;
					}
				}
			}
			return (T)value;
		}
	}
}
