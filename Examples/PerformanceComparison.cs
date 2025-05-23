using System.Diagnostics;
using Optimization.Core.Algorithms;
using Optimization.Core.Models;

namespace Optimization.Core.Examples;

/// <summary>
/// Performance comparison examples showcasing optimization improvements
/// </summary>
public static class PerformanceComparison
{
    public static void RunPerformanceComparison()
    {
        Console.WriteLine("=== Performance Optimization Comparison ===\n");
        
        CompareAlgorithmPerformance();
        CompareEvaluationPerformance();
        CompareMemoryUsage();
        CompareVectorization();
        
        Console.WriteLine("Performance comparison completed!\n");
    }

    private static void CompareAlgorithmPerformance()
    {
        Console.WriteLine("1. Algorithm Performance Comparison");
        Console.WriteLine("   Standard vs Optimized Nelder-Mead");
        
        // Generate test data
        var trueParams = new double[] { 1.5, -0.8, 0.6, 1.2, 1.0, 0.4 };
        var random = new Random(42);
        var xData = new double[200];
        var yData = new double[200];
        
        for (int i = 0; i < 200; i++)
        {
            xData[i] = -3.0 + 6.0 * i / 199.0;
            double clean = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
            double noise = 0.05 * clean * random.NextGaussian();
            yData[i] = clean + noise;
        }

        var initialGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData).ToArray();
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-8,
            MaxIterations = 1000
        };

        // Standard algorithm
        var sw = Stopwatch.StartNew();
        var objectiveStd = ObjectiveFunctions.CreateSumSquaredResidualsFunction<double>(xData, yData);
        var resultStd = NelderMead<double>.Minimize(objectiveStd, initialGuess, options);
        sw.Stop();
        var timeStandard = sw.ElapsedMilliseconds;

        // Optimized algorithm
        sw.Restart();
        var objectiveOpt = DoubleGaussianOptimizedFixed.CreateOptimizedObjective<double>(xData, yData);
        var resultOpt = NelderMeadOptimized<double>.Minimize(objectiveOpt, initialGuess, options);
        sw.Stop();
        var timeOptimized = sw.ElapsedMilliseconds;

        Console.WriteLine($"   Standard:  {timeStandard,4} ms, {resultStd.Iterations,3} iterations, {resultStd.FunctionEvaluations,4} evaluations");
        Console.WriteLine($"   Optimized: {timeOptimized,4} ms, {resultOpt.Iterations,3} iterations, {resultOpt.FunctionEvaluations,4} evaluations");
        Console.WriteLine($"   Speedup:   {(double)timeStandard / timeOptimized:F2}x");
        Console.WriteLine();
    }

    private static void CompareEvaluationPerformance()
    {
        Console.WriteLine("2. Function Evaluation Performance");
        Console.WriteLine("   Standard vs Optimized Double Gaussian");
        
        var parameters = new double[] { 1.5, -0.8, 0.6, 1.2, 1.0, 0.4 };
        var xData = new double[1000];
        for (int i = 0; i < xData.Length; i++)
        {
            xData[i] = -5.0 + 10.0 * i / (xData.Length - 1);
        }

        const int iterations = 10000;

        // Standard evaluation
        var sw = Stopwatch.StartNew();
        double sumStd = 0;
        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = 0; i < xData.Length; i++)
            {
                sumStd += DoubleGaussian.Evaluate<double>(parameters, xData[i]);
            }
        }
        sw.Stop();
        var timeStandard = sw.ElapsedMilliseconds;

        // Optimized evaluation
        sw.Restart();
        double sumOpt = 0;
        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = 0; i < xData.Length; i++)
            {
                sumOpt += DoubleGaussianOptimizedFixed.Evaluate<double>(parameters, xData[i]);
            }
        }
        sw.Stop();
        var timeOptimized = sw.ElapsedMilliseconds;

        Console.WriteLine($"   Standard:  {timeStandard,4} ms ({iterations * xData.Length:N0} evaluations)");
        Console.WriteLine($"   Optimized: {timeOptimized,4} ms ({iterations * xData.Length:N0} evaluations)");
        Console.WriteLine($"   Speedup:   {(double)timeStandard / timeOptimized:F2}x");
        Console.WriteLine($"   Results match: {Math.Abs(sumStd - sumOpt) < 1e-10}");
        Console.WriteLine();
    }

    private static void CompareMemoryUsage()
    {
        Console.WriteLine("3. Memory Usage Comparison");
        Console.WriteLine("   Analyzing allocation patterns");
        
        var parameters = new double[] { 1.5, -0.8, 0.6, 1.2, 1.0, 0.4 };
        var xData = new double[500];
        var yData = new double[500];
        
        for (int i = 0; i < xData.Length; i++)
        {
            xData[i] = -3.0 + 6.0 * i / (xData.Length - 1);
            yData[i] = DoubleGaussian.Evaluate<double>(parameters, xData[i]);
        }

        // Force garbage collection before measurements
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Standard approach
        long memBefore = GC.GetTotalMemory(false);
        var sw = Stopwatch.StartNew();
        
        double ssrStd = 0;
        for (int i = 0; i < 1000; i++)
        {
            ssrStd = ObjectiveFunctions.SumSquaredResiduals<double>(parameters, xData, yData);
        }
        
        sw.Stop();
        long memAfter = GC.GetTotalMemory(false);
        var timeStandard = sw.ElapsedMilliseconds;
        var allocStandard = memAfter - memBefore;

        // Force GC again
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Optimized approach
        memBefore = GC.GetTotalMemory(false);
        sw.Restart();
        
        double ssrOpt = 0;
        for (int i = 0; i < 1000; i++)
        {
            ssrOpt = DoubleGaussianOptimizedFixed.SumSquaredResidualsOptimized<double>(parameters, xData, yData);
        }
        
        sw.Stop();
        memAfter = GC.GetTotalMemory(false);
        var timeOptimized = sw.ElapsedMilliseconds;
        var allocOptimized = memAfter - memBefore;

        Console.WriteLine($"   Standard:  {timeStandard,4} ms, {allocStandard,8:N0} bytes allocated");
        Console.WriteLine($"   Optimized: {timeOptimized,4} ms, {allocOptimized,8:N0} bytes allocated");
        Console.WriteLine($"   Time improvement:   {(double)timeStandard / timeOptimized:F2}x");
        Console.WriteLine($"   Memory improvement: {(double)allocStandard / Math.Max(allocOptimized, 1):F2}x");
        Console.WriteLine($"   Results match: {Math.Abs(ssrStd - ssrOpt) < 1e-12}");
        Console.WriteLine();
    }

    private static void CompareVectorization()
    {
        Console.WriteLine("4. Vectorization Performance");
        Console.WriteLine("   Large dataset range evaluation");
        
        var parameters = new double[] { 1.5, -0.8, 0.6, 1.2, 1.0, 0.4 };
        var largeXData = new double[50000];
        
        for (int i = 0; i < largeXData.Length; i++)
        {
            largeXData[i] = -5.0 + 10.0 * i / (largeXData.Length - 1);
        }

        const int iterations = 100;

        // Standard range evaluation
        var sw = Stopwatch.StartNew();
        var resultStd = new double[largeXData.Length];
        
        for (int iter = 0; iter < iterations; iter++)
        {
            DoubleGaussian.EvaluateRange<double>(parameters, largeXData, resultStd);
        }
        
        sw.Stop();
        var timeStandard = sw.ElapsedMilliseconds;

        // Optimized range evaluation
        sw.Restart();
        var resultOpt = new double[largeXData.Length];
        
        for (int iter = 0; iter < iterations; iter++)
        {
            DoubleGaussianOptimizedFixed.EvaluateRange<double>(parameters, largeXData, resultOpt);
        }
        
        sw.Stop();
        var timeOptimized = sw.ElapsedMilliseconds;

        // Check accuracy
        double maxDiff = 0;
        for (int i = 0; i < largeXData.Length; i++)
        {
            maxDiff = Math.Max(maxDiff, Math.Abs(resultStd[i] - resultOpt[i]));
        }

        Console.WriteLine($"   Dataset size: {largeXData.Length:N0} points");
        Console.WriteLine($"   Standard:  {timeStandard,4} ms ({iterations} iterations)");
        Console.WriteLine($"   Optimized: {timeOptimized,4} ms ({iterations} iterations)");
        Console.WriteLine($"   Speedup:   {(double)timeStandard / timeOptimized:F2}x");
        Console.WriteLine($"   Max difference: {maxDiff:E2}");
        Console.WriteLine($"   Vectorization enabled: {System.Runtime.Intrinsics.X86.Avx2.IsSupported}");
        Console.WriteLine();
    }
}

