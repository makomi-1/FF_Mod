using MelonLoader;
using HarmonyLib;
using UnityEngine;
using static MelonLoader.MelonLogger;
using static VillagerOccupation;
using System.Collections.Generic;
using System.Collections;
using static CrowdAnimationTransition;
using static Unity.Burst.Intrinsics.Arm;
/*1.Object Pool Optimization*/



[assembly: MelonInfo(typeof(Display_military_companies_on_the_minimap.DMCM), "Display military companies on the minimap", "1.1.0", "Truth S4id", null)]
[assembly: MelonGame("Crate Entertainment", "Farthest Frontier")]

namespace Display_military_companies_on_the_minimap
{
    public class DMCM : MelonMod
    {
        private MelonPreferences_Category MinimapPOI_Cfg;
        private readonly string modName = "Display military companies on the minimap";
        public static MelonPreferences_Entry<int> priorityCfg;
        public static MelonPreferences_Entry<float> scaleCfg;
        public static MelonPreferences_Entry<bool> enableAddSubPOICfg;
        public static MelonPreferences_Entry<float> relativeScaleCfg;
        public static MelonPreferences_Entry<KeyCode> switchKeyCfg;
        public static List<VillagerOccupationSoldier> banneredSoldiersIsVisited = [];
        

        public class MoreMethodPOI
        {
            public static void StaticSoldierAddSubPOI(VillagerOccupationSoldier soldier)
            {
                foreach (SoldierDivision soldierDivision in soldier.militaryCompany.divisions)
                {
                    foreach (VillagerOccupationSoldier villagerOccupationSoldier in soldierDivision.soldiers)
                    {
                        if (villagerOccupationSoldier == null)
                        {
                            MelonLogger.Msg("villagerOccupationSoldier is null");
                        }
                        else if (villagerOccupationSoldier != soldier)
                        {
                            SoldierAddSubPOI(villagerOccupationSoldier);
                        }
                    }
                }
            }
            public static void StaticSoldierRemoveSubPOI()
            {
                banneredSoldiersIsVisited.RemoveAll(item => item.villager == null);
                foreach (VillagerOccupationSoldier soldier in banneredSoldiersIsVisited)
                {
                    foreach (SoldierDivision soldierDivision in soldier.militaryCompany.divisions)
                    {
                        foreach (VillagerOccupationSoldier villagerOccupationSoldier in soldierDivision.soldiers)
                        {
                            if (villagerOccupationSoldier != soldier)
                            {
                                MinimapPOI component = villagerOccupationSoldier.villager.gameObject.GetComponent<MinimapPOI>();
                                if (component != null)
                                {
                                    UnityEngine.Object.Destroy(component);
                                    MelonLogger.Msg($"Remove subPOI of soldier: {villagerOccupationSoldier.villager.name}");

                                }
                            }
                        }
                    }

                }

            }
            public static void SoldierAddSubPOI(VillagerOccupationSoldier soldier)
            {
                MinimapPOI orAddComponent = soldier.villager.gameObject.GetOrAddComponent<MinimapPOI>();
                orAddComponent.enabled = false;
                orAddComponent.poiType = POIType.RaidBanner;
                orAddComponent.priority = DMCM.priorityCfg.Value + 100;
                orAddComponent.iconNonVisited = soldier.militaryCompany.bannerSprite;
                orAddComponent.isInFoW = false;
                orAddComponent.miniMapIconScale = (float)(DMCM.scaleCfg.Value * DMCM.relativeScaleCfg.Value);
                orAddComponent.showEnemyPulse = false;
                orAddComponent.enabled = true;
                MelonLogger.Msg($"Added subPOI to soldier: {soldier.villager.name}");
            }

        }
        public void ConfigureMiniPOI()
        {
            this.MinimapPOI_Cfg = MelonPreferences.CreateCategory(this.modName);
            this.MinimapPOI_Cfg.SetFilePath("UserData/" + this.modName + ".cfg");
            DMCM.priorityCfg = this.MinimapPOI_Cfg.CreateEntry<int>("priority", 300, null, "This is the configuration of the priority and scale of the military banner icon.", false, false, null, null);
            DMCM.scaleCfg = this.MinimapPOI_Cfg.CreateEntry<float>("scale", 1.3f, null, null, false, false, null, null);
            DMCM.enableAddSubPOICfg = this.MinimapPOI_Cfg.CreateEntry<bool>("enableAddSubPOI", false, null, "Display the non-banner-wearing soldiers of each military company", false, false, null, null);
            DMCM.relativeScaleCfg = this.MinimapPOI_Cfg.CreateEntry<float>("relative_scale", 0.6f, null, "The scaling ratio of the icon of non-bannwe-wielding soldiers compared to the icon of banner-wielding soldiers", false, false, null, null);
            DMCM.switchKeyCfg = this.MinimapPOI_Cfg.CreateEntry<KeyCode>("switchKey", KeyCode.F10, null, "The key for switching the display mode of the icons", false, false, null, null);
            MelonLogger.Msg(".cfg has created or loaded");

        }
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
            
        }
        [Obsolete]
        public override void OnApplicationStart()
        {
            this.ConfigureMiniPOI();
        }
        
        public override void OnUpdate()
        {              
            if (Input.GetKeyDown(switchKeyCfg.Value))
            {
                enableAddSubPOICfg.Value = !enableAddSubPOICfg.Value;
                if (enableAddSubPOICfg.Value == true)
                {
                    banneredSoldiersIsVisited.RemoveAll(item => item.villager == null);
                    foreach (VillagerOccupationSoldier solider in banneredSoldiersIsVisited)
                    {
                        MoreMethodPOI.StaticSoldierAddSubPOI(solider);
                    }
                }
                else
                {                  
                    MoreMethodPOI.StaticSoldierRemoveSubPOI();                   
                }               
            }
            
        }





        [HarmonyPatch(typeof(VillagerOccupationSoldier), nameof(VillagerOccupationSoldier.OnAssignedBannerHolder))]
        public class SoldierAddPOI
        {
            public static void Postfix(VillagerOccupationSoldier __instance)
            {
                banneredSoldiersIsVisited.RemoveAll(item => item.villager == null);
                MinimapPOI orAddComponent = __instance.villager.gameObject.GetOrAddComponent<MinimapPOI>();
                orAddComponent.enabled = false;
                orAddComponent.poiType = POIType.RaidBanner;
                orAddComponent.priority = DMCM.priorityCfg.Value;
                orAddComponent.iconNonVisited = __instance.militaryCompany.bannerSprite;
                orAddComponent.isInFoW = false;
                orAddComponent.miniMapIconScale = DMCM.scaleCfg.Value;
                orAddComponent.showEnemyPulse = false;
                orAddComponent.enabled = true;
                MelonLogger.Msg($"Added POI to soldier: {__instance.villager.name}");  
                banneredSoldiersIsVisited.Add(__instance);

            }
           
        }
        [HarmonyPatch(typeof(VillagerOccupationSoldier), nameof(VillagerOccupationSoldier.OnUnassignedBannerHolder))]
        public class SoldierRemovePOI
        {
            public static void Postfix(VillagerOccupationSoldier __instance)
            {
                banneredSoldiersIsVisited.RemoveAll(item => item.villager == null);
                banneredSoldiersIsVisited.Remove(__instance); 
                MinimapPOI component = __instance.villager.gameObject.GetComponent<MinimapPOI>();
                
                
                if (__instance.villager.occupation.GetOccupation() == Occupation.Soldier && enableAddSubPOICfg.Value == true && __instance.militaryCompany != null)//!
                {
                    MoreMethodPOI.SoldierAddSubPOI(__instance);     
                }
                else if(component != null)
                {
                    UnityEngine.Object.Destroy(component);
                    MelonLogger.Msg($"Remove POI of soldier: {__instance.villager.name}");

                }

            }
        }
        [HarmonyPatch(typeof(MilitaryCompany), nameof(MilitaryCompany.AddSoldier))]
        public class DynamicAddSubPOI
        {
            public static void Postfix(VillagerOccupationSoldier soldier)
            {
                banneredSoldiersIsVisited.RemoveAll(item => item.villager == null);
                if (DMCM.enableAddSubPOICfg.Value == true && !banneredSoldiersIsVisited.Contains(soldier))
                {                  
                    MoreMethodPOI.SoldierAddSubPOI(soldier);                 
                }
                
            }
        }
        [HarmonyPatch(typeof(MilitaryCompany), nameof(MilitaryCompany.RemoveSoldier))]
        public class DynamicRemoveSubPOI
        {
            public static void Postfix(VillagerOccupationSoldier soldier)
            {
               
                MinimapPOI component = soldier.villager.gameObject.GetComponent<MinimapPOI>();
                if (component != null)
                {
                    UnityEngine.Object.Destroy(component);                                       
                    MelonLogger.Msg($"DRemove subPOI of soldier: {soldier.villager.name}");
                }
            }
        }
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Load))]
        public class ClearOnLoadPatch
        {
            public static void Prefix()
            {    
                banneredSoldiersIsVisited.Clear();
                MelonLogger.Msg("banneredSoldiersIsVisited has clear");

            }
        }
    }
}

   

    
