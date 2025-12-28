using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace TPDespair.ContentDisabler
{
	[BepInPlugin(ModGuid, ModName, ModVer)]

	public class ContentDisablerPlugin : BaseUnityPlugin
	{
		public const string ModVer = "1.4.1";
		public const string ModName = "ContentDisabler";
		public const string ModGuid = "com.TPDespair.ContentDisabler";

		public static ConfigFile configFile;
		public static ManualLogSource logSource;

		public static ConfigEntry<bool> SkillFamilySafeguard { get; set; }
		public static ConfigEntry<bool> ScanConfigSectionsStartup { get; set; }
		public static ConfigEntry<bool> ScanConfigSectionsFinalize { get; set; }



		public static ConfigEntry<bool> ConfigItem;
		public static ConfigEntry<bool> ConfigEquipment;
		public static ConfigEntry<bool> ConfigArtifact;
		public static ConfigEntry<bool> ConfigDifficulty;
		public static ConfigEntry<bool> ConfigSurvivor;
		public static ConfigEntry<bool> ConfigSkill;
		public static ConfigEntry<bool> ConfigSkin;
		public static ConfigEntry<bool> ConfigBody;
		public static ConfigEntry<bool> ConfigSpawnCard;



		public static Dictionary<string, int> ConfigKeys = new Dictionary<string, int>();

		public static List<ArtifactDef> DisabledArtifacts = new List<ArtifactDef>();

		public static List<SurvivorDef> DisabledSurvivors = new List<SurvivorDef>();

		public static Dictionary<string, ConfigEntry<bool>> SkillConfigs = new Dictionary<string, ConfigEntry<bool>>();
		public static List<string> InvalidSkillStateNames = new List<string>();

		public static List<string> DisabledBodies = new List<string>();

		public static Dictionary<SpawnCard, string> SpawnCardBodyNames = new Dictionary<SpawnCard, string>();
		public static Dictionary<SpawnCard, ConfigEntry<bool>> SpawnCardConfigs = new Dictionary<SpawnCard, ConfigEntry<bool>>();



		public void Awake()
		{
			configFile = Config;
			logSource = Logger;



			GlobalConfig();

			if (ScanConfigSectionsStartup.Value)
			{
				ConfigStartup();
			}



			if (ConfigArtifact.Value || ConfigDifficulty.Value)
			{
				RoR2Application.onLoad += ExcludeRuleChoices;
			}

			if (ConfigItem.Value)
			{
				On.RoR2.ItemCatalog.Init += ItemCatalogInit;
			}

			if (ConfigEquipment.Value)
			{
				On.RoR2.EquipmentCatalog.Init += EquipmentCatalogInit;
			}

			if (ConfigArtifact.Value)
			{
				On.RoR2.ArtifactCatalog.Init += ArtifactCatalogInit;
			}

			if (ConfigSurvivor.Value)
			{
				On.RoR2.SurvivorCatalog.Init += SurvivorCatalogInit;
				On.RoR2.UI.LogBook.LogBookController.CanSelectSurvivorBodyEntry += LogBookControllerCanSelectSurvivorBodyEntry;
				IL.RoR2.CharacterMaster.PickRandomSurvivorBodyPrefab += CharacterMasterPickRandomSurvivorBodyPrefabHook;
			}

			if (ConfigSkill.Value)
			{
				On.RoR2.Skills.SkillCatalog.Init += SkillCatalogInit;
				RoR2Application.onLoad += SkillStrip2;
			}

			if (ConfigSkin.Value)
			{
				RoR2Application.onLoad += RemoveSkins;
			}

			if (ConfigBody.Value)
			{
				On.RoR2.BodyCatalog.Init += BodyCatalogInit;
				On.RoR2.UI.LogBook.LogBookController.CanSelectMonsterEntry += LogBookControllerCanSelectMonsterEntry;
			}

			if (ConfigSpawnCard.Value)
			{
				On.RoR2.ClassicStageInfo.RebuildCards += ClassicStageInfoRebuildCards;
				IL.RoR2.ClassicStageInfo.RebuildCards += ClassicStageInfoRebuildCardsHook;
				SceneDirector.onGenerateInteractableCardSelection += GatherInteractableCards;
				On.RoR2.SceneDirector.GenerateInteractableCardSelection += SceneDirectorGenerateInteractableCardSelection;
				On.RoR2.DirectorCard.IsAvailable += DirectorCardIsAvailable;
			}



			if (ScanConfigSectionsFinalize.Value)
			{
				RoR2Application.onLoad += ConfigFinalize;
			}
		}



		private static void GlobalConfig()
		{
			SkillFamilySafeguard = configFile.Bind(
				"00-General", "SkillFamilySafeguard", true,
				"Ensures that each skillFamily has at least one skill. Set to false to remove any SkillFamily that becomes empty."
			);

			ScanConfigSectionsStartup = configFile.Bind(
				"00-General", "ScanConfigSectionsStartup", true,
				"Check config entries on startup and enable Generate Config if any entry in that section is set to true."
			);

			ScanConfigSectionsFinalize = configFile.Bind(
				"00-General", "ScanConfigSectionsFinalize", true,
				"Check config entries on finalize and disable Generate Config if no entry in that section is set to true. Does not apply to SpawnCard."
			);



			ConfigItem = configFile.Bind(
				"01-Generate", "ConfigItem", false,
				"Generate Item configs and apply patches."
			);
			ConfigEquipment = configFile.Bind(
				"01-Generate", "ConfigEquipment", false,
				"Generate Equipment configs and apply patches."
			);
			ConfigArtifact = configFile.Bind(
				"01-Generate", "ConfigArtifact", false,
				"Generate Artifact configs and apply patches."
			);
			ConfigDifficulty = configFile.Bind(
				"01-Generate", "ConfigDifficulty", false,
				"Generate Difficulty configs and apply patches."
			);
			ConfigSurvivor = configFile.Bind(
				"01-Generate", "ConfigSurvivor", false,
				"Generate Survivor configs and apply patches."
			);
			ConfigSkill = configFile.Bind(
				"01-Generate", "ConfigSkill", false,
				"Generate Skill configs and apply patches."
			);
			ConfigSkin = configFile.Bind(
				"01-Generate", "ConfigSkin", false,
				"Generate Skin configs and apply patches."
			);
			ConfigBody = configFile.Bind(
				"01-Generate", "ConfigBody", false,
				"Generate Body configs and apply patches."
			);
			ConfigSpawnCard = configFile.Bind(
				"01-Generate", "ConfigSpawnCard", false,
				"Generate SpawnCard configs and apply patches."
			);
		}

		private static void ConfigStartup()
		{
			LogWarn("Scanning Config Entries [Startup]");

			Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(configFile);
			if (orphanedEntries == null || orphanedEntries.Count == 0)
			{
				LogWarn("There Are No Config Entries");

				return;
			}



			List<string> sectionsToEnable = new List<string>();

			foreach (var entry in orphanedEntries)
			{
				string rawValue = entry.Value;
				if (!string.IsNullOrEmpty(rawValue) && rawValue == "true")
				{
					ConfigDefinition def = entry.Key;
					if (def != null)
					{
						string section = def.Section;
						if (!sectionsToEnable.Contains(section))
						{
							LogWarn("Config Section: " + section + " Contains Disabled Entry");

							sectionsToEnable.Add(section);
						}
					}
				}
			}



			if (sectionsToEnable.Count == 0)
			{
				LogWarn("Found No Sections To Enable");

				return;
			}



			if (sectionsToEnable.Contains("Item") && !ConfigItem.Value)
			{
				LogWarn("Enabling Item Configs And Patches");
				ConfigItem.Value = true;
			}

			if (sectionsToEnable.Contains("Equipment") && !ConfigEquipment.Value)
			{
				LogWarn("Enabling Equipment Configs And Patches");
				ConfigEquipment.Value = true;
			}

			if (sectionsToEnable.Contains("Artifact") && !ConfigArtifact.Value)
			{
				LogWarn("Enabling Artifact Configs And Patches");
				ConfigArtifact.Value = true;
			}

			if (sectionsToEnable.Contains("Difficulty") && !ConfigDifficulty.Value)
			{
				LogWarn("Enabling Difficulty Configs And Patches");
				ConfigDifficulty.Value = true;
			}

			if (sectionsToEnable.Contains("Survivor") && !ConfigSurvivor.Value)
			{
				LogWarn("Enabling Survivor Configs And Patches");
				ConfigSurvivor.Value = true;
			}

			if (sectionsToEnable.Contains("Skill") && !ConfigSkill.Value)
			{
				LogWarn("Enabling Skill Configs And Patches");
				ConfigSkill.Value = true;
			}

			if (sectionsToEnable.Contains("Skin") && !ConfigSkin.Value)
			{
				LogWarn("Enabling Skin Configs And Patches");
				ConfigSkin.Value = true;
			}

			if (sectionsToEnable.Contains("Body") && !ConfigBody.Value)
			{
				LogWarn("Enabling Body Configs And Patches");
				ConfigBody.Value = true;
			}

			if (sectionsToEnable.Contains("SpawnCard") && !ConfigSpawnCard.Value)
			{
				LogWarn("Enabling SpawnCard Configs And Patches");
				ConfigSpawnCard.Value = true;
			}
		}

		private static void ConfigFinalize()
		{
			LogWarn("Scanning Config Entries [Finalize]");

			Dictionary<ConfigDefinition, ConfigEntryBase> entries = (Dictionary<ConfigDefinition, ConfigEntryBase>)typeof(ConfigFile).GetProperty("Entries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(configFile);
			if (entries == null || entries.Count == 0)
			{
				LogWarn("There Are No Config Entries");

				return;
			}



			List<string> sectionsToDisable = new List<string>(){ "Item", "Equipment", "Artifact", "Difficulty", "Survivor", "Skill", "Skin", "Body" };

			foreach (var entry in entries)
			{
				ConfigEntryBase entryBase = entry.Value;
				if (entryBase == null) continue;

				string rawValue = entryBase.GetSerializedValue();
				if (!string.IsNullOrEmpty(rawValue) && rawValue == "true")
				{
					ConfigDefinition def = entry.Key;
					if (def != null)
					{
						string section = def.Section;
						if (sectionsToDisable.Contains(section))
						{
							sectionsToDisable.Remove(section);
						}
					}
				}
			}



			if (sectionsToDisable.Count == 0)
			{
				LogWarn("Found No Sections To Disable");

				return;
			}



			foreach (string sec in sectionsToDisable)
			{
				LogWarn("Config Section: " + sec + " Is Unused");
			}



			if (sectionsToDisable.Contains("Item") && ConfigItem.Value)
			{
				LogWarn("Disabling Item Configs And Patches, Wont Apply Until Next Startup.");
				ConfigItem.Value = false;
			}

			if (sectionsToDisable.Contains("Equipment") && ConfigEquipment.Value)
			{
				LogWarn("Disabling Equipment Configs And Patches, Wont Apply Until Next Startup.");
				ConfigEquipment.Value = false;
			}

			if (sectionsToDisable.Contains("Artifact") && ConfigArtifact.Value)
			{
				LogWarn("Disabling Artifact Configs And Patches, Wont Apply Until Next Startup.");
				ConfigArtifact.Value = false;
			}

			if (sectionsToDisable.Contains("Difficulty") && ConfigDifficulty.Value)
			{
				LogWarn("Disabling Difficulty Configs And Patches, Wont Apply Until Next Startup.");
				ConfigDifficulty.Value = false;
			}

			if (sectionsToDisable.Contains("Survivor") && ConfigSurvivor.Value)
			{
				LogWarn("Disabling Survivor Configs And Patches, Wont Apply Until Next Startup.");
				ConfigSurvivor.Value = false;
			}

			if (sectionsToDisable.Contains("Skill") && ConfigSkill.Value)
			{
				LogWarn("Disabling Skill Configs And Patches, Wont Apply Until Next Startup.");
				ConfigSkill.Value = false;
			}

			if (sectionsToDisable.Contains("Skin") && ConfigSkin.Value)
			{
				LogWarn("Disabling Skin Configs And Patches, Wont Apply Until Next Startup.");
				ConfigSkin.Value = false;
			}

			if (sectionsToDisable.Contains("Body") && ConfigBody.Value)
			{
				LogWarn("Disabling Body Configs And Patches, Wont Apply Until Next Startup.");
				ConfigBody.Value = false;
			}
		}



		private static void ExcludeRuleChoices()
		{
			List<string> excluded = new List<string>();

			foreach (ArtifactDef artifactDef in DisabledArtifacts)
			{
				string name = artifactDef.cachedName;
				if (name != null && name != "") excluded.Add("Artifacts." + name);

				name = artifactDef.nameToken;
				if (name != null && name != "") excluded.Add(name);
			}

			if (ConfigDifficulty.Value)
			{
				RuleDef difficultyRuleDef = RuleCatalog.FindRuleDef("Difficulty");
				if (difficultyRuleDef != null)
				{
					foreach (RuleChoiceDef ruleDefChoice in difficultyRuleDef.choices)
					{
						string name = ruleDefChoice.tooltipNameToken;
						if (name == null || name == "") name = "UnknownDifficulty";

						if (name == "UnknownDifficulty")
						{
							LogWarn("Tried to create ConfigEntry for [" + name + "] but it does not have a valid name!");
						}
						else
						{
							ConfigEntry<bool> configEntry = ConfigEntry("Difficulty", name, false, "Disable Difficulty : " + name);
							if (configEntry.Value)
							{
								excluded.Add(name);

								LogWarn("Disabled Difficulty : " + name);
							}
						}
					}
				}
			}

			LogWarn("----- Hiding RuleCatalog Choices -----");

			foreach (RuleDef ruleDef in RuleCatalog.allRuleDefs)
			{
				if (ruleDef.choices != null && ruleDef.choices.Count > 0)
				{
					//LogWarn("RuleGlobalName : " + ruleDef.globalName);

					if (excluded.Contains(ruleDef.globalName) || excluded.Contains(ruleDef.displayToken))
					{
						foreach (RuleChoiceDef ruleDefChoice in ruleDef.choices)
						{
							ruleDefChoice.excludeByDefault = true;
						}

						LogWarn("Hiding RuleCatalog Entries For [" + ruleDef.globalName + " - " + ruleDef.displayToken + "]");
					}

					if (ruleDef.globalName == "Difficulty")
					{
						foreach (RuleChoiceDef ruleDefChoice in ruleDef.choices)
						{
							if (excluded.Contains(ruleDefChoice.tooltipNameToken))
							{
								ruleDefChoice.excludeByDefault = true;

								LogWarn("Hiding RuleCatalog Entry For [" + ruleDef.globalName + " - " + ruleDefChoice.tooltipNameToken + "]");
							}
						}
					}
				}
			}

			LogWarn("----- RuleCatalog Choices Hidden -----");
		}



		private static void ItemCatalogInit(On.RoR2.ItemCatalog.orig_Init orig)
		{
			foreach (ItemDef itemDef in ContentManager.itemDefs)
			{
				string name = itemDef.name;
				if (name == "") name = itemDef.nameToken;
				if (name == "") name = "UnknownItem";

				ConfigEntry<bool> configEntry = ConfigEntry("Item", name, false, "Disable Item : " + name);
				if (configEntry.Value)
				{
					itemDef._itemTierDef = null;
					AssignDepricatedTier(itemDef, ItemTier.NoTier);
					//itemDef.hidden = true;

					LogWarn("Disabled Item : " + name);
				}
			}

			orig();
		}

		private static void AssignDepricatedTier(ItemDef itemDef, ItemTier itemTier)
		{
			#pragma warning disable CS0618 // Type or member is obsolete
			itemDef.deprecatedTier = itemTier;
			#pragma warning restore CS0618 // Type or member is obsolete
		}



		private static void EquipmentCatalogInit(On.RoR2.EquipmentCatalog.orig_Init orig)
		{
			foreach (EquipmentDef equipDef in ContentManager.equipmentDefs)
			{
				string name = equipDef.name;
				if (name == "") name = equipDef.nameToken;
				if (name == "") name = "UnknownEquipment";

				ConfigEntry<bool> configEntry = ConfigEntry("Equipment", name, false, "Disable Equipment : " + name);
				if (configEntry.Value)
				{
					equipDef.canDrop = false;
					equipDef.appearsInSinglePlayer = false;
					equipDef.appearsInMultiPlayer = false;
					equipDef.canBeRandomlyTriggered = false;
					equipDef.enigmaCompatible = false;
					equipDef.dropOnDeathChance = 0f;

					LogWarn("Disabled Equipment : " + name);
				}
			}

			orig();
		}



		private void ArtifactCatalogInit(On.RoR2.ArtifactCatalog.orig_Init orig)
		{
			foreach (ArtifactDef artifactDef in ContentManager.artifactDefs)
			{
				string name = artifactDef.cachedName;
				if (name == "") name = artifactDef.nameToken;
				if (name == "") name = "UnknownArtifact";

				if (name == "UnknownArtifact")
				{
					LogWarn("Tried to create ConfigEntry for [" + name + "] but it does not have a valid name!");
				}
				else
				{
					ConfigEntry<bool> configEntry = ConfigEntry("Artifact", name, false, "Disable Artifact : " + name);
					if (configEntry.Value)
					{
						if (!DisabledArtifacts.Contains(artifactDef))
						{
							DisabledArtifacts.Add(artifactDef);
						}

						LogWarn("Disabled Artifact : " + name);
					}
				}
			}

			orig();
		}



		private static void SurvivorCatalogInit(On.RoR2.SurvivorCatalog.orig_Init orig)
		{
			foreach (SurvivorDef survivorDef in ContentManager.survivorDefs)
			{
				string name = survivorDef.cachedName;
				if (name == "") name = survivorDef.displayNameToken;
				if (name == "") name = "UnknownSurvivor";

				ConfigEntry<bool> configEntry = ConfigEntry("Survivor", name, false, "Disable Survivor : " + name);
				if (configEntry.Value)
				{
					survivorDef.hidden = true;

					if (!DisabledSurvivors.Contains(survivorDef))
					{
						DisabledSurvivors.Add(survivorDef);
					}

					LogWarn("Disabled Survivor : " + name);
				}
			}

			orig();
		}

		private static bool LogBookControllerCanSelectSurvivorBodyEntry(On.RoR2.UI.LogBook.LogBookController.orig_CanSelectSurvivorBodyEntry orig, CharacterBody body, Dictionary<RoR2.ExpansionManagement.ExpansionDef, bool> expansionAvailability)
		{
			if (body)
			{
				SurvivorDef survivorDef = SurvivorCatalog.GetSurvivorDef(SurvivorCatalog.GetSurvivorIndexFromBodyIndex(body.bodyIndex));
				if (survivorDef)
				{
					if (DisabledSurvivors.Contains(survivorDef)) return false;
				}
			}

			return orig(body, expansionAvailability);
		}

		private static void CharacterMasterPickRandomSurvivorBodyPrefabHook(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			bool found = c.TryGotoNext(
				x => x.MatchStloc(1)
			);

			if (found)
			{
				c.Index += 1;

				c.Emit(OpCodes.Ldloc, 1);
				c.EmitDelegate<Func<SurvivorDef[], SurvivorDef[]>>((survivorDefs) =>
				{
					List<SurvivorDef> survivorDefsList = survivorDefs.ToList();

					foreach (SurvivorDef survivorDef in DisabledSurvivors)
					{
						if (survivorDefsList.Contains(survivorDef))
						{
							survivorDefsList.Remove(survivorDef);

							string name = survivorDef.cachedName;
							if (name == "") name = survivorDef.displayNameToken;

							LogWarn("Removed Survivor [" + name + "] From Metamorph Choices.");
						}
					}

					return survivorDefsList.ToArray();
				});
				c.Emit(OpCodes.Stloc, 1);
			}
			else
			{
				LogWarn("PickRandomSurvivorHook Failed");
			}
		}



		private static void SkillCatalogInit(On.RoR2.Skills.SkillCatalog.orig_Init orig)
		{
			/*
			CreateSkillConfig("Valdo's Invalid Na'ma'e");
			CreateSkillConfig("[REDACTED]");
			CreateSkillConfig("Bingus = Beloved");
			CreateSkillConfig("\n\tStellaris At 8,\nwait no Stellaris at 8:30");
			CreateSkillConfig("  Fuahahahahaha!!!  ");
			CreateSkillConfig("  [Eve'ry[=] T\thi\nng is wr\"o\"ng [ ? = ! ] ");
			*/

			foreach (SkillDef skillDef in ContentManager.skillDefs)
			{
				string skillName = GetSkillName(skillDef);

				if (skillName == "" || skillName == "UnknownSkill")
				{
					string stateName = GetStateName(skillDef);

					LogInvalidSkillStateName(stateName);
				}
				else
				{
					CreateSkillConfig(skillName);
				}
			}

			LogWarn("----- Rebuilding SkillFamilies -----");

			List<SkillFamily.Variant> variants = new List<SkillFamily.Variant>();

			foreach (SkillFamily skillFamily in ContentManager.skillFamilies)
			{
				variants.Clear();

				uint originalIndex = skillFamily.defaultVariantIndex;
				uint newIndex = 0u;

				for (int i = 0; i < skillFamily.variants.Length; i++)
				{
					SkillFamily.Variant variant = skillFamily.variants[i];

					if (!variant.skillDef)
					{
						LogWarn("SkillFamilyVariant.skillDef is null !!!");

						string familyName = (skillFamily as ScriptableObject).name;
						if (familyName != null)
						{
							LogWarn("--SkillFamily Name: " + familyName);
						}

						var unlock = variant.unlockableDef;
						if (unlock != null && unlock.nameToken != null)
						{
							LogWarn("--Associated Unlock: " + unlock.nameToken);
						}

						variants.Add(variant);

						continue;
					}

					string skillName = GetSkillName(variant.skillDef);

					if (!IsSkillDisabled(skillName))
					{
						if (i == originalIndex) newIndex = (uint)variants.Count;

						variants.Add(variant);
					}
				}

				if (variants.Count == 0)
				{
					variants.Add(skillFamily.variants[originalIndex]);
				}

				skillFamily.defaultVariantIndex = newIndex;
				skillFamily.variants = variants.ToArray();
			}

			LogWarn("----- SkillFamilies Rebuilt -----");

			orig();
		}

		private static void SkillStrip2()
		{
			LogWarn("----- SkillStrip2 Start -----");

			List<SkillFamily.Variant> variants = new List<SkillFamily.Variant>();

			foreach (GameObject bodyPrefab in BodyCatalog.allBodyPrefabs)
			{
				if (bodyPrefab)
				{
					var genericSkills = bodyPrefab.GetComponents<GenericSkill>();
					foreach (GenericSkill genericSkill in genericSkills)
					{
						SkillFamily skillFamily = genericSkill.skillFamily;

						if (skillFamily)
						{
							variants.Clear();

							uint originalIndex = skillFamily.defaultVariantIndex;
							uint newIndex = 0u;

							for (int i = 0; i < skillFamily.variants.Length; i++)
							{
								SkillFamily.Variant variant = skillFamily.variants[i];

								string skillName = GetSkillName(variant.skillDef);

								if (skillName == "" || skillName == "UnknownSkill")
								{
									string stateName = GetStateName(variant.skillDef);

									LogInvalidSkillStateName(stateName);
								}
								else if (!SkillConfigs.ContainsKey(skillName))
								{
									CreateSkillConfig(skillName);
								}

								if (!IsSkillDisabled(skillName))
								{
									if (i == originalIndex) newIndex = (uint)variants.Count;

									variants.Add(variant);
								}
							}

							bool destroySkill = false;

							if (variants.Count == 0)
							{
								SkillDef defaultSkillDef = skillFamily.variants[originalIndex].skillDef;

								string skillName = GetSkillName(defaultSkillDef);

								LogWarn("[" + skillName + "] was enabled because its skill family has no options and it is the default skill variant.");

								variants.Add(skillFamily.variants[originalIndex]);

								if (!SkillFamilySafeguard.Value) destroySkill = true;
							}

							skillFamily.defaultVariantIndex = newIndex;
							skillFamily.variants = variants.ToArray();

							if (destroySkill)
							{
								SkillLocator skillLocator = bodyPrefab.GetComponent<SkillLocator>();
								if (skillLocator)
								{
									string bodyName = GetBodyName(bodyPrefab);

									LogWarn("Removing GenericSkill from SkillLocator of [" + bodyName + "].");

									if (skillLocator.primary == genericSkill) skillLocator.primary = null;
									if (skillLocator.secondary == genericSkill) skillLocator.secondary = null;
									if (skillLocator.utility == genericSkill) skillLocator.utility = null;
									if (skillLocator.special == genericSkill) skillLocator.special = null;

									// removes info on skills tab
									genericSkill.hideInCharacterSelect = true;

									// removes info on loadout tab
									DestroyImmediate(genericSkill);

									// probably causes some other shenanigans
								}
								else
								{
									LogWarn("Tried to remove GenericSkill but SkillLocator could not be found!");
								}
							}
						}
					}
				}
			}

			LogWarn("----- SkillStrip2 End -----");
		}

		private static string GetSkillName(SkillDef skillDef)
		{
			string name = skillDef.skillName;
			string token = skillDef.skillNameToken;

			token = token.Trim();
			name = name.Trim();

			string skillName = "UnknownSkill";
			if (token != "" && name != "")
			{
				skillName = token + " " + name;
			}
			else if (token != "")
			{
				skillName = token;
			}
			else if (name != "")
			{
				skillName = name;
			}

			return skillName.Trim();
		}

		private static string GetStateName(SkillDef skillDef)
		{
			string machine = skillDef.activationStateMachineName;
			string state = "";

			if (machine == null) machine = "";
			if (skillDef.activationState.stateType != null) state = skillDef.activationState.typeName;
			if (state == null) state = "";
			if (state.Contains(",")) state = state.Substring(0, state.IndexOf(","));

			machine = machine.Trim();
			state = state.Trim();

			string skillState = "UnknownState";
			if (machine != "" && state != "")
			{
				skillState = machine + " " + state;
			}
			else if (state != "")
			{
				skillState = state;
			}
			else if (machine != "")
			{
				skillState = machine;
			}

			return skillState.Trim();
		}

		private static void LogInvalidSkillStateName(string stateName)
		{
			stateName = stateName.Trim();

			if (!InvalidSkillStateNames.Contains(stateName))
			{
				InvalidSkillStateNames.Add(stateName);

				LogWarn("Tried to create ConfigEntry for [" + stateName + "] but its SkillDef does not have a valid name!");
			}
		}

		private static void CreateSkillConfig(string skillName)
		{
			if (skillName == "" || skillName == "UnknownSkill" || skillName == "UnknownState") return;

			if (!SkillConfigs.ContainsKey(skillName))
			{
				ConfigEntry<bool> configEntry = ConfigEntry("Skill", skillName, false, "Disable Skill : " + skillName);
				SkillConfigs.Add(skillName, configEntry);

				if (configEntry.Value) LogWarn("Disabled Skill : " + skillName);
			}
			else
			{
				LogWarn("Tried to create another ConfigEntry for [" + skillName +"]. Using the one already available.");
			}
		}

		private static bool IsSkillDisabled(string skillName)
		{
			if (skillName == "" || skillName == "UnknownSkill") return false;

			if (!SkillConfigs.ContainsKey(skillName)) return false;

			if (SkillConfigs[skillName].Value) return true;

			return false;
		}



		private static void RemoveSkins()
		{
			LogWarn("----- Rebuilding SkinDefs -----");

			List<SkinDef> skinList = new List<SkinDef>();

			foreach (GameObject bodyPrefab in BodyCatalog.allBodyPrefabs)
			{
				if (bodyPrefab)
				{
					bool modified = false;

					BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(bodyPrefab);
					SkinDef[] skinArray = SkinCatalog.GetBodySkinDefs(bodyIndex);

					if (skinArray == null)
					{
						LogWarn("No Skins found for : " + GetBodyName(bodyPrefab));

						continue;
					}

					if (skinArray.Length > 0)
					{
						skinList.Clear();
						string bodyName = GetBodyName(bodyPrefab);

						foreach (SkinDef skinDef in skinArray)
						{
							string name = skinDef.nameToken;
							if (name == "") name = "UnknownSkin";

							name = bodyName + " " + name;

							ConfigEntry<bool> configEntry = ConfigEntry("Skin", name, false, "Disable Skin : " + name);
							if (configEntry.Value)
							{
								modified = true;

								LogWarn("Disabled Skin : " + name);
							}
							else
							{
								skinList.Add(skinDef);
							}
						}

						if (modified)
						{
							skinArray = skinList.ToArray();

							BodyCatalog.skins[(int)bodyIndex] = skinArray;
							SkinCatalog.skinsByBody[(int)bodyIndex] = skinArray;

							ModelLocator modelLocator = bodyPrefab.GetComponent<ModelLocator>();
							if (modelLocator)
							{
								ModelSkinController skinController = modelLocator.modelTransform.gameObject.GetComponent<ModelSkinController>();
								if (skinController)
								{
									skinController.skins = skinArray;
								}
							}
						}
					}
				}
			}

			LogWarn("----- SkinDefs Rebuilt -----");
		}

		private static string GetBodyName(GameObject bodyPrefab)
		{
			CharacterBody charBody = bodyPrefab.GetComponent<CharacterBody>();
			if (charBody)
			{
				string name = charBody.name;
				if (name == "") name = charBody.baseNameToken;

				if (name != "") return name;
			}

			return "UnknownBody";
		}



		private static System.Collections.IEnumerator BodyCatalogInit(On.RoR2.BodyCatalog.orig_Init orig)
		{
			foreach (GameObject gameObject in ContentManager.bodyPrefabs)
			{
				CharacterBody charBody = gameObject.GetComponent<CharacterBody>();
				if (charBody)
				{
					string name = charBody.name;
					if (name == "") name = charBody.baseNameToken;
					if (name == "") name = "UnknownBody";

					ConfigEntry<bool> configEntry = ConfigEntry("Body", name, false, "Disable Body : " + name);

					if (configEntry.Value)
					{
						if (!DisabledBodies.Contains(name))
						{
							DisabledBodies.Add(name);
						}

						LogWarn("Disabled Body : " + name);
					}
				}
			}

			yield return orig();
		}

		private static bool LogBookControllerCanSelectMonsterEntry(On.RoR2.UI.LogBook.LogBookController.orig_CanSelectMonsterEntry orig, CharacterBody body, Dictionary<RoR2.ExpansionManagement.ExpansionDef, bool> expansionAvailability)
		{
			if (body)
			{
				string name = body.name;
				if (name == "") name = body.baseNameToken;

				if (DisabledBodies.Contains(name)) return false;
			}

			return orig(body, expansionAvailability);
		}



		private static void ClassicStageInfoRebuildCards(On.RoR2.ClassicStageInfo.orig_RebuildCards orig, ClassicStageInfo self, DirectorCardCategorySelection fmc, DirectorCardCategorySelection fic)
		{
			orig(self, fmc, fic);

			ProccessWeightedSelectionEntries(self.monsterSelection);
		}

		private static void ClassicStageInfoRebuildCardsHook(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			bool found = c.TryGotoNext(
				x => x.MatchCallOrCallvirt(typeof(DccsPool), "GenerateWeightedCategorySelection")
			);

			if (found)
			{
				c.Index += 1;

				c.EmitDelegate<Func<WeightedSelection<DccsPool.Category>, WeightedSelection<DccsPool.Category>>>((weiSel) =>
				{
					for (int i = weiSel.Count - 1; i >= 0; i--)
					{
						WeightedSelection<DccsPool.Category>.ChoiceInfo choiceInfo = weiSel.GetChoice(i);
						DccsPool.Category category = choiceInfo.value;

						if (ShouldRemoveCategory(category))
						{
							weiSel.RemoveChoice(i);

							LogWarn("Removing Invalid MonsterFamily From Selection.");
						}
					}

					return weiSel;
				});
			}
			else
			{
				LogWarn("RebuildCardsHook Failed");
			}
		}

		private static bool ShouldRemoveCategory(DccsPool.Category category)
		{
			DirectorCardCategorySelection[] dccsArray = GetAllAllowedFamilyCategoryDccs(category);
			if (dccsArray != null && dccsArray.Length > 0)
			{
				foreach (var dccs in dccsArray)
				{
					if (dccs != null && IsMonsterFamilyValid(dccs))
					{
						// there is a usable monster family in this category
						return false;
					}
				}

				// none of the monster families are valid
				return true;
			}

			// there are no monster families in this category
			return false;
		}

		private static DirectorCardCategorySelection[] GetAllAllowedFamilyCategoryDccs(DccsPool.Category category)
		{
			List<DirectorCardCategorySelection> dccsList = new List<DirectorCardCategorySelection>();

			DccsPool.PoolEntry[] array = category.alwaysIncluded;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].dccs != null && array[i].dccs as FamilyDirectorCardCategorySelection != null)
				{
					dccsList.Add(array[i].dccs);
				}
			}



			bool conditionsWereMet = false;

			DccsPool.ConditionalPoolEntry[] conditionalPoolEntries = category.includedIfConditionsMet;
			for (int i = 0; i < conditionalPoolEntries.Length; i++)
			{
				DccsPool.ConditionalPoolEntry conditionalPoolEntry = conditionalPoolEntries[i];

				if (conditionalPoolEntry.dccs != null && conditionalPoolEntry.dccs as FamilyDirectorCardCategorySelection != null)
				{
					if (conditionalPoolEntry.dccs.IsAvailable() && DCCSBlender.AreConditionsMet(conditionalPoolEntry))
					{
						conditionsWereMet = true;

						dccsList.Add(conditionalPoolEntry.dccs);
					}
				}
			}

			if (!conditionsWereMet)
			{
				array = category.includedIfNoConditionsMet;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].dccs != null && array[i].dccs as FamilyDirectorCardCategorySelection != null)
					{
						if (array[i].dccs.IsAvailable())
						{
							dccsList.Add(array[i].dccs);
						}
					}
				}
			}

			return dccsList.ToArray();
		}

		private static bool IsMonsterFamilyValid(DirectorCardCategorySelection dccs)
		{
			for (int i = 0; i < dccs.categories.Length; i++)
			{
				DirectorCardCategorySelection.Category catagory = dccs.categories[i];

				for (int j = catagory.cards.Length - 1; j >= 0; j--)
				{
					DirectorCard directorCard = catagory.cards[j];
					if (directorCard != null)
					{
						SpawnCard spawnCard = directorCard.spawnCard;
						if (spawnCard && spawnCard.prefab)
						{
							CreateSpawnCardConfig(spawnCard);

							if (IsCharacterSpawnCard(spawnCard))
							{
								// any characterSpawnCard in this family is still allowed to spawn so the family is valid
								if (!IsSpawnCardDisabled(spawnCard) && !DisabledBodies.Contains(SpawnCardBodyNames[spawnCard])) return true;
							}
						}
					}
				}
			}

			return false;
		}

		private static void GatherInteractableCards(SceneDirector sceneDirector, DirectorCardCategorySelection dccs)
		{
			for (int i = 0; i < dccs.categories.Length; i++)
			{
				DirectorCardCategorySelection.Category catagory = dccs.categories[i];

				for (int j = catagory.cards.Length - 1; j >= 0; j--)
				{
					DirectorCard directorCard = catagory.cards[j];
					if (directorCard != null)
					{
						SpawnCard spawnCard = directorCard.spawnCard;
						if (spawnCard)
						{
							CreateSpawnCardConfig(spawnCard);
						}
					}
				}
			}
		}

		private static WeightedSelection<DirectorCard> SceneDirectorGenerateInteractableCardSelection(On.RoR2.SceneDirector.orig_GenerateInteractableCardSelection orig, SceneDirector self)
		{
			WeightedSelection<DirectorCard> selection = orig(self);

			ProccessWeightedSelectionEntries(selection);

			return selection;
		}

		private static void ProccessWeightedSelectionEntries(WeightedSelection<DirectorCard> selection)
		{
			if (selection == null) return;
			
			for (int i = 0; i < selection.Count; i++)
			{
				WeightedSelection<DirectorCard>.ChoiceInfo choiceInfo = selection.GetChoice(i);

				DirectorCard directorCard = choiceInfo.value;
				if (directorCard != null)
				{
					SpawnCard spawnCard = directorCard.spawnCard;
					if (spawnCard)
					{
						CreateSpawnCardConfig(spawnCard);
					}
				}
			}
		}

		private static bool DirectorCardIsAvailable(On.RoR2.DirectorCard.orig_IsAvailable orig, DirectorCard self)
		{
			if (self.spawnCard && self.spawnCard.prefab)
			{
				SpawnCard spawnCard = self.spawnCard;

				CreateSpawnCardConfig(spawnCard);

				if (IsCharacterSpawnCard(spawnCard))
				{
					if (DisabledBodies.Contains(SpawnCardBodyNames[spawnCard])) return false;
				}

				if (IsSpawnCardDisabled(spawnCard)) return false;
			}

			return orig(self);
		}

		private static bool IsCharacterSpawnCard(SpawnCard spawnCard)
		{
			if (spawnCard == null) return false;

			if (!SpawnCardBodyNames.ContainsKey(spawnCard))
			{
				CharacterMaster charMaster = spawnCard.prefab.GetComponent<CharacterMaster>();
				if (charMaster && charMaster.bodyPrefab)
				{
					CharacterBody charBody = charMaster.bodyPrefab.GetComponent<CharacterBody>();
					if (charBody)
					{
						string name = charBody.name;
						if (name == "") name = charBody.baseNameToken;

						SpawnCardBodyNames.Add(spawnCard, name);
					}
				}
			}

			return SpawnCardBodyNames.ContainsKey(spawnCard);
		}

		private static void CreateSpawnCardConfig(SpawnCard spawnCard)
		{
			if (spawnCard == null)
			{
				LogWarn("Tried to create ConfigEntry for null SpawnCard!");
				return;
			}

			if (!SpawnCardConfigs.ContainsKey(spawnCard))
			{
				string name = spawnCard.name;
				if (name == "") name = "UnknownSpawnCard";

				if (name == "UnknownSpawnCard")
				{
					LogWarn("Tried to create ConfigEntry for [" + name + "] but it does not have a valid name!");
					GameObject prefab = spawnCard.prefab;
					if (prefab)
					{
						name = prefab.name;
						if (name != null && name != "")
						{
							LogWarn("--- PrefabName: " + prefab.name);
						}
					}
					
				}
				else
				{
					ConfigEntry<bool> configEntry = ConfigEntry("SpawnCard", name, false, "Disable SpawnCard : " + name);
					SpawnCardConfigs.Add(spawnCard, configEntry);

					if (configEntry.Value) LogWarn("Disabled SpawnCard : " + name);
				}
			}
		}

		private static bool IsSpawnCardDisabled(SpawnCard spawnCard)
		{
			if (spawnCard == null) return true;

			if (!SpawnCardConfigs.ContainsKey(spawnCard)) return false;

			if (SpawnCardConfigs[spawnCard].Value) return true;

			return false;
		}



		private static ConfigEntry<bool> ConfigEntry(string section, string key, bool defaultValue, string description)
		{
			string fullConfigKey = section + "_" + key;
			key += ValidateConfigKey(fullConfigKey);
			key = SanitizeConfigKey(key);

			ConfigEntry<bool> configEntry = configFile.Bind(section, key, defaultValue, description);

			return configEntry;
		}

		private static string ValidateConfigKey(string configKey)
		{
			if (!ConfigKeys.ContainsKey(configKey))
			{
				ConfigKeys.Add(configKey, 1);

				return "";
			}
			else
			{
				LogWarn("ConfigEntry for " + configKey + " already exists!");

				int value = ConfigKeys[configKey];

				ConfigKeys[configKey] += 1;

				return "_" + value;
			}
		}

		private static readonly char[] InvalidConfigChars = { '=', '\n', '\t', '\\', '"', '\'', '[', ']' };

		private static string SanitizeConfigKey(string configKey)
		{
			bool cleaned = false;

			if (configKey.Any(c => InvalidConfigChars.Contains(c)))
			{
				LogWarn("ConfigEntry [" + configKey + "] contains invalid characters!");

				configKey = RemoveInvalidChars(configKey);

				cleaned = true;
			}

			string trimmedKey = configKey.Trim();

			if (configKey != trimmedKey)
			{
				LogWarn("ConfigEntry [" + configKey + "] contains trimable whitespace characters!");

				configKey = trimmedKey;

				cleaned = true;
			}

			if (cleaned)
			{
				LogWarn("ConfigEntry key has been changed to [" + configKey + "].");
			}

			return configKey;
		}

		private static string RemoveInvalidChars(string str)
		{
			StringBuilder sb = new StringBuilder();

			foreach (char c in str)
			{
				if (!InvalidConfigChars.Contains(c)) sb.Append(c);
			}

			return sb.ToString();
		}



		private static void LogWarn(object data)
		{
			logSource.LogWarning(data);
		}
	}
}
