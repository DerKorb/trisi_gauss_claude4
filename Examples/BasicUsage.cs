using Optimization.Core.Algorithms;
using Optimization.Core.Models;

namespace Optimization.Core.Examples;

/// <summary>
/// Basic usage examples for the Nelder-Mead optimization library
/// </summary>
public static class BasicUsage
{
    public static void RunAllExamples()
    {
        Console.WriteLine("=== Nelder-Mead Optimization Library Examples ===\n");
        
        SimpleQuadraticExample();
        DoubleGaussianFittingExample();
        BoundedOptimizationExample();
        NoiseRobustnessExample();
        MultiDimensionalExample();
        
        Console.WriteLine("\n" + new string('=', 50));
        PerformanceComparison.RunPerformanceComparison();
        
        Console.WriteLine("All examples completed successfully!");
    }

    private static void SimpleQuadraticExample()
    {
        Console.WriteLine("1. Simple Quadratic Minimization");
        Console.WriteLine("   Minimize f(x,y) = (x-3)² + (y-4)²");
        
        // Define objective function
        static double Quadratic(Span<double> x) => 
            Math.Pow(x[0] - 3.0, 2) + Math.Pow(x[1] - 4.0, 2);
        
        // Set up optimization
        var initialGuess = new double[] { 0.0, 0.0 };
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-10,
            MaxIterations = 1000
        };
        
        // Run optimization
        var result = NelderMead<double>.Minimize(Quadratic, initialGuess, options);
        
        // Display results
        Console.WriteLine($"   Initial guess: ({initialGuess[0]}, {initialGuess[1]})");
        Console.WriteLine($"   Optimal point: ({result.OptimalParameters.Span[0]:F6}, {result.OptimalParameters.Span[1]:F6})");
        Console.WriteLine($"   Optimal value: {result.OptimalValue:E6}");
        Console.WriteLine($"   Converged: {result.Converged} ({result.Iterations} iterations, {result.FunctionEvaluations} evaluations)");
        Console.WriteLine();
    }

    private static void DoubleGaussianFittingExample()
    {
        Console.WriteLine("2. Double Gaussian Curve Fitting");
        Console.WriteLine("   Fitting sum of two Gaussians to synthetic data");
        
        // Generate synthetic data
        var trueParams = new double[] { 1.5, -1.0, 0.6, 1.2, 1.5, 0.4 };
        var xData = new double[100];
        var yData = new double[100];
        var random = new Random(42);
        
        for (int i = 0; i < 100; i++)
        {
            xData[i] = -3.0 + 6.0 * i / 99.0;
            double clean = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
            double noise = 0.05 * clean * random.NextGaussian();
            yData[i] = clean + noise;
        }
        
        // Generate initial guess
        var initialGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);
        
        // Set up bounds for physical constraints
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-8,
            MaxIterations = 2000,
            LowerBounds = new double[] { 0.0, -5.0, 0.01, 0.0, -5.0, 0.01 },
            UpperBounds = new double[] { 10.0, 5.0, 5.0, 10.0, 5.0, 5.0 }
        };
        
        // Fit the model
        var result = DoubleGaussian.Fit(xData, yData, initialGuess, options);
        
        // Display results
        Console.WriteLine("   True parameters:   [A1, μ1, σ1, A2, μ2, σ2]");
        Console.WriteLine($"   True:              [{string.Join(", ", trueParams.Select(x => x.ToString("F3")))}]");
        Console.WriteLine($"   Fitted:            [{string.Join(", ", result.OptimalParameters.Span.ToArray().Select(x => x.ToString("F3")))}]");
        Console.WriteLine($"   Sum squared error: {result.OptimalValue:E6}");
        Console.WriteLine($"   Converged: {result.Converged} ({result.Iterations} iterations)");
        Console.WriteLine();
    }

    private static void BoundedOptimizationExample()
    {
        Console.WriteLine("3. Bounded Optimization");
        Console.WriteLine("   Minimize f(x,y) = x² + y² subject to 0 ≤ x,y ≤ 1 and x+y ≥ 1.5");
        
        // Objective function with penalty for constraint x+y ≥ 1.5
        static double BoundedObjective(Span<double> x)
        {
            double objective = x[0] * x[0] + x[1] * x[1];
            
            // Add penalty for constraint x + y >= 1.5
            if (x[0] + x[1] < 1.5)
            {
                double violation = 1.5 - (x[0] + x[1]);
                objective += 1000.0 * violation * violation;
            }
            
            return objective;
        }
        
        var initialGuess = new double[] { 0.8, 0.8 };
        var options = new NelderMeadOptions<double>
        {
            LowerBounds = new double[] { 0.0, 0.0 },
            UpperBounds = new double[] { 1.0, 1.0 },
            FunctionTolerance = 1e-8,
            MaxIterations = 1000
        };
        
        var result = NelderMead<double>.Minimize(BoundedObjective, initialGuess, options);
        
        Console.WriteLine($"   Initial guess: ({initialGuess[0]}, {initialGuess[1]})");
        Console.WriteLine($"   Optimal point: ({result.OptimalParameters.Span[0]:F6}, {result.OptimalParameters.Span[1]:F6})");
        Console.WriteLine($"   Sum: {result.OptimalParameters.Span[0] + result.OptimalParameters.Span[1]:F6}");
        Console.WriteLine($"   Objective value: {result.OptimalValue:F6}");
        Console.WriteLine($"   Converged: {result.Converged}");
        Console.WriteLine();
    }

    private static void NoiseRobustnessExample()
    {
        Console.WriteLine("4. Noise Robustness Test");
        Console.WriteLine("   Testing fitting performance with different noise levels");
        
        var trueParams = new double[] { 2.0, 0.0, 1.0, 1.0, 2.0, 0.5 };
        var noiselevels = new double[] { 0.01, 0.05, 0.10, 0.15, 0.20 };
        
        foreach (double noiseLevel in noiselevels)
        {
            var random = new Random(42);
            var xData = new double[150];
            var yData = new double[150];
            
            for (int i = 0; i < 150; i++)
            {
                xData[i] = -3.0 + 6.0 * i / 149.0;
                double clean = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
                double noise = noiseLevel * clean * random.NextGaussian();
                yData[i] = clean + noise;
            }
            
            var initialGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);
            var result = DoubleGaussian.Fit(xData, yData, initialGuess);
            
            double maxError = 0;
            for (int i = 0; i < 6; i++)
            {
                double error = Math.Abs(result.OptimalParameters.Span[i] - trueParams[i]);
                maxError = Math.Max(maxError, error);
            }
            
            string status = result.Converged ? "✓" : "✗";
            Console.WriteLine($"   Noise {noiseLevel:P0}: Max param error = {maxError:F3}, SSR = {result.OptimalValue:E2} {status}");
        }
        Console.WriteLine();
    }

    private static void MultiDimensionalExample()
    {
        Console.WriteLine("5. Multi-dimensional Optimization");
        Console.WriteLine("   Minimizing 10-dimensional quadratic function");
        
        // f(x) = sum((xi - i)²) for i = 0..9
        static double MultiDimObjective(Span<double> x)
        {
            double sum = 0;
            for (int i = 0; i < x.Length; i++)
                sum += Math.Pow(x[i] - i, 2);
            return sum;
        }
        
        var initialGuess = new double[10]; // All zeros
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-6,
            MaxIterations = 10000,
            InitialSimplexSize = 1.0
        };
        
        var result = NelderMead<double>.Minimize(MultiDimObjective, initialGuess, options);
        
        Console.WriteLine($"   Dimensions: {initialGuess.Length}");
        Console.WriteLine($"   Optimal value: {result.OptimalValue:E6}");
        Console.WriteLine($"   Converged: {result.Converged} ({result.Iterations} iterations)");
        
        // Check solution quality
        double maxError = 0;
        for (int i = 0; i < 10; i++)
        {
            double error = Math.Abs(result.OptimalParameters.Span[i] - i);
            maxError = Math.Max(maxError, error);
        }
        Console.WriteLine($"   Maximum parameter error: {maxError:E6}");
        Console.WriteLine();
    }
}

// Extension for Gaussian random numbers
public static class RandomExtensions
{
    public static double NextGaussian(this Random random, double mean = 0.0, double stdDev = 1.0)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}