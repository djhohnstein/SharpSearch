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
            //string result = String.Concat(Enumerable.Repeat(" ", numSpaces - item.ToString().Length));

            string result = "";
            for(int i = 0; i < (numSpaces - item.ToString().Length); i++)
            {
                result += " ";
            }
            return result;
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
            string usageString = @"
Usage:
    Arguments:
        Required:
            path          - Path to search for files. Note: If using quoted paths, ensure you
                            escape backslashes properly.
        
        Optional:
            patttern      - Type of files to search for, e.g. ""*.txt"" (Optional)
            searchterm    - Term to search for within files. (Optional)

    Examples:
        
        Find all files that have the phrase ""password"" in them.
        
            SharpSearch.exe path:""C:\\Users\\User\\My Documents\\"" searchterm:password

        Search for all batch files on a remote share that contain the word ""Administrator""

            SharpSearch.exe path:""\\\\server01\\SYSVOL\\domain\\scripts\\"" pattern:*.bat searchTerm:Administrator 
";
            Console.WriteLine(usageString);
        }

        static string[] GetAllFiles(string path, string pattern="")
        {
            List<string> results = new List<string>();
            string[] files = null;
            string[] directories = null;

            // Fetch files for current path
            try
            {
                if (pattern != "" && pattern != null)
                {
                    files = Directory.GetFiles(path, pattern);
                }
                else
                {
                    files = Directory.GetFiles(path);
                }
                if (files.Length > 0)
                {
                    foreach(string f in files)
                    {
                        results.Add(f);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("[-] Could not list files in {0}: {1}", path, ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Unhandled Exception encountered while listing files:");
                Console.WriteLine(ex);
            }

            // Fetch files in other directoriees

            try
            {
                // Attempt to get directories
                directories = Directory.GetDirectories(path);
                if (directories != null && directories.Length > 0)
                {
                    foreach (string dir in directories)
                    {
                        string[] dirFiles = GetAllFiles(dir, pattern);
                        if (dirFiles != null && dirFiles.Length > 0)
                        {
                            foreach(string f in dirFiles)
                            {
                                results.Add(f);
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("[-] Could not list directories in {0}: {1}", path, ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Unhandled Exception encountered while listing directories:");
                Console.WriteLine(ex);
            }
            return results.ToArray();
        }

        static bool FileContainsString(string path, string searchTerm)
        {
            try
            {
                var data = File.ReadAllLines(path);

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
                            return true;
                        }

                    }
                }
            }
            catch (IOException ex)
            {

            }
            return false;
        }

        static void Main(string[] args)
        {

            string path = "";
            string pattern = "";
            string searchTerm = "";

            // argument parsing

            var arguments = new Dictionary<string, string>();
            try
            {
                foreach (var argument in args)
                {
                    var idx = argument.IndexOf(':');
                    if (idx > 0)
                        arguments[argument.Substring(0, idx)] = argument.Substring(idx + 1);
                    else
                        arguments[argument] = string.Empty;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("[-] Exception parsing arguments:");
                Console.WriteLine(ex);
            }

            if (!arguments.ContainsKey("path"))
            {
                Console.WriteLine("[-] Error: No path was given.");
                Usage();
                Environment.Exit(1);
            }

            path = arguments["path"];
            if (!Directory.Exists(path))
            {
                Console.WriteLine("[X] Error: Directory {0} does not exist.", path);
                Environment.Exit(1);
            }

            if (arguments.ContainsKey("pattern"))
            {
                pattern = arguments["pattern"];
            }
            if (arguments.ContainsKey("searchterm"))
            {
                searchTerm = arguments["searchterm"];
            }

            string[] files = GetAllFiles(path, pattern);
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
                FileExtensionHandler fileExtHandler = new FileExtensionHandler();
                foreach (string f in files)
                {
                    FileInfo fileInfo = new FileInfo(f);
                    string fileStatusLine = GetFileStatusString(fileInfo);
                    string size = HumanBytes(fileInfo.Length);

                    string lastWrite = File.GetLastWriteTime(fileInfo.FullName).Date.ToString();

                    if (searchTerm == "")
                    {
                        try
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
                        catch (Exception ex)
                        {
                            Console.WriteLine("[-] ERROR printing results.");
                            Console.WriteLine(ex);
                        }
                    }
                    else
                    {
                        bool hasSearchTerm = false;
                        if (pattern == "")
                        {
                            // Ensure this is not a bad file format.
                            if (fileExtHandler.HasCleanExtension(f))
                            {
                                hasSearchTerm = FileContainsString(f, searchTerm);
                            }
                            else
                            {
                                //Console.WriteLine("[-] Skipping reading {0}. To force this, specify the pattern of files to read with --pattern.", f);
                            }
                        }
                        else
                        {
                            hasSearchTerm = FileContainsString(f, searchTerm);
                        }
                        if (hasSearchTerm)
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
