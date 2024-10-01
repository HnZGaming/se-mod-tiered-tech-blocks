using System;
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

        [XmlArray]
        [XmlArrayItem("item")]
        public List<string> LootFactionTags;

        [XmlArray]
        [XmlArrayItem("item")]
        public List<LogConfig> LogConfigs;

        [XmlElement]
        public string DataPadDescription;

        public void TryInitialize()
        {
            LangUtils.AssertNull(Common);
            LangUtils.AssertNull(Rare);
            LangUtils.AssertNull(Exotic);
            LangUtils.NullOrDefault(ref LootFactionTags, new List<string>());
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
                GpsRadius = 10,
            },
            Rare = new TechProperty
            {
                Chance = 0.07f,
                MinAmount = 3,
                MaxAmount = 20,
                GpsRadius = 10,
            },
            Exotic = new TechProperty
            {
                Chance = 0.02f,
                MinAmount = 2,
                MaxAmount = 10,
                GpsRadius = -1,
            },
            LootFactionTags = new List<string>
            {
                "FOO",
            },
            LogConfigs = new List<LogConfig>
            {
                new LogConfig
                {
                    Severity = MyLogSeverity.Info,
                    Prefix = "",
                },
            },
        };
    }
}