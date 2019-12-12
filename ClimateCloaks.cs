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
        static int counterDebuff = 0;     
        static PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
        static PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
        static PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
        static EntityEffectManager playerEffectManager = playerEntity.EntityBehaviour.GetComponent<EntityEffectManager>();

        private static void TemperatureEffects_OnNewMagicRound()
        {

            //Checks that player is awake, in control, not dead, not fast traveling etc and not inside a building.
            if (playerEntity.CurrentHealth > 0
                && !playerEntity.IsResting && !DaggerfallUI.Instance.FadeBehaviour.FadeInProgress
                //(playerEntity.IsResting || !DaggerfallUI.Instance.FadeBehaviour.FadeInProgress) For mod effect while sleeping.
                && !GameManager.Instance.EntityEffectBroker.SyntheticTimeIncrease
                && !playerEnterExit.IsPlayerInsideBuilding
                && !GameManager.IsGamePaused)
            {
                int raceTemp = RaceTemp();
                int climateTemp = DungeonTemp(ClimateTemp());
                int seasonTemp = DungeonTemp(SeasonTemp());
                int weatherTemp = WeatherTemp();
                int nightTemp = NightTemp();
                int clothingTemp = ClothTemp();
                bool cloakOn = CloakSwitch();
                bool naked = NakedSwitch();
                int natTempEffect = climateTemp + nightTemp + seasonTemp + weatherTemp + raceTemp;
                int resNatTempEffect = ResistTemp(natTempEffect);
                string skyTemp = SkyTemp(resNatTempEffect);
                string dngTemp = DngTemp(resNatTempEffect);
                int armorTemp = ArmorTemp();


                //Increases armorTemp exponentially during the day.
                if (DaggerfallUnity.Instance.WorldTime.Now.IsDay && !playerEnterExit.IsPlayerInsideDungeon && cloakOn != true)
                {
                    armorTemp *= Mathf.Max(1, natTempEffect / 20);
                }

                //To counter a bug where you have 0 Stamina with no averse effects.
                if (playerEntity.CurrentFatigue == 0)
                { playerEntity.DecreaseHealth(2); }

                //If feet are bare it is too hot ot cold, you take damage.
                //Does not affect Argonians and Khajiit.
                int endBonus = 10 + (playerEntity.Stats.LiveEndurance / 2);
                int resNatTempAbs = Mathf.Abs(resNatTempEffect);
                if (playerEntity.ItemEquipTable.GetItem(EquipSlots.Feet) == null
                    && (resNatTempAbs > endBonus)
                    && (playerEntity.RaceTemplate.ID != 7 || playerEntity.RaceTemplate.ID != 8)
                    && GameManager.Instance.TransportManager.TransportMode == TransportModes.Foot)
                {
                    DaggerfallUI.AddHUDText("Your bare feet are hurting.");
                    playerEntity.DecreaseHealth(1);
                }

                int temperatureEffect = ResistTemp(natTempEffect + armorTemp + clothingTemp);

                //Shows the current temp ingame for testing purposes.
                //DaggerfallUI.SetMidScreenText("Temp: " + temperatureEffect.ToString() + " Month: " + seasonTemp.ToString() + " Nat: " + natTempEffect.ToString() + " Arm: " + armorTemp.ToString());


                //If you look up, midtext displays how the weather is.
                if (GameManager.Instance.PlayerMouseLook.Pitch <= -70)
                {
                    if (!playerEnterExit.IsPlayerInsideDungeon) { DaggerfallUI.SetMidScreenText(skyTemp); }
                    else { DaggerfallUI.SetMidScreenText(dngTemp); }
                }

                //Start of the lowest level of effects. 
                //This code need to know if it is working in positive (hot) 
                //or negative (cold) numbers to display correct text to the player.
                //Counter makes sure this triggers every 5th magicround.
                if (temperatureEffect > 10 || temperatureEffect < -10)
                {

                    ++counter;
                    if (counter > 5)
                    {
                        counter = 0;

                        //Displays text informing player how /warm/cold he feels.
                        DaggerfallUI.AddHUDText(TempText(temperatureEffect));

                        //Checks if character is naked in hot day or cold night/day. 
                        //The Naked() method returns False if you are argonian or khajiit.
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
                    }

                    //Calculation makes negative temperatureEffect into a positive.
                    temperatureEffect = Mathf.Abs(temperatureEffect);

                    //Starts decreasing fatigue as the temperature rises.
                    int fatigueTemp = temperatureEffect / 20;
                    if (playerEntity.RaceTemplate.ID == 8)
                    {
                        fatigueTemp = Mathf.Max(0, fatigueTemp - 1) * 2;
                    }
                    playerEntity.DecreaseFatigue(fatigueTemp, true);

                    //Code triggers if the temperature is over 30 (note code above making it allways positive)
                    if (temperatureEffect > 30)
                    {
                        //counterDebuff staggers Attribute debuffs. So instead instantly debuffing you, it ticks up by 
                        //1 each round until it hits the current temperature. 
                        if (counterDebuff < 80) { counterDebuff++; }
                        int countOrTemp = Mathf.Min(temperatureEffect - 30, counterDebuff);
                        int tempAttDebuff = Mathf.Max(0, countOrTemp);
                        //Staggered Argonian temperature effect. His later but harder.
                        if (playerEntity.RaceTemplate.ID == 8)
                            if (tempAttDebuff > 20) { tempAttDebuff *= 2; }
                            else { tempAttDebuff = 0; }
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

                        //Code for extreme heat/cold.
                        //The damage and text will trigger faster and faster as the temp increases.
                        if (temperatureEffect > 50)
                        {
                            counterDmg += (temperatureEffect - 50);
                            if (counterDmg > 10)
                            {
                                counterDmg = 0;
                                DaggerfallUI.AddHUDText("You cannot go on much longer in this weather...");
                                playerEntity.DecreaseHealth(1);
                            }
                        }
                        else { counterDmg = 0; }
                    }
                    else { counterDebuff = 0; }  
                }               
            }
            else { counter = 0; counterDmg = 0; counterDebuff = 0; }
        }
        //The counter resets above is so if the player manages to cool off, the increasing negative effects will start from the
        //beginning next time the player gets too warm/cold.







       //Methods called by TemperatureEffects_OnNewMagicRound


        static int ClimateTemp()
        {
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

        static int NightTemp()
        {
            bool isNight = DaggerfallUnity.Instance.WorldTime.Now.IsNight;
            int climate = playerGPS.CurrentClimateIndex;

            if (isNight && !playerEnterExit.IsPlayerInsideDungeon)
            {
                switch (climate)
                {
                    case (int)MapsFile.Climates.Desert2:
                        return -55;
                    case (int)MapsFile.Climates.Desert:
                        return -45;
                    case (int)MapsFile.Climates.Rainforest:
                    case (int)MapsFile.Climates.Subtropical:
                    case (int)MapsFile.Climates.Swamp:
                    case (int)MapsFile.Climates.Woodlands:
                    case (int)MapsFile.Climates.HauntedWoodlands:
                    case (int)MapsFile.Climates.MountainWoods:
                        return -20;
                    case (int)MapsFile.Climates.Mountain:
                        return -30;
                }
            }
            return 0;
        }

        static int SeasonTemp()
        {
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

        static int WeatherTemp()
        {
            int temp = 0;
            bool cloakOn = CloakSwitch();
            if (!playerEnterExit.IsPlayerInsideDungeon)
            {
                bool isRaining = GameManager.Instance.WeatherManager.IsRaining;
                bool isOvercast = GameManager.Instance.WeatherManager.IsOvercast;
                bool isStorming = GameManager.Instance.WeatherManager.IsStorming;
                bool isSnowing = GameManager.Instance.WeatherManager.IsSnowing;

                if (cloakOn == true)
                {
                    temp += 15;
                }
                if (isRaining)
                {
                    temp -= 20;
                }
                else if (isStorming)
                {
                    temp -= 25;
                }
                else if (isSnowing)
                {
                    temp -= 18;
                }
                else if (isOvercast)
                {
                    temp -= 5;
                }
            }
            return temp;
        }





        static int ClothTemp()
        {
            var chestCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.ChestClothes);
            var feetCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.Feet);
            var legsCloth = playerEntity.ItemEquipTable.GetItem(EquipSlots.LegsClothes);
            var cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            var cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);
            var gender = playerEntity.Gender;
            int chest = 0;
            int feet = 0;
            int legs = 0;
            int cloak = 0;
            int temp = 0;
            bool cloakOn = CloakSwitch();

            if (chestCloth != null)
            {
                if (gender == Genders.Male)
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
                            chest = 1;
                            break;
                        case (int)MensClothing.Short_shirt_unchangeable:
                        case (int)MensClothing.Short_shirt:
                        case (int)MensClothing.Short_shirt_with_belt:
                            chest = 5;
                            break;
                        case (int)MensClothing.Short_tunic:
                        case (int)MensClothing.Toga:
                        case (int)MensClothing.Short_shirt_closed_top:
                        case (int)MensClothing.Short_shirt_closed_top2:
                        case (int)MensClothing.Long_shirt:
                        case (int)MensClothing.Long_shirt_with_belt:
                        case (int)MensClothing.Long_shirt_unchangeable:
                            chest = 8;
                            break;
                        case (int)MensClothing.Open_Tunic:
                        case (int)MensClothing.Long_shirt_closed_top:
                        case (int)MensClothing.Long_shirt_closed_top2:
                        case (int)MensClothing.Kimono:
                            chest = 10;
                            break;
                        case (int)MensClothing.Priest_robes:
                        case (int)MensClothing.Anticlere_Surcoat:
                        case (int)MensClothing.Formal_tunic:
                        case (int)MensClothing.Reversible_tunic:                       
                        case (int)MensClothing.Dwynnen_surcoat:
                        case (int)MensClothing.Plain_robes:
                            chest = 15;
                            break;
                    }
                }
                else
                {
                    switch (chestCloth.TemplateIndex)
                    {
                        case (int)WomensClothing.Brassier:
                        case (int)WomensClothing.Formal_brassier:
                        case (int)WomensClothing.Eodoric:
                        case (int)WomensClothing.Formal_eodoric:
                        case (int)WomensClothing.Vest:
                            chest = 1;
                            break;
                        case (int)WomensClothing.Short_shirt:
                        case (int)WomensClothing.Short_shirt_belt:
                        case (int)WomensClothing.Short_shirt_unchangeable:
                            chest = 5;
                            break;
                        case (int)WomensClothing.Short_shirt_closed:
                        case (int)WomensClothing.Short_shirt_closed_belt:
                        case (int)WomensClothing.Long_shirt:
                        case (int)WomensClothing.Long_shirt_belt:
                        case (int)WomensClothing.Long_shirt_unchangeable:
                        case (int)WomensClothing.Peasant_blouse:
                        case (int)WomensClothing.Strapless_dress:
                            chest = 8;
                            break;
                        case (int)WomensClothing.Evening_gown:
                        case (int)WomensClothing.Casual_dress:
                        case (int)WomensClothing.Long_shirt_closed:
                        case (int)WomensClothing.Open_tunic:
                            chest  = + 10;
                            break;
                        case (int)WomensClothing.Priestess_robes:
                        case (int)WomensClothing.Plain_robes:
                        case (int)WomensClothing.Long_shirt_closed_belt:
                        case (int)WomensClothing.Day_gown:
                            chest = 15;
                            break;
                    }
                }
            }
            if (feetCloth != null)
            {
                if (gender == Genders.Male)
                {
                    switch (feetCloth.TemplateIndex)
                    {
                        case (int)MensClothing.Sandals:
                            feet = 0;
                            break;
                        case (int)MensClothing.Shoes:
                            feet = 2;
                            break;
                        case (int)MensClothing.Tall_Boots:
                        case (int)MensClothing.Boots:
                            feet = 5;
                            break;
                    }
                }
                else
                {
                    switch (feetCloth.TemplateIndex)
                    {
                        case (int)WomensClothing.Sandals:
                            feet = 0;
                            break;
                        case (int)WomensClothing.Shoes:
                            feet = 2;
                            break;
                        case (int)WomensClothing.Tall_boots:
                        case (int)WomensClothing.Boots:
                            feet = 5;
                            break;
                    }
                }

            }
            if (legsCloth != null)
            {
                if (gender == Genders.Male)
                {
                    switch (legsCloth.TemplateIndex)
                    {
                        case (int)MensClothing.Loincloth:
                        case (int)MensClothing.Wrap:
                            legs = 1;
                            break;
                        case (int)MensClothing.Khajiit_suit:
                            legs = 2;
                            break;
                        case (int)MensClothing.Short_skirt:
                            legs = 4;
                            break;
                        case (int)MensClothing.Long_Skirt:
                            legs = 8;
                            break;
                        case (int)MensClothing.Casual_pants:
                        case (int)MensClothing.Breeches:
                            legs = 10;
                            break;
                    }
                }
                else
                {
                    switch (legsCloth.TemplateIndex)
                    {
                        case (int)WomensClothing.Loincloth:
                        case (int)WomensClothing.Wrap:
                            legs = 1;
                            break;
                        case (int)WomensClothing.Khajiit_suit:
                            legs = 2;
                            break;
                        case (int)WomensClothing.Tights:
                            legs = 4;
                            break;
                        case (int)WomensClothing.Long_skirt:
                            legs = 8;
                            break;
                        case (int)WomensClothing.Casual_pants:
                            legs = 10;
                            break;
                    }
                }
            }
            if (cloakOn == true)
            {
                if (cloak1 != null && cloak2 != null) { cloak += 35; }
                else { cloak += 15; }            
            }           
            temp = chest + feet + legs + cloak;
            return temp;  
        }

        static int ArmorTemp()
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
                        temp += 2;
                        break;
                    default:
                        temp += 5;
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
                        temp += 2;
                        break;
                    default:
                        temp += 4;
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


            return temp;
        }

        static int RaceTemp()
        {
            switch (playerEntity.RaceTemplate.ID)
            {
                case (int)Races.Breton:
                    return +5;
                case (int)Races.Redguard:
                    return -5;
                case (int)Races.Nord:
                    return +5;
                case (int)Races.DarkElf:
                    return -5;
                case (int)Races.HighElf:
                    return 0;
                case (int)Races.WoodElf:
                    return 0;
                case (int)Races.Khajiit:
                    return 0;
                case (int)Races.Argonian:
                    return -10;
            }
            return 0;
        }



        static int ResistTemp(int temp)
        {
            int resFire = playerEntity.Resistances.LiveFire;
            int resFrost = playerEntity.Resistances.LiveFrost;

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

        static bool CloakSwitch()
        {
            var cloak1 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak1);
            var cloak2 = playerEntity.ItemEquipTable.GetItem(EquipSlots.Cloak2);

            if (cloak1 != null || cloak2 != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static int DungeonTemp(int temp)
        {
            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                temp = temp / 2;
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
                    tempText = "You feel like you are burning up!";
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
            if (temperatureEffect < -10)
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

        static string SkyTemp(int natTemp)
        {
            string tempText = "The weather is nice.";

            if (natTemp > 2)
            {
                if (natTemp > 60)
                {
                    tempText = "A dragon would seek shade in this weather.";
                }
                else if (natTemp > 50)
                {
                    tempText = "Only the hardiest go outside in this heat.";
                }
                else if (natTemp > 40)
                {
                    tempText = "The heat is unrelenting.";
                }
                else if (natTemp > 30)
                {
                    tempText = "The weather is scorching.";
                }
                else if (natTemp > 20)
                {
                    tempText = "The weather is hot.";
                }
                else if (natTemp > 10)
                {
                    tempText = "The weather is nice and warm.";
                }
            }
            else if (natTemp < -3)
            {
                if (natTemp < -60)
                {
                    tempText = "An Ice Atronach would light a fire in this weather.";
                }
                else if (natTemp < -50)
                {
                    tempText = "Only the hardiest go outside in this cold.";
                }
                else if (natTemp < -40)
                {
                    tempText = "The cold is unrelenting.";
                }
                else if (natTemp < -30)
                {
                    tempText = "The weather is freezing.";
                }
                else if (natTemp < -20)
                {
                    tempText = "The weather is cold.";
                }
                else if (natTemp < -10)
                {
                    tempText = "The weather is nice and cool.";
                }
            }
            return tempText;
        }

        static string DngTemp(int natTemp)
        {
            string tempText = "The air is temperate.";

            if (natTemp > 2)
            {
                if (natTemp > 60)
                {
                    tempText = "You feel as though you are trapped in an oven.";
                }
                else if (natTemp > 50)
                {
                    tempText = "The air is so warm it is suffocating.";
                }
                else if (natTemp > 40)
                {
                    tempText = "The heat in here is awful.";
                }
                else if (natTemp > 30)
                {
                    tempText = "The air in here is swelteringly hot.";
                }
                else if (natTemp > 20)
                {
                    tempText = "The air in here is very warm.";
                }
                else if (natTemp > 10)
                {
                    tempText = "The air in this place is stuffy and warm.";
                }
            }
            else if (natTemp < -3)
            {
                if (natTemp < -60)
                {
                    tempText = "You feel as though you are trapped in a glacier.";
                }
                else if (natTemp < -50)
                {
                    tempText = "This place is as cold as ice.";
                }
                else if (natTemp < -40)
                {
                    tempText = "The cold is unrelenting.";
                }
                else if (natTemp < -30)
                {
                    tempText = "The air in here is freezing.";
                }
                else if (natTemp < -20)
                {
                    tempText = "The air in here is very cold.";
                }
                else if (natTemp < -10)
                {
                    tempText = "The air in here is chilly.";
                }
            }
            return tempText;
        }
    }   
}