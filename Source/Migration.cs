using System;

namespace DbMigrator
{
    public class Migration
    {
        private const int OBJECT_MIGRATION_NUM = 9999;

        public static Migration MakeChangeset(int num, string name, string content)
        {
            return new Migration { Num = num, Name = name, Hash = "00000000000000000000000000000000", Content = content };
        }
        
        public static Migration MakeObjectMigration(string name, string content)
        {
            var hash = Utils.ComputeMd5Hash(content);
            return new Migration {Num = OBJECT_MIGRATION_NUM,Hash = hash, Name = name, Content = content};
        }

        public static Migration MakeAppliedMigration(int num, string name, string hash, DateTime applyDate)
        {
            return new Migration { Num = num, Hash = hash, Name = name, ApplyDate = applyDate };
        }

        public int Num { get; private set; }
        public string Name { get; private set; }
        public string Content { get; private set; }
        public string Hash { get; private set; }
        public DateTime ApplyDate { get; private set; }

        public override string ToString()
        {
            if (Num != OBJECT_MIGRATION_NUM)
                return string.Format("#{0} {1}", Num, Name);
            return string.Format("{0} ({1})", Name, Hash);
        }

        public override bool Equals(object obj)
        {
            var m = obj as Migration;
            if (m == null)
                return false;

            return (m.Num == Num) &&(m.Hash == Hash) && (m.Name == Name);
        }

        public override int GetHashCode()
        {
            return Num.GetHashCode() ^ Hash.GetHashCode() ^ Name.GetHashCode();
        }
    }
}
