using System;

namespace DbMigrator
{
    public class Migration
    {
        public static Migration MakeChangeset(int num, string name, string content)
        {
            return new Migration { Num = num, Name = name, Revision = "none", Content = content };
        }
        
        public static Migration MakeObjectMigration(string revision, string name, string content)
        {
            return new Migration {Num = -1,Revision = revision, Name = name, Content = content};
        }

        public static Migration MakeAppliedMigration(int num, string revision, string name, DateTime applyDate)
        {
            return new Migration { Num = num, Revision = revision, Name = name, ApplyDate = applyDate };
        }

        public int Num { get; private set; }
        public string Revision { get; private set; }
        public string Name { get; private set; }
        public string Content { get; private set; }
        public DateTime ApplyDate { get; private set; }


        public override string ToString()
        {
            if (Num != 0)
                return string.Format("#{0} {1}", Num, Name);
            if (string.IsNullOrWhiteSpace(Revision))
                return string.Format("r{0} {1}", Revision, Name);

            return string.Format("#{0} r{1} {2}", Num, Revision, Name);
        }

        public override bool Equals(object obj)
        {
            var m = obj as Migration;
            if (m == null)
                return false;

            return (m.Num == Num) &&(m.Revision == Revision) && (m.Name == Name);
        }

        public override int GetHashCode()
        {
            return Num.GetHashCode() ^Revision.GetHashCode() ^ Name.GetHashCode();
        }
    }
}
