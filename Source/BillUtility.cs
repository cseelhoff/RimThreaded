using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class BillUtility_Patch
    {
        public static bool Notify_ColonistUnavailable(Pawn pawn)
        {
            //List<Bill> billList = BillUtility.GlobalBills().ToList();
            try
            {                
                foreach (Bill item in BillUtility.GlobalBills())
                {
                    item.ValidateSettings();
                }
            }
            catch (Exception arg)
            {
                Log.Error("Could not notify bills: " + arg);
            }
            return false;
        }
    }
}
