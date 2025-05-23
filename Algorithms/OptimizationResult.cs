using System.Numerics;

namespace Optimization.Core.Algorithms;

public readonly struct OptimizationResult<T> where T : IFloatingPoint<T>
{
    public ReadOnlyMemory<T> OptimalParameters { get; init; }
    public T OptimalValue { get; init; }
    public int Iterations { get; init; }
    public int FunctionEvaluations { get; init; }
    public bool Converged { get; init; }
    public string? Message { get; init; }

    public OptimizationResult(
        ReadOnlyMemory<T> optimalParameters,
        T optimalValue,
        int iterations,
        int functionEvaluations,
        bool converged,
        string? message = null)
    {
        OptimalParameters = optimalParameters;
        OptimalValue = optimalValue;
        Iterations = iterations;
        FunctionEvaluations = functionEvaluations;
        Converged = converged;
        Message = message;
    }
}