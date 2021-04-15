using BepInEx;
using System;
using RoR2;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BepInEx.Configuration;
using UnityEngine;

namespace RaiseMonsterLevelCap
{
    [BepInPlugin("com.Moffein.RaiseMonsterLevelCap", "Raise Monster Level Cap", "1.0.3")]
    public class RaiseMonsterLevelCap : BaseUnityPlugin
    {
		public static float maxLevel;
        public void Awake()
        {
			maxLevel = (float)base.Config.Bind<uint>(new ConfigDefinition("Settings", "Max Level"), 1000000, new ConfigDescription("Max level that monsters can reach. 94 is vanilla. I have no clue how high it actually can go or what happens if it goes too high, so I just set it to somewhere near the uint limit.")).Value;
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

			//Use this to confirm monsters are actually leveling up.
			/*On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
			{
				orig(self);
				if (self.teamComponent.teamIndex != TeamIndex.Player)
                {
					Debug.Log("HP: " + self.healthComponent.health);
                }
			};*/
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

namespace EnigmaticThunder
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public class ManualNetworkRegistrationAttribute : Attribute
	{
	}
}
