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
    class FileWorkers
    {

        private static Semaphore _pool = new Semaphore(0, 1000);

        public static List<string> matchingFiles = new List<string>();

        private static List<Thread> runningThreads = new List<Thread>();

        private static FileExtensionHandler fileHandler = new FileExtensionHandler();

        static string[] GetFilesInDirectory(string path, string pattern = null)
        {
            try
            {
                if (pattern != null)
                {
                    return Directory.GetFiles(path, pattern);
                } else
                {
                    return Directory.GetFiles(path);
                }
            } catch
            {
                return new string[] { };
            }
        }

        static string[] GetDirectoriesInDirectory(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch
            {
                return new string[] { };
            }
        }

        static void ParseDirectory(string path,
                                   string pattern = null,
                                   string[] ext_whitelist = null,
                                   string[] ext_blacklist = null,
                                   string[] searchterms = null,
                                   string year = null)
        {
            string[] files = null;
            string[] directories = null;
            // Get all the directories in the passed path
            directories = GetDirectoriesInDirectory(path);
            if (directories.Length > 0)
            {
                // For each directory, do the same file parsing and add to the thread queue.
                //Console.WriteLine("There's {0} directoreis to comb through", directories.Length);
                foreach(string dir in directories)
                {
                    //Console.WriteLine("Searching {0}", dir);
                    Thread t = new Thread(() => ParseDirectory(dir, pattern, ext_whitelist, ext_blacklist, searchterms, year));
                    t.Start();
                    //Console.WriteLine("Started thread");
                    _pool.WaitOne(500);
                    //Console.WriteLine("Waited");
                    runningThreads.Add(t);
                    _pool.Release(1);
                    //Console.WriteLine("released");
                }
            }
            files = GetFilesInDirectory(path, pattern);

            List<string> validExtensionFiles = new List<string>();
            if (ext_whitelist != null)
            {
                foreach(string fName in files)
                {
                    //Console.WriteLine("Checking {0}", fName);
                    if (FileExtensionHandler.EndsWithExtension(fName, ext_whitelist))
                    {
                        //Console.WriteLine("{0} looks valid", fName);
                        validExtensionFiles.Add(fName);
                    }
                }
            } else if (ext_blacklist != null)
            {
                foreach (string fName in files)
                {
                    if (!FileExtensionHandler.EndsWithExtension(fName, ext_blacklist))
                    {
                        //Console.WriteLine("{0} looks valid");
                        validExtensionFiles.Add(fName);
                    }
                }
            } else
            {
                foreach(string f in files)
                {
                    validExtensionFiles.Add(f);
                }
            }

            if (year != null && year != "")
            {
                foreach(string fname in validExtensionFiles.ToArray())
                {
                    FileInfo fileInfo = new FileInfo(fname);
                    string lastWrite = File.GetLastWriteTime(fileInfo.FullName).Date.ToString();
                    if (!lastWrite.Contains(year))
                    {
                        validExtensionFiles.Remove(fname);
                    }
                }
            }

            if (searchterms != null)
            {
                foreach(string fName in validExtensionFiles)
                {
                    if (FileExtensionHandler.HasCleanExtension(fName) && FileContainsStrings(fName, searchterms))
                    {
                        //Console.WriteLine("Waiting for pool thread");
                        _pool.WaitOne(500);
                        //Console.WriteLine("added {0}", fName);
                        matchingFiles.Add(fName);
                        _pool.Release(1);
                    }
                }
            } else
            {
                _pool.WaitOne(500);
                foreach(string fName in validExtensionFiles)
                {
                    //Console.WriteLine("yolo {0}", fName);
                    matchingFiles.Add(fName);
                }
                _pool.Release(1);
            }
            //Console.WriteLine("Done searching {0}", path);
        }

        static string[] GetValueFromDict(Dictionary<string, string[]> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return null;
        }

        public static string[] GetAllFiles(Dictionary<string, string[]> args)
        {
            string whitelistKey = "ext_whitelist";
            string blacklistKey = "ext_blacklist";
            string searchtermsKey = "searchterms";
            string patternKey = "pattern";
            string yearKey = "year";
            string[] ext_whitelist = GetValueFromDict(args, whitelistKey);
            string[] ext_blacklist = GetValueFromDict(args, blacklistKey);
            string[] searchterms = GetValueFromDict(args, searchtermsKey);

            string pattern = null;
            string year = null;
            string path = args["path"][0];

            if (GetValueFromDict(args, patternKey) != null)
            {
                pattern = GetValueFromDict(args, patternKey)[0];
            }

            if (GetValueFromDict(args, yearKey) != null)
            {
                year = GetValueFromDict(args, yearKey)[0];
            }


            ThreadPool.SetMaxThreads(200, 200);

            ParseDirectory(path, pattern, ext_whitelist, ext_blacklist, searchterms, year);
            // Artificial sleep to ensure thread queue gets populated
            //Thread.Sleep(1000);
            while (runningThreads.Count > 0)
            {
                var t = runningThreads[0];
                t.Join();
                _pool.WaitOne(500);
                runningThreads.RemoveAt(0);
                _pool.Release(1);
            }
            return matchingFiles.ToArray();
        }

        static bool FileContainsStrings(string path, string[] searchTerms)
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
                        foreach(string searchTerm in searchTerms)
                        {
                            var regex = new Regex(searchTerm);
                            var isMatch = regex.Match(s.ToLower());
                            if (isMatch.Success)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {

            }
            return false;
        }
    }

    //static bool FileContainsString(string path, string searchTerm)
    //    {
    //        try
    //        {
    //            var data = File.ReadAllLines(path);

    //            foreach (var s in data)
    //            {
    //                // make sure its not null doesn't start with an empty line or something.
    //                if (s != null && !string.IsNullOrEmpty(s) && !s.StartsWith("  ") && s.Length > 0)
    //                {
    //                    string line = s.ToLower().Trim();

    //                    // use regex to find some key in your case the "ID".
    //                    // look into regex and word boundry find only lines with ID
    //                    // double check the below regex below going off memory. \B is for boundry
    //                    var regex = new Regex(searchTerm);
    //                    var isMatch = regex.Match(s.ToLower());
    //                    if (isMatch.Success)
    //                    {
    //                        return true;
    //                    }

    //                }
    //            }
    //        }
    //        catch (IOException ex)
    //        {

    //        }
    //        return false;
    //    }
    //}
}
