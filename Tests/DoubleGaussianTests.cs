using Xunit;
using Optimization.Core.Models;
using Optimization.Core.Algorithms;

namespace Optimization.Core.Tests;

public class DoubleGaussianTests
{
    [Fact]
    public void DoubleGaussian_EvaluatesCorrectly()
    {
        // Test with known parameters
        var parameters = new double[] { 1.0, 0.0, 1.0, 0.5, 2.0, 0.5 };
        double x = 0.0;

        double result = DoubleGaussian.Evaluate<double>(parameters, x);

        // At x=0: First Gaussian = 1*exp(0) = 1, Second Gaussian = 0.5*exp(-2) ≈ 0.068
        double expected = 1.0 + 0.5 * Math.Exp(-2.0);
        Assert.True(Math.Abs(result - expected) < 1e-10);
    }

    [Fact]
    public void DoubleGaussian_HandlesZeroSigma()
    {
        // Test with zero sigma values (should be handled gracefully)
        var parameters = new double[] { 1.0, 0.0, 0.0, 1.0, 1.0, 0.0 };
        double x = 0.0;

        // Should not throw and should return finite result
        double result = DoubleGaussian.Evaluate<double>(parameters, x);
        Assert.True(double.IsFinite(result));
    }

    [Fact]
    public void DoubleGaussian_FitsKnownData()
    {
        // Generate synthetic double Gaussian data
        var trueParams = new double[] { 1.0, -1.0, 0.5, 0.8, 1.0, 0.3 };
        var xData = new double[100];
        var yData = new double[100];

        for (int i = 0; i < 100; i++)
        {
            xData[i] = -3.0 + 6.0 * i / 99.0; // Range from -3 to 3
            yData[i] = DoubleGaussian.Evaluate<double>(trueParams, xData[i]);
        }

        // Add small amount of noise
        var random = new Random(42);
        for (int i = 0; i < yData.Length; i++)
        {
            yData[i] += 0.01 * random.NextGaussian();
        }

        var initialGuess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-10,
            MaxIterations = 2000
        };

        var result = DoubleGaussian.Fit(xData, yData, initialGuess, options);

        Assert.True(result.Converged);
        
        // Check that fitted parameters are reasonably close to true parameters
        var fitted = result.OptimalParameters.Span;
        for (int i = 0; i < 6; i++)
        {
            Assert.True(Math.Abs(fitted[i] - trueParams[i]) < 0.2, 
                $"Parameter {i}: expected {trueParams[i]}, got {fitted[i]}");
        }
    }

    [Fact]
    public void DoubleGaussian_EvaluateRangeWorksCorrectly()
    {
        var parameters = new double[] { 1.0, 0.0, 1.0, 0.5, 2.0, 0.5 };
        var xValues = new double[] { -1.0, 0.0, 1.0, 2.0 };
        var result = new double[4];

        DoubleGaussian.EvaluateRange<double>(parameters, xValues, result);

        for (int i = 0; i < xValues.Length; i++)
        {
            double expected = DoubleGaussian.Evaluate<double>(parameters, xValues[i]);
            Assert.True(Math.Abs(result[i] - expected) < 1e-10);
        }
    }

    [Fact]
    public void DoubleGaussian_GenerateInitialGuessReturnsValidParameters()
    {
        var xData = new double[] { -2, -1, 0, 1, 2, 3, 4 };
        var yData = new double[] { 0.1, 0.5, 1.0, 0.8, 0.3, 0.6, 0.2 };

        var guess = DoubleGaussian.GenerateInitialGuess<double>(xData, yData);

        Assert.Equal(6, guess.Length);
        
        // All parameters should be finite
        for (int i = 0; i < 6; i++)
        {
            Assert.True(double.IsFinite(guess[i]));
        }
        
        // Sigma values should be positive
        Assert.True(guess[2] > 0); // σ1
        Assert.True(guess[5] > 0); // σ2
    }

    [Fact]
    public void ObjectiveFunctions_SumSquaredResidualsWorksCorrectly()
    {
        var parameters = new double[] { 1.0, 0.0, 1.0, 0.5, 2.0, 0.5 };
        var xData = new double[] { -1.0, 0.0, 1.0, 2.0 };
        var yData = new double[4];

        // Generate perfect data (no noise)
        for (int i = 0; i < xData.Length; i++)
        {
            yData[i] = DoubleGaussian.Evaluate<double>(parameters, xData[i]);
        }

        double ssr = ObjectiveFunctions.SumSquaredResiduals<double>(parameters, xData, yData);

        // Should be zero (or very close) for perfect fit
        Assert.True(Math.Abs(ssr) < 1e-10);
    }

    [Theory]
    [InlineData(1.0, 0.0, 1.0, 0.0)]
    [InlineData(2.0, 1.0, 0.5, 1.0)]
    [InlineData(0.5, -1.0, 2.0, -1.0)]
    public void DoubleGaussian_SingleGaussianBehavior(double amplitude, double mean, double sigma, double x)
    {
        // Test with second Gaussian having zero amplitude
        var parameters = new double[] { amplitude, mean, sigma, 0.0, 0.0, 1.0 };
        
        double result = DoubleGaussian.Evaluate<double>(parameters, x);
        double expected = amplitude * Math.Exp(-0.5 * Math.Pow((x - mean) / sigma, 2));
        
        Assert.True(Math.Abs(result - expected) < 1e-10);
    }
}

// Extension method for generating Gaussian random numbers
public static class RandomExtensions
{
    public static double NextGaussian(this Random random, double mean = 0.0, double stdDev = 1.0)
    {
        // Box-Muller transform
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}