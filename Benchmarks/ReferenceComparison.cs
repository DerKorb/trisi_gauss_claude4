using Optimization.Core.Algorithms;
using Optimization.Core.Models;

namespace Optimization.Core.Benchmarks;

/// <summary>
/// Comparison tests against reference implementations and validation
/// This would normally compare against MathNet.Numerics or NLopt
/// For now, we include analytical test cases and cross-validation
/// </summary>
public static class ReferenceComparison
{
    public static void RunAllComparisons()
    {
        Console.WriteLine("Running Reference Comparisons...\n");
        
        TestAnalyticalSolutions();
        TestConsistencyAcrossTypes();
        TestConvergenceRates();
        TestNumericalAccuracy();
        
        Console.WriteLine("All reference comparisons completed.");
    }

    private static void TestAnalyticalSolutions()
    {
        Console.WriteLine("Testing against analytical solutions:");
        
        // Test 1: Simple quadratic with known minimum
        TestQuadraticMinimization();
        
        // Test 2: Sphere function in multiple dimensions
        TestSphereFunction();
        
        // Test 3: Known Gaussian fitting case
        TestKnownGaussianFit();
        
        Console.WriteLine();
    }

    private static void TestQuadraticMinimization()
    {
        Console.Write("  Quadratic minimization... ");
        
        // f(x,y) = (x-3)² + (y-4)², minimum at (3,4)
        static double Quadratic(Span<double> x) => 
            Math.Pow(x[0] - 3.0, 2) + Math.Pow(x[1] - 4.0, 2);
        
        var initialGuess = new double[] { 0.0, 0.0 };
        var options = new NelderMeadOptions<double> { FunctionTolerance = 1e-12 };
        
        var result = NelderMead<double>.Minimize(Quadratic, initialGuess, options);
        
        double errorX = Math.Abs(result.OptimalParameters.Span[0] - 3.0);
        double errorY = Math.Abs(result.OptimalParameters.Span[1] - 4.0);
        
        if (errorX < 1e-8 && errorY < 1e-8 && result.OptimalValue < 1e-16)
        {
            Console.WriteLine($"PASS (error: {Math.Max(errorX, errorY):E2})");
        }
        else
        {
            Console.WriteLine($"FAIL (errors: x={errorX:E2}, y={errorY:E2}, f={result.OptimalValue:E2})");
        }
    }

    private static void TestSphereFunction()
    {
        Console.Write("  Sphere function (5D)... ");
        
        // f(x) = sum(xi²), minimum at origin
        static double Sphere(Span<double> x)
        {
            double sum = 0;
            for (int i = 0; i < x.Length; i++)
                sum += x[i] * x[i];
            return sum;
        }
        
        var initialGuess = new double[] { 1.0, -2.0, 0.5, -1.5, 3.0 };
        var options = new NelderMeadOptions<double> 
        { 
            FunctionTolerance = 1e-10,
            MaxIterations = 2000
        };
        
        var result = NelderMead<double>.Minimize(Sphere, initialGuess, options);
        
        double maxError = result.OptimalParameters.Span.ToArray().Max(Math.Abs);
        
        if (maxError < 1e-6 && result.OptimalValue < 1e-10)
        {
            Console.WriteLine($"PASS (max parameter error: {maxError:E2})");
        }
        else
        {
            Console.WriteLine($"FAIL (max error: {maxError:E2}, f={result.OptimalValue:E2})");
        }
    }

    private static void TestKnownGaussianFit()
    {
        Console.Write("  Known Gaussian parameters... ");
        
        var trueParams = new double[] { 2.0, 0.0, 1.0, 1.0, 2.0, 0.5 };
        var xData = new double[100];
        var yData = new double[100];
        
        // Generate perfect data
        for (int i = 0; i < 100; i++)
        {
            xData[i] = -3.0 + 6.0 * i / 99.0;
            yData[i] = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
        }
        
        var initialGuess = new double[] { 1.0, 0.5, 0.8, 0.8, 1.5, 0.6 };
        var result = DoubleGaussian.Fit<double>(xData, yData, initialGuess);
        
        double maxParamError = 0;
        for (int i = 0; i < 6; i++)
        {
            double error = Math.Abs(result.OptimalParameters.Span[i] - trueParams[i]);
            maxParamError = Math.Max(maxParamError, error);
        }
        
        if (maxParamError < 1e-6 && result.OptimalValue < 1e-20)
        {
            Console.WriteLine($"PASS (max parameter error: {maxParamError:E2})");
        }
        else
        {
            Console.WriteLine($"FAIL (max error: {maxParamError:E2}, SSR={result.OptimalValue:E2})");
        }
    }

    private static void TestConsistencyAcrossTypes()
    {
        Console.WriteLine("Testing consistency across float/double:");
        
        static double ObjectiveDouble(Span<double> x) => 
            Math.Pow(x[0] - 1.5, 2) + Math.Pow(x[1] - 2.5, 2);
        
        static float ObjectiveFloat(Span<float> x) => 
            (float)(Math.Pow(x[0] - 1.5f, 2) + Math.Pow(x[1] - 2.5f, 2));
        
        var guessDouble = new double[] { 0.0, 0.0 };
        var guessFloat = new float[] { 0.0f, 0.0f };
        
        var optionsDouble = new NelderMeadOptions<double> { FunctionTolerance = 1e-8 };
        var optionsFloat = new NelderMeadOptions<float> { FunctionTolerance = 1e-6f };
        
        var resultDouble = NelderMead<double>.Minimize(ObjectiveDouble, guessDouble, optionsDouble);
        var resultFloat = NelderMead<float>.Minimize(ObjectiveFloat, guessFloat, optionsFloat);
        
        double errorX = Math.Abs(resultDouble.OptimalParameters.Span[0] - resultFloat.OptimalParameters.Span[0]);
        double errorY = Math.Abs(resultDouble.OptimalParameters.Span[1] - resultFloat.OptimalParameters.Span[1]);
        
        Console.Write("  Double vs Float consistency... ");
        if (errorX < 1e-4 && errorY < 1e-4)
        {
            Console.WriteLine($"PASS (max difference: {Math.Max(errorX, errorY):E2})");
        }
        else
        {
            Console.WriteLine($"FAIL (differences: x={errorX:E2}, y={errorY:E2})");
        }
        
        Console.WriteLine();
    }

    private static void TestConvergenceRates()
    {
        Console.WriteLine("Testing convergence characteristics:");
        
        var results = new List<(string name, int iterations, int evaluations, bool converged)>();
        
        // Test different problem types
        results.Add(TestProblemConvergence("Quadratic", 
            x => Math.Pow(x[0] - 1, 2) + Math.Pow(x[1] - 1, 2),
            new double[] { 0, 0 }));
        
        results.Add(TestProblemConvergence("Rosenbrock", 
            x => Math.Pow(1 - x[0], 2) + 100 * Math.Pow(x[1] - x[0] * x[0], 2),
            new double[] { -1, 1 }));
        
        results.Add(TestProblemConvergence("Himmelblau", 
            x => Math.Pow(x[0] * x[0] + x[1] - 11, 2) + Math.Pow(x[0] + x[1] * x[1] - 7, 2),
            new double[] { 0, 0 }));
        
        foreach (var (name, iterations, evaluations, converged) in results)
        {
            string status = converged ? "CONVERGED" : "MAX_ITER";
            Console.WriteLine($"  {name,-12}: {iterations,3} iter, {evaluations,4} evals, {status}");
        }
        
        Console.WriteLine();
    }

    private static (string, int, int, bool) TestProblemConvergence(
        string name, 
        Func<Span<double>, double> objective, 
        double[] initialGuess)
    {
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-8,
            MaxIterations = 1000
        };
        
        var result = NelderMead<double>.Minimize(objective, initialGuess, options);
        return (name, result.Iterations, result.FunctionEvaluations, result.Converged);
    }

    private static void TestNumericalAccuracy()
    {
        Console.WriteLine("Testing numerical accuracy:");
        
        // Test with functions that have known analytical solutions
        TestBealeFunction();
        TestBoothFunction();
        TestMatyasFunction();
        
        Console.WriteLine();
    }

    private static void TestBealeFunction()
    {
        Console.Write("  Beale function... ");
        
        // f(x,y) = (1.5 - x + xy)² + (2.25 - x + xy²)² + (2.625 - x + xy³)²
        // Global minimum at (3, 0.5) with f = 0
        static double Beale(Span<double> x)
        {
            double term1 = Math.Pow(1.5 - x[0] + x[0] * x[1], 2);
            double term2 = Math.Pow(2.25 - x[0] + x[0] * x[1] * x[1], 2);
            double term3 = Math.Pow(2.625 - x[0] + x[0] * x[1] * x[1] * x[1], 2);
            return term1 + term2 + term3;
        }
        
        var initialGuess = new double[] { 1.0, 1.0 };
        var options = new NelderMeadOptions<double> 
        { 
            FunctionTolerance = 1e-10,
            MaxIterations = 2000
        };
        
        var result = NelderMead<double>.Minimize(Beale, initialGuess, options);
        
        double errorX = Math.Abs(result.OptimalParameters.Span[0] - 3.0);
        double errorY = Math.Abs(result.OptimalParameters.Span[1] - 0.5);
        
        if (errorX < 1e-4 && errorY < 1e-4 && result.OptimalValue < 1e-8)
        {
            Console.WriteLine($"PASS (f={result.OptimalValue:E2})");
        }
        else
        {
            Console.WriteLine($"FAIL (errors: x={errorX:E2}, y={errorY:E2}, f={result.OptimalValue:E2})");
        }
    }

    private static void TestBoothFunction()
    {
        Console.Write("  Booth function... ");
        
        // f(x,y) = (x + 2y - 7)² + (2x + y - 5)²
        // Global minimum at (1, 3) with f = 0
        static double Booth(Span<double> x) => 
            Math.Pow(x[0] + 2 * x[1] - 7, 2) + Math.Pow(2 * x[0] + x[1] - 5, 2);
        
        var initialGuess = new double[] { 0.0, 0.0 };
        var result = NelderMead<double>.Minimize(Booth, initialGuess);
        
        double errorX = Math.Abs(result.OptimalParameters.Span[0] - 1.0);
        double errorY = Math.Abs(result.OptimalParameters.Span[1] - 3.0);
        
        if (errorX < 1e-6 && errorY < 1e-6 && result.OptimalValue < 1e-12)
        {
            Console.WriteLine($"PASS (f={result.OptimalValue:E2})");
        }
        else
        {
            Console.WriteLine($"FAIL (errors: x={errorX:E2}, y={errorY:E2}, f={result.OptimalValue:E2})");
        }
    }

    private static void TestMatyasFunction()
    {
        Console.Write("  Matyas function... ");
        
        // f(x,y) = 0.26(x² + y²) - 0.48xy
        // Global minimum at (0, 0) with f = 0
        static double Matyas(Span<double> x) => 
            0.26 * (x[0] * x[0] + x[1] * x[1]) - 0.48 * x[0] * x[1];
        
        var initialGuess = new double[] { 1.0, 1.0 };
        var result = NelderMead<double>.Minimize(Matyas, initialGuess);
        
        double errorX = Math.Abs(result.OptimalParameters.Span[0]);
        double errorY = Math.Abs(result.OptimalParameters.Span[1]);
        
        if (errorX < 1e-8 && errorY < 1e-8 && result.OptimalValue < 1e-16)
        {
            Console.WriteLine($"PASS (f={result.OptimalValue:E2})");
        }
        else
        {
            Console.WriteLine($"FAIL (errors: x={errorX:E2}, y={errorY:E2}, f={result.OptimalValue:E2})");
        }
    }
}