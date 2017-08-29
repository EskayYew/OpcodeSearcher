﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tera.Game.Messages;

namespace DamageMeter.Heuristic
{
    class S_DESPAWN_USER : AbstractPacketHeuristic
    {

        public static S_DESPAWN_USER Instance => _instance ?? (_instance = new S_DESPAWN_USER());
        private static S_DESPAWN_USER _instance;

        public S_DESPAWN_USER() : base(OpcodeEnum.S_DESPAWN_USER) { }

        public new void Process(ParsedMessage message)
        {
            base.Process(message);
            if (IsKnown || OpcodeFinder.Instance.IsKnown(message.OpCode))
            {
                if (OpcodeFinder.Instance.GetOpcode(OPCODE) == message.OpCode)
                {
                    var id = Reader.ReadUInt64();
                    RemoveUserFromDatabase(id);
                }
                return;
            }
            if (message.Payload.Count != 8 + 4) return;

            var cid = Reader.ReadUInt64();
            var type = Reader.ReadUInt32();
            if (type != 1) return;
            if (!IsUserSpanwed(cid)) return;
            OpcodeFinder.Instance.SetOpcode(message.OpCode, OPCODE);
            RemoveUserFromDatabase(cid);
        }

        private bool IsUserSpanwed(ulong id)
        {
            if (OpcodeFinder.Instance.KnowledgeDatabase.TryGetValue(OpcodeFinder.KnowledgeDatabaseItem.SpawnedUsers, out Tuple<Type, object> result))
            {
                var list = (List<ulong>)result.Item2;
                if (list.Contains(id)) return true;
            }
            return false;
        }
        private void RemoveUserFromDatabase(ulong id)
        {
            if (OpcodeFinder.Instance.KnowledgeDatabase.TryGetValue(OpcodeFinder.KnowledgeDatabaseItem.SpawnedUsers, out Tuple<Type, object> result))
            {
                OpcodeFinder.Instance.KnowledgeDatabase.Remove(OpcodeFinder.KnowledgeDatabaseItem.SpawnedUsers);
            }
            var list = (List<ulong>)result.Item2;
            if (list.Contains(id)) list.Remove(id);
            OpcodeFinder.Instance.KnowledgeDatabase.Add(OpcodeFinder.KnowledgeDatabaseItem.SpawnedUsers, new Tuple<Type, object>(typeof(List<ulong>), list));
        }

    }
}
