using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class CompAttachBase_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(CompAttachBase);
            Type patched = typeof(CompAttachBase_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CompTick));
            //RimThreadedHarmony.Prefix(original, patched, nameof(PostDestroy));
            RimThreadedHarmony.Prefix(original, patched, nameof(CompInspectStringExtra));
            RimThreadedHarmony.Prefix(original, patched, nameof(GetAttachment));
            RimThreadedHarmony.Prefix(original, patched, nameof(AddAttachment));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveAttachment));
        }
        public static bool CompTick(CompAttachBase __instance)
        {
            List<AttachableThing> attachments = __instance.attachments;
            if (attachments != null)
            {
                for (int i = 0; i < attachments.Count; i++)
                {
                    attachments[i].Position = __instance.parent.Position;
                }
            }
            return false;
        }

        public static bool PostDestroy(CompAttachBase __instance, DestroyMode mode, Map previousMap)
        {
            ThingComp thingComp = (ThingComp)__instance;
            thingComp.PostDestroy(mode, previousMap);
            List<AttachableThing> attachments = __instance.attachments;
            if (attachments != null)
            {
                for (int num = attachments.Count - 1; num >= 0; num--)
                {
                    attachments[num].Destroy();
                }
            }
            return false;
        }

        public static bool CompInspectStringExtra(CompAttachBase __instance, ref string __result)
        {
            List<AttachableThing> attachments = __instance.attachments;
            if (attachments != null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < attachments.Count; i++)
                {
                    stringBuilder.AppendLine(attachments[i].InspectStringAddon);
                }
                __result = stringBuilder.ToString().TrimEndNewlines();
                return false;
            }
            __result = null;
            return false;
        }
        public static bool GetAttachment(CompAttachBase __instance, ref Thing __result, ThingDef def)
        {
            List<AttachableThing> attachments = __instance.attachments;
            if (attachments != null)
            {
                for (int i = 0; i < attachments.Count; i++)
                {
                    if (attachments[i].def == def)
                    {
                        __result = attachments[i];
                        return false;
                    }
                }
            }
            __result = null;
            return false;
        }

        public static bool AddAttachment(CompAttachBase __instance, AttachableThing t)
        {
            lock (__instance)
            {
                if (__instance.attachments == null)
                {
                    __instance.attachments = new List<AttachableThing>();
                }
                __instance.attachments.Add(t);
            }
            return false;
        }
        public static bool RemoveAttachment(CompAttachBase __instance, AttachableThing t)
        {
            lock (__instance)
            {
                List<AttachableThing> attachments = new List<AttachableThing>(__instance.attachments);
                attachments.Remove(t);
                __instance.attachments = attachments;
            }
            return false;
        }


    }


}
