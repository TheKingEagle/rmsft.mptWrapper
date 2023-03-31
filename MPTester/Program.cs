﻿using System;
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
        static void Main(string[] args)
        {
            string p = Path.GetFullPath(@"resource\\TP_IT.mptm");
            IntPtr moduleExt = ModuleAPI.LoadFromFile(p);
            IntPtr moduleStd = ModuleAPI.GetStdModule(moduleExt);
            Console.WriteLine("Starting module stream thread yall.");

            Task.Run(() => ModuleAPI.StartModuleStream(moduleStd, 5));
            Console.WriteLine("This module has 6 sub songs and 17 channels.");
            Console.WriteLine("Commands:\r\n\texit\r\n\tfadeout <int ch>\r\n\tfadein <int ch>\r\n\tswap <int SongIndex>\r\n\tnav <int order> <int row>");
            Console.WriteLine("Starting command thread yall.");

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

                    ModuleAPI.ChannelFadeOut(moduleExt, ch);
                }
                if (line.StartsWith("fadein"))
                {
                    string arg = line.Replace("fadein", "").Trim();

                    bool s = int.TryParse(arg, out int ch);

                    Console.WriteLine("Attempting fadein for channel {0}", ch);

                    ModuleAPI.ChannelFadeIn(moduleExt, ch);
                }

                if (line.StartsWith("swap"))
                {
                    string arg = line.Replace("swap", "").Trim();

                    bool s = int.TryParse(arg, out int ss);

                    Console.WriteLine("switching to song {0}", ss);

                    Console.WriteLine(ModuleAPI.SetSubSong(moduleStd, ss));
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
    }
}
