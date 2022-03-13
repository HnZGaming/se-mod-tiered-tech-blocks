using System;
using HNZ.Utils.Communications.Gps;
using HNZ.Utils.Logging;
using HNZ.Utils.Pools;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace HNZ.TieredTechBlocks
{
    public abstract class TierForgeBase : MyGameLogicComponent, IGpsEntity
    {
        static readonly Logger Log = LoggerManager.Create(nameof(TierForgeBase));

        long IGpsEntity.Id => Entity.EntityId;
        string IGpsEntity.Name => Block.DisplayNameText;
        string IGpsEntity.Description => "Forge blocks allow you to convert Tiered Tech Source components to Tiered Tech components.";
        Vector3D IGpsEntity.Position => Entity.GetPosition();
        Color IGpsEntity.Color => Color.Orange;
        double IGpsEntity.Radius => GpsRadius;

        MyCubeBlock Block => (MyCubeBlock)Entity;
        IMyCargoContainer Cargo => (IMyCargoContainer)Entity;

        protected abstract int ForgeMod { get; }
        protected abstract int MaxForgeCount { get; }
        protected abstract float GpsRadius { get; }

        protected abstract bool CanForge(MyItemType itemType, out MyObjectBuilder_PhysicalObject builder);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            Core.Instance.OnForgeOpened(this);
        }

        public override void Close()
        {
            if (!MyAPIGateway.Session.IsServer) return;

            Core.Instance.OnForgeClosed(this);
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Session.IsServer) return;

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

            var reachedMaxForgeCount = false;
            var inventory = Cargo.GetInventory(0);
            inventory.GetItems(items);
            foreach (var item in items)
            {
                MyObjectBuilder_PhysicalObject builder;
                if (CanForge(item.Type, out builder))
                {
                    inventory.RemoveItems(item.ItemId, 1);
                    inventory.AddItems(1, DefinitionUtils.TechComp8xBuilder);
                    reachedMaxForgeCount = IncrementForgeCount() >= MaxForgeCount;
                    break;
                }
            }

            if (reachedMaxForgeCount)
            {
                Destroy();
            }

            ListPool<MyInventoryItem>.Release(items);
        }

        int IncrementForgeCount()
        {
            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent();
            }

            var guid = Guid.Parse("78441755-F0CC-4005-AA58-C736864591E1");

            string forgeCountStr;
            Entity.Storage.TryGetValue(guid, out forgeCountStr);

            int forgeCount;
            int.TryParse(forgeCountStr ?? "", out forgeCount);

            forgeCount += 1;
            Entity.Storage.SetValue(guid, forgeCount.ToString("0"));

            return forgeCount;
        }

        public void BeforeDamage(ref MyDamageInformation info)
        {
            if (MaxForgeCount >= 0)
            {
                Log.Info($"damage: {info.Amount}, lifespan: {MaxForgeCount}");
                info.Amount = 0;
            }
        }
    }
}