using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace SharpSearch
{
    class Program
    {

        public static string HumanBytes(double len)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = String.Format("{0:0.##} {1}", len, sizes[order]);
            return result;
        }

        public static void PrintResults(ref int allFilesCount,
                                        ref double allFileSizes,
                                        ref int allDirectoryCount,
                                        ref string lastDir,
                                        FileInfo fileInfo,
                                        ref double dirFileSizes,
                                        ref int dirFileCount,
                                        string fileStatusLine)
        {
            allFilesCount += 1;
            allFileSizes += fileInfo.Length;
            if (lastDir == fileInfo.Directory.ToString())
            {
                dirFileSizes += fileInfo.Length;
                dirFileCount += 1;
                Console.WriteLine(fileStatusLine);
            }
            else
            {
                if (dirFileCount > 0)
                {
                    Console.WriteLine();
                    string statusString = GetDirectoryStatusString(dirFileCount, dirFileSizes);
                    Console.WriteLine(statusString);
                    dirFileSizes = 0;
                    dirFileCount = 0;
                }
                allDirectoryCount += 1;
                dirFileCount += 1;
                dirFileSizes += fileInfo.Length;
                lastDir = fileInfo.Directory.ToString();
                Console.WriteLine();
                Console.WriteLine("Directory of {0}", fileInfo.Directory);
                Console.WriteLine(fileStatusLine);
            }
        }

        public static string GetSpaces(object item)
        {
            int numSpaces = 16;
            return String.Concat(Enumerable.Repeat(" ", numSpaces - item.ToString().Length));
        }

        public static string GetFileStatusString(FileInfo fileInfo)
        {
            string size = HumanBytes(fileInfo.Length);
            string lastWrite = File.GetLastWriteTime(fileInfo.FullName).Date.ToString();
            string result = String.Format("\t{0}" + GetSpaces(size) + "{1} {2}", lastWrite, size, fileInfo.Name);
            return result;
        }

        public static string GetDirectoryStatusString(int totalCount, double totalSizes)
        {
            string bytes = HumanBytes(totalSizes);
            string result = GetSpaces(totalCount) + totalCount.ToString() + " Files(s)";
            result += GetSpaces(bytes) + bytes;
            return result;
        }

        static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tSharpSearch.exe Path\\To\\Search SearchPattern StringToSearchFor");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("\tSearch for all .ps1 files in C:\\Users\\");
            Console.WriteLine("\t\tSharpSearch.exe C:\\Users\\ *.ps1");
            Console.WriteLine();
            Console.WriteLine("\tSearch for all txt files containing the word \"password\"");
            Console.WriteLine("\t\tSharpSearch.exe C:\\Users\\ *.txt password");
        }

        static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("[X] Error: Not enough arguments given.");
                Usage();
                Environment.Exit(1);
            }

            string path = args[0];
            string pattern = args[1];

            if (!Directory.Exists(path))
            {
                Console.WriteLine("[X] Error: Directory {0} does not exist.", path);
            }
            else
            {
                string[] files = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    Dictionary<string, string[]> sorted = new Dictionary<string, string[]>();
                    string lastDir = "";
                    // total size of all files listed
                    double allFileSizes = 0;
                    int allDirectoryCount = 0;
                    int allFilesCount = 0;
                    // directory specific file count
                    double dirFileSizes = 0;
                    int dirFileCount = 0;
                    string searchTerm = "";
                    //List<ThreadStart> threads = null;
                    if (args.Length == 3)
                    {
                        searchTerm = args[2];
                        //threads = new List<ThreadStart>();
                    }
                    int listSpaces = 16;

                    foreach (string f in files)
                    {
                        FileInfo fileInfo = new FileInfo(f);
                        string fileStatusLine = GetFileStatusString(fileInfo);
                        string size = HumanBytes(fileInfo.Length);

                        string lastWrite = File.GetLastWriteTime(fileInfo.FullName).Date.ToString();
                        if (searchTerm == "")
                        {
                            PrintResults(ref allFilesCount,
                                         ref allFileSizes,
                                         ref allDirectoryCount,
                                         ref lastDir,
                                         fileInfo,
                                         ref dirFileSizes,
                                         ref dirFileCount,
                                         fileStatusLine);
                        }
                        else
                        {

                            try
                            {
                                IEnumerable<string> data = File.ReadLines(f);

                                foreach (var s in data)
                                {
                                    // make sure its not null doesn't start with an empty line or something.
                                    if (s != null && !string.IsNullOrEmpty(s) && !s.StartsWith("  ") && s.Length > 0)
                                    {
                                        string line = s.ToLower().Trim();

                                        // use regex to find some key in your case the "ID".
                                        // look into regex and word boundry find only lines with ID
                                        // double check the below regex below going off memory. \B is for boundry
                                        var regex = new Regex(searchTerm);
                                        var isMatch = regex.Match(s.ToLower());
                                        if (isMatch.Success)
                                        {
                                            PrintResults(ref allFilesCount,
                                                         ref allFileSizes,
                                                         ref allDirectoryCount,
                                                         ref lastDir,
                                                         fileInfo,
                                                         ref dirFileSizes,
                                                         ref dirFileCount,
                                                         fileStatusLine);
                                            break;
                                        }

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("[X] Error: {0}", ex.ToString());
                            }
                        }
                    }
                    if (dirFileCount > 0)
                    {
                        Console.WriteLine();
                        string statusString = GetDirectoryStatusString(dirFileCount, dirFileSizes);
                        Console.WriteLine(statusString);
                        dirFileSizes = 0;
                        dirFileCount = 0;
                    }
                    Console.WriteLine();
                    Console.WriteLine("\tTotal Files Listed:");
                    string line_1 = GetSpaces(allFilesCount) + allFilesCount.ToString() +
                                  " File(s)" + GetSpaces(allFileSizes) + HumanBytes(allFileSizes);
                    string line_2 = GetSpaces(allDirectoryCount) + allDirectoryCount.ToString() + " Dir(s)";
                    Console.WriteLine(line_1);
                    Console.WriteLine(line_2);
                }
                else
                {
                    Console.WriteLine("[-] No files found in {0} with pattern {1}", path, pattern);
                }
            }
        }
    }
}
