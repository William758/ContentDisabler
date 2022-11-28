# ContentDisabler

This is a last resort for disabling thing in the game, you should use the config from source mod if available.

# Items and Equipment

Create a config for every item and equipment that allows hiding said item or equipment.

- Disabled Items and Equipment are removed from the logbook.

Prevents items and equipment from showing up anywhere that draws from the normal drop pool :

- Chests, Sacrifice, MultiShops, CommandMenus.

Probably does not prevent items from showing up by other means (untested) :

- HelperItems, FixedRewards, ItemTransformation, BossRewards

# Survivors

Create a config for every survivor that prevents survivor from being picked in character select or from rolling in metamorphosis.

- Disabled Survivors are removed from the logbook.

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

v1.1.0 - SpawnCard support.

v1.0.1 - Added Survivor disabling.

v1.0.0 - Initial Release.