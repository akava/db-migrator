using System;
using System.Collections.Generic;
using System.Net;

namespace DbMigrator
{
    public class Migration
    {
        public Migration(string fileName, string fileContent)
        {
            try
            {
                Num = int.Parse(fileName.Substring(0, 4));
                Name = fileName.Substring(4 + 1).Split('.')[0];
                Content = fileContent;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format(
                    "Filename has invalid format. Given: {0} Expected: '0000_text_text_text.sql'. Inner message: {1}",
                    fileName, ex.Message), ex);
            }
        }

        public Migration(int num, string name, DateTime applyDate)
        {
            Num = num;
            Name = name;
            ApplyDate = applyDate;
        }

        public int Num { get; private set; }
        public string Name { get; private set; }
        public string Content { get; private set; }
        public DateTime ApplyDate { get; private set; }


        public override string ToString()
        {
            return string.Format("#{0} {1}", Num, Name);
        }

        public override bool Equals(object obj)
        {
            var m = obj as Migration;
            if (m == null)
                return false;

            return (m.Num == Num) && (m.Name == Name);
        }

        public override int GetHashCode()
        {
            return Num.GetHashCode() ^ Name.GetHashCode();
        }
    }
}