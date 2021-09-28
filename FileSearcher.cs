using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading;

namespace SharpSearch
{
    public sealed class FileSearcher
    {
        public readonly string[] FilterExtensions;
        public readonly string[] BlockExtensions;
        public readonly string[] SearchTerms;
        public readonly string FilterPattern;
        public readonly string Year;

        private ConcurrentQueue<Task<string[]>> _filteredFiles = new ConcurrentQueue<Task<string[]>>();
        private ConcurrentQueue<Task> _tasks = new ConcurrentQueue<Task>();
        public FileSearcher(string[] filterExtensions = null, string[] blockExtensions = null, string[] searchTerms = null, string filterPattern=null, string year = null)
        {
            FilterExtensions = filterExtensions == null ? new string[0] : filterExtensions;
            BlockExtensions = blockExtensions == null ? new string[0] : blockExtensions;
            SearchTerms = searchTerms == null ? new string[0] : searchTerms;
            FilterPattern = filterPattern == null ? "*" : filterPattern;
            Year = year == null ? "" : year;
        }


        public string[] Search(string path)
        {
            Task t = new Task(() =>
            {
                ParseDirectory(path);
            });

            t.Start();
            _tasks.Enqueue(t);

            while(_tasks.TryDequeue(out Task runningT))
            {
                try
                {
                    runningT.Wait();
                } catch (Exception ex)
                {
                    Console.WriteLine($"[-] Error waiting for task: {ex.Message}");
                }
            }

            List<string> results = new List<string>();
            while(_filteredFiles.TryDequeue(out Task<string[]> filterTask))
            {
                if (filterTask == null)
                {
                    continue;
                }
                try
                {
                    filterTask.Wait();
                    string[] files = filterTask.Result;
                    if (files.Length > 0)
                    {
                        results.AddRange(files);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[-] Error waiting for filter file task: {ex.Message}");
                }
            }
            

            return results.ToArray();
        }

        private void ParseDirectory(string dir)
        {
            try
            {
                string[] files = Directory.GetFiles(dir, FilterPattern);
                Task<string[]> t = ParseFiles(files);
                t.Start();
                _filteredFiles.Enqueue(t);
                _tasks.Enqueue(t);
            } catch { }

            try
            {
                string[] dirs = Directory.GetDirectories(dir);
                Parallel.ForEach(dirs, sDir => 
                {
                    Task t = new Task(() => { ParseDirectory(sDir); });
                    _tasks.Enqueue(t);
                    t.Start();
                });
            } catch { }

        }

        private Task<string[]> ParseFiles(string[] files)
        {
            return new Task<string[]>(() =>
            {
                List<string> validFiles = new List<string>(files);
                Mutex mtx = new Mutex();
                if (FilterExtensions.Length > 0)
                {
                    Parallel.ForEach(validFiles.ToArray(), fName =>
                    {
                        if (!FileExtensionHandler.EndsWithExtension(fName, FilterExtensions))
                        {
                            mtx.WaitOne();
                            validFiles.Remove(fName);
                            mtx.ReleaseMutex();
                        }
                    });
                }

                if (BlockExtensions.Length > 0)
                {
                    Parallel.ForEach(validFiles.ToArray(), fName =>
                    {
                        if (FileExtensionHandler.EndsWithExtension(fName, BlockExtensions))
                        {
                            mtx.WaitOne();
                            validFiles.Remove(fName);
                            mtx.ReleaseMutex();
                        }
                    });
                }

                if (!string.IsNullOrEmpty(Year))
                {
                    Parallel.ForEach(validFiles.ToArray(), fName =>
                    {
                        FileInfo fInfo = new FileInfo(fName);
                        string lastWrite = File.GetLastWriteTime(fInfo.FullName).Date.ToString();
                        if (!lastWrite.Contains(Year))
                        {
                            mtx.WaitOne();
                            validFiles.Remove(fName);
                            mtx.ReleaseMutex();
                        }
                    });
                }

                if (SearchTerms.Length > 0)
                {
                    Parallel.ForEach(validFiles.ToArray(), fName =>
                    {
                        if (FileExtensionHandler.HasCleanExtension(fName) &&
                            !FileContainsStrings(fName))
                        {
                            mtx.WaitOne();
                            validFiles.Remove(fName);
                            mtx.ReleaseMutex();
                        } else if (!FileExtensionHandler.HasCleanExtension(fName))
                        {
                            Console.WriteLine($"[-] Removing file {fName} as it cannot be parsed for search terms.");
                            mtx.WaitOne();
                            validFiles.Remove(fName);
                            mtx.ReleaseMutex();
                        }
                    });
                }

                return validFiles.ToArray();

            });
        }

        private bool FileContainsStrings(string path)
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
                        foreach (string searchTerm in SearchTerms)
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
}
