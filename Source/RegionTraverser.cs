﻿using System;
using System.Collections.Generic;

namespace RimThreaded
{

    public class RegionTraverser_Patch
    {
        [ThreadStatic]
        public static Queue<object> freeWorkers;
        [ThreadStatic]
        public static int NumWorkers;

    }
    
    

}
