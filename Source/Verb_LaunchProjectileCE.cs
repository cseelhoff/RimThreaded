
namespace RimThreaded
{

    public class Verb_LaunchProjectileCE_Patch
	{
		/*
		private bool CanHitFromCellIgnoringRange(Vector3 shotSource, LocalTargetInfo targ, out IntVec3 goodDest)
		{
			if (targ.Thing != null)
			{
				if (targ.Thing.Map != this.caster.Map)
				{
					goodDest = IntVec3.Invalid;
					return false;
				}
				Verb_LaunchProjectileCE.tempDestList.Clear();
				Verb_LaunchProjectileCE.tempDestList.Add(targ.Cell);
				foreach (IntVec3 intVec in Verb_LaunchProjectileCE.tempDestList)
				{
					if (this.CanHitCellFromCellIgnoringRange(shotSource, intVec, targ.Thing))
					{
						goodDest = intVec;
						return true;
					}
				}
			}
			else if (this.CanHitCellFromCellIgnoringRange(shotSource, targ.Cell, targ.Thing))
			{
				goodDest = targ.Cell;
				return true;
			}
			goodDest = IntVec3.Invalid;
			return false;
		}
		*/
	}
}
