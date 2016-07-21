using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer
{
    public enum CutterCode
    {
        None = (byte)0x0,
        KnifeUp = 0x20,
        KnifeDown = 0x30,
        StartTurnCW25 = 0x40,
        StartTurnCW51 = 0x41,
        StartTurnCW76 = 0x42,
        StartTurnCW112 = 0x43,
        StartTurnCW127 = 0x44,
        StartTurnCW153 = 0x45,
        StartTurnCW188 = 0x46,
        StartTurnCW209 = 0x47,
        StartTurnCW234 = 0x48,
        StartTurnCW255 = 0x49,
        StartTurnCW280 = 0x4A,
        StartTurnCW306 = 0x4B,
        StartTurnCW331 = 0x4C,
        StartTurnCW357 = 0x4D,
        StartTurnCW382 = 0x4E,
        StartTurnCW408 = 0x4F,
        SledgeAbsoluteCenter = 0x50,
        SledgeAbsoluteMid = 0x51,
        SledgeAbsoluteOuter = 0x52,
        TurnCW = 0x60,
        TurnCCW = 0x61,        
        TurnWhatEver = 0x62,
        SledgeOut = 0x70,
        SledgeIn = 0x71,
        StartTurnCCW25 = 0x80,
        StartTurnCCW51 = 0x81,
        StartTurnCCW76 = 0x82,
        StartTurnCCW112 = 0x83,
        StartTurnCCW127 = 0x84,
        StartTurnCCW153 = 0x85,
        StartTurnCCW188 = 0x86,
        StartTurnCCW209 = 0x87,
        StartTurnCCW234 = 0x88,
        StartTurnCCW255 = 0x89,
        StartTurnCCW280 = 0x8A,
        StartTurnCCW306 = 0x8B,
        StartTurnCCW331 = 0x8C,
        StartTurnCCW357 = 0x8D,
        StartTurnCCW382 = 0x8E,
        StartTurnCCW408 = 0x8F,
        StartSledgeAndWaitForGoCenter = 0x90,
        StartSledgeAndWaitForGoMid = 0x91,
        StartSledgeAndWaitForGoOuter = 0x92,
        Finish = 0xF0


    }
}
