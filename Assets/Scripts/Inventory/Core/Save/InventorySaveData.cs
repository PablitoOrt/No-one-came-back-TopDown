using System;
using System.Collections.Generic;

namespace NoOneCameBack.Inventory
{
    [Serializable]
    public class ItemSaveData
    {
        public string itemId;
        public int x = -1;
        public int y = -1;
        public bool rotated;
        public int quantity = 1;
        public int durability;
        public int loadedAmmo;
    }

    [Serializable]
    public class InventorySaveData
    {
        public int version = 1;

        public List<ItemSaveData> grid = new List<ItemSaveData>();
        public List<ItemSaveData> locker = new List<ItemSaveData>();
        public List<ItemSaveData> keyItems = new List<ItemSaveData>();

        // When equippedInGrid is true, equippedWeapon.x/y point at the grid item; otherwise it's carried loose (no tablet yet).
        public bool hasEquippedWeapon;
        public bool equippedInGrid;
        public ItemSaveData equippedWeapon = new ItemSaveData();

        public bool tabletFound;
        public List<string> unlockedApps = new List<string>();
    }
}
