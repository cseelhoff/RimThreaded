using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimThreaded
{
    public class FloatMenuMakerMap_Transpile
    {
        public static IEnumerable<CodeInstruction> AddHumanlikeOrders(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* Original FloatMenuMakerMap.AddHumanlikeOrders
            ---ORIGINAL---
	        // Apparel apparel = pawn.Map.thingGrid.ThingAt<Apparel>(c);
	        IL_1962: ldloc.s 59	
	        --IL_1964: ldloc.s 59
	        IL_1966: ldfld class RimWorld.FloatMenuMakerMap/'<>c__DisplayClass8_0' RimWorld.FloatMenuMakerMap/'<>c__DisplayClass8_14'::'CS$<>8__locals13'
	        IL_196b: ldfld class Verse.Pawn RimWorld.FloatMenuMakerMap/'<>c__DisplayClass8_0'::pawn
	        IL_1970: callvirt instance class Verse.Map Verse.Thing::get_Map()
	        IL_1975: ldfld class Verse.ThingGrid Verse.Map::thingGrid
	        IL_197a: ldloc.1
           *-IL_197b: callvirt instance !!0 Verse.ThingGrid::ThingAt<class RimWorld.Apparel>(valuetype Verse.IntVec3)		
	        IL_1980: stfld class RimWorld.Apparel RimWorld.FloatMenuMakerMap/'<>c__DisplayClass8_14'::apparel	
	        // if (apparel != null)
	        IL_1985: ldloc.s 59
	        IL_1987: ldfld class RimWorld.Apparel RimWorld.FloatMenuMakerMap/'<>c__DisplayClass8_14'::apparel
	        IL_198c: brfalse IL_1c8b

            ---REPLACE WITH---
	        // Thing thing2 = pawn.Map.thingGrid.ThingAt(c, ThingCategory.Item);
	        // sequence point: (line 434, col 17) to (line 434, col 90) in C:\Steam\steamapps\common\RimWorld\Mods\RimThreaded\Source\FloatMenuMakerMap.cs
	        IL_186c: ldloc.s 56
	        --ldloc.s 59
	        IL_186e: ldfld class RimThreaded.FloatMenuMakerMap_Patch/'<>c__DisplayClass0_0' RimThreaded.FloatMenuMakerMap_Patch/'<>c__DisplayClass0_11'::'CS$<>8__locals10'
	        IL_1873: ldfld class ['Assembly-CSharp']Verse.Pawn RimThreaded.FloatMenuMakerMap_Patch/'<>c__DisplayClass0_0'::pawn
	        IL_1878: callvirt instance class ['Assembly-CSharp']Verse.Map ['Assembly-CSharp']Verse.Thing::get_Map()
	        IL_187d: ldfld class ['Assembly-CSharp']Verse.ThingGrid ['Assembly-CSharp']Verse.Map::thingGrid
	        IL_1882: ldloc.1
	        +IL_1883: ldc.i4.2
	        +IL_1884: callvirt instance class ['Assembly-CSharp']Verse.Thing ['Assembly-CSharp']Verse.ThingGrid::ThingAt(valuetype ['Assembly-CSharp']Verse.IntVec3, valuetype ['Assembly-CSharp']Verse.ThingCategory)
	        +IL_1889: stloc.s 57 (new Thing)
	        // Apparel apparel = thing2 as Apparel;
	        +IL_188b: ldloc.s 56 (59- apparel)
	        +IL_188d: ldloc.s 57 (new Thing)
	        +IL_188f: isinst ['Assembly-CSharp']RimWorld.Apparel
	
	        IL_1894: stfld class ['Assembly-CSharp']RimWorld.Apparel RimThreaded.FloatMenuMakerMap_Patch/'<>c__DisplayClass0_11'::apparel
	        // if (apparel != null)
	        IL_1899: ldloc.s 56
	        IL_189b: ldfld class ['Assembly-CSharp']RimWorld.Apparel RimThreaded.FloatMenuMakerMap_Patch/'<>c__DisplayClass0_11'::apparel
	        IL_18a0: brfalse IL_1ba1
            */
            LocalBuilder thingAt = iLGenerator.DeclareLocal(typeof(Thing));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    i+6 < instructionsList.Count && instructionsList[i + 6].opcode == OpCodes.Callvirt &&
                    instructionsList[i + 6].operand.ToString().Equals("RimWorld.Apparel ThingAt[Apparel](Verse.IntVec3)")
                    )
                {
                    yield return instructionsList[i + 1];
                    yield return instructionsList[i + 2];
                    yield return instructionsList[i + 3];
                    yield return instructionsList[i + 4];
                    yield return instructionsList[i + 5];
                    yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                    instructionsList[i + 6].operand = AccessTools.Method(typeof(ThingGrid), "ThingAt", new Type[] { typeof(IntVec3), typeof(ThingCategory) });
                    yield return instructionsList[i + 6];
                    yield return new CodeInstruction(OpCodes.Stloc_S, thingAt);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 59); //not a good way to reference apparel
                    yield return new CodeInstruction(OpCodes.Ldloc_S, thingAt);
                    yield return new CodeInstruction(OpCodes.Isinst, typeof(Apparel));
                    i += 6;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
        }

    }
}
