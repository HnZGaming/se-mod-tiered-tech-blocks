﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using HNZ.Utils;
using HNZ.Utils.Logging;
using VRage.Utils;

namespace HNZ.TieredTechBlocks
{
    [Serializable]
    public class Config
    {
        public static Config Instance { get; set; }

        [XmlElement]
        public bool AgreeToKeepThisModPrivate;

        [XmlElement]
        public TechProperty Common;

        [XmlElement]
        public TechProperty Rare;

        [XmlElement]
        public TechProperty Exotic;

        [XmlElement]
        public List<string> ExcludeGridNames;

        [XmlElement]
        public List<CargoReplacement> CargoReplacements;

        [XmlElement]
        public List<LogConfig> LogConfigs;

        [XmlElement]
        public string DataPadDescription;

        public void TryInitialize()
        {
            LangUtils.AssertNull(Common);
            LangUtils.AssertNull(Rare);
            LangUtils.AssertNull(Exotic);
            LangUtils.NullOrDefault(ref ExcludeGridNames, new List<string>());
            LangUtils.NullOrDefault(ref CargoReplacements, new List<CargoReplacement>());
            LangUtils.NullOrDefault(ref LogConfigs, new List<LogConfig>());
            LangUtils.NullOrDefault(ref DataPadDescription, "");
        }

        public static Config CreateDefault() => new Config
        {
            Common = new TechProperty
            {
                Chance = 0.15f,
                MinAmount = 6,
                MaxAmount = 40,
                ForgeMod = 100,
                GpsRadius = 10,
                MaxForgeCount = 90,
                DamageMultiply = 0,
            },
            Rare = new TechProperty
            {
                Chance = 0.07f,
                MinAmount = 3,
                MaxAmount = 20,
                ForgeMod = 200,
                GpsRadius = 10,
                MaxForgeCount = 60,
                DamageMultiply = 0,
            },
            Exotic = new TechProperty
            {
                Chance = 0.02f,
                MinAmount = 2,
                MaxAmount = 10,
                ForgeMod = 300,
                GpsRadius = -1,
                MaxForgeCount = 30,
                DamageMultiply = 0,
            },
            ExcludeGridNames = new List<string>
            {
                "respawn",
            },
            CargoReplacements = new List<CargoReplacement>
            {
                new CargoReplacement
                {
                    FactionTag = "PEAVER",
                    Tier = 4,
                    Chance = 0.2f,
                },
            },
            LogConfigs = new List<LogConfig>
            {
                new LogConfig
                {
                    Severity = MyLogSeverity.Info,
                    Prefix = "",
                },
            },
            DataPadDescription = "You can forge your {0} source components into {0} tech components using this block.",
        };
    }
}