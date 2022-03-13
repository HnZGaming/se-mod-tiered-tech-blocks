using System;
using HNZ.Utils.Pools;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace HNZ.TieredTechBlocks
{
    public sealed class MoreLoot
    {
        struct Loot
        {
            public MyObjectBuilder_Component Builder;
            public int MinAmount;
            public int MaxAmount;
            public double Chance;
        }

        Loot[] _loots;

        public void LoadData()
        {
            _loots = new[]
            {
                new Loot
                {
                    Builder = DefinitionUtils.TechSource2xBuilder,
                    Chance = Config.Instance.Common.Chance,
                    MinAmount = Config.Instance.Common.MinAmount,
                    MaxAmount = Config.Instance.Common.MaxAmount,
                },
                new Loot
                {
                    Builder = DefinitionUtils.TechSource4xBuilder,
                    Chance = Config.Instance.Rare.Chance,
                    MinAmount = Config.Instance.Rare.MinAmount,
                    MaxAmount = Config.Instance.Rare.MaxAmount,
                },
                new Loot
                {
                    Builder = DefinitionUtils.TechSource8xBuilder,
                    Chance = Config.Instance.Exotic.Chance,
                    MinAmount = Config.Instance.Exotic.MinAmount,
                    MaxAmount = Config.Instance.Exotic.MaxAmount,
                },
            };

            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed += OnGridSpawned;
            }
        }

        public void UnloadData()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed -= OnGridSpawned;
            }
        }

        void OnGridSpawned(long entityId, string prefabName)
        {
            var grid = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;
            if (grid?.Physics == null) return;

            var gridName = grid.DisplayName.ToLower();
            foreach (var excludeName in Config.Instance.ExcludeGridNames)
            {
                if (gridName.Contains(excludeName))
                {
                    return;
                }
            }

            var inventories = ListPool<IMyInventory>.Create();
            var blocks = ListPool<IMySlimBlock>.Create();

            grid.GetBlocks(blocks);
            foreach (var block in blocks)
            {
                var cargo = block.FatBlock as IMyCargoContainer;
                if (cargo == null) continue;
                if (cargo.MarkedForClose) continue;
                if (!cargo.IsWorking) continue;
                if (cargo.Components.Has<TierForgeBase>()) continue;

                var inventory = cargo.GetInventory();
                if (inventory != null)
                {
                    inventories.Add(inventory);
                }
            }

            inventories.ShuffleList();

            var addedCount = 0;
            foreach (var inventory in inventories)
            foreach (var loot in _loots)
            {
                if (TryAddLoot(inventory, loot))
                {
                    if (addedCount++ >= 5) break;
                }
            }

            ListPool<IMyInventory>.Release(inventories);
            ListPool<IMySlimBlock>.Release(blocks);
        }

        static bool TryAddLoot(IMyInventory inventory, Loot loot)
        {
            if (loot.Chance >= MyUtils.GetRandomDouble(0, 1))
            {
                var amount = MyUtils.GetRandomInt(loot.MinAmount, loot.MaxAmount);
                inventory.AddItems(amount, loot.Builder);
                return true;
            }

            return false;
        }
    }
}