using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public enum ShopAreaShopkeeperType
    {
        None,
        Normal,
        StrongMan
    }

    public static class ShopAreaVisualFactory
    {
        public struct ShopAreaBuild
        {
            public Transform ShopRoot;
            public GameObject Counter;
            public GameObject Board;
            public GameObject SlotCab;
            public GameObject Shopkeeper;
        }

        public static ShopAreaBuild Create(
            Transform parent,
            Vector3 anchorLocalPosition,
            Quaternion anchorLocalRotation,
            float slotFloorWorldY,
            ShopAreaShopkeeperType shopkeeperType,
            float shopkeeperFloorWorldY)
        {
            const float counterLocalZ = -1.1f;
            const float counterLocalY = 0.6f;

            var shopRoot = new GameObject("ShopArea").transform;
            shopRoot.SetParent(parent, false);
            shopRoot.localPosition = anchorLocalPosition;
            shopRoot.localRotation = anchorLocalRotation;

            var counter = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                shopRoot.position,
                new Vector3(3f, 1.2f, 1f),
                new Color(0.45f, 0.28f, 0.15f),
                "ShopCounter",
                shopRoot);
            counter.transform.localPosition = new Vector3(0f, counterLocalY, counterLocalZ);

            GameObject shopkeeper = null;
            if (shopkeeperType != ShopAreaShopkeeperType.None)
            {
                var shopkeeperRotation = Quaternion.Euler(0f, 180f, 0f);
                shopkeeper = shopkeeperType == ShopAreaShopkeeperType.StrongMan
                    ? LowPolyPeopleVisualFactory.CreateShopAssistant(
                        shopRoot,
                        Vector3.zero,
                        shopkeeperRotation,
                        shopkeeperFloorWorldY)
                    : LowPolyPeopleVisualFactory.CreateShopkeeper(
                        shopRoot,
                        Vector3.zero,
                        shopkeeperRotation,
                        shopkeeperFloorWorldY);
            }

            var board = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                shopRoot.position,
                new Vector3(0.1f, 1.6f, 2.2f),
                new Color(0.35f, 0.25f, 0.12f),
                "ShopBoard",
                shopRoot);
            board.transform.localPosition = new Vector3(-2.2f, 1.8f, counterLocalZ - WorldScale.Feet(3f));

            var slotCab = SlotMachineVisualFactory.CreateShopSlotMachine(
                shopRoot,
                new Vector3(2.5f, 0f, counterLocalZ),
                Quaternion.Euler(0f, 180f, 0f),
                slotFloorWorldY);

            return new ShopAreaBuild
            {
                ShopRoot = shopRoot,
                Counter = counter,
                Board = board,
                SlotCab = slotCab,
                Shopkeeper = shopkeeper
            };
        }
    }
}
