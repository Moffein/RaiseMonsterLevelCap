using BepInEx;
using System;
using UnityEngine;
using RoR2;
using R2API.Utils;
using UnityEngine.Networking;
using System.Collections.ObjectModel;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BepInEx.Configuration;

namespace RaiseMonsterLevelCap
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Moffein.RaiseMonsterLevelCap", "Raise Monster Level Cap", "1.0.1")]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class RaiseMonsterLevelCap : BaseUnityPlugin
    {
        public void Awake()
        {
			MonsterMissingLevels.maxLevel = base.Config.Bind<uint>(new ConfigDefinition("Raise Monster Level Cap", "Max Level"), 4000000000, new ConfigDescription("Max level that monsters can reach. 94 is vanilla. I have no clue how high it actually can go or what happens if it goes too high, so I just set it to somewhere near the uint limit.")).Value;
			//Remove level capping when calculating monster level
			IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += (il) =>
			{
				ILCursor c = new ILCursor(il);
				c.GotoNext(
					x => x.MatchLdsfld<TeamManager>(nameof(TeamManager.naturalLevelCap))
					);
				c.Remove();
				c.Emit<MonsterMissingLevels>(OpCodes.Ldsfld, nameof(MonsterMissingLevels.maxLevel));
			};

			On.RoR2.Run.Awake += (orig, self) =>
			{
				orig(self);
				self.gameObject.AddComponent<MonsterMissingLevels>();
			};

			//Save intended level somewhere else
            On.RoR2.TeamManager.SetTeamLevel += (orig, self, teamIndex, targetLevel) =>
            {
                orig(self, teamIndex, targetLevel);
                if (NetworkServer.active && teamIndex == TeamIndex.Monster && targetLevel > TeamManager.naturalLevelCap)
                {
					uint missingLevels = targetLevel - TeamManager.naturalLevelCap;
					MonsterMissingLevels m = Run.instance.GetComponent<MonsterMissingLevels>();
					if (!m)
                    {
						m = Run.instance.gameObject.AddComponent<MonsterMissingLevels>();
                    }
					m.trueLevel = Math.Min(targetLevel, MonsterMissingLevels.maxLevel);

					//This section handles levelup effects for levelups above the level cap, but after a certain point they get spammed so I disabled them.
					/*GameObject teamLevelUpEffect = TeamManager.GetTeamLevelUpEffect(teamIndex);
					string teamLevelUpSoundString = TeamManager.GetTeamLevelUpSoundString(teamIndex);
					ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex);
					for (int i = 0; i < teamMembers.Count; i++)
					{
						TeamComponent teamComponent = teamMembers[i];
						if (teamComponent)
						{
							CharacterBody component = teamComponent.GetComponent<CharacterBody>();
							if (component)
							{
								Transform transform = component.mainHurtBox ? component.mainHurtBox.transform : component.transform;
								EffectData effectData = new EffectData
								{
									origin = transform.position
								};
								if (component.mainHurtBox)
								{
									effectData.SetHurtBoxReference(component.gameObject);
									effectData.scale = component.radius;
								}
								GlobalEventManager.instance.StartCoroutine(GlobalEventManager.instance.CreateLevelUpEffect(UnityEngine.Random.Range(0f, 0.5f), teamLevelUpEffect, effectData));
							}
						}
					}
					if (teamMembers.Count > 0)
					{
						Util.PlaySound(teamLevelUpSoundString, RoR2Application.instance.gameObject);
					}*/
				}
            };

			//grab intended monster level from somewhere else
			On.RoR2.TeamManager.GetTeamLevel += (orig, self, teamIndex) =>
			{
				uint toReturn = orig(self, teamIndex);
				if (teamIndex == TeamIndex.Monster && toReturn == TeamManager.naturalLevelCap)
				{
					MonsterMissingLevels m = Run.instance.gameObject.GetComponent<MonsterMissingLevels>();
					if (m)
					{
						toReturn = m.trueLevel;
					}
				}
				return toReturn;
			};
		}
    }

    public class MonsterMissingLevels : NetworkBehaviour
    {
		[SyncVar]
		public uint trueLevel = 0;

		public static uint maxLevel = 4000000000;
	}
}
