using System;
using Verse;

public class GenLabel_Patch
{
    public static bool ThingLabel(ref string __result, Thing t, int stackCount, bool includeHp = true)
    {
        /*
        LabelRequest key = default(LabelRequest);
        key.entDef = t.def;
        key.stuffDef = t.Stuff;
        key.stackCount = stackCount;
        t.TryGetQuality(out key.quality);
        if (t.def.useHitPoints && includeHp)
        {
            key.health = t.HitPoints;
            key.maxHealth = t.MaxHitPoints;
        }

        Apparel apparel = t as Apparel;
        if (apparel != null)
        {
            key.wornByCorpse = apparel.WornByCorpse;
        }

        if (!labelDictionary.TryGetValue(key, out string value))
        {
            if (labelDictionary.Count > 2000)
            {
                labelDictionary.Clear();
            }

            value = NewThingLabel(t, stackCount, includeHp);
            labelDictionary.Add(key, value);
        }
        __result = value;
        */
        return false;
        
    }
}
