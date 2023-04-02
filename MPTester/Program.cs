using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using rmsft.mptWrapper;
using System.IO;
namespace MPTester
{
    class Program
    {
        static bool triggerCalled = false;
        static bool triggerRan = false;
        static IntPtr moduleExt;
        static IntPtr moduleStd;
        static int nextsub = 0;
        private static bool triggerCalled1;
        private static bool triggerRan1;
        private static bool triggerCalled3;
        private static bool triggerRan3;

        static void Main(string[] args)
        {
            string p = Path.GetFullPath(@"resource\\TP_IT.mptm");
             moduleExt = ModuleAPI.LoadFromFile(p);
             moduleStd = ModuleAPI.GetStdModule(moduleExt);

            
            Console.WriteLine("Starting module stream thread yall.");

            Task.Run(() => ModuleAPI.StartModuleStream(moduleStd, 5));

            int tch = ModuleAPI.GetChannelCount(moduleStd);
            int tss = ModuleAPI.GetSongCount(moduleStd);
            Console.WriteLine("This module has {0} sub songs and {1} channels.",tss,tch);
            Console.WriteLine("Commands:\r\n\texit\r\n\tfadeout <int ch>\r\n\tfadein <int ch>\r\n\tswap <int SongIndex>\r\n\tfswap <int SongIndex>\r\n\tnav <int order> <int row>");
            Console.WriteLine("Starting command thread yall.");

            ModuleAPI.PatternStarted += ModuleAPI_PatternStarted;
            ModuleAPI.PatternEnded += ModuleAPI_PatternEnded;
            ModuleAPI.PatternRowChanged += ModuleAPI_PatternRowChanged;

            while (true)
            {
                string line = Console.ReadLine();

                if (line == "exit")
                {
                    break;
                }

                if(line.StartsWith("fadeout"))
                {
                    string arg = line.Replace("fadeout", "").Trim();

                    bool s = int.TryParse(arg, out int ch);

                    Console.WriteLine("Attempting fadeout for channel {0}", ch);

                    ModuleAPI.FadeChannelVolume(moduleExt, ch, 0, 0.375);

                }
                if (line.StartsWith("fadein"))
                {
                    string arg = line.Replace("fadein", "").Trim();

                    bool s = int.TryParse(arg, out int ch);

                    Console.WriteLine("Attempting fadein for channel {0}", ch);

                    ModuleAPI.FadeChannelVolume(moduleExt, ch,1,0.375);
                }

                if (line.StartsWith("fswap"))
                {
                    string arg = line.Replace("fswap", "").Trim();

                    bool s = int.TryParse(arg, out int ss);

                    Console.WriteLine("Fading to song {0}", ss);

                    ModuleAPI.FadeToSubSong(moduleStd,moduleExt, ss,1);
                }

                if (line.StartsWith("tfswap"))
                {
                    string arg = line.Replace("tfswap", "").Trim();

                    bool s = int.TryParse(arg, out int ss);

                    Console.WriteLine("Fading to song {0} on new pattern", ss);

                    nextsub = ss;
                    triggerCalled1 = true;
                    triggerRan1 = false;
                }

                if (line.StartsWith("swap"))
                {
                    string arg = line.Replace("swap", "").Trim();

                    bool s = int.TryParse(arg, out int ss);

                    Console.WriteLine("switching to song {0}", ss);

                    ModuleAPI.SetSubSong(moduleStd,ss);
                }
                if (line.StartsWith("tswap"))
                {
                    string arg = line.Replace("tswap", "").Trim();

                    bool s = int.TryParse(arg, out int ss);

                    Console.WriteLine("switching to song {0} on pattern start event", ss);

                    nextsub = ss;
                    triggerCalled = true;
                    triggerRan = false;
                }
                if (line.StartsWith("eswap"))
                {
                    string arg = line.Replace("eswap", "").Trim();

                    bool s = int.TryParse(arg, out int ss);

                    Console.WriteLine("switching to song {0} on pattern end event", ss);

                    nextsub = ss;
                    triggerCalled3 = true;
                    triggerRan3 = false;
                }
                if (line.StartsWith("nav"))
                {
                    string[] ags = line.Replace("nav", "").Trim().Split(' ');

                    if(ags.Length != 2)
                    {
                        Console.WriteLine("Invalid arg count");
                        continue;
                    }

                    bool s1 = int.TryParse(ags[0], out int order);
                    bool s2 = int.TryParse(ags[1], out int row);
                    if(s1 && s2)
                    {
                        ModuleAPI.SetOrderRow(moduleStd, order, row);
                    } 
                    else
                    {
                        Console.WriteLine("invalid format. expected two integers.");
                        continue;
                    }
                }
            }

            Console.WriteLine("Terminating");


        }

        private static void ModuleAPI_PatternRowChanged(object sender, PatternEventArgs e)
        {
            //Console.WriteLine("ROW {0:000} | PATTERN {1:000} | ORDER {2:000} | SONG {3:000}", e.Row, e.Pattern, e.Order, e.SubSong);
            //if(e.SubSong == 3 && e.Row == 127 && e.Order == 5)
            //{
            //    ModuleAPI.SetOrderRow(moduleStd, 19, 0);
            //}
        }

        private static void ModuleAPI_PatternEnded(object sender, PatternEventArgs e)
        {
            if (triggerCalled3 && !triggerRan3)
            {
                ModuleAPI.SetSubSong(moduleStd, nextsub);
                triggerCalled3 = false;
                triggerRan3 = true;
            }
        }

        private static void ModuleAPI_PatternStarted(object sender, PatternEventArgs e)
        {
            //Console.WriteLine("Start: pattern {0} - Order {1}",e.Pattern,e.Order);

            if (triggerCalled && !triggerRan)
            {
                ModuleAPI.SetSubSong(moduleStd, nextsub);
                triggerCalled = false;
                triggerRan = true;
            }
            if (triggerCalled1 && !triggerRan1)
            {
                ModuleAPI.FadeToSubSong(moduleStd,moduleExt,nextsub,1);
                triggerCalled1 = false;
                triggerRan1 = true;
            }
        }
    }
}
