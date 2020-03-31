// Project:         Filling Food mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace FillingFood
{
    public class ItemBread : DaggerfallUnityItem
    {
        public const int templateIndex = 534;

        // In leu of a real enum.
        public const int StatusFresh = 0;
        public const int StatusStale = 1;
        public const int StatusMouldy = 2;
        public const int StatusRotten = 3;
        public const int StatusPutrid = 4;

        private int foodStatus = StatusFresh;

        public ItemBread() : base(ItemGroups.UselessItems2, templateIndex)
        {
        }

        public virtual string GetFoodStatus()
        {
            switch (foodStatus)
            {
                case StatusStale:
                    return "Stale ";
                case StatusMouldy:
                    return "Mouldy ";
                case StatusRotten:
                    return "Rotten ";
                case StatusPutrid:
                    return "Putrid ";
                default:
                    return "";
            }
        }

        public int FoodStatus
        {
            get { return foodStatus; }
        }

        public void RotFood()
        {
            if (foodStatus < StatusPutrid)
            {
                foodStatus++;
                shortName = GetFoodStatus() + ItemTemplate.name;
            }
        }

        // Use template world archive for fresh/stale food, or template index for other states
        public override int InventoryTextureArchive
        {
            get
            {
                if (foodStatus == StatusFresh || foodStatus == StatusStale)
                    return WorldTextureArchive;
                else
                    return TemplateIndex;
            }
        }

        // Use template world record for fresh food, or status for other states
        public override int InventoryTextureRecord
        {
            get
            {
                switch (foodStatus)
                {
                    case StatusFresh:
                    case StatusStale:
                    default:
                        return WorldTextureRecord;
                    case StatusMouldy:
                    case StatusRotten:
                        return 0;
                    case StatusPutrid:
                        return 1;
                }
            }
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemBread).ToString();
            return data;
        }
    }
}

