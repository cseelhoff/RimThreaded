using RimWorld;
using System;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    class ApparelGraphicRecordGetter_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(ApparelGraphicRecordGetter);
            Type patched = typeof(ApparelGraphicRecordGetter_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryGetGraphicApparel));
        }

        public static bool TryGetGraphicApparel(ref bool __result, Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                return true;
            }

            if (bodyType == null)
            {
                Log.Error("Getting apparel graphic with undefined body type.");
                bodyType = BodyTypeDefOf.Male;
            }
            if (apparel.WornGraphicPath.NullOrEmpty())
            {
                rec = new ApparelGraphicRecord(null, null);
                __result = false;
                return false;
            }
            string path = apparel.def.apparel.LastLayer != ApparelLayerDefOf.Overhead && apparel.def.apparel.LastLayer != ApparelLayerDefOf.EyeCover && !PawnRenderer.RenderAsPack(apparel) && !(apparel.WornGraphicPath == BaseContent.PlaceholderImagePath) && !(apparel.WornGraphicPath == BaseContent.PlaceholderGearImagePath) ? apparel.WornGraphicPath + "_" + bodyType.defName : apparel.WornGraphicPath;
            Shader shader = ShaderDatabase.Cutout;
            if (apparel.def.apparel.useWornGraphicMask)
            {
                shader = ShaderDatabase.CutoutComplex;
            }
            Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
            rec = new ApparelGraphicRecord(graphic, apparel);
            __result = true;
            return false;
            /*
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                return true;
            }
            Func<object[], object> FuncTryGetGraphicApparel = p => 
                ApparelGraphicRecordGetter.TryGetGraphicApparel((Apparel)p[0], (BodyTypeDef)p[1], out ((ApparelGraphicRecord)p[2]));
            threadInfo.safeFunctionRequest = new object[] { FuncTryGetGraphicApparel, new object[] { apparel, bodyType, rec } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (bool)threadInfo.safeFunctionResult;
            return false;
            */
        }

    }
}
