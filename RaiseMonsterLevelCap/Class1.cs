using BepInEx;
using System;
using RoR2;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BepInEx.Configuration;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RaiseMonsterLevelCap
{
    [BepInPlugin("com.Moffein.RaiseMonsterLevelCap", "Raise Monster Level Cap", "1.0.5")]
    public class RaiseMonsterLevelCap : BaseUnityPlugin
    {
		public static float maxLevel;
        public void Awake()
        {
			maxLevel = base.Config.Bind<float>(new ConfigDefinition("Settings", "Max Level"), 1000f, new ConfigDescription("Max level that monsters can reach. 99 is vanilla. Past a certain point in the run, monsters will level up every few seconds.")).Value;
			float stopSound = base.Config.Bind<float>(new ConfigDefinition("Settings","Max Levelup Sound"), 300f, new ConfigDescription("Max level for the levelup sound to play. This prevents sound spam later in runs")).Value;

			Run.ambientLevelCap = (int)maxLevel;
			//Remove level capping when calculating monster level
			IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += (il) =>
			{
				ILCursor c = new ILCursor(il);
				c.GotoNext(
					x => x.MatchLdsfld<Run>(nameof(Run.ambientLevelCap))
					);
				c.Remove();
				c.Emit<RaiseMonsterLevelCap>(OpCodes.Ldsfld, nameof(RaiseMonsterLevelCap.maxLevel));
			};

			On.RoR2.LevelUpEffectManager.OnRunAmbientLevelUp += (orig, run) =>
			{
				if (run.ambientLevel <= stopSound)
                {
					orig(run);
                }
			};
		}
    }
}

namespace R2API.Utils
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public class ManualNetworkRegistrationAttribute : Attribute
	{
	}
}
