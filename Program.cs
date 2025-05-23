using Optimization.Core.Examples;
using Optimization.Core.Benchmarks;

namespace Optimization.Core;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("High-Performance Nelder-Mead Optimization Library");
        Console.WriteLine("==================================================\n");

        if (args.Length > 0)
        {
            switch (args[0].ToLower())
            {
                case "benchmark":
                case "bench":
                    Console.WriteLine("Running benchmarks...");
                    Benchmarks.BenchmarkProgram.RunBenchmarks();
                    break;
                    
                case "compare":
                case "reference":
                    ReferenceComparison.RunAllComparisons();
                    break;
                    
                case "examples":
                case "demo":
                default:
                    BasicUsage.RunAllExamples();
                    break;
            }
        }
        else
        {
            // Default: run examples
            BasicUsage.RunAllExamples();
            
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("To run other modes:");
            Console.WriteLine("  dotnet run examples  - Run usage examples (default)");
            Console.WriteLine("  dotnet run benchmark - Run performance benchmarks");
            Console.WriteLine("  dotnet run compare   - Run reference comparisons");
        }
    }
}