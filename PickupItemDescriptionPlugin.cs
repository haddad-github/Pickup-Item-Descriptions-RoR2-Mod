//BepInEx for mod integration
//HarmonyLib for altering methods at their execution
//RoR2 & RoR2 for RoR2 game objects and UI management
//UnityEngine for game interaction
using BepInEx;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using UnityEngine;

//Defines the plugin for BepInPlugin to load it and the metadata for mod management
//Format: [BepInPlugon("UNIQUE_PLUGIN_ID", "MOD NAME", "VERSION")]
[BepInPlugin("com.gigahanma.PickupItemDescription", "Add Description To Pickup Item", "1.0.0")]

//The mod's class inherits from BepInEx's BaseUnityPlugin class..
//..gains the functionalities of Unity MonoBehavior (has initialization (i.e. Awake), has physics, input events, etc.)..
//..also has BepInEx features such as logging
public class PickupItemDescriptionPlugin : BaseUnityPlugin
{
    //Initiliazes the mod at the "Awake" lifecycle of the game (effectively at the start of the game)
    private void Awake()
    {
        //Creates an instance of Harmony (library used for manipulating game behavior without touching original files) for this specific mod
        //Info.Metadata.GUID is the unique identifier of this specific mod found in BepInPlugin (as to avoid conflicting with other mods)
        var harmony = new Harmony(Info.Metadata.GUID);

        //Haromy finds all the patches defined in this script and applies them all
        //Patch --> modifications to existing game methods
        harmony.PatchAll();
    }
}

//Specify where Harmony should apply modifications..
//..to which class the patch(es) will be applied
[HarmonyPatch(typeof(ContextManager))]

//..more specifically, to which method in the aformentioned class will they be applied (the Update method in the ContextManager class)
[HarmonyPatch("Update")]

//Class that represents the patching
public static class UpdateItemDescriptionPatch
{
    //It's a postfix patch, meaning this code after the original method has ran (usually used to change the results of a method)..
    //..therefore instructs Harmony to run the following method after the original method
    //*Note: 2 other patch types are prefix (runs before original method (i.e. checks for conditions) and transpiler (changes instructions within the original method)
    [HarmonyPostfix]

    //We create the method for it (takes an instance of the class that'll be patched; here it's ContextManager)
    public static void Postfix(ContextManager __instance)
    {
        //Checks if the that context menu contains the word "Get"; only happens when trying to pick up an item, which indicates we're looking at an item to be picked up
        if (__instance.descriptionTMP.text.Contains("Get"))
        {
            //CharacterBody is a Unity component that contains data about the character..
            //..more specifically, we're using it as a vessel to check the character's HUD..and in turn what he's interacting with (target body object)
            //Null checkpoints (like hud?, etc.) to return null instead of running into a runtime error
            CharacterBody body = __instance.hud?.targetBodyObject ? __instance.hud.targetBodyObject.GetComponent<CharacterBody>() : null;

            //If the HUD is indeed targeting the player.. (so body is not null)
            if (body != null)
            {
                //Extract the interactable object
                InteractionDriver interactionDriver = body.GetComponent<InteractionDriver>();

                //If it does indeed exist..
                if (interactionDriver != null)
                {
                    //Make sure that the interactable object is a pickup prompt
                    IInteractable interactable = interactionDriver.FindBestInteractableObject()?.GetComponent<IInteractable>();

                    //If it is indeed...
                    if (interactable is GenericPickupController pickupController)
                    {
                        //Use PickupCatalog's GetPickupDef method to retrieve the item's index
                        PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupController.pickupIndex);

                        //If there is indeed an item/item index..
                        if (pickupDef != null)
                        {
                            
                            //Feed the item index to the ItemCatalog's GetItemDef method in order to retrieve the item's data (where the description is stored)
                            ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);

                            //If the item data exists..
                            if (itemDef != null)
                            {
                                //Use Language's GetString method to extract the item's description from the item's data
                                string itemDescription = Language.GetString(itemDef.descriptionToken);

                                //Append the item's description surrounded by parentheses to the final string that is displayed to the user (descriptionTMP)
                                __instance.descriptionTMP.text += $" ({itemDescription})";
                            }
                        }
                    }
                }
            }
        }
    }
}
