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
                    
                case "optimized":
                case "opt":
                    Console.WriteLine("Running optimized benchmarks...");
                    Benchmarks.OptimizedBenchmarkRunner.RunOptimizedBenchmarks();
                    break;
                    
                case "compare":
                case "reference":
                    ReferenceComparison.RunAllComparisons();
                    break;
                    
                case "validate":
                case "nlopt":
                    Console.WriteLine("Running NLopt equivalence validation...");
                    Validation.NLoptEquivalence.RunEquivalenceValidation();
                    Console.WriteLine("\nRunning Double Gaussian equivalence validation...");
                    Validation.NLoptEquivalence.ValidateDoubleGaussianEquivalence();
                    break;
                    
                case "performance":
                case "perf":
                    Console.WriteLine("Running NLopt performance comparison...");
                    NLoptPerformanceComparison.RunPerformanceComparison();
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
            Console.WriteLine("  dotnet run optimized - Run optimized performance benchmarks");
            Console.WriteLine("  dotnet run compare   - Run reference comparisons");
            Console.WriteLine("  dotnet run validate  - Run NLopt equivalence validation");
            Console.WriteLine("  dotnet run perf      - Run NLopt performance comparison with charts");
        }
    }
}