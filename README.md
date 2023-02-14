# Create-And-Filter-Crafting-Recipies
Do you hate having 12 thousand crafting recipes at once crashing your game when you accidently click on the anvil when you want that stupid iron ingot off the anvil? Of course you do, I do as well.

This will generate crafting recipes based on Keywords applied and add a condition to the the crafting recipe to block it from showing unless you hold the crafting book for the mod.

The Book you will need to get using something like AddItemMenu or find it using help command. It will always be called "[Mod plugin Name] Crafting book".

It is a console application that needs you to input, as command line arguments, the absolute path to the plugin and the output folder path.

A base bat file to run it will be uploaded.

Example:
CreateAndFilterCraftingRecipies.exe "E:\SkyrimMods\MO2\mods\xEdit Cache\test.esp" "E:\SkyrimMods\MO2\mods\xEdit Cache"

You can increase and decrease the amount of materials for new recipes to what ever you would like by editing the json inside the properties folder.
