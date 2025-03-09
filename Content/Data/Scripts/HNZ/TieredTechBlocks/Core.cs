using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HNZ.Utils;
using HNZ.Utils.Communications;
using HNZ.Utils.Logging;
using Sandbox.Game;
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
        TierTechInjector _techInjector;
        ProtobufModule _protobufModule;
        CommandModule _commandModule;
        Dictionary<string, Action<Command>> _serverCommands;
        ConcurrentQueue<GridSpawn> _gridSpawns;

        public override void LoadData()
        {
            Instance = this;

            LoggerManager.SetPrefix(nameof(TieredTechBlocks));

            if (MyAPIGateway.Session.IsServer)
            {
                _techInjector = new TierTechInjector();
                ReloadConfig();

                if (!Config.Instance.AgreeToKeepThisModPrivate)
                {
                    throw new InvalidOperationException(
                        "This is an experimental fork of the Tiered Tech Block mod. " +
                        "Replace this mod with the original Tiered Tech Block mod.");
                }

                _gridSpawns = new ConcurrentQueue<GridSpawn>();
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed += OnGridSpawned;


                _serverCommands = new Dictionary<string, Action<Command>>
                {
                    { "reload", Command_Reload },
                };
            }

            _protobufModule = new ProtobufModule((ushort)nameof(TieredTechBlocks).GetHashCode());
            _protobufModule.Initialize();

            _commandModule = new CommandModule(_protobufModule, 1, "ttb", this);
            _commandModule.Initialize();
        }

        void ReloadConfig()
        {
            _configFile = new ContentFile<Config>("TieredTech.cfg", Config.CreateDefault());
            _configFile.ReadOrCreateFile();
            Config.Instance = _configFile.Content;
            Config.Instance.TryInitialize();
            LoggerManager.SetConfigs(Config.Instance.LogConfigs);
            _techInjector.Update();
            Log.Info("config reloaded");
        }

        protected override void UnloadData()
        {
            _protobufModule?.Close();
            _commandModule?.Close();

            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed -= OnGridSpawned;
            }
        }

        void OnGridSpawned(long entityId, string prefabName)
        {
            _gridSpawns.Enqueue(new GridSpawn
            {
                EntityId = entityId,
                PrefabName = prefabName,
            });
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

                GridSpawn spawn;
                while (_gridSpawns.TryDequeue(out spawn))
                {
                    var grid = MyAPIGateway.Entities.GetEntityById(spawn.EntityId) as IMyCubeGrid;
                    if (grid?.Physics == null) continue; // grid is a projection or doesn't exist anymore

                    Log.Debug($"trying to inject techs/forges: '{grid.DisplayName}'");

                    _techInjector.TryInsertTechs(grid);
                }
            }
        }

        static void BeforeDamage(object target, ref MyDamageInformation info)
        {
            var cargo = (target as IMySlimBlock)?.FatBlock as IMyCargoContainer;
            if (cargo == null) return;

            IMyCargoContainer smallCargo;
            if (!Utils.TryGetSmallCargo(cargo, out smallCargo)) return;

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