using System;
using System.Collections.Generic;
using System.Linq;
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
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMyInventoryItem = VRage.Game.ModAPI.IMyInventoryItem;

namespace HNZ.TieredTechBlocks
{
    public abstract class TierForgeBase : MyGameLogicComponent
    {
        static readonly Logger Log = LoggerManager.Create(nameof(TierForgeBase));
        static readonly Guid StorageGuid = Guid.Parse("78441755-F0CC-4005-AA58-C736864591E1");

        const float AutoDeleteSecs = 30;

        bool _runOnce;
        DateTime? _usedUpTime;

        MyCubeBlock Block => (MyCubeBlock)Entity;
        IMyCargoContainer Cargo => (IMyCargoContainer)Entity;

        protected abstract int ForgeMod { get; }
        protected abstract int MaxForgeCount { get; }
        protected abstract float GpsRadius { get; }
        protected abstract string TierString { get; }

        protected abstract bool CanForge(MyItemType itemType, out MyObjectBuilder_PhysicalObject builder);

        int ForgeCount
        {
            get { return Entity.Storage.GetIntValue(StorageGuid); }
            set { Entity.Storage.SetValue(StorageGuid, value.ToString("0")); }
        }

        bool UsedUp => ForgeCount >= MaxForgeCount;
        TimeSpan? RemainingTime => _usedUpTime + TimeSpan.FromSeconds(AutoDeleteSecs) - DateTime.UtcNow;

        string DebugName => $"{GetType().Name}, '{Block.CubeGrid.DisplayName}' ({Entity.EntityId})";

        FlashGpsSource GetFlashGpsSource()
        {
            var remainingTime = RemainingTime;
            if (remainingTime.HasValue)
            {
                var remainingTimeStr = LangUtils.HoursToString(remainingTime.Value.TotalHours);
                return new FlashGpsSource
                {
                    Id = Entity.EntityId,
                    Name = $"Used up; exploding in {remainingTimeStr}",
                    Position = Entity.GetPosition(),
                    Color = Color.Orange,
                    EntityId = Entity.EntityId,
                    Radius = 100,
                    DecaySeconds = 3,
                };
            }
            else
            {
                return new FlashGpsSource
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
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;
            Log.Info($"forge opened: {Block.CubeGrid.DisplayName}, {Entity.EntityId}");

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent();
            }

            Core.Instance.GpsApi.AddOrUpdate(GetFlashGpsSource());
        }

        public override void Close()
        {
            if (!MyAPIGateway.Session.IsServer) return;

            Log.Info($"forge closed: {DebugName}");
            Log.Info($"inventory: {GameUtils.GetAllInventoryItems(Entity).DicToString()}");
            GameUtils.DumpAllInventories(Entity);
            Core.Instance.GpsApi.Remove(GetFlashGpsSource().Id);
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Session.IsServer) return;

            var remainingTime = RemainingTime;
            if (remainingTime.HasValue && remainingTime.Value <= TimeSpan.Zero)
            {
                Log.Info($"forge times up: {DebugName}");
                DestroyBlock();
                return;
            }

            if (!Block.IsWorking) return;

            if (MyAPIGateway.Session.GameplayFrameCounter % ForgeMod == 0)
            {
                UpdateInventory();
            }

            if (GameUtils.EverySeconds(0.1f))
            {
                var gps = GetFlashGpsSource();
                Core.Instance.GpsApi.AddOrUpdate(gps);
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

            if (GameUtils.EverySeconds(1))
            {
                // delete forges inside a safe zone
                if (!GameUtils.IsDamageAllowed(Block.CubeGrid))
                {
                    Log.Info($"forge in safe zone: {DebugName}");
                    SendSafeZoneMessage();
                    DestroyBlock();
                }
            }
        }

        void UpdateInventory()
        {
            if (UsedUp) return;

            var forged = false;
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
                    forged = true;
                    break;
                }
            }

            ListPool<MyInventoryItem>.Release(items);

            if (UsedUp && _usedUpTime == null)
            {
                _usedUpTime = DateTime.UtcNow;
            }

            if (forged)
            {
                Log.Info($"forge forged: {DebugName}, used up: {UsedUp}");
            }
        }

        void DestroyBlock()
        {
            Log.Info($"inventory: {GameUtils.GetAllInventoryItems(Entity).DicToString()}");
            GameUtils.DumpAllInventories(Entity);
            Block.CubeGrid.RemoveBlock(Block.SlimBlock, true);
        }

        public void BeforeDamage(ref MyDamageInformation info)
        {
            info.Amount = 0;
            info.IsDeformation = false;
        }

        void PutDataPad()
        {
            var inventory = Cargo.GetInventory(0);

            // don't add a new datapad if one already exists
            var items = new List<MyInventoryItem>();
            inventory.GetItems(items);
            foreach (var item in items)
            {
                if (item.Type.SubtypeId == "Datapad")
                {
                    return;
                }
            }

            var builder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Datapad>("Datapad");
            builder.Name = $"{TierString} forge block description";
            builder.Data = string.Format(Config.Instance.DataPadDescription, TierString);

            inventory.AddItems(1, builder);
        }

        void SendSafeZoneMessage()
        {
            var characters = ListPool<IMyCharacter>.Get();
            Entity.GetCharacters(500, characters);
            foreach (var character in characters)
            {
                if (!character.IsPlayer) continue;

                var playerId = character.ControllerInfo?.ControllingIdentityId ?? 0;
                if (playerId == 0) continue;

                MyVisualScriptLogicProvider.SendChatMessageColored(
                    "Forge block vaporized in a player safe zone.",
                    Color.Red,
                    "Tiered Tech Blocks",
                    playerId);
            }
        }
    }
}