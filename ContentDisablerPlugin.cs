using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using RoR2.ContentManagement;

using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace TPDespair.ContentDisabler
{
	[BepInPlugin(ModGuid, ModName, ModVer)]

	public class ContentDisablerPlugin : BaseUnityPlugin
	{
		public const string ModVer = "1.0.0";
		public const string ModName = "ContentDisabler";
		public const string ModGuid = "com.TPDespair.ContentDisabler";

		public static ConfigFile configFile;
		public static ManualLogSource logSource;

		public static Dictionary<string, int> ConfigKeys = new Dictionary<string, int>();



		public void Awake()
		{
			configFile = Config;
			logSource = Logger;

			On.RoR2.ItemCatalog.Init += ItemCatalogInit;
			On.RoR2.EquipmentCatalog.Init += EquipmentCatalogInit;
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
