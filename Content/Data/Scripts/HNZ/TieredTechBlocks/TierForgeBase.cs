using System;
using HNZ.Utils;
using HNZ.Utils.Communications.Gps;
using HNZ.Utils.Logging;
using HNZ.Utils.Pools;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace HNZ.TieredTechBlocks
{
    public abstract class TierForgeBase : MyGameLogicComponent, IGpsEntity
    {
        static readonly Logger Log = LoggerManager.Create(nameof(TierForgeBase));

        CharacterToPlayerLookup _playerLookup;
        DateTime _startTime;
        bool _pastFirstFrame;
        string _gpsName;

        long IGpsEntity.Id => Entity.EntityId;
        string IGpsEntity.Name => _gpsName;
        string IGpsEntity.Description => "Forge blocks allow you to convert Tiered Tech Source components to Tiered Tech components.";
        Vector3D IGpsEntity.Position => Entity.GetPosition();
        Color IGpsEntity.Color => Color.Orange;
        double IGpsEntity.Radius => GpsRadius;

        MyCubeBlock Block => (MyCubeBlock)Entity;
        IMyCargoContainer Cargo => (IMyCargoContainer)Entity;

        protected abstract int ForgeMod { get; }
        protected abstract float LifeSpan { get; }
        protected abstract float GpsRadius { get; }
        protected abstract float DestroyOnSpawnChance { get; }

        protected abstract bool TryForge(MyItemType itemType, out MyObjectBuilder_PhysicalObject builder);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            _playerLookup = new CharacterToPlayerLookup();
            _startTime = DateTime.UtcNow;
            _gpsName = "";

            Core.Instance.OnForgeOpened(this);
        }

        public override void Close()
        {
            if (!MyAPIGateway.Session.IsServer) return;

            _playerLookup.Clear();

            Core.Instance.OnForgeClosed(this);
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Session.IsServer) return;

            if (LangUtils.RunOnce(ref _pastFirstFrame))
            {
                // fail to spawn
                if (DestroyOnSpawnChance > MyUtils.GetRandomDouble(0, 1))
                {
                    Destroy();
                    Log.Info($"forge destroying on spawn: {Block.DisplayNameText}");
                    return;
                }
            }

            // do countdown
            var pastTime = DateTime.UtcNow - _startTime;
            var remainingTime = TimeSpan.FromMinutes(LifeSpan) - pastTime;

            if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0)
            {
                _gpsName = $"{Block.DisplayNameText} ({LangUtils.HoursToString(remainingTime.TotalHours)})";
            }

            if (remainingTime.TotalMinutes <= 0)
            {
                Destroy();
                return;
            }

            if (!Block.IsWorking) return;

            if (MyAPIGateway.Session.GameplayFrameCounter % ForgeMod == 0)
            {
                UpdateInventory();
            }

            // make sure the block is "shared to all"
            if (Block.IDModule?.ShareMode != MyOwnershipShareModeEnum.All)
            {
                Block.ChangeOwner(Block.OwnerId, MyOwnershipShareModeEnum.All);
            }
        }

        void Destroy()
        {
            ((IMyDestroyableObject)Block.SlimBlock).DoDamage(float.MaxValue, MyDamageType.Explosion, true);
            MarkForClose();
        }

        void UpdateInventory()
        {
            var items = ListPool<MyInventoryItem>.Create();

            var inventory = Cargo.GetInventory(0);
            inventory.GetItems(items);
            foreach (var item in items)
            {
                MyObjectBuilder_PhysicalObject builder;
                if (TryForge(item.Type, out builder))
                {
                    inventory.RemoveItems(item.ItemId, 1);
                    inventory.AddItems(1, DefinitionUtils.TechComp8xBuilder);
                }
            }

            ListPool<MyInventoryItem>.Release(items);
        }

        public void BeforeDamage(ref MyDamageInformation info)
        {
            if (LifeSpan >= 0)
            {
                info.Amount = 0;
            }
        }
    }
}