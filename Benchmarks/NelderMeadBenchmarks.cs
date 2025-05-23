using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Optimization.Core.Algorithms;
using Optimization.Core.Models;

namespace Optimization.Core.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(baseline: true)]
public class NelderMeadBenchmarks
{
    private double[] _xData = null!;
    private double[] _yData = null!;
    private double[] _initialGuess = null!;
    private NelderMeadOptions<double> _options = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate synthetic double Gaussian data for benchmarking
        var trueParams = new double[] { 1.5, -0.8, 0.6, 1.2, 1.0, 0.4 };
        var random = new Random(42);
        
        _xData = new double[200];
        _yData = new double[200];
        
        for (int i = 0; i < 200; i++)
        {
            _xData[i] = -3.0 + 6.0 * i / 199.0;
            double clean = DoubleGaussian.Evaluate<double>(trueParams, _xData[i]);
            double noise = 0.05 * clean * random.NextGaussian();
            _yData[i] = clean + noise;
        }

        _initialGuess = DoubleGaussian.GenerateInitialGuess<double>(_xData, _yData).ToArray();
        _options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-8,
            MaxIterations = 2000
        };
    }

    [Benchmark(Baseline = true)]
    public OptimizationResult<double> DoubleGaussianFitting()
    {
        var objective = ObjectiveFunctions.CreateSumSquaredResidualsFunction<double>(_xData, _yData);
        var guess = _initialGuess.ToArray().AsSpan();
        return NelderMead<double>.Minimize(objective, guess, _options);
    }

    [Benchmark]
    public OptimizationResult<double> DoubleGaussianWithBounds()
    {
        var objective = ObjectiveFunctions.CreateSumSquaredResidualsFunction<double>(_xData, _yData);
        var guess = _initialGuess.ToArray().AsSpan();
        
        var boundsOptions = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-8,
            MaxIterations = 2000,
            LowerBounds = new double[] { 0.0, -5.0, 0.01, 0.0, -5.0, 0.01 },
            UpperBounds = new double[] { 10.0, 5.0, 5.0, 10.0, 5.0, 5.0 }
        };
        
        return NelderMead<double>.Minimize(objective, guess, boundsOptions);
    }

    [Benchmark]
    public OptimizationResult<float> DoubleGaussianFloat()
    {
        var xDataFloat = _xData.Select(x => (float)x).ToArray();
        var yDataFloat = _yData.Select(y => (float)y).ToArray();
        var guessFloat = _initialGuess.Select(g => (float)g).ToArray();
        
        var objective = ObjectiveFunctions.CreateSumSquaredResidualsFunction<float>(xDataFloat, yDataFloat);
        var optionsFloat = new NelderMeadOptions<float>
        {
            FunctionTolerance = 1e-6f,
            MaxIterations = 2000
        };
        
        return NelderMead<float>.Minimize(objective, guessFloat, optionsFloat);
    }

    [Benchmark]
    public double RosenbrockOptimization()
    {
        static double Rosenbrock(Span<double> x)
        {
            double a = 1.0, b = 100.0;
            return Math.Pow(a - x[0], 2) + b * Math.Pow(x[1] - x[0] * x[0], 2);
        }
        
        var initialGuess = new double[] { -1.0, 1.0 };
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-6,
            MaxIterations = 1000
        };

        var result = NelderMead<double>.Minimize(Rosenbrock, initialGuess, options);
        return result.OptimalValue;
    }

    [Benchmark]
    [Arguments(2)]
    [Arguments(5)]
    [Arguments(10)]
    public OptimizationResult<double> HighDimensionalQuadratic(int dimensions)
    {
        double Quadratic(Span<double> x)
        {
            double sum = 0;
            for (int i = 0; i < x.Length; i++)
                sum += Math.Pow(x[i] - i, 2);
            return sum;
        }
        
        var initialGuess = new double[dimensions];
        var options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-6,
            MaxIterations = dimensions * 500
        };

        return NelderMead<double>.Minimize(Quadratic, initialGuess, options);
    }

    [Benchmark]
    public void DoubleGaussianEvaluation()
    {
        var parameters = new double[] { 1.5, -0.8, 0.6, 1.2, 1.0, 0.4 };
        
        for (int i = 0; i < _xData.Length; i++)
        {
            DoubleGaussian.Evaluate<double>(parameters, _xData[i]);
        }
    }

    [Benchmark]
    public void DoubleGaussianEvaluationRange()
    {
        var parameters = new double[] { 1.5, -0.8, 0.6, 1.2, 1.0, 0.4 };
        var result = new double[_xData.Length];
        
        DoubleGaussian.EvaluateRange<double>(parameters, _xData, result);
    }
}

public static class BenchmarkProgram
{
    public static void RunBenchmarks()
    {
        BenchmarkRunner.Run<NelderMeadBenchmarks>();
    }
}

// Extension for random Gaussian numbers
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