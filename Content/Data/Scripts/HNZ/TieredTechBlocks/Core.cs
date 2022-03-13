using System;
using System.Collections.Generic;
using HNZ.Utils;
using HNZ.Utils.Communications;
using HNZ.Utils.Communications.Gps;
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
        static readonly Logger Log = LoggerManager.Create(nameof(Core));

        public static Core Instance { get; private set; }

        bool _firstFramePassed;
        ContentFile<Config> _configFile;
        MoreLoot _moreLoot;
        ProtobufModule _protobufModule;
        CommandModule _commandModule;
        GpsModule _gpsModule;
        GpsEntityModule _gpsEntityModule;

        Dictionary<string, Action<Command>> _serverCommands;

        public override void LoadData()
        {
            Instance = this;

            LoggerManager.SetPrefix("TieredTechBlocks");

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

            _gpsModule = new GpsModule(_protobufModule, 2);
            _gpsModule.Initialize();

            _gpsEntityModule = new GpsEntityModule(_gpsModule);
        }

        void ReloadConfig()
        {
            _configFile = new ContentFile<Config>("TieredTech.cfg", Config.CreateDefault());
            _configFile.ReadOrCreateFile();
            Config.Instance = _configFile.Content;
            LoggerManager.SetLogConfig(Config.Instance.LogConfigs);
        }

        protected override void UnloadData()
        {
            _moreLoot?.UnloadData();
            _protobufModule?.Close();
            _commandModule?.Close();
            _gpsModule?.Close();
            _gpsEntityModule?.Clear();
        }

        public override void UpdateBeforeSimulation()
        {
            _protobufModule.Update();
            _commandModule.Update();

            if (MyAPIGateway.Session.GameplayFrameCounter % 60 == 0)
            {
                _gpsEntityModule.SendAddOrUpdate();
            }

            if (MyAPIGateway.Session.IsServer)
            {
                if (LangUtils.RunOnce(ref _firstFramePassed))
                {
                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0,
                        (object o, ref MyDamageInformation info) => BeforeDamage(o, ref info));
                }
            }
        }

        static void BeforeDamage(object target, ref MyDamageInformation info)
        {
            var block = target as IMySlimBlock;
            if (block == null) return;

            TierForgeBase forge;
            if (block.FatBlock.Components.TryGet(out forge))
            {
                forge.BeforeDamage(ref info);
            }
        }

        public void OnForgeOpened(TierForgeBase forge)
        {
            _gpsEntityModule.Track(forge);
            Log.Info($"forge opened: {forge.Entity.DisplayName}");
        }

        public void OnForgeClosed(TierForgeBase forge)
        {
            _gpsEntityModule.UntrackAndSendRemove(forge);
            Log.Info($"forge closed: {forge.Entity.DisplayName}");
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