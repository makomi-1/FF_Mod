using MelonLoader;
using HarmonyLib;
using static UnityEngine.Rendering.PostProcessing.SubpixelMorphologicalAntialiasing;
using System.Numerics;
using UnityEngine;
using static RankingForEndOfRaid;
using System;
using System.Security.Cryptography;

[assembly: MelonInfo(typeof(Spoils_of_War.SW), "Spoils of War", "1.0.0", "Truth S4id", null)]
[assembly: MelonGame("Crate Entertainment", "Farthest Frontier")]
//Note:the protection of null!!!
/*Possible updates:
    1.增加预采样表
    2.金币掉落计算优化
    3.攻城单位掉落奖励
    4.士兵装备回收
    5.掉落概型自定义
*/
namespace Spoils_of_War
{
    public class SW : MelonMod
    {
        //cfg
        private MelonPreferences_Category MinimapPOI_Cfg;
        private readonly string modName = "Spoils of War";

        //dropP
        private static MelonPreferences_Entry<float> dropProbability;
        private static MelonPreferences_Entry<uint> numDropMultiplyingPower;
        //dropHorseMeat(spawn horseCarcass)
        private static MelonPreferences_Entry<bool> spawnHorseCarcass;
        //dropGold
        private static MelonPreferences_Entry<bool> dropGold;
        private static MelonPreferences_Entry<uint> goldNumDropMultiplyingPower;//++

        private static MelonPreferences_Entry<uint> numRaiderUnit_Thief;
        private static MelonPreferences_Entry<uint> numRaiderUnit_Brawle;
        private static MelonPreferences_Entry<uint> numRaiderUnit_Archer;
        private static MelonPreferences_Entry<uint> numRaiderUnit_Warrior;
        private static MelonPreferences_Entry<uint> numRaiderUnit_Armored;
        private static MelonPreferences_Entry<uint> numRaiderUnit_Horseman;
        private static MelonPreferences_Entry<uint> numRaiderUnit_Champion;
        private static MelonPreferences_Entry<uint> numRaiderUnit_Pikeman;
        private static MelonPreferences_Entry<uint> numInvaderUnit_Arbalest;
        private static MelonPreferences_Entry<uint> numInvaderUnit_Pikeman;
        private static MelonPreferences_Entry<uint> numInvaderUnit_Footman;
        private static MelonPreferences_Entry<uint> numInvaderUnit_HeavyInfantry;
        private static MelonPreferences_Entry<uint> numInvaderUnit_Horseman;
        private static MelonPreferences_Entry<uint> numInvaderUnit_Champion;
        private static MelonPreferences_Entry<uint> numDefault;
        public void Configure()
        {
            MinimapPOI_Cfg = MelonPreferences.CreateCategory(this.modName);
            MinimapPOI_Cfg.SetFilePath("UserData/" + this.modName + ".cfg");

            // 基础掉落设置
            dropProbability = MinimapPOI_Cfg.CreateEntry<float>("dropProbability", 1.0f, null, "Basic Drop Configuration", false, false, null, null);
            numDropMultiplyingPower = MinimapPOI_Cfg.CreateEntry<uint>("numDropMultiplyingPower", 1u, null,null, false, false, null, null);

            // 马肉掉落（生成马尸体）
            spawnHorseCarcass = MinimapPOI_Cfg.CreateEntry<bool>("spawnHorseCarcass", true, null, "Generate the horse carcass for the deceased horsemen of raider.", false, false, null, null);

            // 金币掉落
            dropGold = MinimapPOI_Cfg.CreateEntry<bool>("dropGold", true, null, "Base configuration for gold ingot drop", false, false, null, null);
            goldNumDropMultiplyingPower = MinimapPOI_Cfg.CreateEntry<uint>("goldNumDropMultiplyingPower", 1u, null, null, false, false, null, null);

            // 掠夺者单位金币设置
            numRaiderUnit_Thief = MinimapPOI_Cfg.CreateEntry<uint>("numRaiderUnit_Thief", 10u, null, "Configuration for the number of gold ingots dropped by the raider unit", false, false, null, null);
            numRaiderUnit_Brawle = MinimapPOI_Cfg.CreateEntry<uint>("numRaiderUnit_Brawle", 15u, null, null, false, false, null, null);
            numRaiderUnit_Archer = MinimapPOI_Cfg.CreateEntry<uint>("numRaiderUnit_Archer", 20u, null, null, false, false, null, null);
            numRaiderUnit_Warrior = MinimapPOI_Cfg.CreateEntry<uint>("numRaiderUnit_Warrior", 30u, null, null, false, false, null, null);
            numRaiderUnit_Armored = MinimapPOI_Cfg.CreateEntry<uint>("numRaiderUnit_Armored", 50u, null, null, false, false, null, null);
            numRaiderUnit_Horseman = MinimapPOI_Cfg.CreateEntry<uint>("numRaiderUnit_Horseman", 130u, null, null, false, false, null, null);
            numRaiderUnit_Champion = MinimapPOI_Cfg.CreateEntry<uint>("numRaiderUnit_Champion", 170u, null, null, false, false, null, null);
            numRaiderUnit_Pikeman = MinimapPOI_Cfg.CreateEntry<uint>("numRaiderUnit_Pikeman", 70u, null,null , false, false, null, null);

            // 入侵者单位金币设置
            numInvaderUnit_Arbalest = MinimapPOI_Cfg.CreateEntry<uint>("numInvaderUnit_Arbalest", 75u, null, "Configuration for the number of gold ingots dropped by the Invader unit", false, false, null, null);
            numInvaderUnit_Pikeman = MinimapPOI_Cfg.CreateEntry<uint>("numInvaderUnit_Pikeman", 120u, null, null, false, false, null, null);
            numInvaderUnit_Footman = MinimapPOI_Cfg.CreateEntry<uint>("numInvaderUnit_Footman", 100u, null, null, false, false, null, null);
            numInvaderUnit_HeavyInfantry = MinimapPOI_Cfg.CreateEntry<uint>("numInvaderUnit_HeavyInfantry", 150u, null,null , false, false, null, null);
            numInvaderUnit_Horseman = MinimapPOI_Cfg.CreateEntry<uint>("numInvaderUnit_Horseman", 180u, null, null, false, false, null, null);
            numInvaderUnit_Champion = MinimapPOI_Cfg.CreateEntry<uint>("numInvaderUnit_Champion", 200u, null, null, false, false, null, null);

            // 默认数量设置
            numDefault = MinimapPOI_Cfg.CreateEntry<uint>("numDefault", 100u, null, "Default configuration for the number of gold coins dropped", false, false, null, null);

            MelonLogger.Msg(".cfg has created or loaded");

        }



        //percentIntactPFSampler
        private static readonly TruncatedNormalSampler shieldSamplerPI = new(50, 15, 20, 80);
        private static readonly BetaSampler hideCoatOrderSamplerPI = new(2, 6);
        private static readonly TruncatedNormalSampler hauberkSamplerPI = new(65, 10, 40, 90);
        private static readonly BetaSampler platemailSamplerPI = new(4, 2);
        private static readonly BetaSampler SimpleWeaponSamplerPI = new(1.5, 5.0);
        private static readonly TruncatedNormalSampler weaponSamplerPI = new(75, 8, 50, 95);
        private static readonly BetaSampler heavyWeaponSamplerPI = new(2, 4);
        private static readonly BetaSampler bowSamplerPI = new(3, 4);
        private static readonly TruncatedNormalSampler CrossbowSamplerPI = new(60, 12, 30, 85);
        private static readonly TruncatedNormalSampler arrowSamplerNum = new(20, 15, 10, 50);
        private static readonly BetaSampler boltSamplerNum = new(3, 6);
        public static bool ProbabilityJudge(float probability)
        {
            float randomValue = UnityEngine.Random.value;
            if (randomValue <= probability) return true;
            else return false;
        }


        //MelonLoader
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");

        }
        [Obsolete]
        public override void OnApplicationStart()
        {
            this.Configure();
        }



        //GetUniversalEquipment
        public static ItemStorage GetUniversalEquipment//archer has meleeweapon!
        (
            bool addShield = false,
            bool addHideCoat = false,
            bool addHauberk = false,
            bool addPlatemail = false,
            bool addSimpleWeapon = false,
            bool addWeapon = false,
            bool addHeavyWeapon = false,
            bool addBow = false,
            bool addCrossbow = false,
            bool addHorseCarcass = false

        )
        {
            uint uinversalNum = 1u * numDropMultiplyingPower.Value;
            ItemStorage UniversalEquipment = new();//          
            if (addShield) UniversalEquipment.AddItems(new ItemBundle(new ItemShield(), uinversalNum, shieldSamplerPI.SampleUint()));
            if (addHideCoat) UniversalEquipment.AddItems(new ItemBundle(new ItemHideCoat(), uinversalNum, hideCoatOrderSamplerPI.SampleUint()));
            if (addHauberk) UniversalEquipment.AddItems(new ItemBundle(new ItemHauberk(), uinversalNum, hauberkSamplerPI.SampleUint()));
            if (addPlatemail) UniversalEquipment.AddItems(new ItemBundle(new ItemPlatemail(), uinversalNum, platemailSamplerPI.SampleUint()));
            if (addSimpleWeapon) UniversalEquipment.AddItems(new ItemBundle(new ItemSimpleWeapon(), uinversalNum, SimpleWeaponSamplerPI.SampleUint()));
            if (addWeapon) UniversalEquipment.AddItems(new ItemBundle(new ItemWeapon(), uinversalNum, weaponSamplerPI.SampleUint()));
            if (addHeavyWeapon) UniversalEquipment.AddItems(new ItemBundle(new ItemHeavyWeapon(), uinversalNum, heavyWeaponSamplerPI.SampleUint()));
            if (addBow)
            {
                uint arrowNum = arrowSamplerNum.SampleUint();
                UniversalEquipment.AddItems(new ItemBundle(new ItemArrow(), arrowNum, 100u));
                UniversalEquipment.AddItems(new ItemBundle(new ItemBow(), uinversalNum, bowSamplerPI.SampleUint()));
            }
            if (addCrossbow)
            {
                uint boltNum = boltSamplerNum.SampleUint();
                UniversalEquipment.AddItems(new ItemBundle(new ItemArrow(), boltNum, 100u));
                UniversalEquipment.AddItems(new ItemBundle(new ItemCrossbow(), uinversalNum, CrossbowSamplerPI.SampleUint()));
            }
            if (addHorseCarcass && spawnHorseCarcass.Value) UniversalEquipment.AddItems(new ItemBundle(new ItemCarcass(), uinversalNum, 100u));

            return UniversalEquipment;
        }

        //GetEveryEquipment
        public static ItemStorage GetEquipmentUnknown()
        {
            MelonLogger.Msg("---------------------------unkowntype raider---------------------------");//debug
            return GetUniversalEquipment(addSimpleWeapon: true, addHideCoat: true);
        }
        public static ItemStorage GetSimpeEquipmentA()
        {
            return GetUniversalEquipment(addSimpleWeapon: true, addHideCoat: true);
        }
        public static ItemStorage GetSimpeEquipmentB()
        {
            return GetUniversalEquipment(addSimpleWeapon: true, addHauberk: true);
        }
        public static ItemStorage GetEquipmentA()
        {
            return GetUniversalEquipment(addWeapon: true, addHauberk: true, addShield: true);
        }
        public static ItemStorage GetEquipmentB()
        {
            MelonLogger.Msg("Spawn a carcass of horse");
            return GetUniversalEquipment(addWeapon: true, addHauberk: true, addShield: true, addHorseCarcass: true);
        }
        public static ItemStorage GetLargeEquipmentA()
        {
            return GetUniversalEquipment(addHeavyWeapon: true, addPlatemail: true);
        }
        public static ItemStorage GetLargeEquipmentB()
        {
            return GetUniversalEquipment(addHeavyWeapon: true, addPlatemail: true, addShield: true);
        }
        public static ItemStorage GetLargeEquipmentC()
        {
            MelonLogger.Msg("Spawn a carcass of horse");
            return GetUniversalEquipment(addHeavyWeapon: true, addPlatemail: true, addShield: true, addHorseCarcass: true);
        }
        public static ItemStorage GetLargeEquipmentD()
        {
            return GetUniversalEquipment(addHeavyWeapon: true, addHauberk: true, addShield: true);
        }
        public static ItemStorage GetBowEquipment()
        {
            ItemStorage equipment = GetUniversalEquipment(addBow: true, addHideCoat: true);
            equipment.AddItems(new ItemBundle(new ItemSimpleWeapon(), 1u, 65u));
            return equipment;
        }
        public static ItemStorage GetCrossBowEquipment()
        {
            ItemStorage equipment = GetUniversalEquipment(addCrossbow: true, addHauberk: true);
            equipment.AddItems(new ItemBundle(new ItemWeapon(), 1u, 85u));
            return equipment;
        }

        // RaiderClassfier
        public static uint NumGold(string displayName)
        {
            string raiderType = displayName;

            uint numGold = raiderType switch
            {
                //RaiderUnit_
                "RaiderUnit_Thief" => numRaiderUnit_Thief.Value,
                "RaiderUnit_Brawler" => numRaiderUnit_Brawle.Value,
                "RaiderUnit_Archer" => numRaiderUnit_Archer.Value,
                "RaiderUnit_Warrior" => numRaiderUnit_Warrior.Value,
                "RaiderUnit_Armored" => numRaiderUnit_Armored.Value,
                "RaiderUnit_Horseman" => numRaiderUnit_Horseman.Value,
                "RaiderUnit_Champion" => numRaiderUnit_Champion.Value,
                "RaiderUnit_Pikeman" => numRaiderUnit_Pikeman.Value,

                //InvaderUnit_
                "InvaderUnit_Arbalest" => numInvaderUnit_Arbalest.Value,
                "InvaderUnit_Pikeman" => numInvaderUnit_Pikeman.Value,
                "InvaderUnit_Footman" => numInvaderUnit_Footman.Value,
                "InvaderUnit_HeavyInfantry" => numInvaderUnit_HeavyInfantry.Value,
                "InvaderUnit_Horseman" => numInvaderUnit_Horseman.Value,
                "InvaderUnit_Champion" => numInvaderUnit_Champion.Value,

                //default
                _ => numDefault.Value
            };
            return numGold;
        }

        public static ItemStorage RaiderClassfier(string dispalyName)
        {
            string raiderType = dispalyName;

            ItemStorage itemdrops = raiderType switch
            {
                //RaiderUnit
                "RaiderUnit_Thief" => GetSimpeEquipmentA(),
                "RaiderUnit_Brawler" => GetSimpeEquipmentA(),
                "RaiderUnit_Archer" => GetBowEquipment(),
                "RaiderUnit_Warrior" => GetSimpeEquipmentB(),
                "RaiderUnit_Armored" => GetEquipmentA(),
                "RaiderUnit_Horseman" => GetEquipmentB(),
                "RaiderUnit_Champion" => GetLargeEquipmentB(),
                "RaiderUnit_Pikeman" => GetLargeEquipmentD(),

                //InvaderUnit
                "InvaderUnit_Arbalest" => GetCrossBowEquipment(),
                "InvaderUnit_Pikeman" => GetLargeEquipmentA(),
                "InvaderUnit_Footman" => GetEquipmentA(),
                "InvaderUnit_HeavyInfantry" => GetLargeEquipmentB(),
                "InvaderUnit_Horseman" => GetLargeEquipmentC(),
                "InvaderUnit_Champion" => GetLargeEquipmentB(),

                //default
                _ => GetEquipmentUnknown()
            };
            return itemdrops;
        }





        //BasicProbabilityFunction
        public class TruncatedNormalSampler(double mean, double stdDev, uint min, uint max)
        {
            private readonly System.Random _random = new();
            public uint SampleUint()
            {
                while (true)
                {
                    double u1 = _random.NextDouble();
                    double u2 = _random.NextDouble();
                    double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                    double x = mean + z * stdDev;
                    if (x >= min && x <= max)
                    {
                        return (uint)Math.Round(x);
                    }
                }
            }
        }
        public class BetaSampler(double alpha, double beta)
        {
            private readonly System.Random _random = new();
            private readonly double _alpha = alpha, _beta = beta;

            public uint SampleUint()
            {
                while (true)
                {
                    double x = _random.NextDouble();
                    double y = _random.NextDouble();
                    double unnormalizedPdf = Math.Pow(x, _alpha - 1) * Math.Pow(1 - x, _beta - 1);
                    double xMode = (_alpha - 1) / (_alpha + _beta - 2);
                    double maxUnnormalizedPdf = Math.Pow(xMode, _alpha - 1) * Math.Pow(1 - xMode, _beta - 1);
                    if (y * maxUnnormalizedPdf <= unnormalizedPdf)
                    {
                        return (uint)Math.Round(x * 99 + 1);
                    }
                }
            }
        }





        //HarmonyPatch
        [HarmonyPatch(typeof(Raider), "OnDeath")]
        public class AddEquipmentDrop
        {
            static bool Prefix(Raider __instance)
            {
                if (ProbabilityJudge(dropProbability.Value))
                {
                    if (__instance == null)
                    {
                        MelonLogger.Msg("__instance is null");//debug
                        return true;
                    }
                    UnityEngine.Vector3 position = __instance.transform.position;
                    GameObject gameobject = __instance.gameObject;
                    string displayName = __instance.widgetBlackboard?.displayName ?? "Unknown";
                    if (displayName == "Unknown")
                    {
                        MelonLogger.Msg("No dispalyName");//debug
                        return true;
                    }
                    ItemStorage equipment = RaiderClassfier(displayName);
                    if (equipment == null)
                    {
                        MelonLogger.Msg($"No equipment)");//debug
                        return true;
                    }
                    if (dropGold.Value)
                    {
                        uint num = NumGold(displayName) * goldNumDropMultiplyingPower.Value;
                        equipment.AddItems(new ItemBundle(new ItemGoldIngot(), num, 100u));
                        MelonLogger.Msg($"{num} gold has dropped");
                    }
                    equipment.DropOnGround(position, gameobject, null);
                    MelonLogger.Msg($"equipment has dropped from:{displayName}({gameobject.name})");
                }

                return true;

            }
        }

    }




}