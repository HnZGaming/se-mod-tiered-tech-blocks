using System;
using HNZ.FlashGps.Interface;
using HNZ.Utils;
using HNZ.Utils.Logging;
using HNZ.Utils.Pools;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace HNZ.TieredTechBlocks
{
    public abstract class TierForgeBase : MyGameLogicComponent
    {
        static readonly Logger Log = LoggerManager.Create(nameof(TierForgeBase));
        static readonly Guid StorageGuid = Guid.Parse("78441755-F0CC-4005-AA58-C736864591E1");

        SafeZoneSuppressor _safeZoneSuppressor;
        bool _runOnce;

        public FlashGpsSource FlashGpsSource => new FlashGpsSource
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

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent();
            }

            _safeZoneSuppressor = new SafeZoneSuppressor();

            Core.Instance.OnForgeOpened(this);
        }

        public override void Close()
        {
            if (!MyAPIGateway.Session.IsServer) return;

            DumpInventory();
            _safeZoneSuppressor.Clear();
            Core.Instance.OnForgeClosed(this);
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Session.IsServer) return;

            if (!Block.IsWorking) return;

            // handle "no safe zone" zone
            if (NoSafeZoneRadius > 0)
            {
                if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0)
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

            // make sure the block is "shared to all"
            if (Block.IDModule?.ShareMode != MyOwnershipShareModeEnum.All)
            {
                Block.ChangeOwner(Block.OwnerId, MyOwnershipShareModeEnum.All);
            }

            if (LangUtils.RunOnce(ref _runOnce))
            {
                PutDataPad();
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
                DumpInventory();
                ((IMyDestroyableObject)Block.SlimBlock).DoDamage(float.MaxValue, MyDamageType.Explosion, true);
            }

            ListPool<MyInventoryItem>.Release(items);
        }

        void DumpInventory()
        {
            var inventory = (MyInventory)Cargo.GetInventory(0);
            foreach (var item in inventory.GetItems())
            {
                MyFloatingObjects.EnqueueInventoryItemSpawn(item, Entity.PositionComp.WorldAABB, Vector3D.Zero);
            }

            inventory.Clear(true);
        }

        public void BeforeDamage(ref MyDamageInformation info)
        {
            // let block die if used up
            if (ForgeCount >= MaxForgeCount) return;

            //Log.Info($"damage: {info.Amount}, lifespan: {MaxForgeCount}");
            info.Amount *= DamageMultiply;
        }

        void PutDataPad()
        {
            var builder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Datapad>("Datapad");
            builder.Name = $"{TierString} forge block description";
            builder.Data = string.Format(Config.Instance.DataPadDescription, TierString);

            var inventory = Cargo.GetInventory(0);
            inventory.AddItems(1, builder);
        }
    }
}