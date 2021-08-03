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
        private static List<MethodBase> CacheList = new List<MethodBase>();
        private static List<SerializableMethod> CacheListS = new List<SerializableMethod>();
        private static string Cachepath = Path.Combine(RimThreadedMod.replacementsJsonPath.Replace("replacements.json", ""), "Caches");
        private static string CurrentMethodPath;


        public static void SaveJson()
        {
            string jsonString = JsonConvert.SerializeObject(CacheListS);
            File.WriteAllText(CurrentMethodPath, jsonString);
        }
        public static bool TryGetFromCache(string AssemblyName, out List<MethodBase> ReturnMethodList)
        {
            string CachePath = Path.Combine(Cachepath, AssemblyName + ".json");
            System.IO.Directory.CreateDirectory(Cachepath);
            CurrentMethodPath = CachePath;
            if (!File.Exists(CachePath))
            {
                ReturnMethodList = null;
                return false;
            }
            string jsonstr = File.ReadAllText(CachePath);
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
            SerializableMethod SMethod = new SerializableMethod();
            SMethod.Name = method.Name;
            SMethod.ClassType = type;
            foreach (ParameterInfo p in method.GetParameters())
            {
                SMethod.ParametersType.Add(p.ParameterType);
            }
            CacheListS.Add(SMethod);
            CurrentMethodPath = Path.Combine(Cachepath, AssemblyName + ".json");
            CacheList.Add(method);
        }

    }
}