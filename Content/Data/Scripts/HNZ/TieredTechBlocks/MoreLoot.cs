using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HNZ.Utils;
using HNZ.Utils.Logging;
using HNZ.Utils.Pools;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace HNZ.TieredTechBlocks
{
    public sealed class MoreLoot
    {
        static readonly Logger Log = LoggerManager.Create(nameof(MoreLoot));

        struct Loot
        {
            public MyObjectBuilder_Component Builder;
            public int MinAmount;
            public int MaxAmount;
            public double Chance;
        }

        struct SpawnInfo
        {
            public long EntityId;
            public string PrefabName;
        }

        Loot[] _loots;
        ConcurrentQueue<SpawnInfo> _spawns;

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

            _spawns = new ConcurrentQueue<SpawnInfo>();

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
            _spawns.Enqueue(new SpawnInfo
            {
                EntityId = entityId,
                PrefabName = prefabName,
            });
        }

        public void Update()
        {
            SpawnInfo spawn;
            while (_spawns.TryDequeue(out spawn))
            {
                InsertLoot(spawn.EntityId, spawn.PrefabName);
            }
        }

        void InsertLoot(long entityId, string prefabName)
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

            Log.Info($"grid spawned; prefab: '{prefabName}', entity: '{gridName}', id: {entityId}");

            var blocks = ListPool<IMySlimBlock>.Get();
            var cargoBlocks = ListPool<IMyCargoContainer>.Get();

            grid.GetBlocks(blocks);
            foreach (var block in blocks)
            {
                IMyCargoContainer cargoBlock;
                if (TryGetCargo(block, out cargoBlock))
                {
                    cargoBlocks.Add(cargoBlock);
                }
            }

            IMyCargoContainer smallCargoBlock;
            CargoReplacement cargoReplacement;
            if (TryGetSmallCargo(cargoBlocks, out smallCargoBlock) &&
                TryGetCargoReplacement(grid, out cargoReplacement))
            {
                // don't add tech comps in this one
                cargoBlocks.Remove(smallCargoBlock);

                // replace block
                var blockPos = smallCargoBlock.Position;
                var ownerId = smallCargoBlock.OwnerId;
                grid.RemoveBlock(smallCargoBlock.SlimBlock);
                var forgeName = DefinitionUtils.ForgeBlockSubtypeName(cargoReplacement.Tier);
                grid.AddBlock(new MyObjectBuilder_CargoContainer
                {
                    SubtypeName = forgeName,
                    Orientation = new SerializableQuaternion(),
                    Min = new SerializableVector3I(blockPos.X, blockPos.Y, blockPos.Z),
                    BuiltBy = ownerId,
                    Owner = ownerId,
                }, true);

                Log.Info($"cargo replaced to forge: {forgeName}");
            }

            foreach (var loot in _loots)
            foreach (var cargoBlock in cargoBlocks)
            {
                if (loot.Chance >= MyUtils.GetRandomDouble(0, 1))
                {
                    AddLoot(cargoBlock.GetInventory(), loot);
                }
            }

            ListPool<IMyCargoContainer>.Release(cargoBlocks);
            ListPool<IMySlimBlock>.Release(blocks);
        }

        static bool TryGetCargoReplacement(IMyCubeGrid grid, out CargoReplacement cargoReplacement)
        {
            cargoReplacement = null;
            var ownerFactionTags = SetPool<string>.Create();

            foreach (var ownerId in grid.BigOwners)
            {
                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);
                var factionTag = faction?.Tag;
                if (!string.IsNullOrEmpty(factionTag))
                {
                    ownerFactionTags.Add(factionTag);
                }
            }

            foreach (var replacement in Config.Instance.CargoReplacements)
            {
                // test faction
                if (!ownerFactionTags.Contains(replacement.FactionTag)) continue;

                // test grid id (if specified)
                if (!string.IsNullOrEmpty(replacement.GridId))
                {
                    string gridId;
                    var guid = Guid.Parse("5bca2eb0-1e0a-450d-a5ff-bd213b9654b4");
                    if (!grid.TryGetStorageValue(guid, out gridId)) continue;
                    if (gridId != replacement.GridId) continue;
                }

                // dice roll!
                if (replacement.Chance < MyUtils.GetRandomDouble(0, 1)) continue;

                cargoReplacement = replacement;
                break;
            }

            SetPool<string>.Release(ownerFactionTags);

            return cargoReplacement != null;
        }

        static bool TryGetCargo(IMySlimBlock block, out IMyCargoContainer cargo)
        {
            cargo = block.FatBlock as IMyCargoContainer;
            if (cargo == null) return false;
            if (cargo.MarkedForClose) return false;
            if (!cargo.IsWorking) return false;
            if (cargo.Components.Has<TierForgeBase>()) return false;
            return true;
        }

        static bool TryGetSmallCargo(IEnumerable<IMyCargoContainer> cargoBlocks, out IMyCargoContainer smallCargo)
        {
            foreach (var cargoBlock in cargoBlocks)
            {
                if (TryGetSmallCargo(cargoBlock, out smallCargo))
                {
                    return true;
                }
            }

            smallCargo = default(IMyCargoContainer);
            return false;
        }

        public static bool TryGetSmallCargo(IMyCargoContainer cargoBlock, out IMyCargoContainer smallCargo)
        {
            if (cargoBlock.SlimBlock.BlockDefinition.Id.SubtypeName == "LargeBlockSmallContainer")
            {
                smallCargo = cargoBlock;
                return true;
            }

            smallCargo = default(IMyCargoContainer);
            return false;
        }

        static void AddLoot(IMyInventory inventory, Loot loot)
        {
            var amount = MyUtils.GetRandomInt(loot.MinAmount, loot.MaxAmount);
            inventory.AddItems(amount, loot.Builder);
        }
    }
}