using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using Verse;

namespace RimThreaded
{    
    public class ContentFinder_Texture2D_Patch
    {
		public static bool Get(ref Texture2D __result, string itemPath, bool reportFailure = true)
		{
			Texture2D texture2d;
			//if (TickList_Patch.texture2DResults.TryGetValue(itemPath, out texture2d))
			//{
			//__result = texture2d;
			//return false;
			//}
			int tID = Thread.CurrentThread.ManagedThreadId;
			if (TickList_Patch.texture2DWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart)) {
				TickList_Patch.texture2DRequests.TryAdd(tID, itemPath);
				TickList_Patch.mainThreadWaitHandle.Set();
				eventWaitStart.WaitOne();
				TickList_Patch.texture2DResults.TryGetValue(itemPath, out texture2d);
				__result = texture2d;
				return false;
			}
			return true;
		}
        public static Texture2D GetTexture2D(string itemPath, bool reportFailure = true)
        {
			if (!UnityData.IsInMainThread)
            {
                Log.Error("Tried to get a resource \"" + itemPath + "\" from a different thread. All resources must be loaded in the main thread.");
                return null;
            }

            Texture2D val = null;
            List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
            for (int num = runningModsListForReading.Count - 1; num >= 0; num--)
            {
                ModContentHolder<Texture2D> texture2DContentHolder = runningModsListForReading[num].GetContentHolder<Texture2D>();
                val = texture2DContentHolder.Get(itemPath);
                if (val != null)
                {
                    return val;
                }
            }
            val = Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + itemPath);
            if (val != null)
            {
                return val;
            }
            for (int num2 = runningModsListForReading.Count - 1; num2 >= 0; num2--)
            {
                for (int i = 0; i < runningModsListForReading[num2].assetBundles.loadedAssetBundles.Count; i++)
                {
                    AssetBundle val2 = runningModsListForReading[num2].assetBundles.loadedAssetBundles[i];
                    string path = Path.Combine("Assets", "Data");
                    path = Path.Combine(path, runningModsListForReading[num2].FolderName);
                    string str = Path.Combine(Path.Combine(path, GenFilePaths.ContentPath<Texture2D>()), itemPath);
                    for (int j = 0; j < ModAssetBundlesHandler.TextureExtensions.Length; j++)
                    {
                        val = val2.LoadAsset<Texture2D>(str + ModAssetBundlesHandler.TextureExtensions[j]);
                        if (val != null)
                        {
                            return val;
                        }
                    }
                }
            }            
            if (reportFailure)
            {
                Log.Error("Could not load " + typeof(Texture2D) + " at " + itemPath + " in any active mod or in base resources.");
            }

            return val;
        }

	}

}
