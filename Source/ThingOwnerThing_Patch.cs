using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection.Emit;
using System.Linq;
using System;
using System.Reflection;

namespace RimThreaded
{
    public class ThingOwnerThing_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(ThingOwner<Thing>); 
            Type patched = typeof(ThingOwnerThing_Patch);
            RimThreadedHarmony.TranspileLockAdd3(original, "TryAdd", new Type[] { typeof(Thing), typeof(bool) });
            RimThreadedHarmony.Transpile(original, patched, nameof(Remove));
        }
        /*
        public void ThingOwnerTick(bool removeIfDestroyed = true)
        {
            for (int index = this.Count - 1; index >= 0; --index)
            {
                Thing at = this.GetAt(index);
                if (at.def.tickerType == TickerType.Normal)
                {
                    at.Tick();
                    if (at.Destroyed & removeIfDestroyed)
                        this.Remove(at);
                }
            }
        }

        public void ThingOwnerTickRare(bool removeIfDestroyed = true)
        {
            for (int index = this.Count - 1; index >= 0; --index)
            {
                Thing at = this.GetAt(index);
                if (at.def.tickerType == TickerType.Rare)
                {
                    at.TickRare();
                    if (at.Destroyed & removeIfDestroyed)
                        this.Remove(at);
                }
            }
        }

        public void ThingOwnerTickLong(bool removeIfDestroyed = true)
        {
            for (int index = this.Count - 1; index >= 0; --index)
            {
                Thing at = this.GetAt(index);
                if (at.def.tickerType == TickerType.Long)
                {
                    at.TickRare();
                    if (at.Destroyed & removeIfDestroyed)
                        this.Remove(at);
                }
            }
        }

        public void Clear()
        {
            for (int index = this.Count - 1; index >= 0; --index)
                this.Remove(this.GetAt(index));
        }

        public void ClearAndDestroyContents(DestroyMode mode = DestroyMode.Vanish)
        {
            while (this.Any)
            {
                for (int index = this.Count - 1; index >= 0; --index)
                {
                    Thing at = this.GetAt(index);
                    at.Destroy(mode);
                    this.Remove(at);
                }
            }
        }

        public void ClearAndDestroyContentsOrPassToWorld(DestroyMode mode = DestroyMode.Vanish)
        {
            while (this.Any)
            {
                for (int index = this.Count - 1; index >= 0; --index)
                {
                    Thing at = this.GetAt(index);
                    at.DestroyOrPassToWorld(mode);
                    this.Remove(at);
                }
            }
        }
        public int RemoveAll(Predicate<Thing> predicate)
        {
            int num = 0;
            for (int index = this.Count - 1; index >= 0; --index)
            {
                if (predicate(this.GetAt(index)))
                {
                    this.Remove(this.GetAt(index));
                    ++num;
                }
            }
            return num;
        }

        public int TryTransferToContainer(
          Thing item,
          ThingOwner otherContainer,
          int count,
          out Thing resultingTransferredItem,
          bool canMergeWithExistingStacks = true)
        {
            if (!this.Contains(item))
            {
                Log.Error("Can't transfer item " + (object)item + " because it's not here. owner=" + this.owner.ToStringSafe<IThingHolder>());
                resultingTransferredItem = (Thing)null;
                return 0;
            }
            if (otherContainer == this && count > 0)
            {
                resultingTransferredItem = item;
                return item.stackCount;
            }
            if (!otherContainer.CanAcceptAnyOf(item, canMergeWithExistingStacks))
            {
                resultingTransferredItem = (Thing)null;
                return 0;
            }
            if (count <= 0)
            {
                resultingTransferredItem = (Thing)null;
                return 0;
            }
            if (this.owner is Map || otherContainer.owner is Map)
            {
                Log.Warning("Can't transfer items to or from Maps directly. They must be spawned or despawned manually. Use TryAdd(item.SplitOff(count))");
                resultingTransferredItem = (Thing)null;
                return 0;
            }
            int count1 = Mathf.Min(item.stackCount, count);
            Thing other = item.SplitOff(count1);
            if (this.Contains(other))
                this.Remove(other);
            if (otherContainer.TryAdd(other, canMergeWithExistingStacks))
            {
                resultingTransferredItem = other;
                return other.stackCount;
            }
            resultingTransferredItem = (Thing)null;
            if (otherContainer.Contains(other) || other.stackCount <= 0 || other.Destroyed)
                return other.stackCount;
            int num = count1 - other.stackCount;
            if (item != other)
            {
                item.TryAbsorbStack(other, false);
                return num;
            }
            this.TryAdd(other, false);
            return num;
        }

        public Thing Take(Thing thing, int count)
        {
            if (!this.Contains(thing))
            {
                Log.Error("Tried to take " + thing.ToStringSafe<Thing>() + " but it's not here.");
                return (Thing)null;
            }
            if (count > thing.stackCount)
            {
                Log.Error("Tried to get " + (object)count + " of " + thing.ToStringSafe<Thing>() + " while only having " + (object)thing.stackCount);
                count = thing.stackCount;
            }
            if (count == thing.stackCount)
            {
                this.Remove(thing);
                return thing;
            }
            Thing thing1 = thing.SplitOff(count);
            thing1.holdingOwner = (ThingOwner)null;
            return thing1;
        }

        public bool TryDrop(
          Thing thing,
          IntVec3 dropLoc,
          Map map,
          ThingPlaceMode mode,
          int count,
          out Thing resultingThing,
          Action<Thing, int> placedAction = null,
          Predicate<IntVec3> nearPlaceValidator = null)
        {
            if (!this.Contains(thing))
            {
                Log.Error("Tried to drop " + thing.ToStringSafe<Thing>() + " but it's not here.");
                resultingThing = (Thing)null;
                return false;
            }
            if (thing.stackCount < count)
            {
                Log.Error("Tried to drop " + (object)count + " of " + (object)thing + " while only having " + (object)thing.stackCount);
                count = thing.stackCount;
            }
            if (count == thing.stackCount)
            {
                if (!GenDrop.TryDropSpawn(thing, dropLoc, map, mode, out resultingThing, placedAction, nearPlaceValidator))
                    return false;
                this.Remove(thing);
                return true;
            }
            Thing thing1 = thing.SplitOff(count);
            if (GenDrop.TryDropSpawn(thing1, dropLoc, map, mode, out resultingThing, placedAction, nearPlaceValidator))
                return true;
            thing.TryAbsorbStack(thing1, false);
            return false;
        }

        public bool TryDrop(
          Thing thing,
          ThingPlaceMode mode,
          out Thing lastResultingThing,
          Action<Thing, int> placedAction = null,
          Predicate<IntVec3> nearPlaceValidator = null)
        {
            Map rootMap = ThingOwnerUtility.GetRootMap(this.owner);
            IntVec3 rootPosition = ThingOwnerUtility.GetRootPosition(this.owner);
            if (rootMap != null && rootPosition.IsValid)
                return this.TryDrop(thing, rootPosition, rootMap, mode, out lastResultingThing, placedAction, nearPlaceValidator);
            Log.Error("Cannot drop " + (object)thing + " without a dropLoc and with an owner whose map is null.");
            lastResultingThing = (Thing)null;
            return false;
        }

        public bool TryDrop(
          Thing thing,
          IntVec3 dropLoc,
          Map map,
          ThingPlaceMode mode,
          out Thing lastResultingThing,
          Action<Thing, int> placedAction = null,
          Predicate<IntVec3> nearPlaceValidator = null,
          bool playDropSound = true)
        {
            if (!this.Contains(thing))
            {
                Log.Error(this.owner.ToStringSafe<IThingHolder>() + " container tried to drop  " + thing.ToStringSafe<Thing>() + " which it didn't contain.");
                lastResultingThing = (Thing)null;
                return false;
            }
            if (!GenDrop.TryDropSpawn(thing, dropLoc, map, mode, out lastResultingThing, placedAction, nearPlaceValidator, playDropSound))
                return false;
            this.Remove(thing);
            return true;
        }

        public void Notify_ContainedItemDestroyed(Thing t)
        {
            if (!ThingOwnerUtility.ShouldAutoRemoveDestroyedThings(this.owner))
                return;
            this.Remove(t);
        }
        */
        public static IEnumerable<CodeInstruction> Remove(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				if (i + 9 < instructionsList.Count && instructionsList[i + 9].opcode == OpCodes.Callvirt)
				{
					if (instructionsList[i + 9].operand is MethodInfo methodInfo)
					{
						if (methodInfo.Name.Contains("RemoveAt") && methodInfo.DeclaringType.FullName.Contains("System.Collections"))
						{
							LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
							LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
							List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>()
							{
								new CodeInstruction(OpCodes.Ldarg_0)
							};
							foreach (CodeInstruction lockInstruction in RimThreadedHarmony.EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
							{
								yield return lockInstruction;
							}
							Type collectionType = methodInfo.DeclaringType;
							LocalBuilder collectionCopy = iLGenerator.DeclareLocal(collectionType);
							ConstructorInfo constructorInfo = collectionType.GetConstructor(new Type[] { collectionType });
							List<CodeInstruction> storeReplay = new List<CodeInstruction>();
							storeReplay.Add(instructionsList[i]);
							yield return instructionsList[i++]; //this
							storeReplay.Add(instructionsList[i]);
							yield return instructionsList[i++]; //load field
							yield return new CodeInstruction(OpCodes.Newobj, constructorInfo);
							yield return new CodeInstruction(OpCodes.Stloc, collectionCopy.LocalIndex);
							yield return new CodeInstruction(OpCodes.Ldloc, collectionCopy.LocalIndex);
							yield return instructionsList[i++]; //load item
							yield return instructionsList[i++]; //unbox
							yield return instructionsList[i++]; //last index of
							yield return instructionsList[i++]; //store to loc 0
							i++;//yield return instructionsList[i++]; //this
							i++;//yield return instructionsList[i++]; //load field
							yield return new CodeInstruction(OpCodes.Ldloc, collectionCopy.LocalIndex);
							yield return instructionsList[i++]; //load loc 0
							yield return instructionsList[i++]; //removeAt (void)
							int j = 0;
							while(j < storeReplay.Count - 1)
                            {
								yield return storeReplay[j++];
							}
							yield return new CodeInstruction(OpCodes.Ldloc, collectionCopy.LocalIndex);
							yield return new CodeInstruction(OpCodes.Stfld, storeReplay[j].operand);

							foreach (CodeInstruction lockInstruction in RimThreadedHarmony.ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
							{
								yield return lockInstruction;
							}
							continue;
						}
					}
				}
				yield return instructionsList[i++];
			}
		}

    }
}
