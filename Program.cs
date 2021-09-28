using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace SharpSearch
{

    class Program
    {

        static string FilterKey = "ext_filterlist";
        static string BlockKey = "ext_blocklist";
        static string PatternKey = "pattern";
        static string SearchKey = "searchterms";
        static string YearKey = "year";


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

            ext_filterlist - Specify file extension to filter for. e.g., ext_filterlist=.txt,.bat,.ps1

            ext_blocklist - Specify file extension to ignore. e.g., ext_blocklist=.zip,.tar,.txt

            searchterms   - Specify a comma deliminated list of searchterms. e.g.searchterms=""foo, bar, asdf""

            year          - Filter files by year.


 Examples:
        
        Find all files that have the phrase ""password"" in them.
        
            SharpSearch.exe path=""C:\\Users\\User\\My Documents\\"" searchterms=password

        Find all batch and powershell scripts in SYSVOL that were created in 2018 containing the word Administrator

            SharpSearch.exe path=""\\\\DC01\\SYSVOL"" ext_filterlist=.ps1,.bat searchterms=Administrator year=2018
";
            Console.WriteLine(usageString);
        }


        static Dictionary<string,string[]> ParseArgs(string[] args)
        {
            Dictionary<string, string[]> result = new Dictionary<string, string[]>();
            string[] commaTerms = new string[] { FilterKey, BlockKey, SearchKey};
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

        static void Main(string[] args)
        {
            string path = "";
            var parsedArgs = ParseArgs(args);

            if (!ValidateArguments(parsedArgs))
            {
                Usage();
                Environment.Exit(1);
            }

            path = parsedArgs["path"][0];

            if (!Directory.Exists(path))
            {
                Console.WriteLine("[X] Error: Path {0} does not exist.", path);
                Environment.Exit(1);
            }
            try
            {

                string[] filterList = null;
                string[] blockList = null;
                string[] searchterms = null;
                string pattern = null;
                string year = null;


                if (parsedArgs.TryGetValue(FilterKey, out string[] val))
                {
                    filterList = val;
                }
                if (parsedArgs.TryGetValue(BlockKey, out string[] val2))
                {
                    blockList = val2;
                }
                if (parsedArgs.TryGetValue(SearchKey, out string[] val3))
                {
                    searchterms = val3;
                }
                if (parsedArgs.TryGetValue(PatternKey, out string[] val4))
                {
                    pattern = val4[0];
                }
                if (parsedArgs.TryGetValue(YearKey, out string[] val5))
                {
                    year = val5[0];
                }
                Console.WriteLine($"[*] Searching path: {path}");
                if (filterList != null)
                {
                    Console.WriteLine($"[*] Filtering for extensions: {String.Join(",", filterList)}");
                }

                if (blockList != null)
                {
                    Console.WriteLine($"[*] Blocking files with extension: {String.Join(",", blockList)}");
                }

                if (searchterms != null)
                {
                    Console.WriteLine($"[*] Filtering for files with content containing: {String.Join(",", searchterms)}");
                }

                if (pattern != null)
                {
                    Console.WriteLine($"[*] Filtering for files whose title is like: {pattern}");
                }

                if (year != null)
                {
                    Console.WriteLine($"[*] Filtering for files whose last write date is from {year}");
                }

                FileSearcher searcher = new FileSearcher(filterList, blockList, searchterms, pattern, year);
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                string[] files = searcher.Search(path);
                if (files.Length > 0)
                {
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
