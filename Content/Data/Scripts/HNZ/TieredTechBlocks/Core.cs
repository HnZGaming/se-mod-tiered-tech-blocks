using System;
using System.Collections.Generic;
using HNZ.FlashGps.Interface;
using HNZ.Utils;
using HNZ.Utils.Communications;
using HNZ.Utils.Logging;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace HNZ.TieredTechBlocks
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public sealed class Core : MySessionComponentBase, ICommandListener
    {
        public static Core Instance { get; private set; }

        static readonly Logger Log = LoggerManager.Create(nameof(Core));

        bool _firstFramePassed;
        ContentFile<Config> _configFile;
        MoreLoot _moreLoot;
        ProtobufModule _protobufModule;
        CommandModule _commandModule;
        FlashGpsApi _flashGpsApi;
        Dictionary<string, Action<Command>> _serverCommands;

        public FlashGpsApi GpsApi => _flashGpsApi;

        public override void LoadData()
        {
            Instance = this;

            LoggerManager.SetPrefix(nameof(TieredTechBlocks));

            if (MyAPIGateway.Session.IsServer)
            {
                ReloadConfig();

                if (!Config.Instance.AgreeToKeepThisModPrivate)
                {
                    throw new InvalidOperationException(
                        "This is an experimental fork of the Tiered Tech Block mod. " +
                        "Replace this mod with the original Tiered Tech Block mod.");
                }

                _moreLoot = new MoreLoot();
                _moreLoot.LoadData();

                _serverCommands = new Dictionary<string, Action<Command>>
                {
                    { "reload", Command_Reload },
                };
            }

            _protobufModule = new ProtobufModule((ushort)nameof(TieredTechBlocks).GetHashCode());
            _protobufModule.Initialize();

            _commandModule = new CommandModule(_protobufModule, 1, "ttb", this);
            _commandModule.Initialize();

            _flashGpsApi = new FlashGpsApi(nameof(TieredTechBlocks).GetHashCode());
        }

        void ReloadConfig()
        {
            _configFile = new ContentFile<Config>("TieredTech.cfg", Config.CreateDefault());
            _configFile.ReadOrCreateFile();
            Config.Instance = _configFile.Content;
            Config.Instance.TryInitialize();
            LoggerManager.SetConfigs(Config.Instance.LogConfigs);
        }

        protected override void UnloadData()
        {
            _moreLoot?.UnloadData();
            _protobufModule?.Close();
            _commandModule?.Close();
        }

        public override void UpdateBeforeSimulation()
        {
            _protobufModule.Update();
            _commandModule.Update();

            if (MyAPIGateway.Session.IsServer)
            {
                if (LangUtils.RunOnce(ref _firstFramePassed))
                {
                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0,
                        (object o, ref MyDamageInformation info) => BeforeDamage(o, ref info));
                }

                _moreLoot.Update();
            }
        }

        static void BeforeDamage(object target, ref MyDamageInformation info)
        {
            var block = target as IMySlimBlock;
            if (block == null) return;

            // forge
            var forge = block.FatBlock?.GameLogic?.GetAs<TierForgeBase>();
            forge?.BeforeDamage(ref info);

            // loot
            BeforeDamageLoot(block, ref info);
        }

        static void BeforeDamageLoot(IMySlimBlock target, ref MyDamageInformation info)
        {
            var cargo = target.FatBlock as IMyCargoContainer;
            if (cargo == null) return;

            IMyCargoContainer smallCargo;
            if (!MoreLoot.TryGetSmallCargo(cargo, out smallCargo)) return;

            var ownerFactionTag = smallCargo.GetOwnerFactionTag();
            if (ownerFactionTag == null) return; // unowned block is exempt
            if (ownerFactionTag.Length == 3) return; // player block is exempt

            info.Amount /= 5;
        }

        bool ICommandListener.ProcessCommandOnClient(Command command)
        {
            return false;
        }

        void ICommandListener.ProcessCommandOnServer(Command command)
        {
            _serverCommands.GetValueOrDefault(command.Header, null)?.Invoke(command);
        }

        void Command_Reload(Command command)
        {
            ReloadConfig();
            command.Respond("ttb", Color.White, "Configs reloaded");
        }
    }
}