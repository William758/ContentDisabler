# ContentDisabler

This is a last resort for disabling things in the game, you should use the config from source mod if available.

Each type of content has a config that needs to be enable to generate configs for that section.

Config sections are disabled by default, existing config file will automatically enable edited sections.

# Items and Equipment

Create a config for every item and equipment that allows hiding said item or equipment.

- Disabled Items and Equipment are removed from the logbook.

Prevents items and equipment from showing up anywhere that draws from the normal drop pool :

- Chests, Sacrifice, MultiShops, CommandMenus.

Probably does not prevent items from showing up by other means (untested) :

- HelperItems, FixedRewards, ItemTransformation, BossRewards

# Artifacts

Create a config for every artifact that prevents artifact from being selected in the lobby.

- Only prevents choice in lobby, artifact can be enabled from effects that can enable artifacts during a run.

# Difficulties

Create a config for every difficulty that prevents difficulty from being selected in the lobby.

# Survivors

Create a config for every survivor that prevents survivor from being picked in character select or from rolling in metamorphosis.

- Disabled Survivors are removed from the logbook.

# Skills

Create a config for every named skill that removes said skill from all SkillFamilies.

- Intended for removal of selectable skills in lobby, not sure what might break from disabling other skills.

- If all skills get removed from a SkillFamily, then the default skill will still be available.

- Config to remove empty SkillFamily (disabled by default)

# Skins

Create a config for every skin to remove said skin from being chosen.

# Bodies and SpawnCards

Allows the disabling of monsters and interactables spawned by the Director.

- Removing too many things might break things idk? (use caution)

Generates a config entry for all bodies.

- Disabled bodies are removed from the log book.

- Prevents all SpawnCards that would spawn this body from being used, to disable only some variants of a monster instead look in the [SpawnCard] config section.

- These configs only affect monsters spawned in using SpawnCards, so some configs will do absolutely nothing.

- If you want to disable purchasable drones from spawning, instead look in the [SpawnCard] config section.

Generate a config for every spawn card used by the Director.

- There is no catalog for SpawnCards, so config entries are generated when visiting a stage or when checking if the SpawnCard can be used.

- Some SpawnCards have different variations that you may also want to disable. (for example snow covered variations of monsters and interactables)

- CharacterSpawnCards (starts with csc in config name) allows prevention of specific variants of a monster from spawning.

- InteractableSpawnCards (starts with isc in config name) allows prevention of specific variants of interactables from spawning. (includes purchasable drones)

## Installation:

Requires Bepinex and HookGenPatcher.

Use r2modman or place inside of Risk of Rain 2/Bepinex/Plugins/

## Changelog:

v1.3.3 - Added generate configs for each content type. Avoid generating configs for sections that have not been changed.

v1.3.2 - Nullcheck SpawnCards.

v1.3.1 - Fixed Skin disabling.

v1.3.0 - Updated for SotS.

v1.2.4 - Added support for Difficulties. Config to remove empty SkillFamily.

v1.2.3 - Added an additional check to find and disable skills from some mods that weren't being detected before.

v1.2.2 - Added Skin disabling.

v1.2.1 - Added a safeguard against invalid config names preventing the game from loading.

v1.2.0 - Unnamed SpawnCards no longer generate ConfigEntries. Added support for Artifacts and Skills.

v1.1.0 - SpawnCard support.

v1.0.1 - Added Survivor disabling.

v1.0.0 - Initial Release.