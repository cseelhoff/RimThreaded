using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Verse;
using HarmonyLib;

namespace RimThreaded
{
#pragma warning disable 649
    [Serializable]
    public class SerializableMethod
    {
        public string Name;
        public Type ClassType;
        public List<Type> ParametersType;
        //public bool IsConstructor;
        public SerializableMethod()
        {
            ParametersType = new List<Type>();
        }
    }
#pragma warning restore 649
    public class AssemblyCache
    {
        private static readonly List<MethodBase> CacheList = new List<MethodBase>();
        private static List<SerializableMethod> CacheListS = new List<SerializableMethod>();
        private static readonly string CacheFolder = Path.Combine(RimThreadedMod.replacementsFolder, "Caches");
        private static string CurrentMethodPath;


        public static void SaveJson()
        {
            string jsonString = JsonConvert.SerializeObject(CacheListS);
            File.WriteAllText(CurrentMethodPath, jsonString);
        }
        public static bool TryGetFromCache(string AssemblyName, out List<MethodBase> ReturnMethodList)
        {
            string AssemblyCachePath = Path.Combine(CacheFolder, AssemblyName + ".json");
            System.IO.Directory.CreateDirectory(CacheFolder);
            CurrentMethodPath = AssemblyCachePath;
            if (!File.Exists(AssemblyCachePath))
            {
                ReturnMethodList = null;
                return false;
            }
            string jsonstr = File.ReadAllText(AssemblyCachePath);
            CacheListS = JsonConvert.DeserializeObject<List<SerializableMethod>>(jsonstr);
            foreach (SerializableMethod s in CacheListS)
            {
                if (s.Name == ".ctor")
                {
                    CacheList.Add(AccessTools.Constructor(s.ClassType, s.ParametersType.ToArray()));
                    continue;
                }
                if (s.Name == ".cctor")
                {
                    CacheList.Add(AccessTools.Constructor(s.ClassType, s.ParametersType.ToArray(), true));
                    continue;
                }
                CacheList.Add(AccessTools.Method(s.ClassType, s.Name, s.ParametersType.ToArray()));
            }
            ReturnMethodList = CacheList;
            return true;

        }
        public static void AddToCache(string AssemblyName, MethodBase method, Type type)
        {
            SerializableMethod SMethod = new SerializableMethod
            {
                Name = method.Name,
                ClassType = type
            };
            foreach (ParameterInfo p in method.GetParameters())
            {
                SMethod.ParametersType.Add(p.ParameterType);
            }
            CacheListS.Add(SMethod);
            CurrentMethodPath = Path.Combine(CacheFolder, AssemblyName + ".json");
            CacheList.Add(method);
        }

    }
}