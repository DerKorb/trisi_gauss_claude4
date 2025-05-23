using Xunit;
using Optimization.Core.Algorithms;
using Optimization.Core.Models;

namespace Optimization.Core.Tests;

public class AlgorithmTests
{
    [Fact]
    public void NelderMead_MinimizesQuadraticFunction()
    {
        // f(x) = (x-2)² + (y-3)² with minimum at (2,3)
        static double Quadratic(Span<double> x) => Math.Pow(x[0] - 2.0, 2) + Math.Pow(x[1] - 3.0, 2);
        
        var initialGuess = new double[] { 0.0, 0.0 };
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-10,
            MaxIterations = 1000
        };

        var result = NelderMead<double>.Minimize(Quadratic, initialGuess, options);

        Assert.True(result.Converged);
        Assert.True(Math.Abs(result.OptimalParameters.Span[0] - 2.0) < 1e-6);
        Assert.True(Math.Abs(result.OptimalParameters.Span[1] - 3.0) < 1e-6);
        Assert.True(Math.Abs(result.OptimalValue) < 1e-10);
    }

    [Fact]
    public void NelderMead_RespectsLowerBounds()
    {
        // Minimize x² + y² with lower bounds at (1, 1)
        static double BoundedQuadratic(Span<double> x) => x[0] * x[0] + x[1] * x[1];
        
        var initialGuess = new double[] { 2.0, 2.0 };
        var options = new NelderMeadOptions<double>
        {
            LowerBounds = new double[] { 1.0, 1.0 },
            FunctionTolerance = 1e-8,
            MaxIterations = 1000
        };

        var result = NelderMead<double>.Minimize(BoundedQuadratic, initialGuess, options);

        Assert.True(result.Converged);
        Assert.True(result.OptimalParameters.Span[0] >= 0.99); // Allow small tolerance
        Assert.True(result.OptimalParameters.Span[1] >= 0.99);
    }

    [Fact]
    public void NelderMead_RespectsUpperBounds()
    {
        // Minimize -(x² + y²) with upper bounds at (1, 1) - maximum becomes minimum when negated
        static double NegativeQuadratic(Span<double> x) => -(x[0] * x[0] + x[1] * x[1]);
        
        var initialGuess = new double[] { 0.5, 0.5 };
        var options = new NelderMeadOptions<double>
        {
            UpperBounds = new double[] { 1.0, 1.0 },
            FunctionTolerance = 1e-8,
            MaxIterations = 1000
        };

        var result = NelderMead<double>.Minimize(NegativeQuadratic, initialGuess, options);

        Assert.True(result.Converged);
        Assert.True(result.OptimalParameters.Span[0] <= 1.01); // Allow small tolerance
        Assert.True(result.OptimalParameters.Span[1] <= 1.01);
    }

    [Fact]
    public void NelderMead_ConvergesWithFloatPrecision()
    {
        // Test with float precision
        static float QuadraticFloat(Span<float> x) => (x[0] - 2.0f) * (x[0] - 2.0f) + (x[1] - 3.0f) * (x[1] - 3.0f);
        
        var initialGuess = new float[] { 0.0f, 0.0f };
        var options = new NelderMeadOptions<float>
        {
            FunctionTolerance = 1e-6f,
            MaxIterations = 1000
        };

        var result = NelderMead<float>.Minimize(QuadraticFloat, initialGuess, options);

        Assert.True(result.Converged);
        Assert.True(Math.Abs(result.OptimalParameters.Span[0] - 2.0f) < 1e-4f);
        Assert.True(Math.Abs(result.OptimalParameters.Span[1] - 3.0f) < 1e-4f);
    }

    [Fact]
    public void NelderMead_HandlesLargeDimensionality()
    {
        // Test with 10-dimensional problem: minimize sum of (xi - i)²
        static double HighDimensional(Span<double> x)
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
            MaxIterations = 5000
        };

        var result = NelderMead<double>.Minimize(HighDimensional, initialGuess, options);

        Assert.True(result.Converged || result.OptimalValue < 1e-3); // May not fully converge but should get close
        
        // Check that we're reasonably close to the expected solution
        for (int i = 0; i < 10; i++)
        {
            Assert.True(Math.Abs(result.OptimalParameters.Span[i] - i) < 0.1);
        }
    }

    [Fact]
    public void NelderMead_ReturnsCorrectIterationCount()
    {
        static double Simple(Span<double> x) => x[0] * x[0];
        
        var initialGuess = new double[] { 1.0 };
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-10,
            MaxIterations = 100
        };

        var result = NelderMead<double>.Minimize(Simple, initialGuess, options);

        Assert.True(result.Iterations > 0);
        Assert.True(result.FunctionEvaluations > 0);
        Assert.True(result.FunctionEvaluations >= result.Iterations);
    }
}