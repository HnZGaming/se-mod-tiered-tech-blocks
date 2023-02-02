using System.Collections.Generic;
using HNZ.Utils;
using HNZ.Utils.Logging;
using HNZ.Utils.MES;
using HNZ.Utils.Pools;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace HNZ.TieredTechBlocks
{
    public sealed class TierForgeInjector
    {
        static readonly Logger Log = LoggerManager.Create(nameof(TierTechInjector));

        public void TryInsertForge(IMyCubeGrid grid)
        {
            Log.Debug($"trying to insert forge: '{grid.DisplayName}'");

            IMyCargoContainer smallCargoBlock;
            TierForgeSpec spec;
            var cargoBlocks = Utils.GetVanillaCargoBlocks(grid);
            if (TryGetSmallCargo(cargoBlocks, out smallCargoBlock) &&
                TryGetTierForceSpec(grid, out spec))
            {
                // replace block
                var blockPos = smallCargoBlock.Position;
                var ownerId = smallCargoBlock.OwnerId;
                grid.RemoveBlock(smallCargoBlock.SlimBlock);
                var forgeSubtype = DefinitionUtils.ForgeBlockSubtypeName(spec.Tier);
                grid.AddBlock(new MyObjectBuilder_CargoContainer
                {
                    SubtypeName = forgeSubtype,
                    Orientation = new SerializableQuaternion(),
                    Min = new SerializableVector3I(blockPos.X, blockPos.Y, blockPos.Z),
                    BuiltBy = ownerId,
                    Owner = ownerId,
                }, true);

                Log.Info($"injected forge: {grid.EntityId}', '{grid.DisplayName}', {spec.Tier}x");
            }
        }

        static bool TryGetTierForceSpec(IMyCubeGrid grid, out TierForgeSpec spec)
        {
            spec = null;

            IMyFaction faction;
            if (!grid.TryGetFaction(out faction)) return false;

            foreach (var config in Config.Instance.ForgeSpecs)
            {
                // test faction
                if (faction.Tag != config.FactionTag) continue;

                // test grid id (if specified)
                if (!string.IsNullOrEmpty(config.SpawnGroupId))
                {
                    if (!NpcData.TestSpawnGroup(grid, config.SpawnGroupId)) continue;
                }

                // dice roll!
                if (config.Chance < MyUtils.GetRandomDouble(0, 1)) continue;

                spec = config;
                break;
            }

            return spec != null;
        }

        static bool TryGetSmallCargo(IEnumerable<IMyCargoContainer> cargoBlocks, out IMyCargoContainer smallCargo)
        {
            foreach (var cargoBlock in cargoBlocks)
            {
                if (Utils.TryGetSmallCargo(cargoBlock, out smallCargo))
                {
                    return true;
                }
            }

            smallCargo = default(IMyCargoContainer);
            return false;
        }
    }
}