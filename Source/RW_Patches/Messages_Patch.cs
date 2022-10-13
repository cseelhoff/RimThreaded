using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimThreaded.RW_Patches
{
    class Messages_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Messages);
            Type patched = typeof(Messages_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Update));
            RimThreadedHarmony.Prefix(original, patched, nameof(Message), new Type[] { typeof(Message), typeof(bool) });
            RimThreadedHarmony.Prefix(original, patched, nameof(MessagesDoGUI));
            RimThreadedHarmony.Prefix(original, patched, nameof(CollidesWithAnyMessage));
            RimThreadedHarmony.Prefix(original, patched, nameof(Clear));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_LoadedLevelChanged));
            RimThreadedHarmony.Prefix(original, patched, nameof(AcceptsMessage));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_Mouseover));
        }
        public static bool Update()
        {
            lock (Messages.liveMessages)
            {
                if (Current.ProgramState == ProgramState.Playing && Messages.mouseoverMessageIndex >= 0 && Messages.mouseoverMessageIndex < Messages.liveMessages.Count)
                    Messages.liveMessages[Messages.mouseoverMessageIndex].lookTargets.TryHighlight();
                Messages.mouseoverMessageIndex = -1;
                Messages.liveMessages.RemoveAll(m => m.Expired);
            }
            return false;
        }
        public static bool Message(Message msg, bool historical = true)
        {
            lock (Messages.liveMessages)
            {
                if (!Messages.AcceptsMessage(msg.text, msg.lookTargets))
                    return false;
                if (historical && Find.Archive != null)
                    Find.Archive.Add(msg);
                Messages.liveMessages.Add(msg);
                while (Messages.liveMessages.Count > 12)
                    Messages.liveMessages.RemoveAt(0);
                if (msg.def.sound == null)
                    return false;
                msg.def.sound.PlayOneShotOnCamera();
            }
            return false;
        }
        public static bool MessagesDoGUI()
        {
            Text.Font = GameFont.Small;
            int x = (int)Messages.MessagesTopLeftStandard.x;
            int y = (int)Messages.MessagesTopLeftStandard.y;
            if (Current.Game != null && Find.ActiveLesson.ActiveLessonVisible)
                y += (int)Find.ActiveLesson.Current.MessagesYOffset;
            lock (Messages.liveMessages)
            {
                for (int index = Messages.liveMessages.Count - 1; index >= 0; --index)
                {
                    Messages.liveMessages[index].Draw(x, y);
                    y += 26;
                }
            }
            return false;
        }
        public static bool CollidesWithAnyMessage(ref bool __result, Rect rect, out float messageAlpha)
        {
            bool flag = false;
            float a = 0.0f;
            lock (Messages.liveMessages)
            {
                for (int index = 0; index < Messages.liveMessages.Count; ++index)
                {
                    Message liveMessage = Messages.liveMessages[index];
                    if (rect.Overlaps(liveMessage.lastDrawRect))
                    {
                        flag = true;
                        a = Mathf.Max(a, liveMessage.Alpha);
                    }
                }
            }
            messageAlpha = a;
            return flag;
        }
        public static bool Clear()
        {
            lock (Messages.liveMessages)
            {
                Messages.liveMessages.Clear();
            }
            return false;
        }
        public static bool Notify_LoadedLevelChanged()
        {
            lock (Messages.liveMessages)
            {
                for (int index = 0; index < Messages.liveMessages.Count; ++index)
                    Messages.liveMessages[index].lookTargets = null;
            }
            return false;
        }

        public static bool AcceptsMessage(ref bool __result, string text, LookTargets lookTargets)
        {
            if (text.NullOrEmpty())
            {
                __result = false;
                return false;
            }
            lock (Messages.liveMessages)
            {
                for (int index = 0; index < Messages.liveMessages.Count; ++index)
                {
                    if (Messages.liveMessages[index].text == text && Messages.liveMessages[index].startingFrame == RealTime.frameCount && LookTargets.SameTargets(Messages.liveMessages[index].lookTargets, lookTargets))
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            __result = true;
            return false;
        }

        public static bool Notify_Mouseover(Message msg)
        {
            lock (Messages.liveMessages)
            {
                Messages.mouseoverMessageIndex = Messages.liveMessages.IndexOf(msg);
            }
            return false;
        }
    }

}
