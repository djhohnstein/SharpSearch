using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

namespace SharpSearch
{

    class Program
    {

        private static List<Thread> runningThreads = new List<Thread>();

        static void Usage()
        {
            string usageString = @"
Usage:
    Arguments:
        Required:
            path          - Path to search for files. Note: If using quoted paths, ensure you
                            escape backslashes properly.
        
        Optional:
            pattern       - Type of files to search for, e.g. "" *.txt""

            ext_whitelist - Specify file extension whitelist. e.g., ext_whitelist=.txt,.bat,.ps1

            ext_blacklist - Specify file extension blacklist. e.g., ext_blacklist=.zip,.tar,.txt

            searchterms   - Specify a comma deliminated list of searchterms. e.g.searchterms=""foo, bar, asdf""

            year          - Filter files by year.


 Examples:
        
        Find all files that have the phrase ""password"" in them.
        
            SharpSearch.exe path=""C:\\Users\\User\\My Documents\\"" searchterms=password

        Find all batch and powershell scripts in SYSVOL that were created in 2018 containing the word Administrator

            SharpSearch.exe path=""\\\\DC01\\SYSVOL"" ext_whitelist=.ps1,.bat searchterms=Administrator year=2018
";
            Console.WriteLine(usageString);
        }


        static Dictionary<string,string[]> ParseArgs(string[] args)
        {
            Dictionary<string, string[]> result = new Dictionary<string, string[]>();
            string[] commaTerms = new string[] { "ext_whitelist", "ext_blacklist", "search_whitelist", "searchterms"};
            foreach(string arg in args)
            {
                string[] parts = arg.Split("=".ToCharArray(), 2);
                if (parts.Length != 2)
                {
                    Console.WriteLine("[-] Invalid argument format passed (key/value separated by equals): {0}", arg);
                    continue;
                }
                parts[0] = parts[0].ToLower();
                parts[1] = parts[1].ToLower();
                // Verbosity flag won't have an equal sign
                if (commaTerms.Contains(parts[0]))
                {
                    var tmp = parts[1].Split(',');
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        tmp[i] = tmp[i].Trim();
                    }
                    result[parts[0]] = parts[1].Split(',');
                }
                else
                {
                    result[parts[0]] = new string[] { parts[1] };
                }
            }
            return result;
        }

        static bool ValidateArguments(Dictionary<string, string[]> args)
        {
            if (!args.ContainsKey("path"))
            {
                return false;
            }
            return true;
        }

        static void PrintOptions(Dictionary<string, string[]> args)
        {
            Console.WriteLine("[+] Parsed Arguments:");
            foreach(string key in args.Keys)
            {
                Console.WriteLine("\t{0}: {1}", key, string.Join(", ", args[key]));
            }
            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            string path = "";
            string pattern = "";
            string searchTerm = "";

            var parsedArgs = ParseArgs(args);

            if (!ValidateArguments(parsedArgs))
            {
                Usage();
                Environment.Exit(1);
            }

            PrintOptions(parsedArgs);

            path = parsedArgs["path"][0];

            if (!Directory.Exists(path))
            {
                Console.WriteLine("[X] Error: Path {0} does not exist.", path);
                Environment.Exit(1);
            }
            try
            {
                string[] files = FileWorkers.GetAllFiles(parsedArgs);
                if (files.Length > 0)
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    Utils.PrintResults(files);
                    stopWatch.Stop();
                    TimeSpan timeElapsed = stopWatch.Elapsed;
                    string elapsedTime = String.Format("{0:00}H:{1:00}M:{2:00}.{3:00}S",
                    timeElapsed.Hours, timeElapsed.Minutes, timeElapsed.Seconds,
                    timeElapsed.Milliseconds / 10);
                    Console.WriteLine("Finished in " + elapsedTime);
                }
                else
                {
                    Console.WriteLine("\n[-] No files found in {0} with pattern {1}", path, pattern);
                }
            } catch (Exception ex)
            {
                Console.WriteLine("[X] Error: {0}\n\t{1}", ex.Message, ex.StackTrace);
            }
        }
    }
}
