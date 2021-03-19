using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;

namespace DotNetGcStressTest
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Application(args);
            }

            catch (Exception exception)
            {
                var color = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {exception}");
                Console.ForegroundColor = color;
            }
        }

        private static void Application(string[] args)
        {
            var timer = Stopwatch.StartNew();

            Console.WriteLine($"CPU Target: {(Environment.Is64BitProcess ? "x64" : "x86")}");
            Console.WriteLine($"GC Mode: {(GCSettings.IsServerGC ? "Server" : "Workstation")}");
            Console.WriteLine();

            PrintMemoryStatistics();

            Console.WriteLine("Generating garbage:");

            var kept = GenerateGarbage();
            
            Console.WriteLine($"- Kept {kept.Length:N0} objects alive");
            Console.WriteLine();

            PrintMemoryStatistics();

            Console.WriteLine($"Completed in {timer.Elapsed.TotalSeconds:N1} seconds");
        }

        private static int[][] GenerateGarbage()
        {
            var bag = new ConcurrentBag<int[]>();

            Parallel.For(0, 100, t =>
            {
                var rand = new Random(t);

                for (var i = 0; i < 100; ++i)
                {
                    var array = Enumerable.Range(0, 100_000).Select(v => rand.Next().ToString()).ToArray();
                    var str = string.Join(";", array);
                    var parsed = str.Split(';').Select(int.Parse).ToArray();

                    if (rand.NextDouble() < 0.01)
                    {
                        // Keep 1% if the parsed arrays into memory, such that we create some fragmentation and keep some objects alive.
                        bag.Add(parsed);
                    }
                }
            });

            return bag.ToArray();
        }

        private static void PrintMemoryStatistics()
        {
            const int megaByte = 1024 * 1024;

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();

            using var process = Process.GetCurrentProcess();
            var gcInfo = GC.GetGCMemoryInfo();

            Console.WriteLine("Memory statistics:");

            Console.WriteLine($"- GC GetTotalMemory: {GC.GetTotalMemory(forceFullCollection: true) / megaByte:N0} MiB");

            Console.WriteLine($"- GC Info HeapSizeBytes: {gcInfo.HeapSizeBytes / megaByte:N0} MiB");
            Console.WriteLine($"- GC Info TotalCommittedBytes: {gcInfo.TotalCommittedBytes / megaByte:N0} MiB");
            Console.WriteLine($"- GC Info TotalAvailableMemoryBytes: {gcInfo.TotalAvailableMemoryBytes / megaByte:N0} MiB");
            Console.WriteLine($"- GC Info FragmentedBytes: {gcInfo.FragmentedBytes / megaByte:N0} MiB");
            Console.WriteLine($"- GC Info MemoryLoadBytes: {gcInfo.MemoryLoadBytes / megaByte:N0} MiB");
            Console.WriteLine($"- GC Info HighMemoryLoadThresholdBytes: {gcInfo.HighMemoryLoadThresholdBytes / megaByte:N0} MiB");

            Console.WriteLine($"- Process PagedMemorySize: {process.PagedMemorySize64 / megaByte:N0} MiB");
            Console.WriteLine($"- Process PagedSystemMemorySize: {process.PagedSystemMemorySize64 / megaByte:N0} MiB");
            Console.WriteLine($"- Process PrivateMemorySize: {process.PrivateMemorySize64 / megaByte:N0} MiB");
            Console.WriteLine($"- Process VirtualMemorySize: {process.VirtualMemorySize64 / megaByte:N0} MiB");
            Console.WriteLine($"- Process WorkingSet: {process.WorkingSet64 / megaByte:N0} MiB");
            Console.WriteLine($"- Process PeakPagedMemorySize: {process.PeakPagedMemorySize64 / megaByte:N0} MiB");
            Console.WriteLine($"- Process PeakWorkingSet: {process.PeakWorkingSet64 / megaByte:N0} MiB");

            Console.WriteLine($"- Task Manager Working Set: {new PerformanceCounter("Process", "Working Set - Private", process.ProcessName).RawValue / megaByte:N0} MiB");
            Console.WriteLine();
        }
    }
}
