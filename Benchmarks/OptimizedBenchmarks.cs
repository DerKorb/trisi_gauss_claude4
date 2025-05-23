using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Optimization.Core.Algorithms;
using Optimization.Core.Models;

namespace Optimization.Core.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(baseline: true)]
[DisassemblyDiagnoser(maxDepth: 3)]
public class OptimizedBenchmarks
{
    private double[] _xData = null!;
    private double[] _yData = null!;
    private double[] _initialGuess = null!;
    private NelderMeadOptions<double> _options = null!;
    private double[] _parameters = null!;
    private double[] _smallXData = null!;
    private double[] _smallYData = null!;
    private double[] _largeXData = null!;
    private double[] _largeYData = null!;

    [GlobalSetup]
    public void Setup()
    {
        var trueParams = new double[] { 1.5, -0.8, 0.6, 1.2, 1.0, 0.4 };
        var random = new Random(42);
        
        // Standard dataset for optimization benchmarks
        _xData = new double[200];
        _yData = new double[200];
        
        for (int i = 0; i < 200; i++)
        {
            _xData[i] = -3.0 + 6.0 * i / 199.0;
            double clean = DoubleGaussian.Evaluate<double>(trueParams, _xData[i]);
            double noise = 0.05 * clean * random.NextGaussian();
            _yData[i] = clean + noise;
        }

        // Small dataset for evaluation benchmarks
        _smallXData = new double[10];
        _smallYData = new double[10];
        for (int i = 0; i < 10; i++)
        {
            _smallXData[i] = -1.0 + 2.0 * i / 9.0;
            _smallYData[i] = DoubleGaussian.Evaluate<double>(trueParams, _smallXData[i]);
        }

        // Large dataset for vectorization benchmarks
        _largeXData = new double[10000];
        _largeYData = new double[10000];
        for (int i = 0; i < 10000; i++)
        {
            _largeXData[i] = -5.0 + 10.0 * i / 9999.0;
            _largeYData[i] = DoubleGaussian.Evaluate<double>(trueParams, _largeXData[i]);
        }

        _initialGuess = DoubleGaussian.GenerateInitialGuess<double>(_xData, _yData).ToArray();
        _options = new NelderMeadOptions<double>
        {
            FunctionTolerance = 1e-8,
            MaxIterations = 1000
        };
        
        _parameters = trueParams;
    }

    // ==================== Algorithm Comparison ====================

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Algorithm")]
    public OptimizationResult<double> StandardNelderMead()
    {
        var objective = ObjectiveFunctions.CreateSumSquaredResidualsFunction<double>(_xData, _yData);
        var guess = _initialGuess.ToArray().AsSpan();
        return NelderMead<double>.Minimize(objective, guess, _options);
    }

    [Benchmark]
    [BenchmarkCategory("Algorithm")]
    public OptimizationResult<double> OptimizedNelderMead()
    {
        var objective = DoubleGaussianOptimizedFixed.CreateOptimizedObjective<double>(_xData, _yData);
        return NelderMeadOptimized<double>.Minimize(objective, _initialGuess, _options);
    }

    // ==================== Double Gaussian Evaluation ====================

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Evaluation")]
    public double StandardDoubleGaussianEvaluation()
    {
        double sum = 0;
        for (int i = 0; i < _xData.Length; i++)
        {
            sum += DoubleGaussian.Evaluate<double>(_parameters, _xData[i]);
        }
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Evaluation")]
    public double OptimizedDoubleGaussianEvaluation()
    {
        double sum = 0;
        for (int i = 0; i < _xData.Length; i++)
        {
            sum += DoubleGaussianOptimizedFixed.Evaluate<double>(_parameters, _xData[i]);
        }
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Evaluation")]
    public void StandardDoubleGaussianRange()
    {
        var result = new double[_xData.Length];
        DoubleGaussian.EvaluateRange<double>(_parameters, _xData, result);
    }

    [Benchmark]
    [BenchmarkCategory("Evaluation")]
    public void OptimizedDoubleGaussianRange()
    {
        var result = new double[_xData.Length];
        DoubleGaussianOptimizedFixed.EvaluateRange<double>(_parameters, _xData, result);
    }

    // ==================== Vectorization Benchmarks ====================

    [Benchmark]
    [BenchmarkCategory("Vectorization")]
    public void LargeDatasetStandard()
    {
        var result = new double[_largeXData.Length];
        DoubleGaussian.EvaluateRange<double>(_parameters, _largeXData, result);
    }

    [Benchmark]
    [BenchmarkCategory("Vectorization")]
    public void LargeDatasetOptimized()
    {
        var result = new double[_largeXData.Length];
        DoubleGaussianOptimizedFixed.EvaluateRange<double>(_parameters, _largeXData, result);
    }

    // ==================== Memory Allocation Benchmarks ====================

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public double StandardSumSquaredResiduals()
    {
        return ObjectiveFunctions.SumSquaredResiduals<double>(_parameters, _xData, _yData);
    }

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public double OptimizedSumSquaredResiduals()
    {
        return DoubleGaussianOptimizedFixed.SumSquaredResidualsOptimized<double>(_parameters, _xData, _yData);
    }

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public double LargeSumSquaredResidualsStandard()
    {
        return ObjectiveFunctions.SumSquaredResiduals<double>(_parameters, _largeXData, _largeYData);
    }

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public double LargeSumSquaredResidualsOptimized()
    {
        return DoubleGaussianOptimizedFixed.SumSquaredResidualsOptimized<double>(_parameters, _largeXData, _largeYData);
    }

    // ==================== Precision Benchmarks ====================

    [Benchmark]
    [BenchmarkCategory("Precision")]
    public OptimizationResult<float> FloatOptimization()
    {
        var xDataFloat = _xData.Select(x => (float)x).ToArray();
        var yDataFloat = _yData.Select(y => (float)y).ToArray();
        var guessFloat = _initialGuess.Select(g => (float)g).ToArray();
        
        var objective = DoubleGaussianOptimizedFixed.CreateOptimizedObjective<float>(xDataFloat, yDataFloat);
        
        var optionsFloat = new NelderMeadOptions<float>
        {
            FunctionTolerance = 1e-6f,
            MaxIterations = 1000
        };
        
        return NelderMeadOptimized<float>.Minimize(objective, guessFloat, optionsFloat);
    }

    [Benchmark]
    [BenchmarkCategory("Precision")]
    public OptimizationResult<double> DoubleOptimization()
    {
        var objective = DoubleGaussianOptimizedFixed.CreateOptimizedObjective<double>(_xData, _yData);
        
        return NelderMeadOptimized<double>.Minimize(objective, _initialGuess, _options);
    }

    // ==================== Small vs Large Dataset ====================

    [Benchmark]
    [BenchmarkCategory("Scaling")]
    public double SmallDatasetOptimized()
    {
        return DoubleGaussianOptimizedFixed.SumSquaredResidualsOptimized<double>(_parameters, _smallXData, _smallYData);
    }

    [Benchmark]
    [BenchmarkCategory("Scaling")]
    public double MediumDatasetOptimized()
    {
        return DoubleGaussianOptimizedFixed.SumSquaredResidualsOptimized<double>(_parameters, _xData, _yData);
    }

    [Benchmark]
    [BenchmarkCategory("Scaling")]
    public double LargeDatasetOptimizedSSR()
    {
        return DoubleGaussianOptimizedFixed.SumSquaredResidualsOptimized<double>(_parameters, _largeXData, _largeYData);
    }

    // ==================== Function Call Overhead ====================

    [Benchmark]
    [BenchmarkCategory("Overhead")]
    public double DirectCalculation()
    {
        double sum = 0;
        double a1 = _parameters[0], mu1 = _parameters[1], sigma1 = _parameters[2];
        double a2 = _parameters[3], mu2 = _parameters[4], sigma2 = _parameters[5];
        
        for (int i = 0; i < _xData.Length; i++)
        {
            double x = _xData[i];
            double diff1 = x - mu1;
            double norm1 = diff1 / sigma1;
            double exp1 = Math.Exp(-0.5 * norm1 * norm1);
            
            double diff2 = x - mu2;
            double norm2 = diff2 / sigma2;
            double exp2 = Math.Exp(-0.5 * norm2 * norm2);
            
            sum += a1 * exp1 + a2 * exp2;
        }
        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Overhead")]
    public double OptimizedFunctionCall()
    {
        double sum = 0;
        for (int i = 0; i < _xData.Length; i++)
        {
            sum += DoubleGaussianOptimizedFixed.Evaluate<double>(_parameters, _xData[i]);
        }
        return sum;
    }
}

public static class OptimizedBenchmarkRunner
{
    public static void RunOptimizedBenchmarks()
    {
        Console.WriteLine("Running Optimized Performance Benchmarks...\n");
        
        var summary = BenchmarkRunner.Run<OptimizedBenchmarks>();
        
        Console.WriteLine("\nBenchmark Results Summary:");
        Console.WriteLine("See detailed results above for memory allocations, execution times, and performance improvements.");
    }
}

