using System;
using System.Collections.Generic;
using HNZ.FlashGps.Interface;
using HNZ.Utils;
using HNZ.Utils.Logging;
using HNZ.Utils.Pools;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.ObjectBuilders.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace HNZ.TieredTechBlocks
{
    public abstract class TierForgeBase : MyGameLogicComponent, SafeZoneSuppressor.IFilter
    {
        static readonly Logger Log = LoggerManager.Create(nameof(TierForgeBase));
        static readonly Guid StorageGuid = Guid.Parse("78441755-F0CC-4005-AA58-C736864591E1");

        SafeZoneSuppressor _safeZoneSuppressor;
        bool _runOnce;
        bool _compromised;
        ISet<long> _owningFactionIds;

        FlashGpsSource FlashGpsSource => new FlashGpsSource
        {
            Id = Entity.EntityId,
            Name = $"{Block.DisplayNameText} ({MaxForgeCount - ForgeCount} left)",
            Description = "Forge blocks allow you to convert Tiered Tech Source components to Tiered Tech components.",
            Position = Entity.GetPosition(),
            Color = Color.Orange,
            EntityId = Entity.EntityId,
            Radius = GpsRadius,
            DecaySeconds = 3,
        };

        MyCubeBlock Block => (MyCubeBlock)Entity;
        IMyCargoContainer Cargo => (IMyCargoContainer)Entity;

        protected abstract int ForgeMod { get; }
        protected abstract int MaxForgeCount { get; }
        protected abstract float GpsRadius { get; }
        protected abstract float NoSafeZoneRadius { get; }
        protected abstract float DamageMultiply { get; }
        protected abstract string TierString { get; }

        protected abstract bool CanForge(MyItemType itemType, out MyObjectBuilder_PhysicalObject builder);

        int ForgeCount
        {
            get { return Entity.Storage.GetIntValue(StorageGuid); }
            set { Entity.Storage.SetValue(StorageGuid, value.ToString("0")); }
        }

        bool CanDestroy => _compromised || (ForgeCount >= MaxForgeCount);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            Log.Info($"forge opened: {Block.CubeGrid.DisplayName}, {Entity.EntityId}");

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent();
            }

            _safeZoneSuppressor = new SafeZoneSuppressor(this);
            _owningFactionIds = new HashSet<long>();

            Core.Instance.GpsApi.AddOrUpdate(FlashGpsSource);
        }

        public override void Close()
        {
            if (!MyAPIGateway.Session.IsServer) return;
            Log.Info($"forge closed: {Block.CubeGrid.DisplayName}, {Entity.EntityId}");

            GameUtils.DumpAllInventories(Entity);
            _safeZoneSuppressor.Clear();

            Core.Instance.GpsApi.Remove(FlashGpsSource.Id);
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Session.IsServer) return;

            if (!Block.IsWorking) return;

            // handle "no safe zone" zone
            if (NoSafeZoneRadius > 0)
            {
                if (GameUtils.EverySeconds(1))
                {
                    var sphere = new BoundingSphereD(Entity.GetPosition(), NoSafeZoneRadius);
                    _safeZoneSuppressor.CollectInSphere(ref sphere);
                }

                _safeZoneSuppressor.Suppress();
            }

            if (MyAPIGateway.Session.GameplayFrameCounter % ForgeMod == 0)
            {
                UpdateInventory();
            }

            if (GameUtils.EverySeconds(0.1f))
            {
                Core.Instance.GpsApi.AddOrUpdate(FlashGpsSource);
            }

            // make sure the block is "shared to all"
            if (Block.IDModule?.ShareMode != MyOwnershipShareModeEnum.All)
            {
                Block.ChangeOwner(Block.OwnerId, MyOwnershipShareModeEnum.All);
            }

            if (LangUtils.RunOnce(ref _runOnce))
            {
                PutDataPad();
                Block.CubeGrid.GetGroupFactionIds(_owningFactionIds, GridLinkTypeEnum.Physical);
            }

            if (GameUtils.EverySeconds(1))
            {
                _compromised |= Block.CubeGrid.GetGroupFactionIds(_owningFactionIds, GridLinkTypeEnum.Physical);

                // must delete forges inside a safe zone
                if (!GameUtils.IsDamageAllowed(Block.CubeGrid))
                {
                    DestroyBlock(true);
                }
            }
        }

        void UpdateInventory()
        {
            var items = ListPool<MyInventoryItem>.Get();

            var inventory = Cargo.GetInventory(0);
            inventory.GetItems(items);
            foreach (var item in items)
            {
                MyObjectBuilder_PhysicalObject builder;
                if (CanForge(item.Type, out builder))
                {
                    inventory.RemoveItems(item.ItemId, 1);
                    inventory.AddItems(1, builder);
                    ForgeCount += 1;
                    break;
                }
            }

            if (ForgeCount >= MaxForgeCount)
            {
                DestroyBlock();
                Log.Info($"destroying forge; used up: {Block.CubeGrid.DisplayName}");
            }

            ListPool<MyInventoryItem>.Release(items);
        }

        void DestroyBlock(bool force = false)
        {
            GameUtils.DumpAllInventories(Entity);

            if (!CanDestroy || force)
            {
                Block.CubeGrid.RemoveBlock(Block.SlimBlock, true);
            }
            else
            {
                ((IMyDestroyableObject)Block.SlimBlock).DoDamage(float.MaxValue, MyDamageType.Explosion, true);
            }
        }

        public void BeforeDamage(ref MyDamageInformation info)
        {
            if (!CanDestroy)
            {
                info.Amount *= DamageMultiply;
            }
        }

        void PutDataPad()
        {
            var builder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Datapad>("Datapad");
            builder.Name = $"{TierString} forge block description";
            builder.Data = string.Format(Config.Instance.DataPadDescription, TierString);

            var inventory = Cargo.GetInventory(0);
            inventory.AddItems(1, builder);
        }

        bool SafeZoneSuppressor.IFilter.CanSuppress(MySafeZone safeZone)
        {
            // don't suppress any block-less safe zones
            // which are either FSZ or economy stuff
            return false;
        }

        bool SafeZoneSuppressor.IFilter.CanSuppress(IMySafeZoneBlock safeZoneBlock)
        {
            // Stop suppressing player-made safe zones when compromised
            return !_compromised;
        }
    }
}