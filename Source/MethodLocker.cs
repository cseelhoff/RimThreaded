using System;
using System.Collections.Generic;
using System.Threading;
using Verse;
using System.Reflection;
using HarmonyLib;

namespace RimThreaded
{
    public enum LockFlag
    {
        ReaderLock = 1,
        WriterLock = 2
    }
    public static class MethodLocker
    {
        public static Dictionary<object, ReaderWriterLockSlim> InstanceToLock = new Dictionary<object, ReaderWriterLockSlim>();
        public static Dictionary<Type, ReaderWriterLockSlim> DeclaringTypeToLock = new Dictionary<Type, ReaderWriterLockSlim>();
        public static Dictionary<MethodBase, ReaderWriterLockSlim> MethodBaseToLock = new Dictionary<MethodBase, ReaderWriterLockSlim>();
        /* Some basic informations about reentracy in this class. A reader Lock can reenter read but can't enter write. A writer can reenter write
         and can reenter read. If you want just a basic lock just use writer locks all the times. As a general rule of thumb: If a method that only do reads
        on the resource we want to protect also calls another method that does writes... use WriterLock for both. Do not risk deadlocks!*/

        /// <summary>
        /// Locks a method on the instance of the caller.
        /// These methods SupportsRecursion so be careful to not deadlock yourself.
        /// </summary>
        /// <param name="Original">The type of what you want to lock</param>
        /// <param name="Method">The name of the method you want to lock</param>
        /// <param name="Flag">LockFlag. can be ReaderLock or WriterLock</param>
        public static void LockMethodOnInstance(Type Original, string Method, LockFlag Flag, Type[] OTypes = null)
        {
            if (Flag == LockFlag.ReaderLock)
            {
                RimThreadedHarmony.Prefix(Original, typeof(MethodLocker), Method, destructive: false, PatchMethod: nameof(ReaderPrefix), finalizer: nameof(ReaderFinalizer), origType: OTypes, NullPatchType: true);
            }
            if (Flag == LockFlag.WriterLock)
            {
                RimThreadedHarmony.Prefix(Original, typeof(MethodLocker), Method, destructive: false, PatchMethod: nameof(WriterPrefix), finalizer: nameof(WriterFinalizer), origType: OTypes, NullPatchType: true);
            }
        }
        /// <summary>
        /// Locks a method on the declaring type of the original method. Use this for classes that do not have an instance if you need to lock static methods.
        /// These methods SupportsRecursion so be careful to not deadlock yourself.
        /// </summary>
        /// <param name="Original">The type of what you want to lock</param>
        /// <param name="Method">The name of the method you want to lock</param>
        /// <param name="Flag">LockFlag. can be ReaderLock or WriterLock</param>
        public static void LockMethodOnDeclaringType(Type Original, string Method, LockFlag Flag, Type[] OTypes = null)
        {
            if (Flag == LockFlag.ReaderLock)
            {
                RimThreadedHarmony.Prefix(Original, typeof(MethodLocker), Method, destructive: false, PatchMethod: nameof(ReaderPrefixMB), finalizer: nameof(ReaderFinalizerMB), origType: OTypes, NullPatchType: true);
            }
            if (Flag == LockFlag.WriterLock)
            {
                RimThreadedHarmony.Prefix(Original, typeof(MethodLocker), Method, destructive: false, PatchMethod: nameof(WriterPrefixMB), finalizer: nameof(WriterFinalizerMB), origType: OTypes, NullPatchType: true);
            }
        }
        /// <summary>
        /// The most generic Method Lock, it will lock the method on a ReaderWriterLockSlim given as parameter.
        /// It is up to the programmer to understand if you want your ReaderWriterLockSlim to support recursion or not.
        /// </summary>
        /// <param name="Original">The type of what you want to lock</param>
        /// <param name="Method">The name of the method you want to lock</param>
        /// <param name="Flag">LockFlag. can be ReaderLock or WriterLock</param>
        /// <param name="LockOn">The ReaderWriterLockSlim you want to lock on</param>
        public static void LockMethodOn(Type Original, string Method, LockFlag Flag, ReaderWriterLockSlim LockOn, Type[] OTypes = null)
        {
            MethodBase OMethod = AccessTools.Method(Original, Method, OTypes);
            MethodBaseToLock[OMethod] = LockOn;
            if (Flag == LockFlag.ReaderLock)
            {
                RimThreadedHarmony.Prefix(Original, typeof(MethodLocker), Method, destructive: false, PatchMethod: nameof(ReaderPrefixG), finalizer: nameof(ReaderFinalizerG), origType: OTypes, NullPatchType: true);
            }
            if (Flag == LockFlag.WriterLock)
            {
                RimThreadedHarmony.Prefix(Original, typeof(MethodLocker), Method, destructive: false, PatchMethod: nameof(WriterPrefixG), finalizer: nameof(WriterFinalizerG), origType: OTypes, NullPatchType: true);
            }
        }
        // ----------------LOCKON
        public static void WriterPrefixG(MethodBase __originalMethod)
        {
            MethodBaseToLock[__originalMethod].EnterWriteLock();
        }
        public static void WriterFinalizerG(MethodBase __originalMethod)
        {
            MethodBaseToLock[__originalMethod].ExitWriteLock();
        }
        public static void ReaderPrefixG(MethodBase __originalMethod)
        {
            MethodBaseToLock[__originalMethod].EnterReadLock();
        }
        public static void ReaderFinalizerG(MethodBase __originalMethod)
        {
            MethodBaseToLock[__originalMethod].ExitReadLock();
        }
        // ----------------METHODBASE DECLARING TYPE
        public static void WriterPrefixMB(MethodBase __originalMethod)
        {
            if (!(DeclaringTypeToLock.ContainsKey(__originalMethod.DeclaringType))||DeclaringTypeToLock[__originalMethod.DeclaringType] is null)
            {
                Log.Message("RimThreaded is creating a new ReaderWriterLockSlim for DeclaringType:\t" + __originalMethod.Name);
                DeclaringTypeToLock[__originalMethod.DeclaringType] = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            }
            DeclaringTypeToLock[__originalMethod.DeclaringType].EnterWriteLock();
        }
        public static void WriterFinalizerMB(MethodBase __originalMethod)
        {
            DeclaringTypeToLock[__originalMethod.DeclaringType].ExitWriteLock();
        }
        public static void ReaderPrefixMB(MethodBase __originalMethod)
        {
            if (!(DeclaringTypeToLock.ContainsKey(__originalMethod.DeclaringType)) || DeclaringTypeToLock[__originalMethod.DeclaringType] is null)
            {
                Log.Message("RimThreaded is creating a new ReaderWriterLockSlim for DeclaringType:\t" + __originalMethod.Name);
                DeclaringTypeToLock[__originalMethod.DeclaringType] = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            }
            DeclaringTypeToLock[__originalMethod.DeclaringType].EnterReadLock();
        }
        public static void ReaderFinalizerMB(MethodBase __originalMethod)
        {
            DeclaringTypeToLock[__originalMethod.DeclaringType].ExitReadLock();
        }
        // ----------------INSTANCE
        public static void WriterPrefix(object __instance)
        {
            if (!(InstanceToLock.ContainsKey(__instance)) || InstanceToLock[__instance] is null)
            {
                Log.Message("RimThreaded is creating a new ReaderWriterLockSlim for instance:\t" + __instance.ToString());
                InstanceToLock[__instance] = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            }
            InstanceToLock[__instance].EnterWriteLock();
        }
        public static void WriterFinalizer(object __instance)
        {
            InstanceToLock[__instance].ExitWriteLock();
        }
        public static void ReaderPrefix(object __instance)
        {
            if (!(InstanceToLock.ContainsKey(__instance)) || InstanceToLock[__instance] is null)
            {
                Log.Message("RimThreaded is creating a new ReaderWriterLockSlim for instance:\t" + __instance.ToString());
                InstanceToLock[__instance] = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            }
            InstanceToLock[__instance].EnterReadLock();  
        }
        public static void ReaderFinalizer(object __instance)
        {
            InstanceToLock[__instance].ExitReadLock();
        }
    }
}
