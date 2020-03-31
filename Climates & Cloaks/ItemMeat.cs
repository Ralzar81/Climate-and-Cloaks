// Project:         Filling Food mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace FillingFood
{
    public class ItemMeat : AbstractItemFood
    {
        public const int templateIndex = 537;

        public ItemMeat() : base(ItemGroups.UselessItems2, templateIndex)
        {
        }

        public override string GetFoodStatus()
        {
            switch (FoodStatus)
            {
                case StatusStale:
                    return "Smelly ";
                default:
                    return base.GetFoodStatus();
            }
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemMeat).ToString();
            return data;
        }
    }
}

