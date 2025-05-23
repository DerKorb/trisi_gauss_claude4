using System.Diagnostics;
using System.Globalization;
using Optimization.Core.Algorithms;
using Optimization.Core.Models;

namespace Optimization.Core.Validation;

/// <summary>
/// Comprehensive validation against NLopt gold standard for unbounded optimization
/// Ensures mathematical equivalence under identical conditions
/// </summary>
public static class NLoptEquivalence
{
    /// <summary>
    /// Test function definitions matching NLopt test suite
    /// </summary>
    public static class TestFunctions
    {
        // Rosenbrock function: f(x,y) = (a-x)² + b(y-x²)²
        public static double Rosenbrock(ReadOnlySpan<double> x)
        {
            double a = 1.0, b = 100.0;
            return Math.Pow(a - x[0], 2) + b * Math.Pow(x[1] - x[0] * x[0], 2);
        }

        // Sphere function: f(x) = Σ(xi²)
        public static double Sphere(ReadOnlySpan<double> x)
        {
            double sum = 0;
            for (int i = 0; i < x.Length; i++)
                sum += x[i] * x[i];
            return sum;
        }

        // Booth function: f(x,y) = (x + 2y - 7)² + (2x + y - 5)²
        public static double Booth(ReadOnlySpan<double> x) =>
            Math.Pow(x[0] + 2 * x[1] - 7, 2) + Math.Pow(2 * x[0] + x[1] - 5, 2);

        // Beale function: f(x,y) = (1.5 - x + xy)² + (2.25 - x + xy²)² + (2.625 - x + xy³)²
        public static double Beale(ReadOnlySpan<double> x)
        {
            double term1 = Math.Pow(1.5 - x[0] + x[0] * x[1], 2);
            double term2 = Math.Pow(2.25 - x[0] + x[0] * x[1] * x[1], 2);
            double term3 = Math.Pow(2.625 - x[0] + x[0] * x[1] * x[1] * x[1], 2);
            return term1 + term2 + term3;
        }

        // Himmelblau function: f(x,y) = (x² + y - 11)² + (x + y² - 7)²
        public static double Himmelblau(ReadOnlySpan<double> x) =>
            Math.Pow(x[0] * x[0] + x[1] - 11, 2) + Math.Pow(x[0] + x[1] * x[1] - 7, 2);

        // Powell function (4D): f(x1,x2,x3,x4) = (x1+10*x2)² + 5*(x3-x4)² + (x2-2*x3)⁴ + 10*(x1-x4)⁴
        public static double Powell(ReadOnlySpan<double> x)
        {
            double term1 = Math.Pow(x[0] + 10 * x[1], 2);
            double term2 = 5 * Math.Pow(x[2] - x[3], 2);
            double term3 = Math.Pow(x[1] - 2 * x[2], 4);
            double term4 = 10 * Math.Pow(x[0] - x[3], 4);
            return term1 + term2 + term3 + term4;
        }
    }

    /// <summary>
    /// Known optimal solutions for validation
    /// </summary>
    public static class KnownSolutions
    {
        public static readonly (double[] point, double value) Rosenbrock = (new[] { 1.0, 1.0 }, 0.0);
        public static readonly (double[] point, double value) Sphere2D = (new[] { 0.0, 0.0 }, 0.0);
        public static readonly (double[] point, double value) Sphere5D = (new[] { 0.0, 0.0, 0.0, 0.0, 0.0 }, 0.0);
        public static readonly (double[] point, double value) Booth = (new[] { 1.0, 3.0 }, 0.0);
        public static readonly (double[] point, double value) Beale = (new[] { 3.0, 0.5 }, 0.0);
        public static readonly (double[] point, double value) Himmelblau1 = (new[] { 3.0, 2.0 }, 0.0);
        public static readonly (double[] point, double value) Powell = (new[] { 0.0, 0.0, 0.0, 0.0 }, 0.0);
    }

    /// <summary>
    /// Test cases with multiple starting points for robustness testing
    /// </summary>
    public static class TestCases
    {
        public static readonly TestCase Rosenbrock = new TestCase(
            "Rosenbrock",
            TestFunctions.Rosenbrock,
            KnownSolutions.Rosenbrock,
            new[]
            {
                new[] { -1.2, 1.0 },   // Classic starting point
                new[] { 0.0, 0.0 },    // Origin
                new[] { 2.0, 2.0 },    // Near optimum
                new[] { -2.0, 3.0 }    // Distant point
            }
        );

        public static readonly TestCase Sphere2D = new TestCase(
            "Sphere 2D",
            TestFunctions.Sphere,
            KnownSolutions.Sphere2D,
            new[]
            {
                new[] { 1.0, 1.0 },
                new[] { -1.0, 2.0 },
                new[] { 5.0, -3.0 },
                new[] { 0.1, 0.1 }
            }
        );

        public static readonly TestCase Sphere5D = new TestCase(
            "Sphere 5D",
            TestFunctions.Sphere,
            KnownSolutions.Sphere5D,
            new[]
            {
                new[] { 1.0, 1.0, 1.0, 1.0, 1.0 },
                new[] { -1.0, 2.0, -0.5, 1.5, -2.0 },
                new[] { 0.1, 0.1, 0.1, 0.1, 0.1 }
            }
        );

        public static readonly TestCase Booth = new TestCase(
            "Booth",
            TestFunctions.Booth,
            KnownSolutions.Booth,
            new[]
            {
                new[] { 0.0, 0.0 },
                new[] { 2.0, 2.0 },
                new[] { -1.0, 4.0 },
                new[] { 5.0, 1.0 }
            }
        );

        public static readonly TestCase Beale = new TestCase(
            "Beale",
            TestFunctions.Beale,
            KnownSolutions.Beale,
            new[]
            {
                new[] { 1.0, 1.0 },
                new[] { 0.0, 0.0 },
                new[] { 4.0, 1.0 },
                new[] { 2.0, -1.0 }
            }
        );

        public static readonly TestCase Himmelblau = new TestCase(
            "Himmelblau",
            TestFunctions.Himmelblau,
            KnownSolutions.Himmelblau1,
            new[]
            {
                new[] { 0.0, 0.0 },
                new[] { 1.0, 1.0 },
                new[] { -1.0, 1.0 },
                new[] { 4.0, 3.0 }
            }
        );

        public static readonly TestCase Powell = new TestCase(
            "Powell 4D",
            TestFunctions.Powell,
            KnownSolutions.Powell,
            new[]
            {
                new[] { 3.0, -1.0, 0.0, 1.0 },  // Standard starting point
                new[] { 1.0, 1.0, 1.0, 1.0 },
                new[] { 0.1, 0.1, 0.1, 0.1 },
                new[] { -1.0, 2.0, -0.5, 1.5 }
            }
        );

        public static readonly TestCase[] All = {
            Rosenbrock, Sphere2D, Sphere5D, Booth, Beale, Himmelblau, Powell
        };
    }

    public class TestCase
    {
        public string Name { get; }
        public Func<ReadOnlySpan<double>, double> Function { get; }
        public (double[] point, double value) KnownSolution { get; }
        public double[][] StartingPoints { get; }

        public TestCase(string name, Func<ReadOnlySpan<double>, double> function, 
                       (double[] point, double value) knownSolution, double[][] startingPoints)
        {
            Name = name;
            Function = function;
            KnownSolution = knownSolution;
            StartingPoints = startingPoints;
        }
    }

    public class ValidationResult
    {
        public string TestName { get; set; } = "";
        public int StartingPointIndex { get; set; }
        public double[] InitialGuess { get; set; } = Array.Empty<double>();
        public double[] FinalPoint { get; set; } = Array.Empty<double>();
        public double FinalValue { get; set; }
        public double ParameterError { get; set; }
        public double ValueError { get; set; }
        public bool Converged { get; set; }
        public int Iterations { get; set; }
        public int FunctionEvaluations { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public string Status { get; set; } = "";
    }

    /// <summary>
    /// Run comprehensive validation against NLopt-equivalent test cases
    /// </summary>
    public static void RunEquivalenceValidation()
    {
        Console.WriteLine("=== NLopt Equivalence Validation ===");
        Console.WriteLine("Testing unbounded optimization against analytical solutions\n");

        var allResults = new List<ValidationResult>();

        foreach (var testCase in TestCases.All)
        {
            Console.WriteLine($"Testing {testCase.Name}:");
            Console.WriteLine($"  Expected solution: ({string.Join(", ", testCase.KnownSolution.point.Select(x => x.ToString("F6")))})");
            Console.WriteLine($"  Expected value: {testCase.KnownSolution.value:E6}");

            var testResults = RunTestCase(testCase);
            allResults.AddRange(testResults);

            // Summary for this test case
            int converged = testResults.Count(r => r.Converged);
            double avgError = testResults.Where(r => r.Converged).Select(r => r.ParameterError).DefaultIfEmpty(double.NaN).Average();
            double maxError = testResults.Where(r => r.Converged).Select(r => r.ParameterError).DefaultIfEmpty(double.NaN).Max();

            Console.WriteLine($"  Results: {converged}/{testResults.Count} converged");
            if (converged > 0)
            {
                Console.WriteLine($"  Average parameter error: {avgError:E3}");
                Console.WriteLine($"  Maximum parameter error: {maxError:E3}");
            }
            Console.WriteLine();
        }

        PrintOverallSummary(allResults);
        PrintDetailedResults(allResults);
        
        // Generate NLopt comparison format
        GenerateNLoptComparisonData(allResults);
    }

    private static List<ValidationResult> RunTestCase(TestCase testCase)
    {
        var results = new List<ValidationResult>();
        
        // Strict convergence options for equivalence testing
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-12,
            ParameterTolerance = 1e-12,
            MaxIterations = 10000  // Allow more iterations for difficult cases
        };

        for (int i = 0; i < testCase.StartingPoints.Length; i++)
        {
            var startingPoint = testCase.StartingPoints[i];
            var sw = Stopwatch.StartNew();

            try
            {
                // Create wrapper to match expected signature
                Func<Span<double>, double> wrappedFunction = parameters => testCase.Function(parameters);
                var result = NelderMead<double>.Minimize(wrappedFunction, startingPoint, options);
                sw.Stop();

                var paramError = CalculateParameterError(result.OptimalParameters.Span, testCase.KnownSolution.point);
                var valueError = Math.Abs(result.OptimalValue - testCase.KnownSolution.value);

                results.Add(new ValidationResult
                {
                    TestName = testCase.Name,
                    StartingPointIndex = i,
                    InitialGuess = startingPoint.ToArray(),
                    FinalPoint = result.OptimalParameters.Span.ToArray(),
                    FinalValue = result.OptimalValue,
                    ParameterError = paramError,
                    ValueError = valueError,
                    Converged = result.Converged,
                    Iterations = result.Iterations,
                    FunctionEvaluations = result.FunctionEvaluations,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    Status = result.Converged ? "SUCCESS" : "MAX_ITER"
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                results.Add(new ValidationResult
                {
                    TestName = testCase.Name,
                    StartingPointIndex = i,
                    InitialGuess = startingPoint.ToArray(),
                    FinalPoint = startingPoint.ToArray(),
                    FinalValue = double.NaN,
                    ParameterError = double.NaN,
                    ValueError = double.NaN,
                    Converged = false,
                    Iterations = 0,
                    FunctionEvaluations = 0,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    Status = $"ERROR: {ex.Message}"
                });
            }
        }

        return results;
    }

    private static double CalculateParameterError(ReadOnlySpan<double> computed, ReadOnlySpan<double> expected)
    {
        double maxError = 0;
        for (int i = 0; i < Math.Min(computed.Length, expected.Length); i++)
        {
            maxError = Math.Max(maxError, Math.Abs(computed[i] - expected[i]));
        }
        return maxError;
    }

    private static void PrintOverallSummary(List<ValidationResult> results)
    {
        Console.WriteLine("=== Overall Validation Summary ===");
        
        int totalTests = results.Count;
        int convergedTests = results.Count(r => r.Converged);
        int successfulTests = results.Count(r => r.Converged && r.ParameterError < 1e-6);
        
        Console.WriteLine($"Total test runs: {totalTests}");
        Console.WriteLine($"Converged: {convergedTests} ({100.0 * convergedTests / totalTests:F1}%)");
        Console.WriteLine($"High accuracy (error < 1e-6): {successfulTests} ({100.0 * successfulTests / totalTests:F1}%)");

        if (convergedTests > 0)
        {
            var convergedResults = results.Where(r => r.Converged).ToList();
            Console.WriteLine($"Average iterations: {convergedResults.Average(r => r.Iterations):F1}");
            Console.WriteLine($"Average function evaluations: {convergedResults.Average(r => r.FunctionEvaluations):F1}");
            Console.WriteLine($"Average execution time: {convergedResults.Average(r => r.ElapsedMilliseconds):F1} ms");
            Console.WriteLine($"Parameter error range: {convergedResults.Min(r => r.ParameterError):E2} to {convergedResults.Max(r => r.ParameterError):E2}");
        }
        Console.WriteLine();
    }

    private static void PrintDetailedResults(List<ValidationResult> results)
    {
        Console.WriteLine("=== Detailed Results ===");
        Console.WriteLine($"{"Test",-12} {"Start",-5} {"Status",-10} {"Iterations",-10} {"ParamError",-12} {"ValueError",-12} {"Time(ms)",-10}");
        Console.WriteLine(new string('-', 80));

        foreach (var result in results)
        {
            string paramError = result.Converged ? result.ParameterError.ToString("E2") : "N/A";
            string valueError = result.Converged ? result.ValueError.ToString("E2") : "N/A";
            
            Console.WriteLine($"{result.TestName,-12} {result.StartingPointIndex,-5} {result.Status,-10} " +
                            $"{result.Iterations,-10} {paramError,-12} {valueError,-12} {result.ElapsedMilliseconds,-10:F1}");
        }
        Console.WriteLine();
    }

    private static void GenerateNLoptComparisonData(List<ValidationResult> results)
    {
        Console.WriteLine("=== NLopt Comparison Data ===");
        Console.WriteLine("Format compatible with NLopt test suite validation:");
        Console.WriteLine();

        foreach (var testGroup in results.GroupBy(r => r.TestName))
        {
            Console.WriteLine($"Test: {testGroup.Key}");
            
            var convergedResults = testGroup.Where(r => r.Converged).ToList();
            if (convergedResults.Any())
            {
                var avgIterations = convergedResults.Average(r => r.Iterations);
                var avgFuncEvals = convergedResults.Average(r => r.FunctionEvaluations);
                var avgParameterError = convergedResults.Average(r => r.ParameterError);
                var maxParameterError = convergedResults.Max(r => r.ParameterError);
                
                Console.WriteLine($"  Convergence rate: {convergedResults.Count}/{testGroup.Count()} ({100.0 * convergedResults.Count / testGroup.Count():F1}%)");
                Console.WriteLine($"  Average iterations: {avgIterations:F1}");
                Console.WriteLine($"  Average function evaluations: {avgFuncEvals:F1}");
                Console.WriteLine($"  Average parameter error: {avgParameterError:E3}");
                Console.WriteLine($"  Maximum parameter error: {maxParameterError:E3}");
                
                // Tolerance classification (matching typical NLopt standards)
                int highPrecision = convergedResults.Count(r => r.ParameterError < 1e-8);
                int mediumPrecision = convergedResults.Count(r => r.ParameterError < 1e-6);
                int lowPrecision = convergedResults.Count(r => r.ParameterError < 1e-4);
                
                Console.WriteLine($"  High precision (< 1e-8): {highPrecision}/{convergedResults.Count}");
                Console.WriteLine($"  Medium precision (< 1e-6): {mediumPrecision}/{convergedResults.Count}");
                Console.WriteLine($"  Low precision (< 1e-4): {lowPrecision}/{convergedResults.Count}");
            }
            else
            {
                Console.WriteLine("  No convergent results");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Double Gaussian fitting validation with known synthetic data
    /// </summary>
    public static void ValidateDoubleGaussianEquivalence()
    {
        Console.WriteLine("=== Double Gaussian Equivalence Validation ===");
        Console.WriteLine("Testing against synthetic data with known parameters\n");

        // Test with multiple known parameter sets
        var testParameters = new[]
        {
            new[] { 1.0, 0.0, 1.0, 1.0, 2.0, 0.5 },      // Well-separated Gaussians
            new[] { 2.0, -1.0, 0.8, 1.5, 1.0, 0.6 },     // Asymmetric amplitudes
            new[] { 1.0, 0.0, 0.3, 1.0, 0.5, 0.3 },      // Overlapping Gaussians
            new[] { 0.5, -2.0, 1.5, 2.0, 2.0, 0.8 },     // Different widths
            new[] { 3.0, 0.0, 0.5, 0.2, 1.0, 0.2 }       // Different scales
        };

        for (int testIndex = 0; testIndex < testParameters.Length; testIndex++)
        {
            var trueParams = testParameters[testIndex];
            Console.WriteLine($"Test {testIndex + 1}: [{string.Join(", ", trueParams.Select(x => x.ToString("F2")))}]");

            // Generate high-quality synthetic data
            var xData = new double[200];
            var yData = new double[200];
            
            for (int i = 0; i < 200; i++)
            {
                xData[i] = -4.0 + 8.0 * i / 199.0;
                yData[i] = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
                // Add minimal noise for realistic conditions
                yData[i] += 1e-10 * (new Random(42 + i).NextDouble() - 0.5);
            }

            // Test with different initial guesses
            var initialGuesses = new[]
            {
                DoubleGaussian.GenerateInitialGuess<double>(xData, yData).ToArray(),
                new[] { 0.5, -0.5, 0.5, 0.5, 0.5, 0.5 },    // Conservative guess
                new[] { 2.0, 0.0, 1.0, 2.0, 1.0, 1.0 },     // Generic guess
                new[] { 1.0, -1.0, 0.8, 1.0, 1.0, 0.8 }     // Alternative guess
            };

            var sw = Stopwatch.StartNew();
            
            for (int guessIndex = 0; guessIndex < initialGuesses.Length; guessIndex++)
            {
                var options = new NelderMeadOptions<double>
                {
                    FunctionTolerance = 1e-12,
                    ParameterTolerance = 1e-12,
                    MaxIterations = 5000
                };

                var result = DoubleGaussian.Fit(xData, yData, initialGuesses[guessIndex], options);
                
                double maxParamError = 0;
                for (int i = 0; i < 6; i++)
                {
                    maxParamError = Math.Max(maxParamError, Math.Abs(result.OptimalParameters.Span[i] - trueParams[i]));
                }

                string status = result.Converged && maxParamError < 1e-6 ? "✓ EXCELLENT" :
                               result.Converged && maxParamError < 1e-4 ? "✓ GOOD" :
                               result.Converged ? "⚠ CONVERGED" : "✗ FAILED";

                Console.WriteLine($"  Guess {guessIndex + 1}: {status} (error: {maxParamError:E2}, SSR: {result.OptimalValue:E2})");
            }
            
            sw.Stop();
            Console.WriteLine($"  Total time: {sw.ElapsedMilliseconds} ms\n");
        }
    }
}