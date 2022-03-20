using System;
using ProtoBuf;
using VRageMath;

namespace HNZ.LocalGps.Interface
{
    [Serializable]
    [ProtoContract]
    public sealed class LocalGpsSource
    {
        [ProtoMember(1)]
        public long Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }

        [ProtoMember(3)]
        public Color Color { get; set; }

        [ProtoMember(4)]
        public string Description { get; set; }

        [ProtoMember(5)]
        public Vector3D Position { get; set; }

        [ProtoMember(6)]
        public double Radius { get; set; } // negative value -> everyone

        [ProtoMember(7)]
        public long EntityId { get; set; }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Name)}: {Name}, {nameof(Color)}: {Color}, {nameof(Description)}: {Description}, {nameof(Position)}: {Position}, {nameof(Radius)}: {Radius}, {nameof(EntityId)}: {EntityId}";
        }
    }
}