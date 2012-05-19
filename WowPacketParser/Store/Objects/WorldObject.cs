using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WowPacketParser.Enums;
using System.Runtime.InteropServices;
using WowPacketParser.Misc;
using Guid = WowPacketParser.Misc.Guid;

namespace WowPacketParser.Misc
{
    public class WorldObject
    {
        public Guid Guid;
        public ObjectType Type;
        public uint[] RawUpdateFields;
        public Int32 FieldCount;
        public WorldObject(Guid guid, ObjectType type)
        {
            Guid = guid;
            Type = type;
            FieldCount = 0;
            switch (type)
            {
                case ObjectType.Object:
                    FieldCount = (int)Enums.Version.UpdateFields.GetUpdateFieldOffset(ObjectField.OBJECT_END);
                    break;
                case ObjectType.Container:
                    FieldCount = (int)Enums.Version.UpdateFields.GetUpdateFieldOffset(ContainerField.CONTAINER_END);
                    break;
                case ObjectType.Corpse:
                    FieldCount = (int)Enums.Version.UpdateFields.GetUpdateFieldOffset(CorpseField.CORPSE_END);
                    break;
                case ObjectType.DynamicObject:
                    FieldCount = (int)Enums.Version.UpdateFields.GetUpdateFieldOffset(DynamicObjectField.DYNAMICOBJECT_END);
                    break;
                case ObjectType.GameObject:
                    FieldCount = (int)Enums.Version.UpdateFields.GetUpdateFieldOffset(GameObjectField.GAMEOBJECT_END);
                    break;
                case ObjectType.Unit:
                    FieldCount = (int)Enums.Version.UpdateFields.GetUpdateFieldOffset(UnitField.UNIT_END);
                    break;
                case ObjectType.Player:
                    FieldCount = (int)Enums.Version.UpdateFields.GetUpdateFieldOffset(PlayerField.PLAYER_END);
                    break;
            }
            RawUpdateFields = new uint[FieldCount];
            for (int i = 0; i < FieldCount; ++i)
            {
                RawUpdateFields[i] = 0;
            }
        }
        public UInt64 ReadFieldUInt64(Int32 index)
        {
            ulong low = ReadFieldUInt32(index);
            ulong high = ReadFieldUInt32(index+1);
            ulong ret = (low | (high<<32));
            return ret;
        }
        public Int64 ReadFieldInt64(Int32 index)
        {
            return (Int32)ReadFieldUInt64(index);
        }
        public UInt32 ReadFieldUInt32(Int32 index)
        {
            return RawUpdateFields[index];
        }
        public Int32 ReadFieldInt32(Int32 index)
        {
            return (Int32)RawUpdateFields[index];
        }
        public Single ReadFieldSingle(Int32 index)
        {
            uint a = ReadFieldUInt32(index);
            byte[] tarr = BitConverter.GetBytes(a);
            return BitConverter.ToSingle(tarr, 0);
        }
    }
}
