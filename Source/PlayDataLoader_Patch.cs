using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class PlayDataLoader_Patch
    {
        public static bool DoPlayLoad()
        {
            Log.Message("RimThreaded Harmony is initializing...");
            /*
            Explosion_Patch.cellsToAffectDict.Clear();
            GenTemperature_Patch.SeasonalShiftAmplitudeCache.Clear();
            GenTemperature_Patch.tileTemperature.Clear();
            GenTemperature_Patch.absTickOffset.Clear();
            GenTemperature_Patch.tileAbsTickTemperature.Clear();
            GrammarResolver_Patch.rules.Clear();
            GrammarResolverSimpleStringExtensions_Patch.argsLabelsDict.Clear();
            GrammarResolverSimpleStringExtensions_Patch.argsObjectsDict.Clear();
            //ImmunityHandler_Patch.immunityInfoLists.Clear();
            ListerBuildings_Patch.allBuildingsColonistBuilding_PlantGrower.Clear();
            Lord_Patch.pawnsLord.Clear();
            Map_Patch.skyManagerStartEvents.Clear();
            
            PathFinder_Patch.regionCostCalculatorWrappers.Clear();
            PathFinder_Patch.calcGrids.Clear();
            PathFinder_Patch.openLists.Clear();
            PathFinder_Patch.openValues.Clear();
            PathFinder_Patch.closedValues.Clear();
            PathFinder_Patch.calcGridDict2.Clear();
            /*
            PawnCapacitiesHandler_Patch.cachedCapacityLevelsDict.Clear();
            Pawn_InteractionsTracker_Patch.workingLists.Clear();
            Pawn_PlayerSettings_Patch.pets.Clear();
            Rand_Patch.tmpRanges.Clear();
            ReachabilityCache_Patch.cacheDictDict.Clear();
            ReachabilityCache_Patch.tmpCachedEntries2.Clear();
            Region_Patch.cachedSafeTemperatureRanges.Clear();
            RegionAndRoomUpdater_Patch.threadRebuilding.Clear();
            RegionCostCalculator_Patch.queueDict.Clear();
            RegionListersUpdater_Patch.tmpRegionsLists.Clear();
            RegionTraverser_Patch.regionTraverser2Dict.Clear();
            RimThreaded.tryMakeAndPlayRequests.Clear();
            RimThreaded.safeFunctionRequests.Clear();
            */
            return true;
        }
    }
}
