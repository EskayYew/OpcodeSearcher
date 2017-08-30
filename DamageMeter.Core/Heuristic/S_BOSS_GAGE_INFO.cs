﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tera.Game.Messages;

namespace DamageMeter.Heuristic
{
    class S_BOSS_GAGE_INFO : AbstractPacketHeuristic
    {
        private static S_BOSS_GAGE_INFO _instance;
        public static S_BOSS_GAGE_INFO Instance => _instance ?? (_instance = new S_BOSS_GAGE_INFO());

        public S_BOSS_GAGE_INFO() : base(OpcodeEnum.S_BOSS_GAGE_INFO) { }

        public new void Process(ParsedMessage message)
        {
            base.Process(message);
            if(IsKnown || OpcodeFinder.Instance.IsKnown(message.OpCode)) return;
            if (message.Payload.Count != 8 + 4 + 4 + 8 + 4 + 4 + 1 + 4 + 4 + 1) return;
            var cid = Reader.ReadUInt64();
            var zoneId = Reader.ReadUInt32();
            var templateId = Reader.ReadUInt32();
            var target = Reader.ReadUInt64();
            var unk1 = Reader.ReadUInt32();
            var hpDiff = Reader.ReadSingle();
            var unk2 = Reader.ReadByte();
            var curHp = Reader.ReadSingle();
            var maxHp = Reader.ReadSingle();
            var unk3 = Reader.ReadByte();

            if (unk3 != 1) return;

            //check that the cid is contained in spawned npcs list
            if (OpcodeFinder.Instance.KnowledgeDatabase.ContainsKey(OpcodeFinder.KnowledgeDatabaseItem.SpawnedNpcs))
            {
                var res = OpcodeFinder.Instance.KnowledgeDatabase[OpcodeFinder.KnowledgeDatabaseItem.SpawnedNpcs].Item2;
                var list = (List<Npc>) res;

                if (!list.Any(x => x.Cid == cid && x.ZoneId == zoneId && x.TemplateId == templateId)) return;
            }
            else return;
            OpcodeFinder.Instance.SetOpcode(message.OpCode, OPCODE);
        }
    }
}