using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using RoR2.ContentManagement;

using System.Security;
using System.Security.Permissions;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using System.Linq;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace TPDespair.ContentDisabler
{
	[BepInPlugin(ModGuid, ModName, ModVer)]

	public class ContentDisablerPlugin : BaseUnityPlugin
	{
		public const string ModVer = "1.0.1";
		public const string ModName = "ContentDisabler";
		public const string ModGuid = "com.TPDespair.ContentDisabler";

		public static ConfigFile configFile;
		public static ManualLogSource logSource;

		public static Dictionary<string, int> ConfigKeys = new Dictionary<string, int>();

		public static List<SurvivorDef> DisabledSurvivors = new List<SurvivorDef>();



		public void Awake()
		{
			configFile = Config;
			logSource = Logger;

			On.RoR2.ItemCatalog.Init += ItemCatalogInit;
			On.RoR2.EquipmentCatalog.Init += EquipmentCatalogInit;
			On.RoR2.SurvivorCatalog.Init += SurvivorCatalogInit;
			On.RoR2.UI.LogBook.LogBookController.CanSelectSurvivorBodyEntry += LogBookControllerCanSelectSurvivorBodyEntry;
            IL.RoR2.CharacterMaster.PickRandomSurvivorBodyPrefab += CharacterMasterPickRandomSurvivorBodyPrefab;
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

		private static void SurvivorCatalogInit(On.RoR2.SurvivorCatalog.orig_Init orig)
		{
			foreach (SurvivorDef survivorDef in ContentManager.survivorDefs)
			{
				string name = ((ScriptableObject)survivorDef).name;
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

		private bool LogBookControllerCanSelectSurvivorBodyEntry(On.RoR2.UI.LogBook.LogBookController.orig_CanSelectSurvivorBodyEntry orig, CharacterBody body, Dictionary<RoR2.ExpansionManagement.ExpansionDef, bool> expansionAvailability)
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

		private void CharacterMasterPickRandomSurvivorBodyPrefab(ILContext il)
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

							//LogWarn("Removed Survivor [" + name + "] from Metamorph Choices.");
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



		internal static ConfigEntry<bool> ConfigEntry(string section, string key, bool defaultValue, string description)
		{
			string fullConfigKey = section + "_" + key;
			string extra = ValidateConfigKey(fullConfigKey);
			key += extra;
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

		internal static void AssignDepricatedTier(ItemDef itemDef, ItemTier itemTier)
		{
			#pragma warning disable CS0618 // Type or member is obsolete
			itemDef.deprecatedTier = itemTier;
			#pragma warning restore CS0618 // Type or member is obsolete
		}




		internal static void LogWarn(object data)
		{
			logSource.LogWarning(data);
		}
	}
}
