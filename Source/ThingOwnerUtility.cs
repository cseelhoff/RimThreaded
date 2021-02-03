﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Threading;

namespace RimThreaded
{

    public class ThingOwnerUtility_Patch
    {
		//public static Dictionary<int, List<IThingHolder>> tmpHoldersDict = new Dictionary<int, List<IThingHolder>>();

		public static bool AppendThingHoldersFromThings(List<IThingHolder> outThingsHolders, IList<Thing> container)
		{
			if (container == null)
			{
				return false;
			}
			int i = 0;
			int count = container.Count;
			Thing thing;
			while (i < count)
			{
				try
                {
					thing = container[i];
				} catch (ArgumentOutOfRangeException) { break; }
				IThingHolder thingHolder = thing as IThingHolder;
				if (thingHolder != null)
				{
					lock (outThingsHolders)
					{
						outThingsHolders.Add(thingHolder);
					}
				}
				ThingWithComps thingWithComps = container[i] as ThingWithComps;
				if (thingWithComps != null)
				{
					List<ThingComp> allComps = thingWithComps.AllComps;
					for (int j = 0; j < allComps.Count; j++)
					{
						IThingHolder thingHolder2 = allComps[j] as IThingHolder;
						if (thingHolder2 != null)
						{
							lock (outThingsHolders)
							{
								outThingsHolders.Add(thingHolder2);
							}
						}
					}
				}
				i++;
			}
			return false;
		}

		public static bool GetAllThingsRecursively(IThingHolder holder, List<Thing> outThings, bool allowUnreal = true, Predicate<IThingHolder> passCheck = null)
		{
			outThings.Clear();
			if (passCheck != null && !passCheck(holder))
			{
				return false;
			}
			Stack<IThingHolder> tmpStack = new Stack<IThingHolder>();
			tmpStack.Push(holder);
			while (tmpStack.Count != 0)
			{
				IThingHolder thingHolder = tmpStack.Pop();
				if (allowUnreal || ThingOwnerUtility.AreImmediateContentsReal(thingHolder))
				{
					ThingOwner directlyHeldThings = thingHolder.GetDirectlyHeldThings();
					if (directlyHeldThings != null)
					{
						outThings.AddRange(directlyHeldThings);
					}
				}
				//List<IThingHolder> tmpHolders = tmpHoldersDict[Thread.CurrentThread.ManagedThreadId];
				List<IThingHolder> tmpHolders = new List<IThingHolder>();
				thingHolder.GetChildHolders(tmpHolders);
				for (int i = 0; i < tmpHolders.Count; i++)
				{
					if (passCheck == null || passCheck(tmpHolders[i]))
					{
						tmpStack.Push(tmpHolders[i]);
					}
				}
			}
			//tmpStack.Clear();
			//tmpHolders.Clear();
			return false;
		}
		public static bool GetAllThingsRecursively_Pawn(Map map, 
			ThingRequest request, List<Pawn> outThings, bool allowUnreal = true, 
			Predicate<IThingHolder> passCheck = null, bool alsoGetSpawnedThings = true) 
		{
			lock (outThings) {
				outThings.Clear();
			}
			if (alsoGetSpawnedThings)
			{
				List<Thing> list = map.listerThings.ThingsMatching(request);
				for (int i = 0; i < list.Count; i++)
				{
					Pawn t = list[i] as Pawn;
					if (t != null)
					{
						lock(outThings) {
							outThings.Add(t);
						}
					}
				}
			}
			
			List<IThingHolder> tmpMapChildHolders = new List<IThingHolder>();
			//ThingOwnerUtility.tmpMapChildHolders.Clear();
			map.GetChildHolders(tmpMapChildHolders);
			for (int j = 0; j < tmpMapChildHolders.Count; j++)
			{
				//ThingOwnerUtility.tmpThings.Clear();
				List<Thing> tmpThings = new List<Thing>();
				ThingOwnerUtility.GetAllThingsRecursively(tmpMapChildHolders[j], tmpThings, allowUnreal, passCheck);
				for (int k = 0; k < tmpThings.Count; k++)
				{
					Pawn t2 = tmpThings[k] as Pawn;
					if (t2 != null && request.Accepts(t2))
					{
						lock (outThings)
						{
							outThings.Add(t2);
						}
					}
				}
			}
			return false;
			//tmpThings.Clear();
			//tmpMapChildHolders.Clear();
		}

		public static bool GetAllThingsRecursively_Thing(Map map, ThingRequest request, List<Thing> outThings, bool allowUnreal = true, Predicate<IThingHolder> passCheck = null, bool alsoGetSpawnedThings = true)
		{
			outThings.Clear();
			if (alsoGetSpawnedThings)
			{
				List<Thing> list = map.listerThings.ThingsMatching(request);
				for (int i = 0; i < list.Count; i++)
				{
					Thing val = list[i] as Thing;
					if (val != null)
					{
						outThings.Add(val);
					}
				}
			}
			List<IThingHolder> tmpMapChildHolders = new List<IThingHolder>();
			//tmpMapChildHolders.Clear();
			map.GetChildHolders(tmpMapChildHolders);
			List<Thing> tmpThings = new List<Thing>();
			for (int j = 0; j < tmpMapChildHolders.Count; j++)
			{
				tmpThings.Clear();
				GetAllThingsRecursively(tmpMapChildHolders[j], tmpThings, allowUnreal, passCheck);
				for (int k = 0; k < tmpThings.Count; k++)
				{
                    if (tmpThings[k] is Thing val2 && request.Accepts(val2))
                    {
                        outThings.Add(val2);
                    }
                }
			}
			//tmpThings.Clear();
			//tmpMapChildHolders.Clear();
			return false;
		}


	}
}
