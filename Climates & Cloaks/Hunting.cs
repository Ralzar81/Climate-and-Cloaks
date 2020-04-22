// Project:         Climates & Cloaks mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
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
using DaggerfallWorkshop.Game.Serialization;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Questing.Actions;

namespace ClimatesCloaks
{
    public class Hunting
    {

        static int climate;
        static int luckMod = GameManager.Instance.PlayerEntity.Stats.LiveLuck / 10;
        static bool playerHasBow;

        //Alternative YES/NO popup that onle has one line of text.
        //private static void QuestionBox()
        //{
        //    DaggerfallMessageBox questionBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallMessageBox.CommonMessageBoxButtons.YesNo, "Test", DaggerfallUI.UIManager.TopWindow);
        //    questionBox.OnButtonClick += Answer_OnButtonClick;
        //    DaggerfallUI.UIManager.PushWindow(questionBox);

        //}

        //private static void Answer_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        //{
        //    if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.No)
        //    {
        //        DaggerfallUI.AddHUDText("NO");
        //    }
        //    else
        //    {
        //        DaggerfallUI.AddHUDText("YES");
        //    }
        //    sender.CloseWindow();
        //}


        //Uses OnNewMagicRound to check for animals to hunt.
        public static void Hunting_OnNewMagicRound()
        {
            if (!GameManager.Instance.AreEnemiesNearby()
                && !SaveLoadManager.Instance.LoadInProgress
                && !GameManager.Instance.PlayerEnterExit.IsPlayerInsideBuilding
                && GameManager.Instance.IsPlayerOnHUD
                && !GameManager.IsGamePaused
                && !GameManager.Instance.PlayerGPS.IsPlayerInLocationRect)
            {
                playerHasBow = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Short_Bow) != null && GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Long_Bow) != null && GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Arrow) != null ? false : true;
                luckMod = GameManager.Instance.PlayerEntity.Stats.LiveLuck / 10;
                int roll = UnityEngine.Random.Range(1, 101) - luckMod;
                Debug.Log("[Hunting] Hunting_OnNewMagicRound() roll = " + roll.ToString());
                climate = GameManager.Instance.PlayerGPS.CurrentClimateIndex;
                if ((climate == (int)MapsFile.Climates.Desert || climate == (int)MapsFile.Climates.Desert2) && roll < 2)
                {
                    DesertHuntingRoll();
                }
                else if (climate == (int)MapsFile.Climates.Subtropical && roll < 3)
                {
                    SubtropicalHuntingRoll();
                }
                else if ((climate == (int)MapsFile.Climates.Swamp || climate == (int)MapsFile.Climates.Rainforest) && roll < 5)
                {
                    SwampHuntingRoll();
                }
                else if ((climate == (int)MapsFile.Climates.Woodlands || climate == (int)MapsFile.Climates.HauntedWoodlands) && roll < 100)
                {
                    WoodsHuntingRoll();

                }
                else if ((climate == (int)MapsFile.Climates.Mountain || climate == (int)MapsFile.Climates.MountainWoods) && roll < 3)
                {
                    MountainHuntingRoll();
                }
            }
        }


        //Method for checking hunting in desert. Going to either DesertHunting_OnButtonClick or DesertWater_OnButtonClick.
        private static void DesertHuntingRoll()
        {
            int roll = UnityEngine.Random.Range(1,11);
            DaggerfallMessageBox huntingPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            if (roll > 7 && ClimateCloaks.gotDrink)
            {
                string[] message = {
                            "You spot a cluster of greener vegetation off in the distance.",
                            " ",
                            "There might be a source of water here where you could refill",
                            "your waterskin.",
                            "",
                            "Do you wish to spend some time searching for water?"                           
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += DesertWater_OnButtonClick;
            }
            else
            {
                string[] message = {
                            "You spot a cluster of rocks in the distance..",
                            " ",
                            "There might be animals seeking shelter between",
                            "them to avoid the harsh sun..",
                            "",
                            "Do you wish to spend some time checking the rocks?"
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += DesertHunting_OnButtonClick;
            }            
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
            
            huntingPopUp.Show();
        }
        //When clicking yes, do a DesertHuntingCheck
        private static void DesertHunting_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                MovePlayer();
                TimeSkip();
                DesertHuntingCheck();
            }
            else { sender.CloseWindow(); }
        }
        //Rolls for luck and skill checks to see if and how much meat you find in the desert.
        private static void DesertHuntingCheck()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            bool playerHasBow = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Short_Bow) != null && GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Long_Bow) != null ? false : true;
            int skillSum = 0;
            int huntingRoll = UnityEngine.Random.Range(1, 101);
            int luckRoll = UnityEngine.Random.Range(1, 102);
            int genRoll = UnityEngine.Random.Range(1, 101);
            Poisons poisonType = (Poisons)UnityEngine.Random.Range(128, 140);

            //Very unlucky
            if (luckRoll > GameManager.Instance.PlayerEntity.Stats.LiveLuck+20)
            {
                string[] messages = new string[] { "You spend some time searching among the", "rocks, when you suddenly hear a sound...", "", "It seems you are about to become another hunters meal!" };
                ClimateCloaks.TextPopup(messages);
                SpawnBeast();
            }
            //Lucky. Has bow
            else if (luckRoll <= GameManager.Instance.PlayerEntity.Stats.LiveLuck && playerHasBow)
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Archery);               
                if (huntingRoll < skillSum)
                {
                    string[] messages = new string[] { "You spot a snake among the rocks. You take careful aim and nail it with an arrow.", "", "You spend some time butchering the snake." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(1);
                }
                else
                {
                    string[] messages = new string[] { "You spot a snake among the rocks and take aim with your bow.", "You miss and the snake slithers away.", "", "You spend some more time searching, but no luck." };
                    ClimateCloaks.TextPopup(messages);
                }
            }
            //Lucky. No bow
            else if (luckRoll <= GameManager.Instance.PlayerEntity.Stats.LiveLuck && !playerHasBow)
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth);
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.CriticalStrike);
                if (huntingRoll < skillSum)
                {
                    string[] messages = new string[] { "While searching the rocks, you come upon a sleeping snake.", "Your hand shoots out, grabbing the snakes tail.", "You whip it around and smack it into a rock.", "", "You spend some time butchering the snake." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(1);
                }
                else
                {
                    string[] messages = new string[] { "While searching the rocks, you come upon a sleeping snake.", "You attempt to get within striking distance, but the snake wakes and slither underneath a large rock.", "", "You spend some more time searching, but no luck." };
                    ClimateCloaks.TextPopup(messages);
                }
            }
            //Unlucky. Has bow
            else if (playerHasBow)
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Archery);
                if (huntingRoll < skillSum && genRoll < GameManager.Instance.PlayerEntity.Stats.LiveIntelligence)
                {
                    string[] messages = new string[] { "You spot a snake among the rocks. You take careful aim and nail it with an arrow.", "", "You poke the snake to make sure it is dead before picking it up.", "", "You spend some time butchering the snake." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(1);
                }
                else if (huntingRoll < skillSum)
                {
                    string[] messages = new string[] { "You spot a snake among the rocks. You take careful aim and nail it with an arrow.", "", "As you pick up the dead snake, it suddenly twitches and sinks its fangs into your hand.", "You spend some time butchering the snake.", "", "You hope the snake was not poisonous..." };
                    ClimateCloaks.TextPopup(messages);
                    DaggerfallWorkshop.Game.Formulas.FormulaHelper.InflictPoison(GameManager.Instance.PlayerEntity, poisonType, false);
                    GiveMeat(1);
                }
                else
                {
                    string[] messages = new string[] { "You miss the snake and it slithers away.", "", "As you search among the rocks you suddenly feel a sharp pain on your leg.", "", "You hope whatever bit you was not poisonous..." };
                    ClimateCloaks.TextPopup(messages);
                    DaggerfallWorkshop.Game.Formulas.FormulaHelper.InflictPoison(GameManager.Instance.PlayerEntity, poisonType, false);
                }
            }
            //Unlucky. No bow
            else if (!playerHasBow)
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth);
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.CriticalStrike);
                if (huntingRoll < skillSum && genRoll+30 < GameManager.Instance.PlayerEntity.Stats.LiveSpeed)
                {
                    string[] messages = new string[] { "While searching the rocks, you come upon a snake.", "Before the snake has time to lunge, your grab it.", "You whip the snake around and smack it into a rock.", "", "You spend some time butchering the snake." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(1);
                }
                else if (huntingRoll < skillSum)
                {
                    string[] messages = new string[] { "While searching the rocks, you come upon a snake.", "Its head shoots out, sinking its fangs into your hand.", "You whip it around and smack it into a rock.", "", "You spend some time butchering the snake.", "", "You hope the snake was not poisonous..." };
                    ClimateCloaks.TextPopup(messages);
                    DaggerfallWorkshop.Game.Formulas.FormulaHelper.InflictPoison(GameManager.Instance.PlayerEntity, poisonType, false);
                    GiveMeat(1);
                }
                else
                {
                    string[] messages = new string[] { "While searching the rocks, you come upon a snake.", "Its head shoots out, sinking its fangs into your hand.", "You let out a yelp as the snake dislodges and slithers under a rock.", "", "You hope the snake was not poisonous..." };
                    ClimateCloaks.TextPopup(messages);
                    DaggerfallWorkshop.Game.Formulas.FormulaHelper.InflictPoison(GameManager.Instance.PlayerEntity, poisonType, false);
                }
            }
        }

        //Method for checking hunting in tropics. Going to either SubtropicalHunting_OnButtonClick or DesertWater_OnButtonClick.
        private static void SubtropicalHuntingRoll()
        {
            int roll = UnityEngine.Random.Range(1, 11);
            DaggerfallMessageBox huntingPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            if (roll > 7 && ClimateCloaks.gotDrink)
            {
                string[] message = {
                            "You spot a cluster of greener vegetation off in the distance.",
                            " ",
                            "There might be a source of water here where you could refill",
                            "your waterskin.",
                            "",
                            "Do you wish to spend some time searching for water?"
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += DesertWater_OnButtonClick;
            }
            else
            {
                string[] message = {
                            "You spot a a gathering of trees in the distance..",
                            " ",
                            "There might be some ripe fruits to pick from them.",
                            "",
                            "Do you wish to spend some time checking the trees?"
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += SubtropicalHunting_OnButtonClick;
            }
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
            huntingPopUp.Show();
        }
        //When clicking yes, skip 1 hour and do a SubtropicalHuntingCheck
        private static void SubtropicalHunting_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                MovePlayer();
                TimeSkip();
                SubtropicalHuntingCheck();
            }
            else { sender.CloseWindow(); }
        }
        //Rolls for luck and skill checks to see if and how many oranges you find in the desert.
        private static void SubtropicalHuntingCheck()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            int luckRoll = UnityEngine.Random.Range(1, 101);
            int genRoll = UnityEngine.Random.Range(1, 90);
            Poisons poisonType = (Poisons)UnityEngine.Random.Range(128, 140);

            //Very Lucky
            if (luckRoll <= GameManager.Instance.PlayerEntity.Stats.LiveLuck - 30)
            {
                    string[] messages = new string[] { "You spot some fruits on a small tree and easily pick them."};
                    ClimateCloaks.TextPopup(messages);
                    int fruit = UnityEngine.Random.Range(1,10);
                    GiveOranges(fruit);
            }
            //Lucky
            else if (luckRoll <= GameManager.Instance.PlayerEntity.Stats.LiveLuck)
            {
                if (genRoll < playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Climbing))
                {
                    string[] messages = new string[] { "You spot some fruits left on the highest branches of a tree.", "", "You climb up between the branches and pick some fruit." };
                    ClimateCloaks.TextPopup(messages);
                    int fruit = UnityEngine.Random.Range(1, 5);
                    GiveOranges(fruit);
                }
                else
                {
                    string[] messages = new string[] { "You spot some fruits left on the highest branches of a tree.", "", "You attempt to climb the tree but are unable to get up there.", "", "Frustrated, you give up and continue your journey." };
                    ClimateCloaks.TextPopup(messages);
                }
            }
            //UnLucky.
            else
            {
                    string[] messages = new string[] { "All edible fruits seem to have allready been picked.", "", "Disappointed, you continue your journey." };
                    ClimateCloaks.TextPopup(messages);
            }
        }

        //When clicking yes, do a DeserWaterCheck
        private static void DesertWater_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                MovePlayer();
                TimeSkip();
                DesertWaterCheck();
            }
            else { sender.CloseWindow(); }
        }
        //Rolls for luck and skill checks to see if and how much water you find in the desert.
        private static void DesertWaterCheck()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            int luckRoll = UnityEngine.Random.Range(1, 101);
            int genRoll = UnityEngine.Random.Range(1, 101);
            Diseases diseaseType = (Diseases)UnityEngine.Random.Range(0, 17);

            //Very unlucky
            if (luckRoll > GameManager.Instance.PlayerEntity.Stats.LiveLuck + 30)
            {
                string[] messages = new string[] { "You spend some time searching until you suddenly hear a sound..." };
                ClimateCloaks.TextPopup(messages);
                SpawnBeast();
            }
            //Very Lucky
            if (luckRoll <= GameManager.Instance.PlayerEntity.Stats.LiveLuck)
            {
                string[] messages = new string[] { "After some searching you find a pool of water.", "", "The water seems safe to drink." };
                ClimateCloaks.TextPopup(messages);
                RefillWater(5);
            }
            //Lucky
            else if (luckRoll <= GameManager.Instance.PlayerEntity.Stats.LiveLuck + 10)
            {
                string[] messages = new string[] { "After some searching you find a small pool of water.", "", "The water seems safe to drink." };
                ClimateCloaks.TextPopup(messages);
                RefillWater(2);

            }
            //Very Unlucky
            else if (luckRoll > GameManager.Instance.PlayerEntity.Stats.LiveLuck + 20)
            {

                if (genRoll < playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Medical - 10))
                {
                    string[] messages = new string[] { "After some searching you find a small pool of water.", "You smell the water and decide it is unsafe to drink." };
                    ClimateCloaks.TextPopup(messages);
                }
                else
                {
                    string[] messages = new string[] { "After some searching you find a small pool of water.", "The water tastes somewhat foul, but you fill your waterskin with what you can scoop up.", "", "You are sure it is drinkable..." };
                    ClimateCloaks.TextPopup(messages);
                    RefillWater(1);
                    EntityEffectBundle bundle = GameManager.Instance.PlayerEffectManager.CreateDisease(diseaseType);
                    GameManager.Instance.PlayerEffectManager.AssignBundle(bundle, AssignBundleFlags.BypassSavingThrows);
                }
            }
            //Unlucky
            else if (luckRoll > GameManager.Instance.PlayerEntity.Stats.LiveLuck)
            {
                string[] messages = new string[] { "No matter how much you search, you find nothing but dusty rocks." };
                ClimateCloaks.TextPopup(messages);
            }
        }




        //Method for checking hunting in swamps. Going to either BirdHunting_OnButtonClick or SwampHunt_OnButtonClick.
        private static void SwampHuntingRoll()
        {
            int roll = UnityEngine.Random.Range(1, 11);
            DaggerfallMessageBox huntingPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            if (roll > 7)
            {
                string[] message = {
                            "You spot a flock of birds settling down in the tall grass.",
                            " ",
                            "Do you wish to spend some time attempting to hunt them?"
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += BirdHunting_OnButtonClick;
            }
            else
            {
                string[] message = {
                            "You see something slither under the surface of the murky water.",
                            "",
                            "Do you wish to spend some time attempting to hunt it?"
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += SwampHunt_OnButtonClick;
            }
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
            huntingPopUp.Show();
        }
        //When clicking yes, skip 1 hour and do a BirdHuntingCheck
        private static void BirdHunting_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                MovePlayer();
                TimeSkip();
                BirdHuntingCheck();
            }
            else { sender.CloseWindow(); }
        }
        //Rolls for luck and skill checks to hunt birds.
        private static void BirdHuntingCheck()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            bool playerHasBow = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Short_Bow) != null && GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Long_Bow) != null ? false : true;
            int skillSum = 0;
            int luckRoll = UnityEngine.Random.Range(1, 101);
            int skillRoll = UnityEngine.Random.Range(1, 90);

            //Very unlucky
            if (luckRoll > GameManager.Instance.PlayerEntity.Stats.LiveLuck + 20)
            {
                string[] messages = new string[] { "You slowly and quietly sneak towards the birds.", "", "They all flee into the air as a deep roar is heard nearby!" };
                ClimateCloaks.TextPopup(messages);
                SpawnBeast();
            }
            else if (playerHasBow)
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth)/2;
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Archery)/2;
                if (skillRoll < skillSum - 30)
                {
                    int meat = UnityEngine.Random.Range(2, 5);
                    string[] messages = new string[] { "You slowly and quietly sneak towards the birds, readying your bow and arrow.", "You loose the arrow, piercing one of the birds. The rest take flight", "but you manage to loose several more arrows before they are out of range.", "", "You pick up the "+meat.ToString()+ " dead birds and spend some time preparing them." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(meat);
                }
                else if (skillRoll < skillSum)
                {
                    string[] messages = new string[] { "You slowly and quietly sneak towards the birds, readying your bow and arrow.", "You loose the arrow, piercing one of the birds. The rest take flight", " and your next shot goes wide of your prey. They are soon out of range.", "", "You collect the dead bird and spend some time preparing it." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(1);
                }
                else
                {
                    string[] messages = new string[] { "You slowly and quietly sneak towards the birds, readying your bow and arrow.", "Before you are in position, something spooks the birds and they suddenly take to the air." };
                    ClimateCloaks.TextPopup(messages);
                }
            }
            else
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth) / 4;
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.CriticalStrike) / 4;
                if (skillRoll < skillSum)
                {
                    string[] messages = new string[] { "You slowly and quietly sneak towards the birds, preparing to strike.", "You leap forward, attempting to reach your mark before it takes off.", "Your strike connect with a satisfying sound, the rest of the flock quickly flies away.", "", "You collect the dead bird and spend some time preparing it." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(1);
                }
                else
                {
                    string[] messages = new string[] { "You slowly and quietly sneak towards the birds, preparing to strike.", "Before you are in position, something spooks the birds and they suddenly take to the air." };
                    ClimateCloaks.TextPopup(messages);
                }
            }
        }
        //When clicking yes, skip 1 hour and do a SwampHuntCheck
        private static void SwampHunt_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                MovePlayer();
                TimeSkip();
                SwampHuntCheck();
            }
            else { sender.CloseWindow(); }
        }
        //Rolls for luck and skill checks to hunt in the swamp.
        private static void SwampHuntCheck()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            bool playerHasBow = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Short_Bow) != null && GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Long_Bow) != null ? false : true;
            int skillSum = 0;
            int skillRoll = UnityEngine.Random.Range(1, 110);
            int luckRoll = UnityEngine.Random.Range(1, 110);

            //Very unlucky
            if (luckRoll > GameManager.Instance.PlayerEntity.Stats.LiveLuck + 30)
            {
                string[] messages = new string[] { "You sneak up to the waters edge and keep completely still.", "Time goes by while you stare intently at the water.", "", "Suddenly you hear a sound behind you.", "You are not the hunter, but the hunted!" };
                ClimateCloaks.TextPopup(messages);
                SpawnBeast();
            }
            else if (playerHasBow)
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth);
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Archery) / 2;
                if (skillRoll < skillSum)
                {
                    int meat = UnityEngine.Random.Range(1, 2);
                    string[] messages = new string[] { "You sneak up to the waters edge and keep completely still.", "Time goes by while you stare intently at the water.", "", "Another ripple in the water appear and you release an arrow.","", "You pull your scaly prey out of the swamp and butcher it." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(meat);
                }
                else if (skillRoll > skillSum)
                {
                    string[] messages = new string[] { "You sneak up to the waters edge and keep completely still.", "Time goes by while you stare intently at the water.", "Another ripple in the water appear and you release an arrow.", "", "You miss the animal and the ripple does not reappear." };
                    ClimateCloaks.TextPopup(messages);
                }
            }
            else
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth) / 2;
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.CriticalStrike) / 2;
                if (skillRoll < skillSum)
                {
                    int meat = UnityEngine.Random.Range(1, 2);
                    string[] messages = new string[] { "You sneak up to the waters edge and keep completely still.", "Time goes by while you stare intently at the water.", "Your strike connect with a satisfying sound, and leverage the struggling lizard out of the water.", "", "You spend some time butchering the animal." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(meat);
                }
                else
                {
                    Poisons poisonType = (Poisons)UnityEngine.Random.Range(128, 140);
                    if (luckRoll < GameManager.Instance.PlayerEntity.Stats.LiveLuck)
                    {
                        string[] messages = new string[] { "You sneak up to the waters edge and keep completely still.", "Time goes by while you stare intently at the water.", "", "The ripples never appear again. Finally, you give up." };
                        ClimateCloaks.TextPopup(messages);
                    }
                    else
                    {
                        string[] messages = new string[] { "You sneak up to the waters edge and keep completely still.", "Time goes by while you stare intently at the water.", "", "You strike the water and some kind of fanged lizard", "explodes out of the water, sinking its teeth into your arm.", "You manage to shake it off and it disappears back into the water.","", "You hope it was not poisonous..." };
                        ClimateCloaks.TextPopup(messages);
                        DaggerfallWorkshop.Game.Formulas.FormulaHelper.InflictPoison(GameManager.Instance.PlayerEntity, poisonType, false);
                    }
                }
            }
        }



        //Method for checking hunting in woods. Going to either BirdHunting_OnButtonClick or WoodHunt_OnButtonClick.
        private static void WoodsHuntingRoll()
        {
            int roll = UnityEngine.Random.Range(1, 11);
            DaggerfallMessageBox huntingPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            if (roll > 7)
            {
                string[] message = {
                            "You spot a flock of birds settling down in the tall grass.",
                            " ",
                            "Do you wish to spend some time attempting to hunt them?"
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += BirdHunting_OnButtonClick;
            }
            else
            {
                string[] message = {
                            "You cross a set of animal tracks. They seem fresh.",
                            "",
                            "Do you wish to spend some time on a hunt?"
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += WoodsHunt_OnButtonClick;
            }
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
            huntingPopUp.Show();
        }
        //When clicking yes, skip 1 hour and do a SwampHuntCheck
        private static void WoodsHunt_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                MovePlayer();
                TimeSkip();
                WoodsHuntCheck();
            }
            else { sender.CloseWindow(); }
        }
        //Rolls for luck and skill checks to hunt in the woods.
        private static void WoodsHuntCheck()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            bool playerHasBow = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Short_Bow) != null && GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Long_Bow) != null ? false : true;
            int skillSum = 0;
            int skillRoll = UnityEngine.Random.Range(1, 101);
            int luckRoll = UnityEngine.Random.Range(1, 110);

            //Very unlucky
            if (luckRoll > GameManager.Instance.PlayerEntity.Stats.LiveLuck - 100)
            {
                string[] messages = new string[] { "You track your prey for some time. As you suspect you are", "getting near, you hear a sudden roar behind you.", "", "You are not the hunter, but the hunted!" };
                ClimateCloaks.TextPopup(messages);
                SpawnBeast();
            }
            else if (playerHasBow)
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth);
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Archery) / 2;

                //Success
                if (skillRoll+30 < skillSum)
                {
                    int meat = UnityEngine.Random.Range(4, 6);
                    string[] messages = new string[] { "You track a set of deer prints for some time.", "As you get within range, you knock an arrow and wait for the right moment.", "", "Your arrow flies true. The deer takes a few steps and collapses." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(meat);
                }
                else if (skillRoll < skillSum)
                {
                    string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and stay perfectly still.", "", "After some time, you get a clear shot and your arrow pierces the animal." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(1);
                }
                //Fail
                else if (skillRoll >= skillSum)
                {
                    if (skillRoll < 50)
                    {
                        string[] messages = new string[] { "You track a set of deer prints for some time.", "As you get within range, you knock an arrow and wait for the right moment.", "", "The deer suddenly leaps away and disappears, your arrow going wide of the mark." };
                        ClimateCloaks.TextPopup(messages);
                    }
                    else
                    {
                        string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and stay perfectly still.", "", "Your arrow goes wide of the mark, the rabbit scampers off." };
                        ClimateCloaks.TextPopup(messages);
                    }
                }
            }
            else
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth) / 2;
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.CriticalStrike) / 2;
                //Success
                if (skillRoll < skillSum)
                {
                    int meat = UnityEngine.Random.Range(1, 2);
                    string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and attempt to get closer.", "", "After some time, you have the animal within range and you lunge!", "", "You kill the rabbit in a single strike." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(meat);
                }
                //Fail
                else
                {
                    if (luckRoll < GameManager.Instance.PlayerEntity.Stats.LiveLuck)
                    {
                        string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and attempt to get closer.", "", "After some time, you have the animal within range and you lunge!", "", "The rabbit is too quick and scampers away." };
                        ClimateCloaks.TextPopup(messages);
                    }
                    else
                    {
                        string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and attempt to get closer.", "", "Suddenly the grass splits open as a wild boar charges at you!", "", "After a furious struggle you manage to chase it off." };
                        ClimateCloaks.TextPopup(messages);
                        playerEntity.DecreaseHealth(luckRoll/2);
                    }
                }
            }
        }



        //Method for checking hunting in mountains. Going to either BirdHunting_OnButtonClick or MountainHunt_OnButtonClick.
        private static void MountainHuntingRoll()
        {
            int roll = UnityEngine.Random.Range(1, 11);
            DaggerfallMessageBox huntingPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            if (roll > 7)
            {
                string[] message = {
                            "You spot a flock of birds settling down between some rocks.",
                            " ",
                            "Do you wish to spend some time attempting to hunt them?"
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += BirdHunting_OnButtonClick;
            }
            else
            {
                string[] message = {
                            "You cross a set of animal tracks. They seem fresh.",
                            "",
                            "Do you wish to spend some time on a hunt?"
                        };
                huntingPopUp.SetText(message);
                huntingPopUp.OnButtonClick += MountainHunt_OnButtonClick;
            }
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            huntingPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
            huntingPopUp.Show();
        }
        //When clicking yes, skip 1 hour and do a MountainHuntCheck
        private static void MountainHunt_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                MovePlayer();
                TimeSkip();
                MountainHuntCheck();
            }
            else { sender.CloseWindow(); }
        }
        //Rolls for luck and skill checks to hunt in the mountains.
        private static void MountainHuntCheck()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            bool playerHasBow = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Short_Bow) != null && GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.Weapons, (int)Weapons.Long_Bow) != null ? false : true;
            int skillSum = 0;
            int skillRoll = UnityEngine.Random.Range(1, 101);
            int luckRoll = UnityEngine.Random.Range(1, 110);

            //Very unlucky
            if (luckRoll > GameManager.Instance.PlayerEntity.Stats.LiveLuck + 30)
            {
                string[] messages = new string[] { "You track your prey for some time. As you suspect you are", "getting near, you hear a sudden roar behind you.", "", "You are not the hunter, but the hunted!" };
                ClimateCloaks.TextPopup(messages);
                SpawnBeast();
            }
            else if (playerHasBow)
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth);
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Archery) / 2;

                //Success
                if (skillRoll + 30 < skillSum)
                {
                    int meat = UnityEngine.Random.Range(3, 5);
                    string[] messages = new string[] { "You follow the trail of a mountain goat for some time.", "As you get within range, you knock an arrow and wait for the right moment.", "", "Your arrow flies true. The goat takes a few steps and collapses." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(meat);
                }
                else if (skillRoll < skillSum)
                {
                    string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and stay perfectly still.", "", "After some time, you get a clear shot and your arrow pierces the animal." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(1);
                }
              //Fail
                else if (skillRoll >= skillSum)
                {
                    if (skillRoll < 50)
                    {
                        string[] messages = new string[] { "You follow the trail of a mountain goat for some time.", "As you get within range, you knock an arrow and wait for the right moment.", "", "You miss, the arrow bouncing off the rocks. The goat escapes unscathed." };
                        ClimateCloaks.TextPopup(messages);
                    }
                    else
                    {
                        string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and stay perfectly still.", "", "Your arrow goes wide of the mark, the rabbit scampers off." };
                        ClimateCloaks.TextPopup(messages);
                    }
                }
            }
            else
            {
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth) / 2;
                skillSum += playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.CriticalStrike) / 2;
                //Success
                if (skillRoll < skillSum)
                {
                    int meat = UnityEngine.Random.Range(1, 2);
                    string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and attempt to get closer.", "", "After some time, you have the animal within range and you lunge!", "", "You kill the rabbit in a single strike." };
                    ClimateCloaks.TextPopup(messages);
                    GiveMeat(meat);
                }
                //Fail
                else
                {
                    if (luckRoll < GameManager.Instance.PlayerEntity.Stats.LiveLuck)
                    {
                        string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and attempt to get closer.", "", "After some time, you have the animal within range and you lunge!", "", "The rabbit is too quick and scampers away." };
                        ClimateCloaks.TextPopup(messages);
                    }
                    else
                    {
                        string[] messages = new string[] { "You find traces of rabbits in the area.", "You spot movement in the underbrush and attempt to get closer.", "", "Suddenly the rocks beneath your foot give way and you take a hard fall.", "", "The rabbit scampers off and you are left nursing your bruises." };
                        ClimateCloaks.TextPopup(messages);
                        playerEntity.DecreaseHealth(luckRoll / 4);
                    }
                }
            }
        }


        private static void GiveMeat(int meatAmount)
        {
            for (int i = 0; i < meatAmount; i++)
            {
                GameManager.Instance.PlayerEntity.Items.AddItem(ItemBuilder.CreateItem(ItemGroups.UselessItems2, ItemMeat.templateIndex));
            }
        }

        private static void GiveOranges(int fruitAmount)
        {
            for (int i = 0; i < fruitAmount; i++)
            {
                GameManager.Instance.PlayerEntity.Items.AddItem(ItemBuilder.CreateItem(ItemGroups.UselessItems2, ItemOrange.templateIndex));
            }
        }

        private static void GiveApples(int fruitAmount)
        {
            for (int i = 0; i < fruitAmount; i++)
            {
                GameManager.Instance.PlayerEntity.Items.AddItem(ItemBuilder.CreateItem(ItemGroups.UselessItems2, ItemApple.templateIndex));
            }
        }

        static void RefillWater(float waterAmount)
        {
            float wLeft = 0;
            float skinRoom = 0;
            float fill = 0;
            List<DaggerfallUnityItem> skins = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.UselessItems2, ClimateCloaks.templateIndex_Waterskin);
            foreach (DaggerfallUnityItem skin in skins)
            {
                if (waterAmount <= 0)
                {
                    break;
                }
                if (skin.weightInKg < 2)
                {
                    wLeft = waterAmount - skin.weightInKg;
                    skinRoom = 2 - skin.weightInKg;
                    fill = Mathf.Min(skinRoom, wLeft);
                    waterAmount -= fill;
                    skin.weightInKg += Mathf.Min(fill, 2f);
                    DaggerfallUI.AddHUDText("You refill your water.");
                }
            }
        }

        private static void MovePlayer()
        {
            //int rollX = UnityEngine.Random.Range(-50, 51);
            //int rollY = UnityEngine.Random.Range(-50, 51);
            //int destinationPosX = (int)GameManager.Instance.PlayerObject.transform.position.x + rollX;
            //int destinationPosY = (int)GameManager.Instance.PlayerObject.transform.position.y + rollY;
            //GameManager.Instance.StreamingWorld.TeleportToCoordinates(destinationPosX, destinationPosY, StreamingWorld.RepositionMethods.DirectionFromStartMarker);
        }

        private static void TimeSkip()
        {
            int skipAmount = Mathf.Max(UnityEngine.Random.Range(20, 120) - (GameManager.Instance.PlayerEntity.Stats.LiveSpeed / 10), 5);
            DaggerfallUnity.Instance.WorldTime.Now.RaiseTime(DaggerfallDateTime.SecondsPerMinute * skipAmount);
        }

        private static void SpawnBeast()
        {

            int roll = UnityEngine.Random.Range(0,11);
            GameObject player = GameManager.Instance.PlayerObject;

            //Desert monster
            if ((climate == (int)MapsFile.Climates.Desert || climate == (int)MapsFile.Climates.Desert2) || climate == (int)MapsFile.Climates.Subtropical)
            {
                if (roll < 2)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, MobileTypes.GiantScorpion, 2);
                    GameObject[] mobile2 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, MobileTypes.GiantScorpion, 1);
                }
                else if ( roll < 4)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, MobileTypes.GiantScorpion, 2);
                }
                else if (roll < 9)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, MobileTypes.GiantScorpion, 1);
                }
                else
                {
                    GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, MobileTypes.Dragonling_Alternate, 1);
                }
            }
            //Swamp Monster
            else if ((climate == (int)MapsFile.Climates.Swamp || climate == (int)MapsFile.Climates.Rainforest) && roll < 5)
            {
                if (roll < 2)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)4, 2);
                    GameObject[] mobile2 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)2, 1);
                }
                else if (roll < 4)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)6, 2);
                }
                else if (roll < 9)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)6, 1);
                }
                else
                {
                    GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)40, 1);
                }
            }
            //Forest Monster
            else if (climate == (int)MapsFile.Climates.Woodlands || climate == (int)MapsFile.Climates.HauntedWoodlands)
            {
                
                if (roll < 2)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position - player.transform.forward * 4, MobileTypes.GrizzlyBear, 2);
                    GameObject[] mobile2 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, MobileTypes.Spriggan, 1);
                    mobile1[0].transform.LookAt(mobile1[0].transform.position + (mobile1[0].transform.position + player.transform.position));
                    mobile1[0].SetActive(true);
                    mobile2[0].transform.LookAt(mobile2[0].transform.position - (mobile2[0].transform.position + player.transform.position));
                    mobile2[0].SetActive(true);
                }
                else if (roll < 4)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position - player.transform.forward * 4, MobileTypes.GrizzlyBear, 2);
                    mobile1[0].transform.LookAt(mobile1[0].transform.position + (mobile1[0].transform.position + player.transform.position));
                    mobile1[0].SetActive(true);
                }
                else if (roll < 9)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position - player.transform.forward * 4, MobileTypes.GrizzlyBear, 1);
                    mobile1[0].transform.LookAt(mobile1[0].transform.position + (mobile1[0].transform.position + player.transform.position));
                    mobile1[0].SetActive(true);
                }
                else
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position - player.transform.forward * 6, MobileTypes.Dragonling_Alternate, 1);
                    mobile1[0].transform.LookAt(mobile1[0].transform.position + (mobile1[0].transform.position + player.transform.position));
                    mobile1[0].SetActive(true);
                }

            }
            //Mountain Monster
            else if (climate == (int)MapsFile.Climates.Mountain || climate == (int)MapsFile.Climates.MountainWoods)
            {
                if (roll < 2)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)5, 2);
                    GameObject[] mobile2 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)5, 1);
                }
                else if (roll < 4)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)5, 2);
                }
                else if (roll < 9)
                {
                    GameObject[] mobile1 = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)4, 1);
                }
                else
                {
                    GameObject[] mobile = GameObjectHelper.CreateFoeGameObjects(player.transform.position + player.transform.forward * 2, (MobileTypes)40, 1);
                }
            }


        }
    }
}
