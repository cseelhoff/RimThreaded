using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

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

    }
}
