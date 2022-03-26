using System;
using System.Collections.Generic;
using HNZ.LocalGps.Interface;
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
        static readonly Logger Log = LoggerManager.Create(nameof(Core));

        public static Core Instance { get; private set; }

        bool _firstFramePassed;
        ContentFile<Config> _configFile;
        MoreLoot _moreLoot;
        ProtobufModule _protobufModule;
        CommandModule _commandModule;
        LocalGpsApi _localGpsApi;
        HashSet<TierForgeBase> _forges;

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

            _localGpsApi = new LocalGpsApi(nameof(TieredTechBlocks).GetHashCode());
            _forges = new HashSet<TierForgeBase>();
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

            if (MyAPIGateway.Session.GameplayFrameCounter % 6 == 0)
            {
                foreach (var forge in _forges)
                {
                    _localGpsApi.AddOrUpdateLocalGps(forge.CreateLocalGpsSource);
                }
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
            var forge = block?.FatBlock?.GameLogic?.GetAs<TierForgeBase>();
            forge?.BeforeDamage(ref info);
        }

        public void OnForgeOpened(TierForgeBase forge)
        {
            _forges.Add(forge);
            _localGpsApi.AddOrUpdateLocalGps(forge.CreateLocalGpsSource);
            Log.Info($"forge opened: {forge.Entity.DisplayName}");
        }

        public void OnForgeClosed(TierForgeBase forge)
        {
            _forges.Remove(forge);
            _localGpsApi.RemoveLocalGps(forge.Entity.EntityId);
            Log.Info($"forge closed: {forge.Entity.DisplayName}");
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