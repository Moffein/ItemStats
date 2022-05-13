using BepInEx;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using static RoR2.Chat;

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}

//Based off of https://github.com/ontrigger/ItemStatsMod
namespace ItemStats
{
    [BepInPlugin("com.Moffein.ItemStats", "ItemStats", "1.1.0")]
    public class ItemStats : BaseUnityPlugin
    {
        public static bool pingDetails = true;
        public static bool pingDetailsVerbose = false;

        public static bool pingNotif = true;
        public static bool pingChat = false;

        //public static bool previewDesc = false;
        public static bool detailedHover = true;
        public static bool detailedPickup = true;

        public void Awake()
        {
            ReadConfig();
            if (detailedHover) On.RoR2.UI.ItemIcon.SetItemIndex += ItemIcon_SetItemIndex;
            if (detailedPickup) On.RoR2.UI.GenericNotification.SetItem += GenericNotification_SetItem;
            //if (previewDesc) On.RoR2.GenericPickupController.GetContextString += GenericPickupController_GetContextString;
            if (pingDetails) On.RoR2.PingerController.SetCurrentPing += PingerController_SetCurrentPing;
        }

        public void ReadConfig()
        {
            detailedHover = Config.Bind("Settings", "Detailed Hover", true, "Show full item description when hovering over the item icon.").Value;
            detailedPickup = Config.Bind("Settings", "Detailed Pickup", true, "Show full item description when picking up the item.").Value;

            //Disabled because it looks terrible, and Ping Details does this more elegantly.
            //previewDesc = Config.Bind("Settings", "Show Preview", false, "Show short item description in the interaction tooltip. Warning: causes text to become small.").Value;

            pingDetails = Config.Bind("Settings", "Ping Details", true, "Pinging an item shows its description.").Value;
            pingNotif = Config.Bind("Settings", "Ping Details - Show as Notification", true, "Item description shows as a notification on the HUD.").Value;
            pingChat = Config.Bind("Settings", "Ping Details - Show as Chat Message", false, "Item description shows as a chat message.").Value;
            pingDetailsVerbose = Config.Bind("Settings", "Ping Details - Show as Chat Message - Show Full Description", false, "Chat messages show the full item description.").Value;

            if (!pingChat && !pingNotif) pingDetails = false;
        }

        private void PingerController_SetCurrentPing(On.RoR2.PingerController.orig_SetCurrentPing orig, PingerController self, PingerController.PingInfo newPingInfo)
        {
            orig(self, newPingInfo);
            if (self.hasAuthority)
            {
                if (newPingInfo.targetGameObject)
                {
                    PickupDef pd = null;

                    GenericPickupController gpc = newPingInfo.targetGameObject.GetComponent<GenericPickupController>();
                    if (gpc)
                    {
                        pd = PickupCatalog.GetPickupDef(gpc.pickupIndex);
                    }
                    else
                    {
                        ShopTerminalBehavior stb = newPingInfo.targetGameObject.GetComponent<ShopTerminalBehavior>();
                        if (stb)
                        {
                            pd = PickupCatalog.GetPickupDef(stb.pickupIndex);
                        }
                    }

                    if (pd != null)
                    {
                        ItemDef id = ItemCatalog.GetItemDef(pd.itemIndex);
                        if (id)
                        {
                            if (pingChat)
                            {
                                Chat.AddMessage(new SimpleChatMessage
                                {
                                    baseToken = pingDetailsVerbose ? id.descriptionToken : id.pickupToken
                                });
                            }

                            if (pingNotif)
                            {
                                CharacterMaster cm = self.gameObject.GetComponent<CharacterMaster>();
                                if (cm)
                                {
                                    CharacterMasterNotificationQueue.PushItemNotification(cm, id.itemIndex);
                                }
                            }
                        }
                    }
                }
            }
        }

        /*public static string GenericPickupController_GetContextString(On.RoR2.GenericPickupController.orig_GetContextString orig, GenericPickupController self, Interactor activator)
        {
            string toReturn = orig(self, activator);
            PickupDef pd = PickupCatalog.GetPickupDef(self.pickupIndex);
            if (pd != null)
            {
                ItemDef id = ItemCatalog.GetItemDef(pd.itemIndex);
                if (id)
                {
                    toReturn += "\n" + Language.GetString(id.pickupToken);
                }
            }
            return toReturn;
        }*/

        public static void GenericNotification_SetItem(On.RoR2.UI.GenericNotification.orig_SetItem orig, GenericNotification self, ItemDef itemDef)
        {
            orig(self, itemDef);
            self.descriptionText.token = itemDef.descriptionToken;
        }

        public static void ItemIcon_SetItemIndex(On.RoR2.UI.ItemIcon.orig_SetItemIndex orig, ItemIcon self, ItemIndex newItemIndex, int newItemCount)
        {
            orig(self, newItemIndex, newItemCount);
            ItemDef id = ItemCatalog.GetItemDef(newItemIndex);
            if (id && self.tooltipProvider)
            {
                self.tooltipProvider.overrideBodyText = Language.GetString(id.descriptionToken);
            }
        }
    }
}
