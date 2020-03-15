// Project:         Climates & Cloaks mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;
using System;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace ClimateCloaks
{
    [FullSerializer.fsObject("v1")]
    public class ClimateCloaksSaveData
    {
        public int WetCount;
        public int AttCount;
    }

    public class ClimateCloaks : MonoBehaviour, IHasModSaveData
    {
        static Mod mod;
        static ClimateCloaks instance;

        public Type SaveDataType
        {
            get { return typeof(ClimateCloaksSaveData); }
        }

        public object NewSaveData()
        {
            return new ClimateCloaksSaveData
            {
                WetCount = 0,
                AttCount = 0,
            };
        }

        public object GetSaveData()
        {
            return new ClimateCloaksSaveData
            {
                WetCount = wetCount,
                AttCount = attCount,
            };
        }

        public void RestoreSaveData(object saveData)
        {
            var climateCloaksSaveData = (ClimateCloaksSaveData)saveData;
            wetCount = climateCloaksSaveData.WetCount;
            attCount = climateCloaksSaveData.AttCount;
        }

        static bool statusLookUp = false;
        static bool statusInterval = true;
        static int txtIntervals = 5;
        static bool nudePen = true;
        static bool feetPen = true;
        static bool metalHeatCool = true;
        //static bool dungTemp = true;
        static bool wetPen = true;
        static bool txtSeverity = false;
        static bool clothDmg = true;
        static bool toggleKeyStatus = true;
        static bool encumbranceRPR = false;

        [Invoke(StateManager.StateTypes.Game, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            //GameObject go = new GameObject("ClimateCloaks");          
            go.AddComponent<ClimateCloaks>();
            instance = go.AddComponent<ClimateCloaks>();
            mod.SaveDataInterface = instance;

            StartGameBehaviour.OnStartGame += ClimatesCloaks_OnStartGame;
            EntityEffectBroker.OnNewMagicRound += TemperatureEffects_OnNewMagicRound;

            //Code for camping. Awaiting billboard spirte activation function.
            //PlayerActivate.RegisterModelActivation(41116, CampActivation);
        }


        void Awake()
        {
            Mod roleplayRealism = ModManager.Instance.GetMod("RoleplayRealism");
            if (roleplayRealism != null)
            {
                ModSettings rrSettings = roleplayRealism.GetSettings();
                encumbranceRPR = rrSettings.GetBool("Modules", "encumbranceEffects");
            }

            ModSettings settings = mod.GetSettings();

            int statusTextValue = settings.GetValue<int>("Features", "characterTemperatureText");
            if (statusTextValue == 1)
            {
                statusLookUp = true;
                statusInterval = false;
            }
            else if (statusTextValue == 2)
            {
                statusLookUp = true;
            }
            else if (statusTextValue == 3)
            {
                statusLookUp = false;
                statusInterval = false;
            }

            
            metalHeatCool = settings.GetBool("Features", "metalHeatingAndCooling");
            toggleKeyStatus = settings.GetBool("Features", "temperatureStatus");
            txtIntervals = settings.GetValue<int>("Features", "textIntervals") + 1;
            nudePen = settings.GetBool("Features", "damageWhenNude");
            feetPen = settings.GetBool("Features", "damageWhenBareFoot");
            //dungTemp = settings.GetBool("Features", "TemperatureEffectsInDungeons");
            wetPen = settings.GetBool("Features", "WetFromSwimmingAndRain");
            txtSeverity = settings.GetBool("Features", "onlySevereEffectInformation");
            clothDmg = settings.GetBool("Features", "ClothingAndArmorDamage");

            Debug.Log(
                "C&C Settings: " +
                "ArmSun " + metalHeatCool.ToString() +
                ", StatusUp " + statusLookUp.ToString() +
                ", StatusInt " + statusInterval.ToString() +
                ", TextInterval " + txtIntervals.ToString() +
                ", Text Severity " + txtSeverity.ToString() +
                ", Nude " + nudePen.ToString() +
                ", Feet " + feetPen.ToString() +
                //", Dungeon " + dungTemp.ToString() +
                ", Water " + wetPen.ToString() +
                ", Clothing " + clothDmg.ToString() +
                ", HotKey " + toggleKey.ToString()
                );

            mod.IsReady = true;
            Debug.Log("Climates & Cloaks ready");
        }

        static private int txtCount = 4;
        static private int wetCount = 0;
        static private int attCount = 0;
     

        static PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
        static PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
        static PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
        static EntityEffectManager playerEffectManager = playerEntity.EntityBehaviour.GetComponent<EntityEffectManager>();
        static RaceTemplate playerRace = playerEntity.RaceTemplate;


        static private int offSet = -5; //used to make small adjustments to the mod. Negative numbers makes the character freeze more easily.
        static private int baseNatTemp = Dungeon(Climate() + Month() + DayNight()) + Weather();
        static private int natTemp = Resist(baseNatTemp);
        static private int armorTemp = Armor(baseNatTemp);
        static private int charTemp = Resist(RaceTemp() + Clothes(baseNatTemp) + armorTemp - Water(natTemp)) + offSet;
        static private int pureClothTemp = Clothes(baseNatTemp);
        static private int natCharTemp = Resist(baseNatTemp + RaceTemp()+ offSet);
        static private int totalTemp = natTemp + charTemp;
        static private int absTemp = Mathf.Abs(totalTemp);
        static private bool cloak = Cloak();
        static private bool hood = HoodUp();
        static private bool playerIsWading = GameManager.Instance.PlayerMotor.OnExteriorWater == PlayerMotor.OnExteriorWaterMethod.Swimming;



        static private bool ccMessageBox = false;
        static private float lastToggleTime = Time.unscaledTime;
        static private float tickTimeInterval;
        const float stdInterval = 0.5f;
        static private KeyCode toggleKey = InputManager.Instance.GetBinding(InputManager.Actions.Status);
        static private bool lookingUp = false;
        bool statusClosed = true;

        void Start()
        {
            DaggerfallUI.UIManager.OnWindowChange += UIManager_OnWindowChange;
            //// Alternative Textbox code.
            //lastToggleTime = Time.unscaledTime;
            //tickTimeInterval = stdInterval;
        }


        void Update()
        {
            if (GameManager.Instance.PlayerMouseLook.Pitch <= -70 && !GameManager.Instance.PlayerMotor.IsSwimming && !GameManager.Instance.PlayerMotor.IsClimbing)
            {
                lookingUp = true;
            }


            //// Alternative Textbox code.
            //if (!GameManager.IsGamePaused)
            //{
            //    if (toggleKeyStatus && Input.GetKeyDown(toggleKey))
            //    {
            //        lastToggleTime = Time.unscaledTime;
            //        tickTimeInterval = stdInterval;
            //        ccMessageBox = true;
            //        Debug.Log("[Climates & Cloaks] ccMessageBox = " + ccMessageBox.ToString() + ", tickTimeInterval = " + tickTimeInterval.ToString() + ", lastToggleTime = " + lastToggleTime.ToString());
            //    }
            //    if (ccMessageBox && Time.unscaledTime > lastToggleTime + tickTimeInterval)
            //    {
            //        ccMessageBox = false;
            //        Debug.Log("[Climates & CLoaks] Message Box");
            //        string[] messages = new string[] { TxtClimate(), TxtClothing(), TxtAdvice() };
            //        StatusPopup(messages);
            //    }
            //}
        }



        private void UIManager_OnWindowChange(object sender, EventArgs e)
        {
            if (DaggerfallUI.UIManager.WindowCount == 0)
                statusClosed = true;

            if (DaggerfallUI.UIManager.WindowCount == 2 && statusClosed)
            {
                TemperatureCalculator();
                DaggerfallMessageBox msgBox = DaggerfallUI.UIManager.TopWindow as DaggerfallMessageBox;
                if (msgBox != null && msgBox.ExtraProceedBinding == InputManager.Instance.GetBinding(InputManager.Actions.Status))
                {

                    // Setup next status info box.
                    DaggerfallMessageBox newBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, msgBox);
                    if (encumbranceRPR)
                    { string[] messages = new string[] { TxtClimate(), TxtClothing(), TxtAdvice(), TxtEncumbrance() }; newBox.SetText(messages); }
                    else
                    { string[] messages = new string[] { TxtClimate(), TxtClothing(), TxtAdvice() }; newBox.SetText(messages); }
                    newBox.ExtraProceedBinding = InputManager.Instance.GetBinding(InputManager.Actions.Status); // set proceed binding
                    newBox.ClickAnywhereToClose = true;
                    msgBox.AddNextMessageBox(newBox);
                    statusClosed = false;
                }
            }
        }

        private static void TemperatureCalculator()
        {
            Debug.Log("[Climates & Cloaks] Running TemperatureCalculator()");
            baseNatTemp = Climate() + Month() + DayNight() + Weather();
            natTemp = Resist(baseNatTemp);
            armorTemp = Armor(baseNatTemp);
            pureClothTemp = Clothes(natTemp);
            charTemp = Resist(RaceTemp() + Clothes(natTemp) + armorTemp - Water(natTemp)) + offSet;
            natCharTemp = Resist(baseNatTemp + RaceTemp() + offSet);
            totalTemp = Dungeon(natTemp) + charTemp;
            absTemp = Mathf.Abs(totalTemp);
            cloak = Cloak();
            hood = HoodUp();
        }




        //// Alternative Textbox code.
        //static DaggerfallMessageBox tempInfoBox;
        //public static void StatusPopup(string[] message)
        //{
        //    if (tempInfoBox == null)
        //    {
        //        tempInfoBox = new DaggerfallMessageBox(DaggerfallUI.UIManager);
        //        tempInfoBox.AllowCancel = true;
        //        tempInfoBox.ClickAnywhereToClose = true;
        //        tempInfoBox.ParentPanel.BackgroundColor = Color.clear;
        //    }

        //    tempInfoBox.SetText(message);
        //    DaggerfallUI.UIManager.PushWindow(tempInfoBox);
        //}





        public static string TxtClimate()
        {
            string temperatureTxt = "mild ";
            string weatherTxt = "";
            string seasonTxt = " summer";
            string timeTxt = " in ";
            string climateTxt = "";
            string suitabilityTxt = " is suitable for you.";

            int climate = playerGPS.CurrentClimateIndex;
            int birthRaceID = playerEntity.BirthRaceTemplate.ID;
            int liveRaceID = playerEntity.RaceTemplate.ID;

            bool isRaining = GameManager.Instance.WeatherManager.IsRaining;
            bool isOvercast = GameManager.Instance.WeatherManager.IsOvercast;
            bool isStorming = GameManager.Instance.WeatherManager.IsStorming;
            bool isSnowing = GameManager.Instance.WeatherManager.IsSnowing;

            if (baseNatTemp >= 10)
            {
                if (baseNatTemp >= 50)
                {
                    temperatureTxt = "scorching";
                }
                else if (baseNatTemp >= 30)
                {
                    temperatureTxt = "hot";
                }
                else
                {
                    temperatureTxt = "warm";
                }
            }
            else if (baseNatTemp <= -10)
            {
                if (baseNatTemp <= -50)
                {
                    temperatureTxt = "freezing";
                }
                else if (baseNatTemp <= -30)
                {
                    temperatureTxt = "cold";
                }
                else
                {
                    temperatureTxt = "cool";
                }
            }
            if (!GameManager.Instance.IsPlayerInsideDungeon)
            {
                if (isRaining)
                {
                    weatherTxt = " and rainy";
                }
                else if (isStorming)
                {
                    weatherTxt = " and stormy";
                }
                else if (isOvercast)
                {
                    weatherTxt = " and foggy";
                }
                else if (isSnowing)
                {
                    weatherTxt = " and snowy";
                }
                else if (playerEnterExit.IsPlayerInSunlight)
                {
                    weatherTxt = " and sunny";
                }
            }

            switch (DaggerfallUnity.Instance.WorldTime.Now.SeasonValue)
            {
                //Spring
                case DaggerfallDateTime.Seasons.Fall:
                    seasonTxt = " fall";
                    break;
                case DaggerfallDateTime.Seasons.Spring:
                    seasonTxt = " spring";
                    break;
                case DaggerfallDateTime.Seasons.Winter:
                    seasonTxt = " winter";
                    break;
            }

            if (!GameManager.Instance.IsPlayerInsideDungeon)
            {
                int clock = DaggerfallUnity.Instance.WorldTime.Now.Hour;

                if (clock >= 4 && clock <= 7)
                {
                    timeTxt = " morning in ";
                }
                else if (clock >= 16 && clock <= 19)
                {
                    timeTxt = " evening in ";
                }
                else if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
                {
                    timeTxt = " night in ";
                }
                else
                {
                    timeTxt = " day in ";
                }
            }

            if (GameManager.Instance.IsPlayerInsideDungeon)
            {
                switch (climate)
                {
                    case (int)MapsFile.Climates.Desert2:
                    case (int)MapsFile.Climates.Desert:
                        climateTxt = "desert dungeon";
                        break;
                    case (int)MapsFile.Climates.Rainforest:
                    case (int)MapsFile.Climates.Subtropical:
                        climateTxt = "tropical dungeon";
                        break;
                    case (int)MapsFile.Climates.Swamp:
                        climateTxt = "swampy dungeon";
                        break;
                    case (int)MapsFile.Climates.Woodlands:
                    case (int)MapsFile.Climates.HauntedWoodlands:
                        climateTxt = "woodlands dungeon";
                        break;
                    case (int)MapsFile.Climates.MountainWoods:
                    case (int)MapsFile.Climates.Mountain:
                        climateTxt = "mountain dungeon";
                        break;
                }
            }
            else
            {
                switch (climate)
                {
                    case (int)MapsFile.Climates.Desert2:
                    case (int)MapsFile.Climates.Desert:
                        climateTxt = "the desert";
                        break;
                    case (int)MapsFile.Climates.Rainforest:
                    case (int)MapsFile.Climates.Subtropical:
                        climateTxt = "the tropics";
                        break;
                    case (int)MapsFile.Climates.Swamp:
                        climateTxt = "the swamps";
                        break;
                    case (int)MapsFile.Climates.Woodlands:
                    case (int)MapsFile.Climates.HauntedWoodlands:
                        climateTxt = "the woodlands";
                        break;
                    case (int)MapsFile.Climates.MountainWoods:
                    case (int)MapsFile.Climates.Mountain:
                        climateTxt = "the mountains";
                        break;
                }
            }


            if (playerRace.ID == (int)Races.Vampire && playerEnterExit.IsPlayerInSunlight)
            {
                if (natTemp > 0 && DaggerfallUnity.Instance.WorldTime.Now.IsDay && !hood)
                {
                    suitabilityTxt = " will burn you!";
                }
            }
            else if (natTemp < -60 || baseNatTemp > 50)
            {
                suitabilityTxt = " will be the death of you.";
            }
            else if (natTemp < -40 || baseNatTemp > 30)
            {
                suitabilityTxt = " will wear you down.";
            }
            else if (natTemp < -20)
            {
                suitabilityTxt = " makes you shiver.";
            }
            else if (natTemp > 10)
            {
                suitabilityTxt = " makes you sweat.";
            }
            Debug.Log("[Climates & Cloaks] baseNatTemp = " + baseNatTemp.ToString());

            if (GameManager.Instance.IsPlayerInsideDungeon)
            {
                return "The " + temperatureTxt.ToString() + " air in this " + climateTxt.ToString() + suitabilityTxt.ToString();
            }
            else
            {
                return "This " + temperatureTxt.ToString() + weatherTxt.ToString() + seasonTxt.ToString() + timeTxt.ToString() + climateTxt.ToString() + suitabilityTxt.ToString();
            }
        }

        public static string TxtClothing()
        {
            string clothTxt = "The way you are dressed provides no warmth";
            string wetTxt = ". ";
            string armorTxt = "";


            if (wetCount > 10)
            {
                if (wetCount > 200) { wetTxt = " and you are completely drenched."; }
                else if (wetCount > 100) { wetTxt = " and you are soaking wet."; }
                else if (wetCount > 50) { wetTxt = " and you are quite wet."; }
                else if (wetCount > 20) { wetTxt = " and you are somewhat wet."; }
                else { wetTxt = " and you are a bit wet."; }
            }

            if (pureClothTemp > 40)
            {
                clothTxt = "You are very warmly dressed";
                if (wetCount > 39)
                {
                    wetTxt = " but your clothes are soaked.";
                }
                else if (wetCount > 19)
                {
                    wetTxt = " but your clothes are damp.";
                }
            }
            else if (pureClothTemp > 20)
            {
                clothTxt = "You are warmly dressed";
                if (wetCount > 19)
                {
                    wetTxt = " but your clothes are soaked.";
                }
                else if (wetCount > 9)
                {
                    wetTxt = " but your clothes are damp.";
                }
            }
            else if (pureClothTemp > 10)
            {
                clothTxt = "You are moderately dressed";
                if (wetCount > 9)
                {
                    wetTxt = " but your clothes are soaked.";
                }
                else if (wetCount > 3)
                {
                    wetTxt = " but your clothes are damp.";
                }
            }
            else if (pureClothTemp > 5)
            {
                clothTxt = "You are lightly dressed";
                if (wetCount > 3)
                {
                    wetTxt = " and your clothes are wet.";
                }
                else if (wetCount > 1)
                {
                    wetTxt = " and your clothes are damp.";
                }
            }




            if (armorTemp > 20)
            {
                armorTxt = " Your armor is scorchingly hot.";
            }
            else if (armorTemp > 15)
            {
                armorTxt = " Your armor is very hot.";
            }
            else if (armorTemp > 11)
            {
                armorTxt = " Your armor is hot.";
            }
            else if (armorTemp > 5)
            {
                armorTxt = " Your armor is warm.";
            }
            else if (armorTemp > 0)
            {
                armorTxt = " Your armor is a bit stuffy.";
            }
            else if (armorTemp < -5)
            {
                armorTxt = " The metal of your armor is cold.";
            }
            else if (armorTemp < 0)
            {
                armorTxt = " The metal of your armor is cool.";
            }
            Debug.Log("[Climates & Cloaks] pureClothTemp = " + pureClothTemp.ToString());
            Debug.Log("[Climates & Cloaks] armorTemp = " + armorTemp.ToString());
            return clothTxt.ToString() + wetTxt.ToString() + armorTxt.ToString();
        }

        public static string TxtAdvice()
        {
            bool isDungeon = GameManager.Instance.IsPlayerInsideDungeon;
            bool isRaining = GameManager.Instance.WeatherManager.IsRaining;
            bool isStorming = GameManager.Instance.WeatherManager.IsStorming;
            bool isSnowing = GameManager.Instance.WeatherManager.IsSnowing;
            bool isWeather = isRaining || isStorming || isSnowing;
            bool isNight = DaggerfallUnity.Instance.WorldTime.Now.IsNight;
            bool isDesert = playerGPS.CurrentClimateIndex == (int)MapsFile.Climates.Desert || playerGPS.CurrentClimateIndex == (int)MapsFile.Climates.Desert2 || playerGPS.CurrentClimateIndex == (int)MapsFile.Climates.Subtropical;
            bool isMountain = playerGPS.CurrentClimateIndex == (int)MapsFile.Climates.Mountain || playerGPS.CurrentClimateIndex == (int)MapsFile.Climates.MountainWoods;
            DaggerfallUnityItem cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            DaggerfallUnityItem cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);

            string adviceTxt = "You do not feel the need to make any adjustments.";

            if (totalTemp < -10)
            {
                if (!cloak && isWeather && !isDungeon)
                {
                    adviceTxt = "A cloak would protect you from getting wet.";
                }
                else if ((isRaining || isStorming) && !hood && !isDungeon)
                {
                    adviceTxt = "The rain is soaking your head and running down your neck.";
                }
                else if (wetCount > 19)
                {
                    adviceTxt = "Walking around cold and wet might be hazardous to your health.";
                }
                else if (pureClothTemp < 30)
                {
                    adviceTxt = "In weather like this, it is important to dress warm enough.";

                    if (cloak1 != null)
                    {
                        switch (cloak1.TemplateIndex)
                        {
                            case (int)MensClothing.Casual_cloak:
                            case (int)WomensClothing.Casual_cloak:
                                adviceTxt = "Your casual cloak offers little protection from this cold.";
                                break;
                        }
                        if (cloak2 == null)
                        {
                            adviceTxt = "In this cold, it might help to put on a second cloak.";
                        }
                    }
                    if (cloak2 != null)
                    { 
                        switch (cloak2.TemplateIndex)
                        {
                            case (int)MensClothing.Casual_cloak:
                            case (int)WomensClothing.Casual_cloak:
                                adviceTxt = "Your casual cloak offers little protection from this cold.";
                                break;
                        }
                        if (cloak1 == null)
                        {
                            adviceTxt = "In this cold, it might help to put on a second cloak.";
                        }
                    }
                }
                else if (armorTemp < 0)
                {
                    adviceTxt = "The metal of your armor leeches the warmth from your body.";
                }
                else if (isNight && !isDungeon)
                {
                    adviceTxt = "Most adventurers know the dangers of traveling at night.";
                }
                else if (isDesert && isNight && !isDungeon)
                {
                    adviceTxt = "The desert nights are cold, but might be preferable to the heat of the day.";
                }
            }
            else if (totalTemp > 10)
            {
                if (armorTemp > 11 && playerEnterExit.IsPlayerInSunlight && !ArmorCovered())
                {
                    adviceTxt = "The sun is heating up your armor, perhaps you should cover it.";
                }
                else if (!cloak && baseNatTemp > 30 && playerEnterExit.IsPlayerInSunlight)
                {
                    adviceTxt = "The people of the deserts know to dress lightly and cover up in a casual cloak.";
                }
                else if (cloak && !hood && baseNatTemp > 30 && playerEnterExit.IsPlayerInSunlight)
                {
                    adviceTxt = "The hood of you cloak will protect your head from cooking.";
                }
                else if (pureClothTemp > 8 && baseNatTemp > 10)
                {
                    adviceTxt = "On a hot day like this, it is best to dress as lightly as possible.";
                }
                else if (pureClothTemp > 10)
                {
                    adviceTxt = "You might be more comfortable if you dressed lighter.";
                }
                else if (isMountain && !isNight && !isDungeon)
                {
                    adviceTxt = "Though it is slightly warm now, you know the mountains will be icy cold once night falls.";
                }
                else if (totalTemp > 30 && wetPen && playerGPS.IsPlayerInLocationRect)
                {
                    adviceTxt = "Perhaps there is a pool of water here you could cool off in.";
                }
                else if (isDesert && !isNight)
                {
                    adviceTxt = "Though monsters may roam the deserts at night, it might be preferable to this heat.";
                }
            }

            if (playerRace.ID == (int)Races.Vampire && playerEnterExit.IsPlayerInSunlight)
            {
                if (natTemp > 0 && DaggerfallUnity.Instance.WorldTime.Now.IsDay && !hood)
                {
                    if (cloak && !hood)
                    {
                        adviceTxt = "The rays of the sun burns your face and neck!";
                    }
                    adviceTxt = "Your exposed skin sizzles in the deadly sunlight!";
                }
            }

            return adviceTxt;
        }

        public static string TxtEncumbrance()
        {
            float encPc = playerEntity.CarriedWeight / playerEntity.MaxEncumbrance;
            float encOver = Mathf.Max(encPc - 0.75f, 0f) * 2f;
            if (encOver > 0)
            {
                return "You are over encumbered, which will slow you down and tire you out.";
            }
            return "";
        }

        //Code for making camp. Awaiting ability to activate billboards.
        //private static void CampActivation(Transform transform)
        //{
        //    //Debug.Log("C&C Camping");
        //    IUserInterfaceManager uiManager = DaggerfallUI.UIManager;
        //    uiManager.PushWindow(new DaggerfallRestWindow(uiManager, true));
        //}

        private static void ClimatesCloaks_OnStartGame(object sender, EventArgs e)
        {
            Debug.Log("[Climates & Cloaks] Starting");
            wetCount = 100;
            Debug.Log("[Climates & Cloaks] Start effects applied.");
        }

        private static void TemperatureEffects_OnNewMagicRound()
        {

            //Check that effects are only applied while player is active.
            if (playerEntity.CurrentHealth > 0
                && !playerEntity.IsResting
                && !DaggerfallUI.Instance.FadeBehaviour.FadeInProgress
                && !GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease
                && !playerEnterExit.IsPlayerInsideBuilding
                && !GameManager.IsGamePaused
                //&& GameManager.Instance.IsPlayerOnHUD
                //&& ((dungTemp && playerEnterExit.IsPlayerInsideDungeon) || !playerEnterExit.IsPlayerInsideDungeon)
                && !playerEntity.IsInBeastForm)
            {
                Debug.Log("[Climates & Cloaks] active round start");
                playerIsWading = GameManager.Instance.PlayerMotor.OnExteriorWater == PlayerMotor.OnExteriorWaterMethod.Swimming;

                TemperatureCalculator();

                txtCount++;



                //Basic mod effect starts here at +/- 10+ by decreasing fatigue.
                if (absTemp > 10)
                {
                    int fatigueDmg = absTemp / 20;
                    if (playerRace.ID != (int)Races.Argonian)
                    {
                        if (absTemp < 30) { fatigueDmg /= 2; }
                        else { fatigueDmg *= 2; }
                    }
                    playerEntity.DecreaseFatigue(fatigueDmg, true);
                    Debug.Log("C&C Fatgigue Damage");
                    //Temperature +/- 30+ and starts debuffing attributes.
                    if (absTemp > 30)
                    {
                        attCount++;
                        DebuffAtt(absTemp);
                        Debug.Log("C&C Attribute Debuffs");
                    }
                    else { attCount = 0; }
                    //Temperature +/- 50+ and starts causing damage.
                    if (absTemp > 50)
                    {
                        { playerEntity.DecreaseHealth((absTemp - 40) / 10); }
                        Debug.Log("C&C Health Damage");
                    }
                }

                //If hot or cold, clothing might get damaged
                if ((baseNatTemp > 10 || baseNatTemp < -10) && clothDmg)
                {
                    int dmgRoll = UnityEngine.Random.Range(0, 100);
                    Debug.Log("Cloth Damage Roll = " + dmgRoll.ToString());
                    if (dmgRoll <= 2) { ClothDmg(); }
                }

                //If wet, armor might get damaged
                if (wetCount > 0)
                {
                    int dmgRoll = UnityEngine.Random.Range(0, 100);
                    Debug.Log("Armor Damage Roll = " + dmgRoll.ToString());
                    if (dmgRoll < 5 ) { ArmorDmg(); }                  
                }

                //If you look up, midtext displays how the weather is.
                if (lookingUp)
                {
                    UpText(natTemp);
                    lookingUp = false;
                    if (statusLookUp)
                    {
                        CharTxt(totalTemp);
                        txtCount = 0;
                    }
                }

                //Apply damage for being naked or walking on foot.
                if (playerRace.ID != (int)Races.Argonian && playerRace.ID != (int)Races.Khajiit)
                {
                    NakedDmg(natTemp);
                    if (!playerIsWading)
                    {
                        FeetDmg(natTemp);
                    }
                }
                
                //Displays toptext at intervals
                if (statusInterval)
                {
                    if (txtCount >= txtIntervals && GameManager.Instance.IsPlayerOnHUD)
                    {
                        CharTxt(totalTemp);
                    }
                }

                if (txtCount >= txtIntervals) { txtCount = 0; }

                //To counter a bug where you have 0 Stamina with no averse effects.
                if (playerEntity.CurrentFatigue == 0)
                {
                    playerEntity.DecreaseHealth(2);
                    DaggerfallUI.AddHUDText("You are exhausted and need to rest...");
                }


                Debug.Log("natTemp " + natTemp.ToString() + ", charTemp " + charTemp.ToString() + ", totalTemp " + totalTemp.ToString());
                Debug.Log("C&C active round end.");
            }
            else
            {
                //When inside a house, resting or traveling, counters start to reset.
                Debug.Log("C&C inactive round start");
                txtCount = txtIntervals;
                wetCount = Mathf.Max(wetCount - 2, 0);
                attCount = Mathf.Max(attCount - 2, 0);
                Debug.Log("C&C inactive round end");
            }
        }

        //Resist adjusts the number (usually NatTemp or CharTemp) for class resistances.
        static int Resist(int temp)
        {
            int resFire = playerEntity.Resistances.LiveFire;
            Debug.Log("Live Fire Resistance = " + resFire.ToString());
            int resFrost = playerEntity.Resistances.LiveFrost;
            Debug.Log("Live Frost Resistance = " + resFrost.ToString());
            if (playerEntity.RaceTemplate.ID == (int)Races.Werewolf || playerEntity.RaceTemplate.ID == (int)Races.Wereboar)
            {
                resFrost += 10;
                resFire += 10;
            }
            else if (playerEntity.RaceTemplate.ID == (int)Races.Vampire)
            {
                resFrost += 25;
            }
            if (temp < 0)
            {
                if (playerEntity.RaceTemplate.CriticalWeaknessFlags == DFCareer.EffectFlags.Frost) { resFrost -= 50; }
                else if (playerEntity.RaceTemplate.LowToleranceFlags == DFCareer.EffectFlags.Frost) { resFrost -= 25; }
                else if (playerEntity.RaceTemplate.ResistanceFlags == DFCareer.EffectFlags.Frost) { resFrost += 25; }
                else if (playerEntity.RaceTemplate.ImmunityFlags == DFCareer.EffectFlags.Frost) { resFrost += 50; }
                temp = Mathf.Min(temp + resFrost, 0);
            }
            else
            {
                if (playerEntity.RaceTemplate.CriticalWeaknessFlags == DFCareer.EffectFlags.Fire) { resFire -= 50; }
                else if (playerEntity.RaceTemplate.LowToleranceFlags == DFCareer.EffectFlags.Fire) { resFire -= 25; }
                else if (playerEntity.RaceTemplate.ResistanceFlags == DFCareer.EffectFlags.Fire) { resFire += 25; }
                else if (playerEntity.RaceTemplate.ImmunityFlags == DFCareer.EffectFlags.Fire) { resFire += 50; }
                temp = Mathf.Max(temp - resFire, 0);
            }
            return temp;
        }

        //If inside dungeon, the temperature effects is decreased.
        static int Dungeon(int natTemp)
        {
            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                if (natTemp > -15)
                {
                    natTemp = Mathf.Max((natTemp/2)-20, -15);
                }
                else
                {
                    natTemp = Mathf.Min((natTemp / 2) + 20, -15);
                }
                Debug.Log("[Climates & Cloaks] Dungeon Temp =" + natTemp.ToString());
            }
            return natTemp;
        }

        //If naked, may take damage from temperatures.
        static void NakedDmg(int natTemp)
        {
            if (!nudePen) { return; }
            DaggerfallUnityItem chest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            DaggerfallUnityItem legs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsClothes);
            DaggerfallUnityItem aChest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestArmor);
            DaggerfallUnityItem aLegs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsArmor);
            bool cTop = false;
            bool cBottom = false;

            if (chest != null)
            {
                switch (chest.TemplateIndex)
                {
                    case (int)MensClothing.Short_tunic:
                    case (int)MensClothing.Toga:
                    case (int)MensClothing.Short_shirt:
                    case (int)MensClothing.Short_shirt_with_belt:
                    case (int)MensClothing.Short_shirt_closed_top:
                    case (int)MensClothing.Short_shirt_closed_top2:
                    case (int)MensClothing.Short_shirt_unchangeable:
                    case (int)MensClothing.Long_shirt:
                    case (int)MensClothing.Long_shirt_with_belt:
                    case (int)MensClothing.Long_shirt_unchangeable:
                    case (int)MensClothing.Eodoric:
                    case (int)MensClothing.Kimono:
                    case (int)MensClothing.Open_Tunic:
                    case (int)MensClothing.Long_shirt_closed_top:
                    case (int)MensClothing.Long_shirt_closed_top2:
                    case (int)WomensClothing.Vest:
                    case (int)WomensClothing.Eodoric:
                    case (int)WomensClothing.Short_shirt:
                    case (int)WomensClothing.Short_shirt_belt:
                    case (int)WomensClothing.Short_shirt_closed:
                    case (int)WomensClothing.Short_shirt_closed_belt:
                    case (int)WomensClothing.Short_shirt_unchangeable:
                    case (int)WomensClothing.Long_shirt:
                    case (int)WomensClothing.Long_shirt_belt:
                    case (int)WomensClothing.Long_shirt_unchangeable:
                    case (int)WomensClothing.Peasant_blouse:
                    case (int)WomensClothing.Long_shirt_closed:
                    case (int)WomensClothing.Open_tunic:
                    case (int)MensClothing.Anticlere_Surcoat:
                    case (int)MensClothing.Formal_tunic:
                    case (int)MensClothing.Reversible_tunic:
                    case (int)MensClothing.Dwynnen_surcoat:
                    case (int)WomensClothing.Long_shirt_closed_belt:
                        cTop = true;
                        break;
                    case (int)MensClothing.Priest_robes:
                    case (int)MensClothing.Plain_robes:
                    case (int)WomensClothing.Evening_gown:
                    case (int)WomensClothing.Casual_dress:
                    case (int)WomensClothing.Strapless_dress:
                    case (int)WomensClothing.Formal_eodoric:                    
                    case (int)WomensClothing.Priestess_robes:
                    case (int)WomensClothing.Plain_robes:
                    case (int)WomensClothing.Day_gown:
                        cTop = true;
                        cBottom = true;
                        break;
                }
            }
            if (!cBottom && legs != null)
            {
                switch (legs.TemplateIndex)
                {
                    case (int)MensClothing.Khajiit_suit:
                    case (int)WomensClothing.Khajiit_suit:
                        cTop = true;
                        cBottom = true;
                        break;
                    case (int)WomensClothing.Wrap:
                    case (int)MensClothing.Wrap:
                    case (int)MensClothing.Short_skirt:
                    case (int)WomensClothing.Tights:
                    case (int)MensClothing.Long_Skirt:
                    case (int)WomensClothing.Long_skirt:
                    case (int)MensClothing.Casual_pants:
                    case (int)MensClothing.Breeches:
                    case (int)WomensClothing.Casual_pants:
                        cBottom = true;
                        break;
                }
            }
            if (cloak || aChest != null) { cTop = true; }
            if (aLegs != null) { cBottom = true; }
            if (!cTop || !cBottom)
            {
                if (!cTop) { Debug.Log("Character is Naked Top"); }
                if (!cBottom) { Debug.Log("Character is Naked Bottom"); }
                if (playerEnterExit.IsPlayerInSunlight && natTemp > 10)
                {
                    if ((playerEntity.RaceTemplate.ID == (int)Races.DarkElf || playerEntity.RaceTemplate.ID == (int)Races.Redguard))
                    {
                        if (natTemp > 30)
                        {
                            playerEntity.DecreaseHealth(1);
                            if (txtCount >= txtIntervals && cTop)
                            {
                                DaggerfallUI.AddHUDText("The sun burns your bare skin.");
                            }
                            else if (txtCount >= txtIntervals && cBottom)
                            {
                                DaggerfallUI.AddHUDText("The sun burns your bare legs.");
                            }
                        }
                    }
                    else
                    {
                        playerEntity.DecreaseHealth(1);
                        if (txtCount >= txtIntervals && cTop)
                        {
                            DaggerfallUI.AddHUDText("The sun burns your bare skin.");
                        }
                        else if (txtCount >= txtIntervals && cBottom)
                        {
                            DaggerfallUI.AddHUDText("The sun burns your bare legs.");
                        }
                    }
                }
                else if (natTemp < -10)
                {
                    playerEntity.DecreaseHealth(1);
                    if (txtCount >= txtIntervals)
                    { DaggerfallUI.AddHUDText("The cold air numbs your bare skin."); }
                }
            }
            else { Debug.Log("Character is not Naked"); }
        }

        //If bare feet, may take damage from temperatures.
        static void FeetDmg(int natTemp)
        {
            int endBonus = playerEntity.Stats.LiveEndurance/2;
            if (playerEntity.ItemEquipTable.GetItem(EquipSlots.Feet) == null
               && (Mathf.Abs(natTemp) - endBonus > 0)
               && GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot
               && feetPen)
            {
                playerEntity.DecreaseHealth(1);
                Debug.Log("FeetDmg = 1");
                if (natTemp > 0 && txtCount >= txtIntervals)
                {
                    DaggerfallUI.AddHUDText("Your bare feet are getting burned.");
                    Debug.Log("Feet are naked and burned.");
                }
                else if (txtCount >= txtIntervals)
                {
                    DaggerfallUI.AddHUDText("Your bare feet are freezing.");
                    Debug.Log("Feet are naked and freezing.");
                }
            }
        }

        static int Climate()
        {
            Debug.Log("Calculating Climate");
            switch (playerGPS.CurrentClimateIndex)
            {
                case (int)MapsFile.Climates.Desert2:
                    return 50;
                case (int)MapsFile.Climates.Desert:
                    return 40;
                case (int)MapsFile.Climates.Subtropical:
                    return 30;
                case (int)MapsFile.Climates.Rainforest:
                    return 20;
                case (int)MapsFile.Climates.Swamp:
                    return 10;
                case (int)MapsFile.Climates.Woodlands:
                    return -10;
                case (int)MapsFile.Climates.HauntedWoodlands:
                    return -20;
                case (int)MapsFile.Climates.MountainWoods:
                    return -30;
                case (int)MapsFile.Climates.Mountain:
                    return -40;
            }
            return 0;
        }

        static int Month()
        {
            Debug.Log("Calculating Month");
            switch (DaggerfallUnity.Instance.WorldTime.Now.MonthValue)
            {
                //Spring
                case DaggerfallDateTime.Months.FirstSeed:
                    return -5;
                case DaggerfallDateTime.Months.RainsHand:
                    return 0;
                case DaggerfallDateTime.Months.SecondSeed:
                    return 5;
                //Summer
                case DaggerfallDateTime.Months.Midyear:
                    return 10;
                case DaggerfallDateTime.Months.SunsHeight:
                    return 20;
                case DaggerfallDateTime.Months.LastSeed:
                    return +15;
                //Fall
                case DaggerfallDateTime.Months.Hearthfire:
                    return 0;
                case DaggerfallDateTime.Months.Frostfall:
                    return -5;
                case DaggerfallDateTime.Months.SunsDusk:
                    return -10;
                //Winter
                case DaggerfallDateTime.Months.EveningStar:
                    return -15;
                case DaggerfallDateTime.Months.MorningStar:
                    return -20;
                case DaggerfallDateTime.Months.SunsDawn:
                    return -15;
            }
            return 0;
        }

        static int Weather()
        {
            Debug.Log("Calculating Weather");
            int temp = 0;
            if (!playerEnterExit.IsPlayerInsideDungeon)
            {
                bool isRaining = GameManager.Instance.WeatherManager.IsRaining;
                bool isOvercast = GameManager.Instance.WeatherManager.IsOvercast;
                bool isStorming = GameManager.Instance.WeatherManager.IsStorming;
                bool isSnowing = GameManager.Instance.WeatherManager.IsSnowing;

                if (isRaining)
                {
                    temp -= 10;
                    if (wetPen)
                    {
                        if (cloak && hood)
                        {
                            wetCount += 0;
                        }
                        else if (cloak && !hood)
                        {
                            wetCount += 1;
                        }
                        else
                        { wetCount += 3; }
                    }
                }
                else if (isStorming)
                {
                    temp -= 15;
                    if (wetPen)
                    {
                        if (cloak && hood)
                        {
                            wetCount += 1;
                        }
                        else if (cloak && !hood)
                        {
                            wetCount += 2;
                        }
                        else
                        { wetCount += 5; }
                    }
                }
                else if (isSnowing)
                {
                    temp -= 10;
                    if (wetPen)
                    {
                        if (cloak && hood)
                        {
                            wetCount += 0;
                        }
                        else if (cloak && hood)
                        {
                            wetCount += 1;
                        }
                        else
                        { wetCount += 2; }
                    }
                }
                else if (isOvercast)
                {
                    temp -= 8;
                }
                else if (playerRace.ID == (int)Races.Vampire && playerEnterExit.IsPlayerInSunlight)
                {
                    int heat = Resist(Climate() + Month() + DayNight());
                    if (heat > 0 && DaggerfallUnity.Instance.WorldTime.Now.IsDay && !hood)
                    {
                        playerEntity.DecreaseHealth(heat / 5);
                    }
                }
            }
            return temp;
        }

        static int DayNight()
        {
            Debug.Log("Calculating Time");

            int clock = DaggerfallUnity.Instance.WorldTime.Now.Hour;

            if ((clock >= 16 || clock <= 7) && !playerEnterExit.IsPlayerInsideDungeon)
            {
                int climate = playerGPS.CurrentClimateIndex;
                int night = 1;

                switch (climate)
                {
                    case (int)MapsFile.Climates.Desert2:
                        night = 4;
                        break;
                    case (int)MapsFile.Climates.Desert:
                        night = 3;
                        break;
                    case (int)MapsFile.Climates.Rainforest:
                    case (int)MapsFile.Climates.Subtropical:
                    case (int)MapsFile.Climates.Swamp:
                    case (int)MapsFile.Climates.Woodlands:
                    case (int)MapsFile.Climates.HauntedWoodlands:
                    case (int)MapsFile.Climates.MountainWoods:
                        night = 1;
                        break;
                    case (int)MapsFile.Climates.Mountain:
                        night = 2;
                        break;
                }
                if ((clock >= 16 && clock <= 19) || (clock >= 4 && clock <= 7))
                { return -10 * night; }
                else
                { return -20 * night; }
            }
            return 0;
        }


        static bool Cloak()
        {
            DaggerfallUnityItem cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            DaggerfallUnityItem cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);

            if (cloak1 != null || cloak2 != null)
            {
                Debug.Log("Cloak is true");
                return true;
            }
            else
            {
                Debug.Log("Cloak is false");
                return false;
            }
        }

        static bool ArmorCovered()
        {
            DaggerfallUnityItem chestCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            if (cloak)
            {
                return true;
            }
            if (chestCloth != null)
            {
                switch (chestCloth.TemplateIndex)
                {
                    case (int)MensClothing.Priest_robes:
                    case (int)MensClothing.Plain_robes:
                    case (int)WomensClothing.Evening_gown:
                    case (int)WomensClothing.Casual_dress:
                    case (int)WomensClothing.Priestess_robes:
                    case (int)WomensClothing.Plain_robes:
                    case (int)WomensClothing.Day_gown:
                        return true;
                }
            }
            return false;
        }

        static bool HoodUp()
        {
            Debug.Log("[Climates & Cloaks]Checking Hood");
            DaggerfallUnityItem cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            DaggerfallUnityItem cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);
            DaggerfallUnityItem chestCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            bool up = false;
            if (cloak1 != null)
            {
                Debug.Log("[Climates & Cloaks]Checking Hood for cloak 1");
                switch (cloak1.CurrentVariant)
                {
                    case 0:
                    case 3:
                    case 4:
                        up = false;
                        break;
                    case 1:
                    case 2:
                    case 5:
                        up = true;
                        break;
                }
            }
            else if (cloak2 != null)
            {
                Debug.Log("[Climates & Cloaks]Checking Hood for cloak 2");
                switch (cloak2.CurrentVariant)
                {
                    case 0:
                    case 3:
                    case 4:
                        up = false;
                        break;
                    case 1:
                    case 2:
                    case 5:
                        up = true;
                        break;
                }
            }
            else if (chestCloth != null)
            {
                Debug.Log("[Climates & Cloaks]Checking Hood for chestCloth");
                switch (chestCloth.TemplateIndex)
                {
                    case (int)MensClothing.Plain_robes:
                    case (int)WomensClothing.Plain_robes:
                        switch (chestCloth.CurrentVariant)
                        {
                            case 0:
                                up = true;
                                break;
                            case 1:
                                up = true;
                                break;
                        }
                        break;
                }
            }
            Debug.Log("[Climates & Cloaks] HoodUp = " + up.ToString());
            return up;
        }

        static void ClothDmg()
        {
            DaggerfallUnityItem chestCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            DaggerfallUnityItem feetCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.Feet);
            DaggerfallUnityItem legsCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsClothes);
            DaggerfallUnityItem cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            DaggerfallUnityItem cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);
            DaggerfallUnityItem rArm = playerEntity.ItemEquipTable.GetItem(EquipSlots.RightArm);
            DaggerfallUnityItem lArm = playerEntity.ItemEquipTable.GetItem(EquipSlots.LeftArm);
            DaggerfallUnityItem chest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestArmor);
            DaggerfallUnityItem legs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsArmor);
            DaggerfallUnityItem head = playerEntity.ItemEquipTable.GetItem(EquipSlots.Head);
            DaggerfallUnityItem gloves = playerEntity.ItemEquipTable.GetItem(EquipSlots.Gloves);

            int roll = UnityEngine.Random.Range(1, 10);
            int lRoll = UnityEngine.Random.Range(1, 10);
            DaggerfallUnityItem cloth = cloak2;
            DaggerfallUnityItem lArmor = chest;

            if (cloak)
            {
                Debug.Log("Cloth Damage = Cloak, " + "Roll = " + roll.ToString());
                if (cloak2 == null ) { cloth = cloak1; }
                switch (roll)
                {
                    case 1:
                    case 2:
                    case 3:
                        break;
                    case 4:
                    case 5:
                        if (cloak1 != null) { cloth = cloak1; }
                        break;
                    case 6:
                    case 7:
                        if (chestCloth != null) { cloth = chestCloth; }
                        break;
                    case 8:
                    case 9:
                        if (legsCloth != null) { cloth = legsCloth; }
                        break;
                    case 10:
                        if (gloves != null) { cloth = gloves; }
                        break;
                }
            }
            else
            {
                Debug.Log("Cloth Damage = No Cloak, " + "Roll = " + roll.ToString());
                cloth = chestCloth;
                switch (roll)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        if (legsCloth != null) { cloth = legsCloth; }
                        break;
                    case 9:
                    case 10:
                        if (gloves != null) { cloth = gloves; }
                        break;
                }
            }

            if (cloth != null)
            {
                cloth.LowerCondition(1, playerEntity);
                Debug.Log("Cloth Damage = " + cloth.ItemName.ToString());
                if (GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot && feetCloth != null)
                {
                    feetCloth.LowerCondition(1, playerEntity);
                    if (feetCloth.currentCondition < (feetCloth.maxCondition / 10))
                    {
                        DaggerfallUI.AddHUDText("Your " + feetCloth.ItemName.ToString() + " is getting worn out...");
                    }
                }
                if (cloth.currentCondition < (cloth.maxCondition / 10))
                {
                    DaggerfallUI.AddHUDText("Your " + cloth.ItemName.ToString() + " is getting worn out...");
                }
            }
            else
            {
                Debug.Log("Cloth Damage = Cloth is NULL");
            }

            switch (lRoll)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    break;
                case 5:
                case 6:
                case 7:
                    if (legs != null) { lArmor = legs; }
                    break;
                case 8:
                    if (lArm != null) { lArmor = lArm; }
                    break;
                case 9:
                    if (rArm != null) { lArmor = rArm; }
                    break;
                case 10:
                    if (head != null) { lArmor = head; }
                    break;
            }

            if (lArmor != null)
            {
                if (Leather(lArmor))
                {
                    lArmor.LowerCondition(1, playerEntity);
                    Debug.Log("Leather Armor Damage = " + lArmor.ToString());
                    if (lArmor.currentCondition < (cloth.maxCondition / 10))
                    {
                        DaggerfallUI.AddHUDText("Your " + lArmor.ItemName.ToString() + " is getting worn out...");
                    }
                }
                else
                {
                    Debug.Log("Leather Armor Damage = Armorpiece is not Leather. No damage.");
                }
            }
        }

        static void ArmorDmg()
        {
            DaggerfallUnityItem rArm = playerEntity.ItemEquipTable.GetItem(EquipSlots.RightArm);
            DaggerfallUnityItem lArm = playerEntity.ItemEquipTable.GetItem(EquipSlots.LeftArm);
            DaggerfallUnityItem chest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestArmor);
            DaggerfallUnityItem legs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsArmor);
            DaggerfallUnityItem head = playerEntity.ItemEquipTable.GetItem(EquipSlots.Head);

            int roll = UnityEngine.Random.Range(1, 10);
            DaggerfallUnityItem armor = chest;
            if (chest == null) { armor = legs; }
                switch (roll)
                {
                    case 1:
                    case 2:
                    case 3:
                        break;
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    if (legs != null) { armor = legs; }
                        break;
                    case 8:
                    if (head != null) { armor = head; }
                        break;                 
                    case 9:
                        if (lArm != null) { armor = lArm; }
                        break;
                    case 10:
                        if (rArm != null) { armor = rArm; }
                        break;
                }


            if (armor != null)
            {
                if (!Leather(armor))
                {
                    int armorDmg = Mathf.Max(armor.maxCondition / 100, 1);
                    armor.LowerCondition(armorDmg, playerEntity);
                    Debug.Log("Armor Damage = " + armor.ToString());
                    if (armor.currentCondition < (armor.maxCondition / 10))
                    {
                        DaggerfallUI.AddHUDText("Your " + armor.ItemName.ToString() + " is getting rusty...");
                    }
                }
                else
                {
                    Debug.Log("Armor Damage = Armorpiece is Leather. No damage.");
                }
            }

        }

        static bool Leather(DaggerfallUnityItem armor)
        {
            if (armor != null)
            {
                if (armor.nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                { return true; }
                else
                { return false; }
            }
            else
            { return false; }
        }

        static int RaceTemp()
        {

            Debug.Log("Calculating RaceTemp");

 
            switch (playerEntity.BirthRaceTemplate.ID)
            {
                case (int)Races.Nord:
                    return 5;
                case (int)Races.Breton:
                    return 5;
                case (int)Races.HighElf:
                case (int)Races.WoodElf:
                    return 0;
                case (int)Races.Khajiit:
                case (int)Races.DarkElf:
                case (int)Races.Redguard:
                    return -5;
                case (int)Races.Argonian:
                    return -10;
            }
            return 0;
        }

        static int Clothes(int natTemp)
        {
            DaggerfallUnityItem chestCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            DaggerfallUnityItem feetCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.Feet);
            DaggerfallUnityItem legsCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsClothes);
            DaggerfallUnityItem cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            DaggerfallUnityItem cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);
            int chest = 0;
            int feet = 0;
            int legs = 0;
            int cloak = 0;
            int temp = 0;

            if (chestCloth != null)
            {                
                switch (chestCloth.TemplateIndex)
                {
                    case (int)MensClothing.Straps:
                    case (int)MensClothing.Armbands:
                    case (int)MensClothing.Fancy_Armbands:
                    case (int)MensClothing.Champion_straps:
                    case (int)MensClothing.Sash:
                    case (int)MensClothing.Challenger_Straps:
                    case (int)MensClothing.Eodoric:
                    case (int)MensClothing.Vest:
                    case (int)WomensClothing.Brassier:
                    case (int)WomensClothing.Formal_brassier:
                    case (int)WomensClothing.Eodoric:
                    case (int)WomensClothing.Formal_eodoric:
                    case (int)WomensClothing.Vest:
                        chest = 1;
                        break;
                    case (int)MensClothing.Short_shirt:
                    case (int)MensClothing.Short_shirt_with_belt:
                    case (int)WomensClothing.Short_shirt:
                    case (int)WomensClothing.Short_shirt_belt:
                        chest = 5;
                        break;
                    case (int)MensClothing.Short_tunic:
                    case (int)MensClothing.Toga:
                    case (int)MensClothing.Short_shirt_closed_top:
                    case (int)MensClothing.Short_shirt_closed_top2:
                    case (int)MensClothing.Short_shirt_unchangeable:
                    case (int)MensClothing.Long_shirt:
                    case (int)MensClothing.Long_shirt_with_belt:
                    case (int)MensClothing.Long_shirt_unchangeable:
                    case (int)WomensClothing.Short_shirt_closed:
                    case (int)WomensClothing.Short_shirt_closed_belt:
                    case (int)WomensClothing.Short_shirt_unchangeable:
                    case (int)WomensClothing.Long_shirt:
                    case (int)WomensClothing.Long_shirt_belt:
                    case (int)WomensClothing.Long_shirt_unchangeable:
                    case (int)WomensClothing.Peasant_blouse:
                    case (int)WomensClothing.Strapless_dress:
                        chest = 8;
                        break;
                    case (int)MensClothing.Open_Tunic:
                    case (int)MensClothing.Long_shirt_closed_top:
                    case (int)MensClothing.Long_shirt_closed_top2:
                    case (int)MensClothing.Kimono:
                    case (int)WomensClothing.Evening_gown:
                    case (int)WomensClothing.Casual_dress:
                    case (int)WomensClothing.Long_shirt_closed:
                    case (int)WomensClothing.Open_tunic:
                        chest = 10;
                        break;
                    case (int)MensClothing.Priest_robes:
                    case (int)MensClothing.Anticlere_Surcoat:
                    case (int)MensClothing.Formal_tunic:
                    case (int)MensClothing.Reversible_tunic:
                    case (int)MensClothing.Dwynnen_surcoat:
                    case (int)MensClothing.Plain_robes:
                    case (int)WomensClothing.Priestess_robes:
                    case (int)WomensClothing.Plain_robes:
                    case (int)WomensClothing.Long_shirt_closed_belt:
                    case (int)WomensClothing.Day_gown:
                        chest = 15;
                        break;
                }
            }
            Debug.Log("Checking Feet");
            if (feetCloth != null)
            {
                Debug.Log("Feet Not Bare");
                feet = 5;
                string feetInfo = "No Shoes Apply";
                switch (feetCloth.TemplateIndex)
                {
                    case (int)MensClothing.Sandals:
                    case (int)WomensClothing.Sandals:
                        feet = 0;
                        feetInfo = "Sandals";
                        break;
                    case (int)MensClothing.Shoes:
                    case (int)WomensClothing.Shoes:
                        feet = 2;
                        feetInfo = "Shoes";
                        break;
                    case (int)MensClothing.Tall_Boots:
                    case (int)WomensClothing.Tall_boots:
                        feet = 4;
                        feetInfo = "Tall Boots";
                        break;
                    case (int)MensClothing.Boots:
                    case (int)WomensClothing.Boots:
                        if (feetCloth.CurrentVariant == 0)
                        {
                            feet = 4;
                            feetInfo = "Leather Boots";
                        }
                        else
                        {
                            feet = 0;
                            feetInfo = "Tall Sandals";
                        }
                        break;                        
                }
                if (feet == 5) { feetInfo = "Armored Boots"; }
                Debug.Log(feetInfo.ToString());
            }
            else
            {
                Debug.Log("Feet Bare");
            }
            if (legsCloth != null)
            {
                switch (legsCloth.TemplateIndex)
                {
                    case (int)MensClothing.Loincloth:
                    case (int)WomensClothing.Loincloth:
                        legs = 1;
                        break;
                    case (int)MensClothing.Khajiit_suit:
                    case (int)WomensClothing.Khajiit_suit:
                        legs = 2;
                        break;
                    case (int)MensClothing.Wrap:
                    case (int)MensClothing.Short_skirt:
                    case (int)WomensClothing.Tights:
                    case (int)WomensClothing.Wrap:
                        legs = 4;
                        break;
                    case (int)MensClothing.Long_Skirt:
                    case (int)WomensClothing.Long_skirt:
                        legs = 8;
                        break;
                    case (int)MensClothing.Casual_pants:
                    case (int)MensClothing.Breeches:
                    case (int)WomensClothing.Casual_pants:
                        legs = 10;
                        break;
                }
            }
            if (cloak1 != null)
            {
                int cloak1int = 0;
                switch (cloak1.CurrentVariant)
                {
                    case 0: //closed, hood down
                        cloak1int = 4;
                        break;
                    case 1: //closed, hood up
                        cloak1int = 5;
                        break;
                    case 2: //one shoulder, hood up
                        cloak1int = 3;
                        break;
                    case 3: //one shoulder, hood down
                        cloak1int = 2;
                        break;
                    case 4: //open, hood down
                        cloak1int = 1;
                        break;
                    case 5: //open, hood up
                        cloak1int = 2;
                        break;
                }
                switch (cloak1.TemplateIndex)
                {
                    case (int)MensClothing.Casual_cloak:
                    case (int)WomensClothing.Casual_cloak:
                        cloak += cloak1int;
                        break;
                    case (int)MensClothing.Formal_cloak:
                    case (int)WomensClothing.Formal_cloak:
                        cloak += (cloak1int * 3);
                        break;
                }

            }
            if (cloak2 != null)
            {
                int cloak2int = 0;
                switch (cloak2.CurrentVariant)
                {
                    case 0: //closed, hood down
                        cloak2int = 4;
                        break;
                    case 1: //closed, hood up
                        cloak2int = 5;
                        break;
                    case 2: //one shoulder, hood up
                        cloak2int = 3;
                        break;
                    case 3: //one shoulder, hood down
                        cloak2int = 2;
                        break;
                    case 4: //open, hood down
                        cloak2int = 1;
                        break;
                    case 5: //open, hood up
                        cloak2int = 2;
                        break;
                }
                switch (cloak2.TemplateIndex)
                {
                    case (int)MensClothing.Casual_cloak:
                    case (int)WomensClothing.Casual_cloak:
                        cloak += cloak2int;
                        break;
                    case (int)MensClothing.Formal_cloak:
                    case (int)WomensClothing.Formal_cloak:
                        cloak += (cloak2int * 3);
                        break;
                }
            }
            Debug.Log("Clothes: Chest " + chest.ToString() + ", Legs " + legs.ToString() + ", Feet " + feet.ToString() + ", Cloak " + cloak.ToString());
            pureClothTemp = chest + feet + legs + cloak;
            temp = Mathf.Max(pureClothTemp - wetCount, 0);
            if (natTemp > 30 && playerEnterExit.IsPlayerInSunlight && hood)
            { temp -= 10; }
            return temp;
        }

        static int Armor(int natTemp)
        {
            DaggerfallUnityItem rArm = playerEntity.ItemEquipTable.GetItem(EquipSlots.RightArm);
            DaggerfallUnityItem lArm = playerEntity.ItemEquipTable.GetItem(EquipSlots.LeftArm);
            DaggerfallUnityItem chest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestArmor);
            DaggerfallUnityItem legs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsArmor);
            DaggerfallUnityItem head = playerEntity.ItemEquipTable.GetItem(EquipSlots.Head);
            int temp = 0;
            int metal = 0;

            if (chest != null)
            {
                switch (chest.NativeMaterialValue)
                {
                    case (int)ArmorMaterialTypes.Leather:
                        temp += 1;
                        break;
                    case (int)ArmorMaterialTypes.Chain:
                        temp += 1;
                        metal += 1;
                        break;
                    default:
                        temp += 3;
                        metal += 4;
                        break;
                }
            }

            if (legs != null)
            {
                switch (legs.NativeMaterialValue)
                {
                    case (int)ArmorMaterialTypes.Leather:
                        temp += 1;
                        break;
                    case (int)ArmorMaterialTypes.Chain:
                        temp += 1;
                        metal += 1;
                        break;
                    default:
                        temp += 2;
                        metal += 3;
                        break;
                }
            }

            if (lArm != null)
            {
                switch (lArm.NativeMaterialValue)
                {
                    case (int)ArmorMaterialTypes.Leather:
                        temp += 1;
                        break;
                    case (int)ArmorMaterialTypes.Chain:
                        temp += 1;
                        break;
                    default:
                        temp += 1;
                        metal += 1;
                        break;
                }

            }
            if (rArm != null)
            {
                switch (rArm.NativeMaterialValue)
                {
                    case (int)ArmorMaterialTypes.Leather:
                        temp += 1;
                        break;
                    case (int)ArmorMaterialTypes.Chain:
                        temp += 1;
                        break;
                    default:
                        temp += 1;
                        metal += 1;
                        break;
                }
            }
            if (head != null)
            {
                switch (head.NativeMaterialValue)
                {
                    case (int)ArmorMaterialTypes.Leather:
                        temp += 2;
                        break;
                    case (int)ArmorMaterialTypes.Chain:
                        temp += 2;
                        metal += 1;
                        break;
                    default:
                        temp += 1;
                        metal += 1;
                        break;
                }
            }
            if (metalHeatCool)
            {
                int metalTemp = (metal * natTemp) / 20;

                if (metalTemp > 0 && playerEnterExit.IsPlayerInSunlight && !ArmorCovered())
                {
                    temp += metalTemp;
                    if (txtCount > txtIntervals && metalTemp > 5) { DaggerfallUI.AddHUDText("Your armor is starting to heat up."); }
                }
                else if (metalTemp < 0)
                {
                    temp += metalTemp + 1;
                    if (txtCount > txtIntervals && temp < 0) { DaggerfallUI.AddHUDText("Your armor is getting cold."); }
                }
            }
            Debug.Log("Armor " + temp.ToString());
            if (temp > 0) { temp = Mathf.Max(temp - wetCount, 0); }
            return temp;
        }

        static int Water(int natTemp)
        {
            if (!wetPen) { return 0; }
            int temp = 0;
            if (GameManager.Instance.PlayerMotor.IsSwimming) { wetCount = 300; }
            if (playerIsWading) { wetCount += 50; }
            if (wetCount > 0)
            {
                if (wetCount > 300) { wetCount = 300; }
                temp = (wetCount / 10);
                wetCount--;
                if (natTemp > 10)
                {
                    wetCount -= (natTemp / 10);
                    wetCount = Mathf.Max(wetCount, 0);
                }
            }
            Debug.Log("wetCount " + wetCount.ToString());
            Debug.Log("Water Temp " + temp.ToString());
            return temp;
        }

        static void DebuffAtt(int absTemp)
        {
            int countOrTemp = Mathf.Min(absTemp - 30, attCount);
            int tempAttDebuff = Mathf.Max(0, countOrTemp);
            if (playerEntity.RaceTemplate.ID == 8)
            {
                if (absTemp > 50) { tempAttDebuff *= 2; }
                else { tempAttDebuff /= 2; }
            }
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
            Debug.Log("Attribute Debuffed " + countOrTemp.ToString());
        }

        static private void UpText(int natTemp)
        {
            if (!playerEnterExit.IsPlayerInsideDungeon)
            { SkyTxt(natTemp); }
            else
            { DungTxt(natTemp); }
        }

        static private void CharTxt(int totalTemp)
        {
            if (wetPen && wetCount > 0) { WetTxt(totalTemp); }
            string tempText = "";
            if (totalTemp > 10)
            {
                if (totalTemp > 50)
                {
                    if (playerRace.ID == (int)Races.Argonian) { tempText = "Soon you will be... too warm... to move..."; }
                    else tempText = "You cannot go on much longer in this heat...";
                }
                else if (totalTemp > 30)
                {
                    if (playerRace.ID == (int)Races.Argonian) { tempText = "The heat... is slowing you down..."; }
                    else tempText = "You are getting dizzy from the heat...";
                }
                else if (totalTemp > 20 && !txtSeverity)
                {
                    if (playerRace.ID == (int)Races.Khajiit) { tempText = "You breathe quickly, trying to cool down..."; }
                    else if (playerRace.ID == (int)Races.Argonian) { tempText = "You are absorbing too much heat..."; }
                    else tempText = "You wipe the sweat from your brow...";
                }
                else if (totalTemp > 10 && !txtSeverity)
                {
                    if (GameManager.Instance.IsPlayerInsideDungeon)
                    { tempText = ""; }
                    else { tempText = "You are a bit warm..."; }
                }
            }
            else if (totalTemp < -10)
            {
                if (totalTemp < -50)
                {
                    if (playerRace.ID == (int)Races.Argonian) { tempText = "Soon you will be... too cold... to move..."; }
                    else tempText = "Your teeth are chattering uncontrollably!";
                }
                else if (totalTemp < -30)
                {
                    if (playerRace.ID == (int)Races.Argonian) { tempText = "The cold... is slowing... you down..."; }
                    else tempText = "The cold is seeping into your bones...";
                }
                else if (totalTemp < -20 && !txtSeverity)
                {
                    if (playerRace.ID == (int)Races.Argonian) { tempText = "You are losing too much heat..."; }
                    else tempText = "You shiver from the cold...";
                }
                else if (totalTemp < -10 && !txtSeverity)
                {
                    if (GameManager.Instance.IsPlayerInsideDungeon)
                    { tempText = ""; }
                    else { tempText = "You are a bit chilly..."; }
                }
            }
            Debug.Log("[Climate & Cloaks] CharTxt AddHUDText = " + tempText);
            DaggerfallUI.AddHUDText(tempText);
        }

        static private void SkyTxt(int natTemp)
        {
            string tempText = "The weather is temperate.";

            if (natTemp > 2)
            {
                if (natTemp > 60) { tempText = "As hot as the Ashlands of Morrowind."; }
                else if (natTemp > 50) { tempText = "The heat is suffocating."; }
                else if (natTemp > 40) { tempText = "The heat is unrelenting."; }
                else if (natTemp > 30) { tempText = "The air is scorching."; }
                else if (natTemp > 20) { tempText = "The weather is hot."; }
                else if (natTemp > 10) { tempText = "The weather is nice and warm."; }
            }
            else if (natTemp < -3)
            {
                if (natTemp < -60) { tempText = "As cold as the peaks of Skyrim"; }
                else if (natTemp < -50) { tempText = "The cold weather is deadly."; }
                else if (natTemp < -40) { tempText = "The cold is unrelenting."; }
                else if (natTemp < -30) { tempText = "The weather is freezing."; }
                else if (natTemp < -20) { tempText = "The weather is cold."; }
                else if (natTemp < -10) { tempText = "The weather is nice and cool."; }
            }
            Debug.Log("[Climate & Cloaks] SkyTxt SetMidScreenText = " + tempText);
            DaggerfallUI.SetMidScreenText(tempText);
        }

        static private void DungTxt(int natTemp)
        {
            natTemp = Dungeon(natTemp);
            string tempText = "The air is temperate.";
            if (natTemp > 2)
            {
                if (natTemp > 60) { tempText = "You feel you are trapped in an oven."; }
                else if (natTemp > 50) { tempText = "The air is so warm it is suffocating."; }
                else if (natTemp > 40) { tempText = "The heat in here is awful."; }
                else if (natTemp > 30) { tempText = "The air in here is swelteringly hot."; }
                else if (natTemp > 20) { tempText = "The air in here is very warm."; }
                else if (natTemp > 10) { tempText = "The air in this place is stuffy and warm."; }
                else { tempText = "The air is somewhat warm."; }
            }
            else if (natTemp < -3)
            {
                if (natTemp < -60) { tempText = "You feel you are trapped in a glacier."; }
                else if (natTemp < -50) { tempText = "This place is as cold as ice."; }
                else if (natTemp < -40) { tempText = "The cold is unrelenting."; }
                else if (natTemp < -30) { tempText = "The air in here is freezing."; }
                else if (natTemp < -20) { tempText = "The air in here is very cold."; }
                else if (natTemp < -10) { tempText = "The air in here is chilly."; }
                else { tempText = "The air is cool."; }
            }
            Debug.Log("[Climate & Cloaks] DungTxt SetMidScreenText = " + tempText);
            DaggerfallUI.SetMidScreenText(tempText);
        }

        static private void WetTxt(int totalTemp)
        {
            string wetString = ""; 
            if (wetCount > 200) { wetString = "You are completely drenched."; }
            else if (wetCount > 100) { wetString = "You are soaking wet."; }
            else if (wetCount > 50) { wetString = "You are quite wet."; }
            else if (wetCount > 20) { wetString = "You are somewhat wet."; }
            else if (wetCount > 10) { wetString = "You are a bit wet."; }
            Debug.Log("[Climate & Cloaks] WetTxt AddHUDText = " + wetString);
            DaggerfallUI.AddHUDText(wetString);
            if (totalTemp < -10 && !GameManager.Instance.PlayerMotor.IsSwimming) { DaggerfallUI.AddHUDText("You should make camp and dry off."); }
        }
    }
}