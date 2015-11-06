using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbcReader
{
    class Program
    {
        static void Main(string[] args)
        {
            //Config.ForceSlowMode = true;

            Stopwatch timer = new Stopwatch();
            timer.Start();
            using (var fs = File.OpenRead(@"D:\Users\rob_000\Downloads\mpqediten64\Work\DBFilesClient\ChatProfanity.dbc"))
            using (DbcTable<ChatProfanityRecord> table = new DbcTable<ChatProfanityRecord>(fs, false))
            using (var fsOut = File.OpenWrite("output.txt"))
            using (var sw = new StreamWriter(fsOut, Encoding.UTF8))
            {
                foreach (var item in table)
                {
                    //Console.WriteLine("{0}: {1}", item.ID, item.DirtyWord);
                    //sw.WriteLine("{0,-8}{1,-40}{2}", item.ID, item.DirtyWord, item.LanguageID);
                }

                foreach (var item in table)
                {
                    //Console.WriteLine("{0}: {1}", item.ID, item.DirtyWord);
                    //sw.WriteLine("{0,-8}{1,-40}{2}", item.ID, item.DirtyWord, item.LanguageID);
                }

                foreach (var item in table)
                {
                    //Console.WriteLine("{0}: {1}", item.ID, item.DirtyWord);
                    //sw.WriteLine("{0,-8}{1,-40}{2}", item.ID, item.DirtyWord, item.LanguageID);
                }

                foreach (var item in table)
                {
                    //Console.WriteLine("{0}: {1}", item.ID, item.DirtyWord);
                    //sw.WriteLine("{0,-8}{1,-40}{2}", item.ID, item.DirtyWord, item.LanguageID);
                }

                foreach (var item in table)
                {
                    //Console.WriteLine("{0}: {1}", item.ID, item.DirtyWord);
                    //sw.WriteLine("{0,-8}{1,-40}{2}", item.ID, item.DirtyWord, item.LanguageID);
                }
            }
            timer.Stop();
            Console.WriteLine("Completed in {0}ms", timer.ElapsedMilliseconds);

            Console.WriteLine("Done; press <enter> to exit.");
            Console.ReadLine();
        }
    }
}
