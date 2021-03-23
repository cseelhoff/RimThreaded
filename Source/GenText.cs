using System;
using System.Text;
using Verse;

namespace RimThreaded
{

    public class GenText_Patch
	{
        [ThreadStatic]
        private static StringBuilder tmpSbForCapitalizedSentences;
        public static bool CapitalizeSentences(ref string __result, string input, bool capitalizeFirstSentence = true)
        {
            if (input.NullOrEmpty())
            {
                __result = input;
                return false;
            }

            if (input.Length == 1)
            {
                if (capitalizeFirstSentence)
                {
                    __result = input.ToUpper();
                    return false;
                }

                __result = input;
                return false;
            }

            bool flag = capitalizeFirstSentence;
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
            if (tmpSbForCapitalizedSentences == null)
            {
                tmpSbForCapitalizedSentences = new StringBuilder();
            }
            tmpSbForCapitalizedSentences.Length = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (flag && char.IsLetterOrDigit(input[i]) && !flag2 && !flag3 && !flag4)
                {
                    tmpSbForCapitalizedSentences.Append(char.ToUpper(input[i]));
                    flag = false;
                }
                else
                {
                    tmpSbForCapitalizedSentences.Append(input[i]);
                }

                if (input[i] == '\r' || input[i] == '\n' || (input[i] == '.' && i < input.Length - 1 && !char.IsLetter(input[i + 1])) || input[i] == '!' || input[i] == '?' || input[i] == ':')
                {
                    flag = true;
                }
                else if (input[i] == '<' && i < input.Length - 1 && input[i + 1] != '/')
                {
                    flag2 = true;
                }
                else if (flag2 && input[i] == '>')
                {
                    flag2 = false;
                }
                else if (input[i] == '(' && i < input.Length - 1 && input[i + 1] == '*')
                {
                    flag4 = true;
                }
                else if (flag4 && input[i] == ')')
                {
                    flag4 = false;
                }
                else if (input[i] == '{')
                {
                    flag3 = true;
                    flag = false;
                }
                else if (flag3 && input[i] == '}')
                {
                    flag3 = false;
                    flag = false;
                }
            }

            __result = tmpSbForCapitalizedSentences.ToString();
            return false;
        }

    }
}
