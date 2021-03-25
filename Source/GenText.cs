using System;
using System.Text;
using Verse;

namespace RimThreaded
{

    public class GenText_Patch
	{
        [ThreadStatic] public static StringBuilder tmpSbForCapitalizedSentences;

        public static void InitializedThreadStatics()
        {
            tmpSbForCapitalizedSentences = new StringBuilder();
        }

    }
}
