using System;
using System.Threading;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace RimThreaded.RW_Patches
{
    class FleckSystemBase_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(FleckSystemBase<FleckStatic>);
            Type originalFT = typeof(FleckSystemBase<FleckThrown>);
            Type originalFSplash = typeof(FleckSystemBase<FleckSplash>);
            Type patched = typeof(FleckSystemBase_Patch);

            RimThreadedHarmony.Prefix(original, patched, nameof(Tick));
            RimThreadedHarmony.Prefix(originalFT, patched, "Tick", PatchMethod: nameof(TickFT));
            RimThreadedHarmony.Prefix(originalFSplash, patched, "Tick", PatchMethod: nameof(TickFSplash));

            RimThreadedHarmony.Prefix(original, patched, nameof(CreateFleck));
            RimThreadedHarmony.Prefix(originalFT, patched, "CreateFleck", PatchMethod: nameof(CreateFleckFT));
            RimThreadedHarmony.Prefix(originalFSplash, patched, "CreateFleck", PatchMethod: nameof(CreateFleckFSplash));

            RimThreadedHarmony.Prefix(original, patched, nameof(Draw));
            RimThreadedHarmony.Prefix(originalFT, patched, "Draw", PatchMethod: nameof(DrawFT));
            RimThreadedHarmony.Prefix(originalFSplash, patched, "Draw", PatchMethod: nameof(DrawFSplash));

            RimThreadedHarmony.Prefix(original, patched, nameof(Update));
            RimThreadedHarmony.Prefix(originalFT, patched, "Update", PatchMethod: nameof(UpdateFT));
            RimThreadedHarmony.Prefix(originalFSplash, patched, "Update", PatchMethod: nameof(UpdateFSplash));
        }
        public static ReaderWriterLockSlim FleckStaticLockRT = new ReaderWriterLockSlim();
        public static ReaderWriterLockSlim FleckThrownLockRT = new ReaderWriterLockSlim();
        public static ReaderWriterLockSlim FleckSplashLockRT = new ReaderWriterLockSlim();
        public static ReaderWriterLockSlim FleckStaticLockGT = new ReaderWriterLockSlim();
        public static ReaderWriterLockSlim FleckThrownLockGT = new ReaderWriterLockSlim();
        public static ReaderWriterLockSlim FleckSplashLockGT = new ReaderWriterLockSlim();
        /* tmpRemoveIndices also requires protection here. I wont make a lock for it but the PulsePool should provide enough protection */

        // FLECKSTATIC
        public static bool Tick(FleckSystemBase<FleckStatic> __instance)
        {
            FleckStaticLockGT.EnterWriteLock();
            try
            {
                for (int num = __instance.dataGametime.Count - 1; num >= 0; num--)
                {
                    FleckStatic value = __instance.dataGametime[num];
                    if (value.TimeInterval(0.0166666675f, __instance.parent.parent))
                    {
                        __instance.tmpRemoveIndices.Add(num);
                    }
                    else
                    {
                        __instance.dataGametime[num] = value;
                    }
                }
                __instance.dataGametime.RemoveBatchUnordered(__instance.tmpRemoveIndices);
            }
            finally
            {
                FleckStaticLockGT.ExitWriteLock();
                __instance.tmpRemoveIndices = PulsePool<List<int>>.Pulse(__instance.tmpRemoveIndices);
                __instance.tmpRemoveIndices.Clear();
            }
            return false;
        }
        public static bool Draw(FleckSystemBase<FleckStatic> __instance, DrawBatch drawBatch)
        {
            foreach (FleckDef handledDef in __instance.handledDefs)
            {
                if (handledDef.graphicData != null)
                {
                    handledDef.graphicData.ExplicitlyInitCachedGraphic();
                }
                if (handledDef.randomGraphics == null)
                {
                    continue;
                }
                foreach (GraphicData randomGraphic in handledDef.randomGraphics)
                {
                    randomGraphic.ExplicitlyInitCachedGraphic();
                }
            }
            int parallelizationDegree;
            if (__instance.ParallelizedDrawing)
            {
                if (__instance.CachedDrawParallelWaitCallback == null)
                {
                    __instance.CachedDrawParallelWaitCallback = FleckSystemBase<FleckStatic>.DrawParallel;
                }
                parallelizationDegree = Environment.ProcessorCount;
                FleckStaticLockRT.EnterReadLock();
                try
                {
                    Process(__instance.dataRealtime);
                }
                finally
                {
                    FleckStaticLockRT.ExitReadLock();
                }
                FleckStaticLockGT.EnterReadLock();
                try
                {
                    Process(__instance.dataGametime);
                }
                finally
                {
                    FleckStaticLockGT.ExitReadLock();
                }
            }
            else
            {
                FleckStaticLockRT.EnterReadLock();
                try
                {
                    Process2(__instance.dataRealtime);
                }
                finally
                {
                    FleckStaticLockRT.ExitReadLock();
                }
                FleckStaticLockGT.EnterReadLock();
                try
                {
                    Process2(__instance.dataGametime);
                }
                finally
                {
                    FleckStaticLockGT.ExitReadLock();
                }
            }
            void Process(List<FleckStatic> data)
            {
                if (data.Count > 0)
                {
                    try
                    {
                        __instance.tmpParallelizationSlices.Clear();
                        GenThreading.SliceWorkNoAlloc(0, data.Count, parallelizationDegree, __instance.tmpParallelizationSlices);
                        foreach (GenThreading.Slice tmpParallelizationSlice in __instance.tmpParallelizationSlices)
                        {
                            FleckParallelizationInfo parallelizationInfo = FleckUtility.GetParallelizationInfo();
                            parallelizationInfo.startIndex = tmpParallelizationSlice.fromInclusive;
                            parallelizationInfo.endIndex = tmpParallelizationSlice.toExclusive;
                            parallelizationInfo.data = data;
                            ThreadPool.QueueUserWorkItem(__instance.CachedDrawParallelWaitCallback, parallelizationInfo);
                            __instance.tmpParallelizationInfo.Add(parallelizationInfo);
                        }
                        foreach (FleckParallelizationInfo item in __instance.tmpParallelizationInfo)
                        {
                            item.doneEvent.WaitOne();
                            drawBatch.MergeWith(item.drawBatch);
                        }
                    }
                    finally
                    {
                        foreach (FleckParallelizationInfo item2 in __instance.tmpParallelizationInfo)
                        {
                            FleckUtility.ReturnParallelizationInfo(item2);
                        }
                        __instance.tmpParallelizationInfo.Clear();
                    }
                }
            }
            void Process2(List<FleckStatic> data)
            {
                for (int num = data.Count - 1; num >= 0; num--)
                {
                    data[num].Draw(drawBatch);
                }
            }
            return false;
        }
        public static bool Update(FleckSystemBase<FleckStatic> __instance)
        {
            FleckStaticLockRT.EnterWriteLock();
            try
            {
                for (int num = __instance.dataRealtime.Count - 1; num >= 0; num--)
                {
                    FleckStatic value = __instance.dataRealtime[num];
                    if (value.TimeInterval(Time.deltaTime, __instance.parent.parent))
                    {
                        __instance.tmpRemoveIndices.Add(num);
                    }
                    else
                    {
                        __instance.dataRealtime[num] = value;
                    }
                }
                __instance.dataRealtime.RemoveBatchUnordered(__instance.tmpRemoveIndices);
            }
            finally
            {
                FleckStaticLockRT.ExitWriteLock();
                __instance.tmpRemoveIndices = PulsePool<List<int>>.Pulse(__instance.tmpRemoveIndices);
                __instance.tmpRemoveIndices.Clear();
            }
            return false;
        }
        public static bool CreateFleck(FleckSystemBase<FleckStatic> __instance, FleckCreationData creationData)
        {
            FleckStatic fleck = new FleckStatic();//the pool baby?
            fleck.Setup(creationData);
            if (creationData.def.realTime)
            {
                FleckStaticLockRT.EnterWriteLock();
                try
                {
                    __instance.dataRealtime.Add(fleck);
                }
                finally
                {
                    FleckStaticLockRT.ExitWriteLock();
                }
            }
            else
            {
                FleckStaticLockGT.EnterWriteLock();
                try
                {
                    __instance.dataGametime.Add(fleck);
                }
                finally
                {
                    FleckStaticLockGT.ExitWriteLock();
                }
            }
            return false;
        }

        // FLECKTHROWN
        public static bool TickFT(FleckSystemBase<FleckThrown> __instance)
        {
            FleckThrownLockGT.EnterWriteLock();
            try
            {
                for (int num = __instance.dataGametime.Count - 1; num >= 0; num--)
                {
                    FleckThrown value = __instance.dataGametime[num];
                    if (value.TimeInterval(0.0166666675f, __instance.parent.parent))
                    {
                        __instance.tmpRemoveIndices.Add(num);
                    }
                    else
                    {
                        __instance.dataGametime[num] = value;
                    }
                }
                __instance.dataGametime.RemoveBatchUnordered(__instance.tmpRemoveIndices);
            }
            finally
            {
                FleckThrownLockGT.ExitWriteLock();
                __instance.tmpRemoveIndices = PulsePool<List<int>>.Pulse(__instance.tmpRemoveIndices);
                __instance.tmpRemoveIndices.Clear();
            };
            return false;
        }
        public static bool DrawFT(FleckSystemBase<FleckThrown> __instance, DrawBatch drawBatch)
        {
            foreach (FleckDef handledDef in __instance.handledDefs)
            {
                if (handledDef.graphicData != null)
                {
                    handledDef.graphicData.ExplicitlyInitCachedGraphic();
                }
                if (handledDef.randomGraphics == null)
                {
                    continue;
                }
                foreach (GraphicData randomGraphic in handledDef.randomGraphics)
                {
                    randomGraphic.ExplicitlyInitCachedGraphic();
                }
            }
            int parallelizationDegree;
            if (__instance.ParallelizedDrawing)
            {
                if (__instance.CachedDrawParallelWaitCallback == null)
                {
                    __instance.CachedDrawParallelWaitCallback = FleckSystemBase<FleckThrown>.DrawParallel;
                }
                parallelizationDegree = Environment.ProcessorCount;
                FleckThrownLockRT.EnterReadLock();
                try
                {
                    Process(__instance.dataRealtime);
                }
                finally
                {
                    FleckThrownLockRT.ExitReadLock();
                }
                FleckThrownLockGT.EnterReadLock();
                try
                {
                    Process(__instance.dataGametime);
                }
                finally
                {
                    FleckThrownLockGT.ExitReadLock();
                }
            }
            else
            {
                FleckThrownLockRT.EnterReadLock();
                try
                {
                    Process2(__instance.dataRealtime);
                }
                finally
                {
                    FleckThrownLockRT.ExitReadLock();
                }
                FleckThrownLockGT.EnterReadLock();
                try
                {
                    Process2(__instance.dataGametime);
                }
                finally
                {
                    FleckThrownLockGT.ExitReadLock();
                }
            }
            void Process(List<FleckThrown> data)
            {
                if (data.Count > 0)
                {
                    try
                    {
                        __instance.tmpParallelizationSlices.Clear();
                        GenThreading.SliceWorkNoAlloc(0, data.Count, parallelizationDegree, __instance.tmpParallelizationSlices);
                        foreach (GenThreading.Slice tmpParallelizationSlice in __instance.tmpParallelizationSlices)
                        {
                            FleckParallelizationInfo parallelizationInfo = FleckUtility.GetParallelizationInfo();
                            parallelizationInfo.startIndex = tmpParallelizationSlice.fromInclusive;
                            parallelizationInfo.endIndex = tmpParallelizationSlice.toExclusive;
                            parallelizationInfo.data = data;
                            ThreadPool.QueueUserWorkItem(__instance.CachedDrawParallelWaitCallback, parallelizationInfo);
                            __instance.tmpParallelizationInfo.Add(parallelizationInfo);
                        }
                        foreach (FleckParallelizationInfo item in __instance.tmpParallelizationInfo)
                        {
                            item.doneEvent.WaitOne();
                            drawBatch.MergeWith(item.drawBatch);
                        }
                    }
                    finally
                    {
                        foreach (FleckParallelizationInfo item2 in __instance.tmpParallelizationInfo)
                        {
                            FleckUtility.ReturnParallelizationInfo(item2);
                        }
                        __instance.tmpParallelizationInfo.Clear();
                    }
                }
            }
            void Process2(List<FleckThrown> data)
            {
                for (int num = data.Count - 1; num >= 0; num--)
                {
                    data[num].Draw(drawBatch);
                }
            }
            return false;
        }
        public static bool UpdateFT(FleckSystemBase<FleckThrown> __instance)
        {
            FleckThrownLockRT.EnterWriteLock();
            try
            {
                for (int num = __instance.dataRealtime.Count - 1; num >= 0; num--)
                {
                    FleckThrown value = __instance.dataRealtime[num];
                    if (value.TimeInterval(Time.deltaTime, __instance.parent.parent))
                    {
                        __instance.tmpRemoveIndices.Add(num);
                    }
                    else
                    {
                        __instance.dataRealtime[num] = value;
                    }
                }
                __instance.dataRealtime.RemoveBatchUnordered(__instance.tmpRemoveIndices);
            }
            finally
            {
                //this is where the pool baby should return the flecks
                FleckThrownLockRT.ExitWriteLock();
                __instance.tmpRemoveIndices = PulsePool<List<int>>.Pulse(__instance.tmpRemoveIndices);
                __instance.tmpRemoveIndices.Clear();
            }
            return false;
        }
        public static bool CreateFleckFT(FleckSystemBase<FleckThrown> __instance, FleckCreationData creationData)
        {
            FleckThrown fleck = new FleckThrown();
            fleck.Setup(creationData);
            if (creationData.def.realTime)
            {
                FleckThrownLockRT.EnterWriteLock();
                try
                {
                    __instance.dataRealtime.Add(fleck);
                }
                finally
                {
                    FleckThrownLockRT.ExitWriteLock();
                }
            }
            else
            {
                FleckThrownLockGT.EnterWriteLock();
                try
                {
                    __instance.dataGametime.Add(fleck);
                }
                finally
                {
                    FleckThrownLockGT.ExitWriteLock();
                }
            }
            return false;
        }

        // FLECKSPLASH
        public static bool TickFSplash(FleckSystemBase<FleckSplash> __instance)
        {
            FleckSplashLockGT.EnterWriteLock();
            try
            {
                for (int num = __instance.dataGametime.Count - 1; num >= 0; num--)
                {
                    FleckSplash value = __instance.dataGametime[num];
                    if (value.TimeInterval(0.0166666675f, __instance.parent.parent))
                    {
                        __instance.tmpRemoveIndices.Add(num);
                    }
                    else
                    {
                        __instance.dataGametime[num] = value;
                    }
                }
                __instance.dataGametime.RemoveBatchUnordered(__instance.tmpRemoveIndices);
            }
            finally
            {
                FleckSplashLockGT.ExitWriteLock();
                __instance.tmpRemoveIndices = PulsePool<List<int>>.Pulse(__instance.tmpRemoveIndices);
                __instance.tmpRemoveIndices.Clear();
            }
            return false;
        }
        public static bool DrawFSplash(FleckSystemBase<FleckSplash> __instance, DrawBatch drawBatch)
        {
            foreach (FleckDef handledDef in __instance.handledDefs)
            {
                if (handledDef.graphicData != null)
                {
                    handledDef.graphicData.ExplicitlyInitCachedGraphic();
                }
                if (handledDef.randomGraphics == null)
                {
                    continue;
                }
                foreach (GraphicData randomGraphic in handledDef.randomGraphics)
                {
                    randomGraphic.ExplicitlyInitCachedGraphic();
                }
            }
            int parallelizationDegree;
            if (__instance.ParallelizedDrawing)
            {
                if (__instance.CachedDrawParallelWaitCallback == null)
                {
                    __instance.CachedDrawParallelWaitCallback = FleckSystemBase<FleckSplash>.DrawParallel;
                }
                parallelizationDegree = Environment.ProcessorCount;
                FleckSplashLockRT.EnterReadLock();
                try
                {
                    Process(__instance.dataRealtime);
                }
                finally
                {
                    FleckSplashLockRT.ExitReadLock();
                }
                FleckSplashLockGT.EnterReadLock();
                try
                {
                    Process(__instance.dataGametime);
                }
                finally
                {
                    FleckSplashLockGT.ExitReadLock();
                }
            }
            else
            {
                FleckSplashLockRT.EnterReadLock();
                try
                {
                    Process2(__instance.dataRealtime);
                }
                finally
                {
                    FleckSplashLockRT.ExitReadLock();
                }
                FleckSplashLockGT.EnterReadLock();
                try
                {
                    Process2(__instance.dataGametime);
                }
                finally
                {
                    FleckSplashLockGT.ExitReadLock();
                }
            }
            void Process(List<FleckSplash> data)
            {
                if (data.Count > 0)
                {
                    try
                    {
                        __instance.tmpParallelizationSlices.Clear();
                        GenThreading.SliceWorkNoAlloc(0, data.Count, parallelizationDegree, __instance.tmpParallelizationSlices);
                        foreach (GenThreading.Slice tmpParallelizationSlice in __instance.tmpParallelizationSlices)
                        {
                            FleckParallelizationInfo parallelizationInfo = FleckUtility.GetParallelizationInfo();
                            parallelizationInfo.startIndex = tmpParallelizationSlice.fromInclusive;
                            parallelizationInfo.endIndex = tmpParallelizationSlice.toExclusive;
                            parallelizationInfo.data = data;
                            ThreadPool.QueueUserWorkItem(__instance.CachedDrawParallelWaitCallback, parallelizationInfo);
                            __instance.tmpParallelizationInfo.Add(parallelizationInfo);
                        }
                        foreach (FleckParallelizationInfo item in __instance.tmpParallelizationInfo)
                        {
                            item.doneEvent.WaitOne();
                            drawBatch.MergeWith(item.drawBatch);
                        }
                    }
                    finally
                    {
                        foreach (FleckParallelizationInfo item2 in __instance.tmpParallelizationInfo)
                        {
                            FleckUtility.ReturnParallelizationInfo(item2);
                        }
                        __instance.tmpParallelizationInfo.Clear();
                    }
                }
            }
            void Process2(List<FleckSplash> data)
            {
                for (int num = data.Count - 1; num >= 0; num--)
                {
                    data[num].Draw(drawBatch);
                }
            }
            return false;
        }
        public static bool UpdateFSplash(FleckSystemBase<FleckSplash> __instance)
        {
            FleckSplashLockRT.EnterWriteLock();
            try
            {
                for (int num = __instance.dataRealtime.Count - 1; num >= 0; num--)
                {
                    FleckSplash value = __instance.dataRealtime[num];
                    if (value.TimeInterval(Time.deltaTime, __instance.parent.parent))
                    {
                        __instance.tmpRemoveIndices.Add(num);
                    }
                    else
                    {
                        __instance.dataRealtime[num] = value;
                    }
                }
                __instance.dataRealtime.RemoveBatchUnordered(__instance.tmpRemoveIndices);
            }
            finally
            {
                FleckSplashLockRT.ExitWriteLock();
                __instance.tmpRemoveIndices = PulsePool<List<int>>.Pulse(__instance.tmpRemoveIndices);
                __instance.tmpRemoveIndices.Clear();
            }
            return false;
        }
        public static bool CreateFleckFSplash(FleckSystemBase<FleckSplash> __instance, FleckCreationData creationData)
        {
            FleckSplash fleck = new FleckSplash();
            fleck.Setup(creationData);
            if (creationData.def.realTime)
            {
                FleckSplashLockRT.EnterWriteLock();
                try
                {
                    __instance.dataRealtime.Add(fleck);
                }
                finally
                {
                    FleckSplashLockRT.ExitWriteLock();
                }
            }
            else
            {
                FleckSplashLockGT.EnterWriteLock();
                try
                {
                    __instance.dataGametime.Add(fleck);
                }
                finally
                {
                    FleckSplashLockGT.ExitWriteLock();
                }
            }
            return false;
        }
    }
}
