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


namespace Temperature
{
    public class ClimateCloaks : MonoBehaviour
    {



        static Mod mod;
        public bool check = false;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<Temperature>();
            EntityEffectBroker.OnNewMagicRound += TemperatureEffects_OnNewMagicRound;
            mod.IsReady = true;
        }






        static int ClimateTemp()
        {
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            int climate = playerGPS.CurrentClimateIndex;
            int temp = 0;
            switch (climate)
            {
                case (int)MapsFile.Climates.Desert2:
                    temp = 0;
                    break;
                case (int)MapsFile.Climates.Desert:
                    temp = 0;
                    break;
                case (int)MapsFile.Climates.Rainforest:
                    temp = 0;
                    break;
                case (int)MapsFile.Climates.Subtropical:
                    temp = 0;
                    break;
                case (int)MapsFile.Climates.Swamp:
                    temp = 0;
                    break;
                case (int)MapsFile.Climates.Woodlands:
                    temp = 0;
                    break;
                case (int)MapsFile.Climates.HauntedWoodlands:
                    temp = -0;
                    break;
                case (int)MapsFile.Climates.MountainWoods:
                    temp = -0;
                    break;
                case (int)MapsFile.Climates.Mountain:
                    temp = -0;
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
                        temp = 30;
                        break;
                    case DaggerfallDateTime.Seasons.Winter:
                        temp = 20;
                        break;
                    case DaggerfallDateTime.Seasons.Fall:
                        temp = 10;
                        break;
                    case DaggerfallDateTime.Seasons.Spring:
                        temp = 5;
                        break;
                    default:
                        temp = 1;
                        break;
                }
            temp = DungeonTemp(temp);
            return temp;
            }

        static int WeatherTemp()
        {
            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;

            int temp = 0;

            if (!playerEnterExit.IsPlayerInsideDungeon)
            {
                bool isRaining = GameManager.Instance.WeatherManager.IsRaining;
                bool isOvercast = GameManager.Instance.WeatherManager.IsOvercast;
                bool isStorming = GameManager.Instance.WeatherManager.IsStorming;
                bool isSnowing = GameManager.Instance.WeatherManager.IsSnowing;

                if (isRaining)
                {
                    temp = -0;
                }
                else if (isOvercast)
                {
                    temp = -0;
                }
                else if (isStorming)
                {
                    temp = -0;
                }
                else if (isSnowing)
                {
                    temp = -0;
                }
            }
            return temp;
        }

        static int ClothingTemp()
        {
            var cloak1 = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            var cloak2 = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);
            var chest = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            var legs = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.LegsClothes);
            var feet = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.Feet);

            int temp = 0;

            if (cloak1 != null)
            {
                temp += 0;
            }
            if (cloak2 != null)
            {
                temp += 0;
            }
            if (chest != null)
            {
                temp += 0;
            }
            if (legs != null)
            {
                temp += 0;
            }
            if (feet != null)
            {
                temp += 0;
            }
            else
            {
                temp = 0;
            }
            return temp;
        }

        static int NightTemp()
        {
            bool isNight = DaggerfallUnity.Instance.WorldTime.Now.IsNight;
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            int climate = playerGPS.CurrentClimateIndex;

            int temp = 0;

            if (isNight)
            {
                switch (climate)
                {
                    case (int)MapsFile.Climates.Desert2:
                        temp = -0;
                        break;
                    case (int)MapsFile.Climates.Desert:
                        temp = -0;
                        break;
                    case (int)MapsFile.Climates.Rainforest:
                    case (int)MapsFile.Climates.Subtropical:
                    case (int)MapsFile.Climates.Swamp:
                    case (int)MapsFile.Climates.Woodlands:
                    case (int)MapsFile.Climates.HauntedWoodlands:
                    case (int)MapsFile.Climates.MountainWoods:
                    case (int)MapsFile.Climates.Mountain:
                        temp = -0;
                        break;
                }
            }
            return temp;
        }

        static int DungeonTemp(int temp)
        {
            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                temp = temp / 2;
            }
            return temp;
        }

        static string TempText(int temperatureEffect)
        {
            string tempText = "";
            if (temperatureEffect > 0)
            {
                if (temperatureEffect >= 40)
                {
                    tempText = "The weather is scorching.";
                }
                else if (temperatureEffect >= 30)
                {
                    tempText = "The weather is unbearably hot.";
                }
                else
                {
                    tempText = "You feel warm.";
                }
            }
            if (temperatureEffect < 0)
            {
                if (temperatureEffect <= -40)
                {
                    tempText = "The weather is freezing.";
                }
                else if (temperatureEffect <= -30)
                {
                    tempText = "The weather is unbearably cold.";
                }
                else
                {
                    tempText = "You feel cold...";
                }
            }
            return tempText;
        }



        private static void TemperatureEffects_OnNewMagicRound()
        {
            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

            int climateTemp = ClimateTemp();
            int seasonTemp = SeasonTemp();
            int weatherTemp = WeatherTemp();
            int nightTemp = NightTemp();
            int clothingTemp = ClothingTemp();

            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                climateTemp = DungeonTemp(climateTemp);
                seasonTemp = DungeonTemp(seasonTemp);
            }


            if (playerEntity.CurrentHealth > 0 && playerEntity.EntityBehaviour.enabled && !playerEntity.IsResting &&
                !GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease && !playerEnterExit.IsPlayerInsideBuilding)
            {
                int currentEndurance = playerEntity.Stats.PermanentEndurance;
                int currentStrength = playerEntity.Stats.PermanentStrength;
                int currentIntelligence = playerEntity.Stats.PermanentIntelligence;

                int temperatureEffect = climateTemp + nightTemp + seasonTemp + weatherTemp + clothingTemp;
                if (temperatureEffect > 0 || temperatureEffect < 0)
                {
                    DaggerfallUI.AddHUDText(TempText(temperatureEffect));
                }

                if (temperatureEffect < 0)
                {
                    temperatureEffect *= -1;
                }
                if (temperatureEffect >= currentEndurance || temperatureEffect >= currentStrength || temperatureEffect >= currentIntelligence)
                {
                    temperatureEffect = Mathf.Min(currentEndurance, currentStrength, currentIntelligence) - 5;
                }
                EntityEffectManager playerEffectManager = playerEntity.EntityBehaviour.GetComponent<EntityEffectManager>();
                int[] statMods = new int[DaggerfallStats.Count];
                statMods[(int)DFCareer.Stats.Endurance] = -temperatureEffect;
                statMods[(int)DFCareer.Stats.Strength] = -temperatureEffect;
                playerEffectManager.MergeDirectStatMods(statMods);



            }
        }
    }
}






