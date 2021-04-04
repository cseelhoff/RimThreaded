using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace RimThreaded
{

    public class LanguageWordInfo_Patch
	{
        public static AccessTools.FieldRef<LanguageWordInfo, Dictionary<string, Gender>> genders =
            AccessTools.FieldRefAccess<LanguageWordInfo, Dictionary<string, Gender>>("genders");
        public static bool TryResolveGender(LanguageWordInfo __instance, ref bool __result, string str, out Gender gender)
        {
            StringBuilder tmpLowercase = new StringBuilder();
            tmpLowercase.Length = 0;
            for (int index = 0; index < str.Length; ++index)
                tmpLowercase.Append(char.ToLower(str[index]));
            if (genders(__instance).TryGetValue(tmpLowercase.ToString(), out gender))
            {
                __result = true;
                return false;
            } 
            gender = Gender.Male;
            __result = false;
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(LanguageWordInfo);
            Type patched = typeof(LanguageWordInfo_Patch);
            RimThreadedHarmony.Prefix(original, patched, "TryResolveGender");
        }
    }
}
