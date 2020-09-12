using System;
using System.Collections.Concurrent;
using Verse;
using Verse.Sound;

namespace RimThreaded
{
    static class MainThreadWorkTasks
    {
        public static readonly ConcurrentQueue<Tuple<SoundDef, SoundInfo>> PlayOneShot = new ConcurrentQueue<Tuple<SoundDef, SoundInfo>>();
        public static readonly ConcurrentQueue<Tuple<SoundDef, Map>> PlayOneShotCamera = new ConcurrentQueue<Tuple<SoundDef, Map>>();
    }
}
