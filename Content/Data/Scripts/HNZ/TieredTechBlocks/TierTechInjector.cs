using System;
using HNZ.Utils;
using HNZ.Utils.Logging;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace HNZ.TieredTechBlocks
{
    public sealed class TierTechInjector
    {
        static readonly Logger Log = LoggerManager.Create(nameof(TierTechInjector));

        struct Loot
        {
            public MyObjectBuilder_Component Builder;
            public int MinAmount;
            public int MaxAmount;
            public double Chance;
        }

        readonly Loot[] _loots =
        {
            new Loot
            {
                Builder = DefinitionUtils.Tech2xBuilder,
                Chance = Config.Instance.Common.Chance,
                MinAmount = Config.Instance.Common.MinAmount,
                MaxAmount = Config.Instance.Common.MaxAmount,
            },
            new Loot
            {
                Builder = DefinitionUtils.Tech4xBuilder,
                Chance = Config.Instance.Rare.Chance,
                MinAmount = Config.Instance.Rare.MinAmount,
                MaxAmount = Config.Instance.Rare.MaxAmount,
            },
            new Loot
            {
                Builder = DefinitionUtils.Tech8xBuilder,
                Chance = Config.Instance.Exotic.Chance,
                MinAmount = Config.Instance.Exotic.MinAmount,
                MaxAmount = Config.Instance.Exotic.MaxAmount,
            },
        };

        public void TryInsertTechs(IMyCubeGrid grid)
        {
            Log.Debug($"trying to insert techs: '{grid.DisplayName}'");

            IMyFaction faction;
            if (!grid.TryGetFaction(out faction))
            {
                Log.Debug("no faction assigned to grid");
                return;
            }

            var factionTag = faction.Tag;
            if (!Config.Instance.LootFactionTags.Contains(factionTag))
            {
                Log.Debug($"not a faction for techs: '{factionTag}'");
                return;
            }

            var cargoBlocks = Utils.GetVanillaCargoBlocks(grid);
            Log.Debug($"injectable cargo blocks: {cargoBlocks.Count}");
            
            foreach (var cargoBlock in cargoBlocks)
            foreach (var loot in _loots)
            {
                if (loot.Chance >= MyUtils.GetRandomDouble(0, 1))
                {
                    var amount = MyUtils.GetRandomInt(loot.MinAmount, loot.MaxAmount);
                    cargoBlock.GetInventory().AddItems(amount, loot.Builder);
                    Log.Info($"injected techs: '{grid.EntityId}', '{grid.DisplayName}', {loot.Builder.SubtypeName}, {amount}");
                }
            }
        }
    }
}