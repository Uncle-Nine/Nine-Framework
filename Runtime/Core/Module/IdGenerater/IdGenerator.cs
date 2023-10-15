﻿using System;
using System.Runtime.InteropServices;

namespace Framework
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IdStruct
    {
        public uint Time; // 30bit
        public ushort Value; // 16bit

        public long ToLong()
        {
            ulong result = 0;
            result |= this.Value;
            result |= (ulong)this.Time << 34;
            return (long)result;
        }

        public IdStruct(uint time, ushort value)
        {
            this.Time = time;
            this.Value = value;
        }

        public IdStruct(long id)
        {
            ulong result = (ulong)id;
            this.Value = (ushort)(result & ushort.MaxValue);
            result >>= 16;
            this.Time = (uint)result;
        }

        public override string ToString()
        {
            return $"time: {this.Time}, value: {this.Value}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InstanceIdStruct
    {
        public uint Time; // 当年开始的tick 28bit
        public uint Value; // 18bit

        public long ToLong()
        {
            ulong result = 0;
            result |= this.Value;
            result |= (ulong)this.Time << 36;
            return (long)result;
        }

        public InstanceIdStruct(long id)
        {
            ulong result = (ulong)id;
            this.Value = (uint)(result & IdGenerator.Mask18bit);
            result >>= 18;
            this.Time = (uint)result;
        }

        public InstanceIdStruct(uint time, uint value)
        {
            this.Time = time;
            this.Value = value;
        }

        // 给SceneId使用
        public InstanceIdStruct(uint value)
        {
            this.Time = 0;
            this.Value = value;
        }

        public override string ToString()
        {
            return $"value: {this.Value} time: {this.Time}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UnitIdStruct
    {
        public uint Time; // 30bit 34年
        public ushort Zone; // 10bit 1024个区
        public ushort Value; // 16bit 每秒每个进程最大16K个Unit

        public long ToLong()
        {
            ulong result = 0;
            result |= this.Value;
            result |= (ulong)this.Zone << 24;
            result |= (ulong)this.Time << 34;
            return (long)result;
        }

        public UnitIdStruct(int zone, uint time, ushort value)
        {
            this.Time = time;
            this.Value = value;
            this.Zone = (ushort)zone;
        }

        public UnitIdStruct(long id)
        {
            ulong result = (ulong)id;
            this.Value = (ushort)(result & ushort.MaxValue);
            result >>= 16;
            this.Zone = (ushort)(result & 0x03ff);
            result >>= 10;
            this.Time = (uint)result;
        }

        public override string ToString()
        {
            return $"value: {this.Value} time: {this.Time}";
        }

        public static int GetUnitZone(long unitId)
        {
            int v = (int)((unitId >> 24) & 0x03ff); // 取出10bit
            return v;
        }
    }

    public class IdGenerator : Singleton<IdGenerator>
    {
        public const int Mask18bit = 0x03ffff;

        public const int MaxZone = 1024;

        private long epoch2020;
        private ushort value;
        private uint lastIdTime;


        private long epochThisYear;
        private uint instanceIdValue;
        private uint lastInstanceIdTime;


        private ushort unitIdValue;
        private uint lastUnitIdTime;

        public IdGenerator()
        {
            long epoch1970tick = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000;
            this.epoch2020 = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 - epoch1970tick;
            this.epochThisYear = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 -
                                 epoch1970tick;

            this.lastInstanceIdTime = TimeSinceThisYear();
            if (this.lastInstanceIdTime <= 0)
            {
                Log.Warning($"lastInstanceIdTime less than 0: {this.lastInstanceIdTime}");
                this.lastInstanceIdTime = 1;
            }

            this.lastIdTime = TimeSince2020();
            if (this.lastIdTime <= 0)
            {
                Log.Warning($"lastIdTime less than 0: {this.lastIdTime}");
                this.lastIdTime = 1;
            }

            this.lastUnitIdTime = TimeSince2020();
            if (this.lastUnitIdTime <= 0)
            {
                Log.Warning($"lastUnitIdTime less than 0: {this.lastUnitIdTime}");
                this.lastUnitIdTime = 1;
            }
        }

        private uint TimeSince2020()
        {
            uint a = (uint)((TimeInfo.Instance.ClientNow() - this.epoch2020) / 1000);
            return a;
        }

        private uint TimeSinceThisYear()
        {
            uint a = (uint)((TimeInfo.Instance.ClientNow() - this.epochThisYear) / 1000);
            return a;
        }

        public long GenerateInstanceId()
        {
            uint time = TimeSinceThisYear();

            if (time > this.lastInstanceIdTime)
            {
                this.lastInstanceIdTime = time;
                this.instanceIdValue = 0;
            }
            else
            {
                ++this.instanceIdValue;

                if (this.instanceIdValue > IdGenerator.Mask18bit - 1) // 18bit
                {
                    ++this.lastInstanceIdTime; // 借用下一秒
                    this.instanceIdValue = 0;

                    Log.Error($"instanceid count per sec overflow: {time} {this.lastInstanceIdTime}");
                }
            }

            InstanceIdStruct instanceIdStruct =
                new InstanceIdStruct(this.lastInstanceIdTime, this.instanceIdValue);
            return instanceIdStruct.ToLong();
        }

        public long GenerateId()
        {
            uint time = TimeSince2020();

            if (time > this.lastIdTime)
            {
                this.lastIdTime = time;
                this.value = 0;
            }
            else
            {
                ++this.value;

                if (value > ushort.MaxValue - 1)
                {
                    this.value = 0;
                    ++this.lastIdTime; // 借用下一秒
                    Log.Error($"id count per sec overflow: {time} {this.lastIdTime}");
                }
            }

            IdStruct idStruct = new IdStruct(this.lastIdTime, value);
            return idStruct.ToLong();
        }

        public long GenerateUnitId(int zone)
        {
            if (zone > MaxZone)
            {
                throw new Exception($"zone > MaxZone: {zone}");
            }

            uint time = TimeSince2020();

            if (time > this.lastUnitIdTime)
            {
                this.lastUnitIdTime = time;
                this.unitIdValue = 0;
            }
            else
            {
                ++this.unitIdValue;

                if (this.unitIdValue > ushort.MaxValue - 1)
                {
                    this.unitIdValue = 0;
                    ++this.lastUnitIdTime; // 借用下一秒
                    Log.Error($"unitid count per sec overflow: {time} {this.lastUnitIdTime}");
                }
            }

            UnitIdStruct unitIdStruct =
                new UnitIdStruct(zone, this.lastUnitIdTime, this.unitIdValue);
            return unitIdStruct.ToLong();
        }
    }
}