using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Helpers;

namespace TWWHeapVisualizer.Heap
{
    public enum fopAc_ac_c__Type
    {
        Regular = 0x0,
        Link = 0x1,
        Enemy = 0x2,
        Wildlife_and_misc = 0x3,
        Some_NPCs = 0x4,
        unknown = 0xffff
    }
    public enum fopAcM__Status
    {
        AppearsOnMiniMap = 0x20,
        DoNotExecuteIfDidNotDraw = 0x80,
        DoNotDrawIfCulled = 0x100,
        Frozen = 0x400,
        IsBeingCarried = 0x2000,
        DoesNotPause = 0x20000,
        DoNotDrawNextFrame = 0x1000000,
        IsBossOrMiniBoss = 0x4000000,
        unkown = 0
    }

    public enum fopAc_ac_c__Condition
    {
        DidNotExecuteThisFrame = 0x2,
        DidNotDrawThisFrame = 0x4,
        Constructed = 0x8,
        DidExecuteThisFrame = 0xC,
        unknown = 0x0,
        __dummy = 0x8000

    }
    public class fopAc_ac_c
    {
        const UInt64 procNameOffset = 0x8;
        const UInt64 gameHeapOffset = 0xF0;
        const UInt64 actorTypeOffset = 0x1BE;
        const UInt64 subTypeOffset = 0x1C1;
        const UInt64 gbaNameOffset = 0x1C2;
        const UInt64 statusOffset = 0x1C4;
        const UInt64 conditionOffset = 0x1C8;
        const UInt64 xPosOffset = 0x1F8;
        const UInt64 yPosOffset = 0x1FC;
        const UInt64 zPosOffset = 0x200;


        public UInt64 address { get; set; }
        public float xPos { get; set; }
        public float yPos { get; set; }
        public float zPos { get; set; }
        public ushort procName { get; set; }
        public uint gameHeapPtr { get; set; }
        public string displayName { get; set; }
        public int size { get; set; }
        //public int size
        //{
        //    get
        //    {
        //        return ActorData.Instance.ActorSize(displayName);
        //    }
        //}
        public byte subType { get; set; }
        public byte gbaName { get; set; }
        public fopAc_ac_c__Type actorType { get; set; }
        public uint? unkownActorType { get; set; }
        public fopAcM__Status status { get; set; }
        public uint? unkownStatusInt { get; set; }
        public fopAc_ac_c__Condition condition { get; set; }
        public uint? unknownConditionInt { get; set; } //used if unknown condition

        public fopAc_ac_c(UInt64 address)
        {
            this.address = address;
            this.procName = Memory.ReadMemory<ushort>((ulong)address + (ulong)procNameOffset);
            this.subType = Memory.ReadMemory<byte>((ulong)address + (ulong)subTypeOffset);
            this.gbaName = Memory.ReadMemory<byte>((ulong)address + (ulong)gbaNameOffset);

            uint actorTypeInt = (uint)Memory.ReadMemory<byte>((ulong)address + (ulong)actorTypeOffset);
            this.actorType = fopAc_ac_c__Type.unknown;
            if (Enum.IsDefined(typeof(fopAc_ac_c__Type), (int)actorTypeInt))
            {
                this.actorType = (fopAc_ac_c__Type)actorTypeInt;
            }
            else
            {
                this.unkownActorType = actorTypeInt;
            }

            uint conditionInt = Memory.ReadMemory<uint>((ulong)address + (ulong)conditionOffset);
            this.condition = fopAc_ac_c__Condition.unknown;
            if (Enum.IsDefined(typeof(fopAc_ac_c__Condition), (int)conditionInt))
            {
                this.condition = (fopAc_ac_c__Condition)conditionInt;
            }
            else
            {
                this.unknownConditionInt = conditionInt;
            }
            uint statusInt = (uint)Memory.ReadMemory<ushort>((ulong)address + (ulong)statusOffset);
            if (Enum.IsDefined(typeof(fopAcM__Status), (int)statusInt))
            {
                this.status = (fopAcM__Status)statusInt;
            }
            else
            {
                this.unkownStatusInt = statusInt;
            }

            this.xPos = Memory.ReadMemory<float>((ulong)address + (ulong)xPosOffset);
            this.yPos = Memory.ReadMemory<float>((ulong)address + (ulong)yPosOffset);
            this.zPos = Memory.ReadMemory<float>((ulong)address + (ulong)zPosOffset);
            this.gameHeapPtr = Memory.ReadMemory<uint>((ulong)address + (ulong)gameHeapOffset);


            ObjectName objectName = ActorData.Instance.ObjectNameTable.Values.FirstOrDefault(o => o.procName == this.procName && o.actorSubTypeIndex == this.subType && o.gbaType == this.gbaName);
            if (objectName != null)
            {

                this.displayName = objectName.ToString();
                this.size = ActorData.Instance.ActorSize(objectName.actorName);
            }

        }
        public static Dictionary<uint, fopAc_ac_c> GetCreatedActors(UInt64 address)
        {
            if (!MemoryHelpers.isValidAddress((uint)address))
            {
                return new Dictionary<uint, fopAc_ac_c>();
            }
            Dictionary<uint, fopAc_ac_c> actors = new Dictionary<uint, fopAc_ac_c>();
            uint nextActorPtr = Memory.ReadMemory<uint>((ulong)address);
            uint fopACTgPtr = Memory.ReadMemory<uint>((ulong)address + 0xC);
            var actor = new fopAc_ac_c(fopACTgPtr);
            actors[actor.gameHeapPtr] = actor;
            while (MemoryHelpers.isValidAddress(nextActorPtr))
            {
                address = nextActorPtr;
                nextActorPtr = Memory.ReadMemory<uint>((ulong)address);
                fopACTgPtr = Memory.ReadMemory<uint>((ulong)address + 0xC);
                actor = new fopAc_ac_c(fopACTgPtr);
                actors[actor.gameHeapPtr] = actor;
                //actors.Add(new fopAc_ac_c(fopACTgPtr));
            }
            actors.Reverse();
            return actors;
        }
    }
}
