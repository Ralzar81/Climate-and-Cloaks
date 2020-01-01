// Project:         Climates & Cloaks mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2019 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;


namespace ClimateCloaks
{
    public class ClimateCloaks : MonoBehaviour
    {
        static Mod mod;

        [Invoke(StateManager.StateTypes.Game, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<ClimateCloaks>();
            EntityEffectBroker.OnNewMagicRound += TemperatureEffects_OnNewMagicRound;

        }



        static bool armorSun = true;
        static bool armorSunHalf = false;
        static bool statusLookUp = false;
        static bool statusInterval = true;
        static bool nudePen = true;
        static bool fatPen = true;
        static bool feetPen = true;
        static bool dungTemp = true;
        static bool wetPen = true;


        void Awake()
        {
            ModSettings settings = mod.GetSettings();

            int armorSunValue = settings.GetValue<int>("Features", "armorSunHeat");
            if (armorSunValue == 0)
            {
                armorSun = false;
            }
            else if (armorSunValue == 1)
            {
                armorSunHalf = true;
            }



            int statusTextValue = settings.GetValue<int>("Features", "statusText");
            if (statusTextValue == 1)
            {
                statusLookUp = true;
                statusInterval = false;
            }
            else if (statusTextValue == 2)
            {
                statusLookUp = true;
            }

            bool nudePenSet = settings.GetBool("Features", "damageWhenNude");
            bool feetPenSet = settings.GetBool("Features", "damageWhenBareFoot");
            bool fatPenSet = settings.GetBool("Features", "DamageWhen 0Fatigue");
            bool dungTempSet = settings.GetBool("Features", "TemperatureEffectsInDungeons");
            bool wetPenSet = settings.GetBool("Features", "WetFromSwimmingAndRain");

            if (!nudePenSet) { nudePen = false; }
            if (!feetPenSet) { feetPen = false; }
            if (!fatPenSet) { fatPen = false; }
            if (!dungTempSet) { dungTemp = false; }
            if (!wetPenSet) { wetPen = false; }

            Debug.Log(
                "C&C Settings: " +
                "ArmSun " + armorSun.ToString() +
                ", ArmSunHalf " + armorSunHalf.ToString() +
                ", StatusUp " + statusLookUp.ToString() +
                ", StatusInt" + statusInterval.ToString() +
                ", Nude" + nudePen.ToString() +
                ", Feet" + feetPen.ToString() +
                ", 0 Fatigue" + fatPen.ToString() +
                ", Dungeon" + dungTemp.ToString() +
                ", Water" + wetPen.ToString()
                );

            mod.IsReady = true;
            Debug.Log("Climates & Cloaks ready");
        }


        static PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
        static PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
        static PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
        static EntityEffectManager playerEffectManager = playerEntity.EntityBehaviour.GetComponent<EntityEffectManager>();
        static RaceTemplate playerRace = playerEntity.RaceTemplate;

        static private int txtCount = 4;
        static private int wetCount = 0;
        static private int attCount = 0;


        private static void TemperatureEffects_OnNewMagicRound()
        {
            int offSet = -5; //used to make small adjustments to the mod. Negative numbers makes the character freeze more easily.
            int natTemp = Resist(Climate() + Month() + Time() + Weather());
            int charTemp = Resist(RaceTemp() + Clothes(natTemp) + Armor(natTemp) + offSet - Water(natTemp));
            int totalTemp = Dungeon(natTemp) + charTemp;
            int absTemp = Mathf.Abs(totalTemp);
            txtCount++;

            //Check that effects are only applied while player is active.
            if (playerEntity.CurrentHealth > 0
                && !playerEntity.IsResting
                && !DaggerfallUI.Instance.FadeBehaviour.FadeInProgress
                && !GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease
                && !playerEnterExit.IsPlayerInsideBuilding
                && !GameManager.IsGamePaused
                && ((dungTemp && playerEnterExit.IsPlayerInsideDungeon) || !playerEnterExit.IsPlayerInsideDungeon))
            {
                Debug.Log("C&C active round start");
                //To counter a bug where you have 0 Stamina with no averse effects.
                if (fatPen && playerEntity.CurrentFatigue == 0)
                { playerEntity.DecreaseHealth(2); }

                //Basic mod effect starts here at +/- 10+ by decreasing fatigue.
                if (absTemp > 10 && !playerEntity.IsInBeastForm)
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

                //If you look up, midtext displays how the weather is.
                if (GameManager.Instance.PlayerMouseLook.Pitch <= -70)
                {
                    UpText(natTemp);
                    if (statusLookUp) { CharTxt(totalTemp);}                    
                }

                //Apply damage for being naked or walking on foot.
                if (playerRace.ID != (int)Races.Argonian && playerRace.ID != (int)Races.Khajiit)
                {
                    NakedDmg(natTemp);
                    FeetDmg(natTemp);
                }
                if (statusInterval && !playerEntity.IsInBeastForm)
                {
                    if (txtCount > 5) { CharTxt(totalTemp);}
                }
                if (txtCount > 5) { txtCount = 0; }
                Debug.Log("natTemp " + natTemp.ToString() + ", charTemp " + charTemp.ToString() + ", totalTemp " + totalTemp.ToString());
            }
            else
            {
                //When inside a house, resting or traveling, counters start to reset.
                txtCount = 4;
                wetCount = Mathf.Max(wetCount - 2, 0);
                attCount = Mathf.Max(attCount - 2, 0);
            }
        }


        //Resist adjusts the number (usually NatTemp or CharTemp) for class resistances.
        static int Resist(int temp)
        {
            int resFire = playerEntity.Resistances.LiveFire;
            int resFrost = playerEntity.Resistances.LiveFrost;
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
        static int Dungeon(int temp)
        {
            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                temp /= 2;
                Debug.Log("C&C Dungeon effect");
            }
            return temp;
        }

        //If naked, may take damage from temperatures.
        static void NakedDmg(int temp)
        {
            if (!nudePen) { return; }
            var chest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            var legs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsClothes);
            var aChest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestArmor);
            var aLegs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsArmor);
            bool cTop = false;
            bool cBottom = false;

            if (chest != null)
            {
                switch (chest.TemplateIndex)
                {
                    case (int)MensClothing.Short_tunic:
                    case (int)MensClothing.Toga:
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
            if (Cloak() || aChest != null) { cTop = true; }
            if (aLegs != null) { cBottom = true; }
            if (!cTop || !cBottom)
            {
                Debug.Log("Character is Naked");
                if (playerEnterExit.IsPlayerInSunlight && temp >= 10 && txtCount > 5)
                {
                    if (playerEntity.RaceTemplate.ID != (int)Races.DarkElf && playerEntity.RaceTemplate.ID != (int)Races.Redguard)
                    { if (temp > 30) { playerEntity.DecreaseHealth(1); } }
                    else
                    { playerEntity.DecreaseHealth(1); }
                    DaggerfallUI.AddHUDText("The sun burns your bare skin.");
                }
                else if (temp <= -10)
                {
                    playerEntity.DecreaseHealth((temp + 10) / 10);
                    DaggerfallUI.AddHUDText("The cold air numbs your bare skin.");
                }
            }
            else { Debug.Log("Character is not Naked"); }
        }

        //If bare feet, may take damage from temperatures.
        static void FeetDmg(int natTemp)
        {
            if (!feetPen) { return; }
            int endBonus = 5 + (playerEntity.Stats.LiveEndurance / 2);
            if (playerEntity.ItemEquipTable.GetItem(EquipSlots.Feet) == null
               && (natTemp > endBonus)
               && GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot
               && txtCount > 5
               && feetPen)
            {
                DaggerfallUI.AddHUDText("Your bare feet are hurting.");
                playerEntity.DecreaseHealth(1);
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
                        if (Cloak() && HoodUp())
                        {
                            wetCount += 0;
                        }
                        else if (Cloak() && !HoodUp())
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
                        if (Cloak() && HoodUp())
                        {
                            wetCount += 1;
                        }
                        else if (Cloak() && !HoodUp())
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
                        if (Cloak() && HoodUp())
                        {
                            wetCount += 0;
                        }
                        else if (Cloak() && !HoodUp())
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
                else if (playerRace.ID == (int)Races.Vampire)
                {
                    int heat = Resist(Climate() + Month() + Time());
                    if (heat > 0 && DaggerfallUnity.Instance.WorldTime.Now.IsDay && !HoodUp())
                    {
                        playerEntity.DecreaseHealth(heat / 5);
                    }
                }
            }
            return temp;
        }

        static int Time()
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
            var cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            var cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);

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

        static bool HoodUp()
        {
            Debug.Log("Checking Hood");
            var cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            var cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);

            if (cloak1 != null)
            {
                switch (cloak1.CurrentVariant)
                {
                    case 0:
                        return false;
                    case 1:
                        return true;
                    case 2:
                        return true;
                    case 3:
                        return false;
                    case 4:
                        return false;
                    case 5:
                        return true;
                }
            }
            else if (cloak2 != null)
            {
                switch (cloak2.CurrentVariant)
                {
                    case 0:
                        return false;
                    case 1:
                        return true;
                    case 2:
                        return true;
                    case 3:
                        return false;
                    case 4:
                        return false;
                    case 5:
                        return true;
                }
            }
            return false;
        }


        static int RaceTemp()
        {

            Debug.Log("Calculating RaceTemp");

 
            switch (playerEntity.BirthRaceTemplate.ID)
            {
                case (int)Races.Nord:
                    return 10;
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
            var chestCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            var feetCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.Feet);
            var legsCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsClothes);
            var cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            var cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);
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
            if (feetCloth != null)
            {
                switch (feetCloth.TemplateIndex)
                {
                    case (int)MensClothing.Sandals:
                    case (int)WomensClothing.Sandals:
                        feet = 0;
                        break;
                    case (int)MensClothing.Shoes:
                    case (int)WomensClothing.Shoes:
                        feet = 2;
                        break;
                    case (int)MensClothing.Tall_Boots:
                    case (int)MensClothing.Boots:
                    case (int)WomensClothing.Tall_boots:
                    case (int)WomensClothing.Boots:
                        feet = 5;
                        break;
                }
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
                    case (int)WomensClothing.Casual_pants:
                        legs = 4;
                        break;
                    case (int)MensClothing.Long_Skirt:
                    case (int)WomensClothing.Long_skirt:
                        legs = 8;
                        break;
                    case (int)MensClothing.Casual_pants:
                    case (int)MensClothing.Breeches:
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
            temp = Mathf.Max(chest + feet + legs + cloak - wetCount, 0);
            if (natTemp > 30 && playerEnterExit.IsPlayerInSunlight && HoodUp())
            { temp -= 10; }
            return temp;
        }

        static int Armor(int natTemp)
        {
            var rArm = playerEntity.ItemEquipTable.GetItem(EquipSlots.RightArm);
            var lArm = playerEntity.ItemEquipTable.GetItem(EquipSlots.LeftArm);
            var chest = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestArmor);
            var legs = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsArmor);
            var head = playerEntity.ItemEquipTable.GetItem(EquipSlots.Head);
            int temp = 0;

            if (chest != null)
            {
                switch (chest.NativeMaterialValue)
                {
                    case (int)ArmorMaterialTypes.Leather:
                        temp += 1;
                        break;
                    case (int)ArmorMaterialTypes.Chain:
                        temp += 1;
                        break;
                    default:
                        temp += 3;
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
                        break;
                    default:
                        temp += 2;
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
                        temp += 2;
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
                        temp += 2;
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
                        break;
                    default:
                        temp += 2;
                        break;
                }
            }
            if (armorSun)
            {
                if (natTemp > 10 && playerEnterExit.IsPlayerInSunlight && !Cloak())
                {
                    temp *= (natTemp / 10);
                    if (armorSunHalf && temp >= 20)
                    { temp /= 2; }
                }
                else if (natTemp < -10)
                {
                    temp -= (natTemp / 10);
                    if (armorSunHalf)
                    { temp /= 2; }
                }
            }
            Debug.Log("Armor " + temp.ToString());
            temp = Mathf.Max(temp - wetCount, 0);
            return temp;
        }

        static int Water(int natTemp)
        {
            if (!wetPen) { return 0; }
            int temp = 0;
            bool playerOnExteriorWater = (GameManager.Instance.PlayerMotor.OnExteriorWater == PlayerMotor.OnExteriorWaterMethod.Swimming || GameManager.Instance.PlayerMotor.OnExteriorWater == PlayerMotor.OnExteriorWaterMethod.WaterWalking);
            if (GameManager.Instance.PlayerMotor.IsSwimming) { wetCount = 300; }
            if (playerOnExteriorWater) { wetCount += 20; }
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
                    else tempText = "You cannot go one much longer in this heat...";
                }
                else if (totalTemp > 30)
                {
                    if (playerRace.ID == (int)Races.Argonian) { tempText = "The heat... is slowing you down..."; }
                    else tempText = "You are getting dizzy from the heat...";
                }
                else if (totalTemp > 10)
                {
                    if (playerRace.ID == (int)Races.Khajiit) { tempText = "You breathe quickly, trying to cool down..."; }
                    else if (playerRace.ID == (int)Races.Argonian) { tempText = "You are absorbing too much heat..."; }
                    else tempText = "You wipe the sweat from your brow...";
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
                    if (playerRace.ID == (int)Races.Argonian) { tempText = "The cold... is slowing you down..."; }
                    else tempText = "The cold is seeping into your bones...";
                }
                else if (totalTemp < -10)
                {
                    if (playerRace.ID == (int)Races.Argonian) { tempText = "You are loosing too much heat..."; }
                    else tempText = "You shiver from the cold...";
                }
            }
            DaggerfallUI.AddHUDText(tempText);
        }

        static private void SkyTxt(int natTemp)
        {
            string tempText = "The air is temperate.";

            if (natTemp > 2)
            {
                if (natTemp > 60) { tempText = "As warm as a volcano."; }
                else if (natTemp > 50) { tempText = "The air is so warm it is suffocating."; }
                else if (natTemp > 40) { tempText = "The heat in here is awful."; }
                else if (natTemp > 30) { tempText = "The air in here is swelteringly hot."; }
                else if (natTemp > 20) { tempText = "The air in here is very warm."; }
                else if (natTemp > 10) { tempText = "The air in this place is stuffy and warm."; }
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
            DaggerfallUI.AddHUDText(wetString);
            if (totalTemp < -10 && !GameManager.Instance.PlayerMotor.IsSwimming) { DaggerfallUI.AddHUDText("You should make camp and dry off."); }
        }
    }
}