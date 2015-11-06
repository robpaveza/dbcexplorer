using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbcReader
{
    public class ChatProfanityRecord
    {
        [DbcRecordPosition(0)]
        public int ID { get; set; }
        [DbcRecordPosition(1)]
        public DbcStringReference DirtyWord;
        [DbcRecordPosition(2)]
        public int LanguageID;
    }
}
