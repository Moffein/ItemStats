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
    [BepInPlugin("com.Moffein.ItemStats", "ItemStats", "1.3.1")]
    public class ItemStats : BaseUnityPlugin
    {
        public static List<ItemDef> IgnoredItems = new List<ItemDef> { };
        public static List<EquipmentDef> IgnoredEquipment = new List<EquipmentDef> { };

        public static bool pingDetails = true;
        public static bool pingDetailsVerbose = false;
        public static float pingDetailsDuration = 3f;

        public static bool pingNotif = true;
        public static bool pingChat = false;


        //public static bool previewDesc = false;
        public static bool detailedHover = true;
        public static bool detailedPickup = true;

        public void Awake()
        {
            ReadConfig();
            if (detailedHover)
            {
                On.RoR2.UI.ItemIcon.SetItemIndex += ItemIcon_SetItemIndex;
                On.RoR2.UI.EquipmentIcon.Update += EquipmentIcon_Update;    //Find something more efficient to hook
            }
            if (detailedPickup)
            {
                On.RoR2.UI.GenericNotification.SetItem += GenericNotification_SetItem;
                On.RoR2.UI.GenericNotification.SetEquipment += GenericNotification_SetEquipment;
            }
            if (pingDetails) On.RoR2.PingerController.SetCurrentPing += PingerController_SetCurrentPing;
        }

        public void ReadConfig()
        {
            detailedHover = Config.Bind("Settings", "Detailed Hover", true, "Show full item description when hovering over the item icon.").Value;
            detailedPickup = Config.Bind("Settings", "Detailed Pickup", true, "Show full item description when picking up the item.").Value;

            pingDetailsDuration = Config.Bind("Settings", "Ping Details - Notification Duration", 3f, "How long the item notification lasts for.").Value;
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
                        if (stb && !stb.pickupIndexIsHidden && !stb.Networkhidden && stb.pickupDisplay)
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
                                    baseToken = (pingDetailsVerbose && !IgnoredItems.Contains(id)) ? id.descriptionToken : id.pickupToken
                                });
                            }

                            if (pingNotif)
                            {
                                CharacterMaster cm = self.gameObject.GetComponent<CharacterMaster>();
                                if (cm)
                                {
                                    PushItemNotificationDuration(cm, id.itemIndex, pingDetailsDuration);
                                }
                            }
                        }
                        else
                        {
                            EquipmentDef ed = EquipmentCatalog.GetEquipmentDef(pd.equipmentIndex);
                            if (ed)
                            {
                                if (pingChat)
                                {
                                    Chat.AddMessage(new SimpleChatMessage
                                    {
                                        baseToken = (pingDetailsVerbose && !IgnoredEquipment.Contains(ed)) ? ed.descriptionToken : ed.pickupToken
                                    });
                                }

                                if (pingNotif)
                                {
                                    CharacterMaster cm = self.gameObject.GetComponent<CharacterMaster>();
                                    if (cm)
                                    {
                                        PushEquipmentNotificationDuration(cm, ed.equipmentIndex, pingDetailsDuration);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void GenericNotification_SetItem(On.RoR2.UI.GenericNotification.orig_SetItem orig, GenericNotification self, ItemDef itemDef)
        {
            orig(self, itemDef);
            if (!IgnoredItems.Contains(itemDef))
            {
                if (!Language.IsTokenInvalid(itemDef.descriptionToken))
                {
                    self.descriptionText.token = itemDef.descriptionToken;
                }
                else
                {
                    self.descriptionText.token = itemDef.pickupToken;
                }
            }
        }

        private void GenericNotification_SetEquipment(On.RoR2.UI.GenericNotification.orig_SetEquipment orig, GenericNotification self, EquipmentDef equipmentDef)
        {
            orig(self, equipmentDef);
            if (!IgnoredEquipment.Contains(equipmentDef))
            {
                if (!Language.IsTokenInvalid(equipmentDef.descriptionToken))
                {
                    self.descriptionText.token = equipmentDef.descriptionToken;
                }
                else
                {
                    self.descriptionText.token = equipmentDef.pickupToken;
                }
            }
        }

        public static void ItemIcon_SetItemIndex(On.RoR2.UI.ItemIcon.orig_SetItemIndex orig, ItemIcon self, ItemIndex newItemIndex, int newItemCount)
        {
            orig(self, newItemIndex, newItemCount);
            ItemDef id = ItemCatalog.GetItemDef(newItemIndex);
            if (id && self.tooltipProvider)
            {
                if (!IgnoredItems.Contains(id))
                {
                    if (!Language.IsTokenInvalid(id.descriptionToken))
                    {
                        self.tooltipProvider.overrideBodyText = Language.GetString(id.descriptionToken);
                    }
                    else
                    {
                        self.tooltipProvider.overrideBodyText = id.pickupToken;
                    }
                }
            }
        }

        private void EquipmentIcon_Update(On.RoR2.UI.EquipmentIcon.orig_Update orig, EquipmentIcon self)
        {
            orig(self);
            if (self.hasEquipment && self.tooltipProvider)
            {
                if (self.targetEquipmentSlot)
                {
                    EquipmentDef ed = EquipmentCatalog.GetEquipmentDef(self.targetEquipmentSlot.equipmentIndex);
                    if (ed && !IgnoredEquipment.Contains(ed))
                    {
                        if (!Language.IsTokenInvalid(ed.descriptionToken))
                        {
                            self.tooltipProvider.overrideBodyText = Language.GetString(ed.descriptionToken);
                        }
                        else
                        {
                            self.tooltipProvider.overrideBodyText = Language.GetString(ed.pickupToken);
                        }
                    }
                }
            }
        }

        public static void PushItemNotificationDuration(CharacterMaster characterMaster, ItemIndex itemIndex, float duration)
        {
            if (!characterMaster.hasAuthority)
            {
                Debug.LogError("Can't PushItemNotification for " + Util.GetBestMasterName(characterMaster) + " because they aren't local.");
                return;
            }
            CharacterMasterNotificationQueue notificationQueueForMaster = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(characterMaster);
            if (notificationQueueForMaster && itemIndex != ItemIndex.None)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef == null || itemDef.hidden)
                {
                    return;
                }
                notificationQueueForMaster.PushNotification(new CharacterMasterNotificationQueue.NotificationInfo(ItemCatalog.GetItemDef(itemIndex), null), duration);
            }
        }

        public static void PushEquipmentNotificationDuration(CharacterMaster characterMaster, EquipmentIndex equipmentIndex, float duration)
        {
            if (!characterMaster.hasAuthority)
            {
                Debug.LogError("Can't PushEquipmentNotification for " + Util.GetBestMasterName(characterMaster) + " because they aren't local.");
                return;
            }
            CharacterMasterNotificationQueue notificationQueueForMaster = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(characterMaster);
            if (notificationQueueForMaster && equipmentIndex != EquipmentIndex.None)
            {
                notificationQueueForMaster.PushNotification(new CharacterMasterNotificationQueue.NotificationInfo(EquipmentCatalog.GetEquipmentDef(equipmentIndex), null), duration);
            }
        }
    }
}
