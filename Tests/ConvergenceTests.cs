using Xunit;
using Optimization.Core.Algorithms;
using Optimization.Core.Models;

namespace Optimization.Core.Tests;

public class ConvergenceTests
{
    [Theory]
    [InlineData(0.05)] // 5% noise
    [InlineData(0.10)] // 10% noise
    [InlineData(0.15)] // 15% noise
    [InlineData(0.20)] // 20% noise
    public void DoubleGaussian_ConvergesWithNoisyData(double noiseLevel)
    {
        var trueParams = new double[] { 2.0, -0.5, 0.8, 1.5, 1.2, 0.6 };
        var random = new Random(42); // Fixed seed for reproducibility
        
        // Generate data with noise
        var xData = new double[200];
        var yData = new double[200];
        
        for (int i = 0; i < 200; i++)
        {
            xData[i] = -3.0 + 6.0 * i / 199.0;
            double clean = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
            double noise = noiseLevel * clean * random.NextGaussian();
            yData[i] = clean + noise;
        }

        var initialGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-8,
            ParameterTolerance = 1e-8,
            MaxIterations = 3000
        };

        var result = DoubleGaussian.Fit(xData, yData, initialGuess, options);
        
        // Should converge even with noise (tolerance depends on noise level)
        Assert.True(result.Converged || result.OptimalValue < 100.0);
        
        // Parameters should be somewhat close to true values (tolerance increases with noise)
        var tolerance = Math.Max(0.3, noiseLevel * 2.0);
        var fitted = result.OptimalParameters.Span;
        
        for (int i = 0; i < 6; i++)
        {
            double error = Math.Abs(fitted[i] - trueParams[i]);
            Assert.True(error < tolerance, 
                $"Noise level {noiseLevel:P0}: Parameter {i} error {error:F3} exceeds tolerance {tolerance:F3}");
        }
    }

    [Fact]
    public void DoubleGaussian_RobustToPoorInitialGuess()
    {
        var trueParams = new double[] { 1.0, 0.0, 1.0, 0.8, 2.0, 0.5 };
        
        // Generate clean data
        var xData = new double[100];
        var yData = new double[100];
        
        for (int i = 0; i < 100; i++)
        {
            xData[i] = -3.0 + 6.0 * i / 99.0;
            yData[i] = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
        }

        // Deliberately poor initial guess
        var poorGuess = new double[] { 0.1, -5.0, 0.1, 0.1, 5.0, 0.1 };
        
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-10,
            MaxIterations = 3000,
            InitialSimplexSize = 1.0 // Larger simplex for exploration
        };

        var result = DoubleGaussian.Fit(xData, yData, poorGuess, options);
        
        Assert.True(result.Converged);
        
        // Should still find reasonably good parameters
        var fitted = result.OptimalParameters.Span;
        for (int i = 0; i < 6; i++)
        {
            Assert.True(Math.Abs(fitted[i] - trueParams[i]) < 0.5);
        }
    }

    [Fact]
    public void NelderMead_ConvergesFromMultipleStartingPoints()
    {
        // Test Rosenbrock function: f(x,y) = (a-x)² + b(y-x²)²
        // Global minimum at (a,a²) where a=1, b=100
        static double Rosenbrock(Span<double> x)
        {
            double a = 1.0, b = 100.0;
            return Math.Pow(a - x[0], 2) + b * Math.Pow(x[1] - x[0] * x[0], 2);
        }
        
        var startingPoints = new double[][]
        {
            new[] { -1.0, 1.0 },
            new[] { 0.0, 0.0 },
            new[] { 2.0, 4.0 },
            new[] { -0.5, 0.25 }
        };

        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-6,
            MaxIterations = 2000
        };

        int convergedCount = 0;
        double expectedX = 1.0, expectedY = 1.0;

        foreach (var start in startingPoints)
        {
            var result = NelderMead<double>.Minimize(Rosenbrock, start, options);
            
            if (result.Converged && 
                Math.Abs(result.OptimalParameters.Span[0] - expectedX) < 0.1 &&
                Math.Abs(result.OptimalParameters.Span[1] - expectedY) < 0.1)
            {
                convergedCount++;
            }
        }

        // At least half should converge to correct solution
        Assert.True(convergedCount >= 2, $"Only {convergedCount} out of {startingPoints.Length} starting points converged");
    }

    [Fact]
    public void DoubleGaussian_HandlesOverlappingGaussians()
    {
        // Test with heavily overlapping Gaussians (challenging case)
        var trueParams = new double[] { 1.0, 0.0, 1.0, 1.2, 0.5, 1.2 };
        
        var xData = new double[150];
        var yData = new double[150];
        
        for (int i = 0; i < 150; i++)
        {
            xData[i] = -4.0 + 8.0 * i / 149.0;
            yData[i] = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
        }

        var initialGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-8,
            MaxIterations = 4000,
            InitialSimplexSize = 0.1
        };

        var result = DoubleGaussian.Fit(xData, yData, initialGuess, options);
        
        Assert.True(result.Converged);
        
        // For overlapping Gaussians, exact parameter recovery is harder,
        // but the fit quality should still be very good
        Assert.True(result.OptimalValue < 1e-6);
    }

    [Fact]
    public void NelderMead_HandlesBoundedOptimization()
    {
        // Minimize sum of squares with bounds
        static double BoundedObjective(Span<double> x) => 
            Math.Pow(x[0] - 0.5, 2) + Math.Pow(x[1] - 0.3, 2);
        
        var initialGuess = new double[] { 0.8, 0.8 };
        var options = new NelderMeadOptions<double>
        {
            LowerBounds = new double[] { 0.0, 0.0 },
            UpperBounds = new double[] { 1.0, 1.0 },
            FunctionTolerance = 1e-8,
            MaxIterations = 1000
        };

        var result = NelderMead<double>.Minimize(BoundedObjective, initialGuess, options);
        
        Assert.True(result.Converged);
        
        // Should find minimum at (0.5, 0.3) since it's within bounds
        Assert.True(Math.Abs(result.OptimalParameters.Span[0] - 0.5) < 1e-4);
        Assert.True(Math.Abs(result.OptimalParameters.Span[1] - 0.3) < 1e-4);
        
        // Should respect bounds
        Assert.True(result.OptimalParameters.Span[0] >= -1e-10);
        Assert.True(result.OptimalParameters.Span[0] <= 1.0 + 1e-10);
        Assert.True(result.OptimalParameters.Span[1] >= -1e-10);
        Assert.True(result.OptimalParameters.Span[1] <= 1.0 + 1e-10);
    }

    [Fact]
    public void DoubleGaussian_ConvergesWithDifferentAmplitudes()
    {
        // Test with very different amplitude scales
        var trueParams = new double[] { 100.0, -1.0, 0.5, 0.01, 1.0, 0.3 };
        
        var xData = new double[120];
        var yData = new double[120];
        
        for (int i = 0; i < 120; i++)
        {
            xData[i] = -3.0 + 6.0 * i / 119.0;
            yData[i] = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
        }

        var initialGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-8,
            MaxIterations = 3000,
            InitialSimplexSize = 0.1
        };

        var result = DoubleGaussian.Fit(xData, yData, initialGuess, options);
        
        Assert.True(result.Converged);
        
        // Should achieve very good fit even with different scales
        Assert.True(result.OptimalValue < 1e-4);
    }
}