using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;


namespace ClimateCloaks
{
    public class ClimateCloaks : MonoBehaviour
    {


        static Mod mod;
        public bool check = false;



        //[Invoke(StateManager.StateTypes.Start, 0)]
        [Invoke(StateManager.StateTypes.Game, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<ClimateCloaks>();
            EntityEffectBroker.OnNewMagicRound += TemperatureEffects_OnNewMagicRound;
            mod.IsReady = true;
        }



        static int counter = 0;
        static int counterDmg = 0;
        static int debuffCounter = 0;
        static PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
        static PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
        static PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
        static EntityEffectManager playerEffectManager = playerEntity.EntityBehaviour.GetComponent<EntityEffectManager>();


        private static void TemperatureEffects_OnNewMagicRound()
        {
            int raceTemp = RaceTemp();
            int climateTemp = ClimateTemp();
            int seasonTemp = SeasonTemp();
            int weatherTemp = WeatherTemp();
            int nightTemp = NightTemp();
            int clothingTemp = ClothingTemp();
            bool naked = NakedSwitch();
            int temperatureEffect = climateTemp + nightTemp + seasonTemp + weatherTemp + clothingTemp + raceTemp;
            int armorTemp = ArmorTemp() * Mathf.Max(1, temperatureEffect / 10);
            int maxFatigue = playerEntity.MaxFatigue;




            if (playerEntity.CurrentHealth > 0 && playerEntity.EntityBehaviour.enabled
                && !playerEntity.IsResting
                && !GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease
                && !playerEnterExit.IsPlayerInsideBuilding)
            {
                temperatureEffect += armorTemp;
                temperatureEffect = ResistTemp(temperatureEffect);

                //DaggerfallUI.SetMidScreenText(temperatureEffect.ToString());
                ++counter;
                if ((temperatureEffect > 10 || temperatureEffect < 10) && counter > 5)
                {
                    counter = 0;
                    DaggerfallUI.AddHUDText(TempText(temperatureEffect));
                    if (temperatureEffect > 20 && naked == true && nightTemp == 0)
                    {
                        string tempDmgTxt = "The sun burns your naked skin.";
                        DaggerfallUI.AddHUDText(tempDmgTxt);
                        playerEntity.DecreaseHealth(2);
                    }
                    else if (temperatureEffect < 20 && naked == true)
                    {
                        string tempDmgTxt = "The icy air numbs your naked skin";
                        DaggerfallUI.AddHUDText(tempDmgTxt);
                        playerEntity.DecreaseHealth(2);
                    }

                    temperatureEffect = Mathf.Max(temperatureEffect, temperatureEffect * -1);
                    int fatigueTemp = temperatureEffect / 10;
                    if (playerEntity.RaceTemplate.ID == 8)
                    {
                        fatigueTemp = Mathf.Max(0, fatigueTemp - 1) * 2;
                    }
                    playerEntity.DecreaseFatigue(fatigueTemp, true);

                }
                if (temperatureEffect > 30)
                {
                    if (debuffCounter < 80) { debuffCounter += 10; }
                    int countOrTemp = Mathf.Min(temperatureEffect, debuffCounter);
                    int tempAttDebuff = Mathf.Max(0, (countOrTemp - 30));
                    if (playerEntity.RaceTemplate.ID == 8 && tempAttDebuff < 50) { tempAttDebuff = 0; }
                    if (playerEntity.RaceTemplate.ID == 8 && tempAttDebuff > 50) { tempAttDebuff *= 2; }
                    int currentEn = playerEntity.Stats.PermanentEndurance;
                    int currentSt = playerEntity.Stats.PermanentStrength;
                    int currentAg = playerEntity.Stats.PermanentAgility;
                    int currentInt = playerEntity.Stats.PermanentIntelligence;
                    int currentWill = playerEntity.Stats.PermanentWillpower;
                    int currentPer = playerEntity.Stats.PermanentPersonality;
                    int currentSpd = playerEntity.Stats.PermanentSpeed;
                    int[] statMods = new int[DaggerfallStats.Count];
                    statMods[(int)DFCareer.Stats.Endurance] = -Mathf.Min(tempAttDebuff, currentEn - 5);
                    statMods[(int)DFCareer.Stats.Strength] = -Mathf.Min(tempAttDebuff, currentSt - 5);
                    statMods[(int)DFCareer.Stats.Agility] = -Mathf.Min(tempAttDebuff, currentAg - 5);
                    statMods[(int)DFCareer.Stats.Intelligence] = -Mathf.Min(tempAttDebuff, currentInt - 5);
                    statMods[(int)DFCareer.Stats.Willpower] = -Mathf.Min(tempAttDebuff, currentWill - 5);
                    statMods[(int)DFCareer.Stats.Personality] = -Mathf.Min(tempAttDebuff, currentPer - 5);
                    statMods[(int)DFCareer.Stats.Speed] = -Mathf.Min(tempAttDebuff, currentSpd - 5);
                    playerEffectManager.MergeDirectStatMods(statMods);


                    if (temperatureEffect > 50)
                    {
                        ++counterDmg;
                        int tempDmg = Mathf.Max(0, (temperatureEffect - 40) / 10);
                        if (tempDmg > 0 && counterDmg > 5)
                        {
                            counterDmg = 0;
                            DaggerfallUI.AddHUDText("You cannot go on much longer in this weather...");
                            playerEntity.DecreaseHealth(tempDmg);
                        }
                    }
                }
                else if (temperatureEffect < 30)
                {
                    debuffCounter = 0;
                }



                    
                
            }
        }




        static int ClimateTemp()
        {
            int temp = 0;
            switch (playerGPS.CurrentClimateIndex)
            {
                case (int)MapsFile.Climates.Desert2:
                    temp = 45;
                    break;
                case (int)MapsFile.Climates.Desert:
                    temp = 40;
                    break;
                case (int)MapsFile.Climates.Subtropical:
                    temp = 30;
                    break;
                case (int)MapsFile.Climates.Rainforest:
                    temp = 20;
                    break;
                case (int)MapsFile.Climates.Swamp:
                    temp = 10;
                    break;
                case (int)MapsFile.Climates.Woodlands:
                    temp = -10;
                    break;
                case (int)MapsFile.Climates.HauntedWoodlands:
                    temp = -20;
                    break;
                case (int)MapsFile.Climates.MountainWoods:
                    temp = -30;
                    break;
                case (int)MapsFile.Climates.Mountain:
                    temp = -40;
                    break;
            }
            temp = DungeonTemp(temp);
            return temp;
        }

        static int SeasonTemp()
        {
            int temp = 0;
            switch (DaggerfallUnity.Instance.WorldTime.Now.SeasonValue)
            {
                case DaggerfallDateTime.Seasons.Summer:
                    temp = 20;
                    break;
                case DaggerfallDateTime.Seasons.Winter:
                    temp = -20;
                    break;
                case DaggerfallDateTime.Seasons.Fall:
                    temp = -10;
                    break;
                case DaggerfallDateTime.Seasons.Spring:
                    temp = -10;
                    break;
            }
            temp = DungeonTemp(temp);
            return temp;
        }

        static int WeatherTemp()
        {
            int temp = 0;
            int cloak = 0;
            if (!playerEnterExit.IsPlayerInsideDungeon)
            {
                var cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
                var cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);
                bool isRaining = GameManager.Instance.WeatherManager.IsRaining;
                bool isOvercast = GameManager.Instance.WeatherManager.IsOvercast;
                bool isStorming = GameManager.Instance.WeatherManager.IsStorming;
                bool isSnowing = GameManager.Instance.WeatherManager.IsSnowing;

                if (cloak1 != null || cloak2 != null)
                {
                    cloak = 10;
                }
                if (isRaining)
                {
                    temp = -20 + cloak;
                }
                else if (isStorming)
                {
                    temp = -25 + cloak;
                }
                else if (isSnowing)
                {
                    temp = -15 + cloak;
                }
                else if (isOvercast)
                {
                    temp = -5;
                }



            }
            return temp;
        }

        static int ClothingTemp()
        {
            var cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            var cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);
            var chest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            var legs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsClothes);
            var feet = playerEntity.ItemEquipTable.GetItem(EquipSlots.Feet);
            var gloves = playerEntity.ItemEquipTable.GetItem(EquipSlots.Gloves);

            int temp = 0;

            //if (cloak1 != null)
            //{
            //    temp += 5;
            //}
            //if (cloak2 != null)
            //{
            //    temp += 5;
            //}
            if (chest != null)
            {
                temp += 15;
            }
            if (legs != null)
            {
                temp += 10;
            }
            if (feet != null)
            {
                temp += 3;
            }
            if (gloves != null)
            {
                temp += 2;
            }



            return temp;
        }

        static int ArmorTemp()
        {
            var rArm = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.RightArm);
            var lArm = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.LeftArm);
            var chest = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.ChestArmor);
            var legs = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.LegsArmor);
            var head = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.Head);

            int temp = 0;

            if (lArm != null)
            {
                temp += 1;
            }
            if (rArm != null)
            {
                temp += 1;
            }
            if (chest != null)
            {
                temp += 3;
            }
            if (legs != null)
            {
                temp += 2;
            }
            if (head != null)
            {
                temp += 1;
            }
            return temp;
        }

        static int RaceTemp()
        {
            int temp = 0;
            switch (playerEntity.RaceTemplate.ID)
            {
                case (int)Races.Breton:
                    temp = +5;
                    break;
                case (int)Races.Redguard:
                    temp = -5;
                    break;
                case (int)Races.Nord:
                    temp = +10;
                    break;
                case (int)Races.DarkElf:
                    temp = -5;
                    break;
                case (int)Races.HighElf:
                    temp = -5;
                    break;
                case (int)Races.WoodElf:
                    temp = 0;
                    break;
                case (int)Races.Khajiit:
                    temp = -5;
                    break;
                case (int)Races.Argonian:
                    temp = -5;
                    break;
            }
            return temp;
        }

        static int ResistTemp(int temp)
        {
            int resFire = playerEntity.Resistances.PermanentFire / 2;
            int resFrost = playerEntity.Resistances.PermanentFrost / 2;
            int raceID = playerEntity.RaceTemplate.ID;
            if (raceID == 3) { resFrost += 15; }

            if (temp < 0)
            {
                temp = Mathf.Min(temp + resFrost, 0);                
            }
            else
            {
                temp = Mathf.Max(temp - resFire, 0);
            }
            return temp;
        }

        static bool NakedSwitch()
        {
            var chest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            var legs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsClothes);
            var aChest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestArmor);
            var aLegs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsArmor);


            if (chest == null && legs == null && aChest == null && aLegs == null && playerEntity.RaceTemplate.ID != 7 && playerEntity.RaceTemplate.ID != 8)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static int NightTemp()
        {
            bool isNight = DaggerfallUnity.Instance.WorldTime.Now.IsNight;
            int climate = playerGPS.CurrentClimateIndex;

            int temp = 0;

            if (isNight && !playerEnterExit.IsPlayerInsideDungeon)
            {
                switch (climate)
                {
                    case (int)MapsFile.Climates.Desert2:
                        temp = -40;
                        break;
                    case (int)MapsFile.Climates.Desert:
                        temp = -35;
                        break;
                    case (int)MapsFile.Climates.Rainforest:
                    case (int)MapsFile.Climates.Subtropical:
                    case (int)MapsFile.Climates.Swamp:
                    case (int)MapsFile.Climates.Woodlands:
                    case (int)MapsFile.Climates.HauntedWoodlands:
                    case (int)MapsFile.Climates.MountainWoods:
                        temp = -20;
                        break;
                    case (int)MapsFile.Climates.Mountain:
                        temp = -30;
                        break;
                }
            }
            return temp;
        }

        static int DungeonTemp(int temp)
        {
            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                temp = temp / 3;
            }
            return temp;
        }

        static string TempText(int temperatureEffect)
        {
            string tempText = "";
            if (temperatureEffect > 10)
            {
                if (temperatureEffect > 50)
                {
                    tempText = "You feel like you're burning up!";
                }
                else if (temperatureEffect > 30)
                {
                    tempText = "Heat stroke is setting in...";
                }
                else if (temperatureEffect > 10)
                {
                    tempText = "It's too hot for you here...";
                }
            }
            if (temperatureEffect < 10)
            {
                if (temperatureEffect < -50)
                {
                    tempText = "Your teeth are chattering uncontrollably!";
                }
                else if (temperatureEffect < -30)
                {
                    tempText = "It's miserably cold here...";
                }
                else if (temperatureEffect < -10)
                {
                    tempText = "A chill rolls through you...";
                }
            }
            return tempText;
        }
    }
}
