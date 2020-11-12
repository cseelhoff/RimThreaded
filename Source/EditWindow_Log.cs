using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static HarmonyLib.AccessTools;
namespace RimThreaded
{

    public class EditWindow_Log_Patch
    {
        /*
        public static FieldRef<EditWindow_Log, float> listingViewHeight =
            FieldRefAccess<EditWindow_Log, float>("listingViewHeight");
        public static FieldRef<EditWindow_Log, bool> borderDragging =
            FieldRefAccess<EditWindow_Log, bool>("borderDragging");

        public static Vector2 messagesScrollPosition =
            StaticFieldRefAccess<Vector2>(typeof(EditWindow_Log), "messagesScrollPosition");
        public static Vector2 detailsScrollPosition =
            StaticFieldRefAccess<Vector2>(typeof(EditWindow_Log), "detailsScrollPosition");

        public static float detailsPaneHeight =
            StaticFieldRefAccess<float>(typeof(EditWindow_Log), "detailsPaneHeight");

        public static bool canAutoOpen =
            StaticFieldRefAccess<bool>(typeof(EditWindow_Log), "canAutoOpen");

        public static LogMessage selectedMessage =
            StaticFieldRefAccess<LogMessage>(typeof(EditWindow_Log), "selectedMessage");
        public static Texture2D SelectedMessageTex =
            StaticFieldRefAccess<Texture2D>(typeof(EditWindow_Log), "SelectedMessageTex");
        public static Texture2D AltMessageTex =
            StaticFieldRefAccess<Texture2D>(typeof(EditWindow_Log), "AltMessageTex");
        public static Texture2D StackTraceBorderTex =
            StaticFieldRefAccess<Texture2D>(typeof(EditWindow_Log), "StackTraceBorderTex");
        public static Texture2D StackTraceAreaTex =
            StaticFieldRefAccess<Texture2D>(typeof(EditWindow_Log), "StackTraceAreaTex");
        public static string MessageDetailsControlName =
            StaticFieldRefAccess<string>(typeof(EditWindow_Log), "MessageDetailsControlName");

        private static void CopyAllMessagesToClipboard()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (LogMessage message in Log.Messages)
            {
                if (stringBuilder.Length != 0)
                {
                    stringBuilder.AppendLine();
                }

                stringBuilder.AppendLine(message.text);
                stringBuilder.Append(message.StackTrace);
                if (stringBuilder[stringBuilder.Length - 1] != '\n')
                {
                    stringBuilder.AppendLine();
                }
            }

            GUIUtility.systemCopyBuffer = stringBuilder.ToString();
        }

        public static bool DoWindowContents(EditWindow_Log __instance, Rect inRect)
        {
            Text.Font = GameFont.Tiny;
            WidgetRow widgetRow = new WidgetRow(0f, 0f);
            if (widgetRow.ButtonText("Clear", "Clear all log messages."))
            {
                Log.Clear();
                EditWindow_Log.ClearAll();
            }

            if (widgetRow.ButtonText("Trace big", "Set the stack trace to be large on screen."))
            {
                detailsPaneHeight = 700f;
            }

            if (widgetRow.ButtonText("Trace medium", "Set the stack trace to be medium-sized on screen."))
            {
                detailsPaneHeight = 300f;
            }

            if (widgetRow.ButtonText("Trace small", "Set the stack trace to be small on screen."))
            {
                detailsPaneHeight = 100f;
            }

            if (canAutoOpen)
            {
                if (widgetRow.ButtonText("Auto-open is ON", ""))
                {
                    canAutoOpen = false;
                }
            }
            else if (widgetRow.ButtonText("Auto-open is OFF", ""))
            {
                canAutoOpen = true;
            }

            if (widgetRow.ButtonText("Copy to clipboard", "Copy all messages to the clipboard."))
            {
                CopyAllMessagesToClipboard();
            }

            Text.Font = GameFont.Small;
            Rect rect = new Rect(inRect);
            rect.yMin += 26f;
            rect.yMax = inRect.height;
            if (selectedMessage != null)
            {
                rect.yMax -= detailsPaneHeight;
            }

            Rect detailsRect = new Rect(inRect);
            detailsRect.yMin = rect.yMax;
            DoMessagesListing(__instance, rect);
            DoMessageDetails2(__instance, detailsRect, inRect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rect))
            {
                EditWindow_Log.ClearSelectedMessage();
            }

            detailsPaneHeight = Mathf.Max(detailsPaneHeight, 10f);
            detailsPaneHeight = Mathf.Min(detailsPaneHeight, inRect.height - 80f);
            return false;
        }
        private static void DoMessageDetails2(EditWindow_Log __instance, Rect detailsRect, Rect outRect)
        {
            if (selectedMessage != null)
            {
                Rect rect = detailsRect;
                rect.height = 7f;
                Rect rect2 = detailsRect;
                rect2.yMin = rect.yMax;
                GUI.DrawTexture(rect, StackTraceBorderTex);
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }

                if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect))
                {
                    borderDragging(__instance) = true;
                    Event.current.Use();
                }

                if (borderDragging(__instance))
                {
                    detailsPaneHeight = outRect.height + Mathf.Round(3.5f) - Event.current.mousePosition.y;
                }

                if (Event.current.rawType == EventType.MouseUp)
                {
                    borderDragging(__instance) = false;
                }

                GUI.DrawTexture(rect2, StackTraceAreaTex);
                string text = selectedMessage.text + "\n" + selectedMessage.StackTrace;
                GUI.SetNextControlName(MessageDetailsControlName);
                if (text.Length > 15000)
                {
                    Widgets.LabelScrollable(rect2, text, ref detailsScrollPosition, dontConsumeScrollEventsIfNoScrollbar: false, takeScrollbarSpaceEvenIfNoScrollbar: true, longLabel: true);
                }
                else
                {
                    Widgets.TextAreaScrollable(rect2, text, ref detailsScrollPosition, readOnly: true);
                }
            }
        }


        private static bool DoMessagesListing(EditWindow_Log __instance, Rect listingRect)
        {
            Rect viewRect = new Rect(0f, 0f, listingRect.width - 16f, listingViewHeight(__instance) + 100f);
            Widgets.BeginScrollView(listingRect, ref messagesScrollPosition, viewRect);
            float width = viewRect.width - 28f;
            Text.Font = GameFont.Tiny;
            float num = 0f;
            bool flag = false;
            List<LogMessage> messages;
            lock (Log.Messages)
            {
                messages = Log.Messages.ToList();
            }

            foreach (LogMessage message in messages)
            {
                string text = message.text;
                if (text.Length > 1000)
                {
                    text = text.Substring(0, 1000);
                }

                float num2 = Math.Min(30f, Text.CalcHeight(text, width));
                GUI.color = new Color(1f, 1f, 1f, 0.7f);
                Widgets.Label(new Rect(4f, num, 28f, num2), message.repeats.ToStringCached());
                Rect rect = new Rect(28f, num, width, num2);
                if (selectedMessage == message)
                {
                    GUI.DrawTexture(rect, SelectedMessageTex);
                }
                else if (flag)
                {
                    GUI.DrawTexture(rect, AltMessageTex);
                }

                if (Widgets.ButtonInvisible(rect))
                {
                    EditWindow_Log.ClearSelectedMessage();
                    selectedMessage = message;
                }

                GUI.color = message.Color;
                Widgets.Label(rect, text);
                num += num2;
                flag = !flag;
            }

            if (Event.current.type == EventType.Layout)
            {
                listingViewHeight(__instance) = num;
            }

            Widgets.EndScrollView();
            GUI.color = Color.white;
            return false;
        }

        */
    }
}
