#include <nlopt.hpp>
#include <iostream>
#include <chrono>
#include <vector>
#include <cmath>
#include <iomanip>
#include <fstream>

// Test function implementations matching our C# versions
class TestFunctions {
public:
    // Rosenbrock function: f(x,y) = (a-x)² + b(y-x²)²
    static double rosenbrock(const std::vector<double>& x, std::vector<double>& grad, void* data) {
        double a = 1.0, b = 100.0;
        return std::pow(a - x[0], 2) + b * std::pow(x[1] - x[0] * x[0], 2);
    }
    
    // Sphere function: f(x) = Σ(xi²)
    static double sphere(const std::vector<double>& x, std::vector<double>& grad, void* data) {
        double sum = 0;
        for (size_t i = 0; i < x.size(); i++) {
            sum += x[i] * x[i];
        }
        return sum;
    }
    
    // Booth function: f(x,y) = (x + 2y - 7)² + (2x + y - 5)²
    static double booth(const std::vector<double>& x, std::vector<double>& grad, void* data) {
        return std::pow(x[0] + 2 * x[1] - 7, 2) + std::pow(2 * x[0] + x[1] - 5, 2);
    }
    
    // Beale function: f(x,y) = (1.5 - x + xy)² + (2.25 - x + xy²)² + (2.625 - x + xy³)²
    static double beale(const std::vector<double>& x, std::vector<double>& grad, void* data) {
        double term1 = std::pow(1.5 - x[0] + x[0] * x[1], 2);
        double term2 = std::pow(2.25 - x[0] + x[0] * x[1] * x[1], 2);
        double term3 = std::pow(2.625 - x[0] + x[0] * x[1] * x[1] * x[1], 2);
        return term1 + term2 + term3;
    }
    
    // Himmelblau function: f(x,y) = (x² + y - 11)² + (x + y² - 7)²
    static double himmelblau(const std::vector<double>& x, std::vector<double>& grad, void* data) {
        return std::pow(x[0] * x[0] + x[1] - 11, 2) + std::pow(x[0] + x[1] * x[1] - 7, 2);
    }
    
    // Powell function (4D)
    static double powell(const std::vector<double>& x, std::vector<double>& grad, void* data) {
        double term1 = std::pow(x[0] + 10 * x[1], 2);
        double term2 = 5 * std::pow(x[2] - x[3], 2);
        double term3 = std::pow(x[1] - 2 * x[2], 4);
        double term4 = 10 * std::pow(x[0] - x[3], 4);
        return term1 + term2 + term3 + term4;
    }
};

// Double Gaussian fitting function
class DoubleGaussianData {
public:
    std::vector<double> x_data;
    std::vector<double> y_data;
    
    static double evaluate(const std::vector<double>& params, double x) {
        // params: [A1, mu1, sigma1, A2, mu2, sigma2]
        double g1 = params[0] * std::exp(-0.5 * std::pow((x - params[1]) / params[2], 2));
        double g2 = params[3] * std::exp(-0.5 * std::pow((x - params[4]) / params[5], 2));
        return g1 + g2;
    }
    
    static double objective(const std::vector<double>& params, std::vector<double>& grad, void* data) {
        DoubleGaussianData* dgd = static_cast<DoubleGaussianData*>(data);
        double ssr = 0.0;
        
        for (size_t i = 0; i < dgd->x_data.size(); i++) {
            double predicted = evaluate(params, dgd->x_data[i]);
            double residual = dgd->y_data[i] - predicted;
            ssr += residual * residual;
        }
        
        return ssr;
    }
};

struct BenchmarkResult {
    std::string test_name;
    std::string algorithm;
    double execution_time_ms;
    int function_evaluations;
    double final_value;
    std::vector<double> final_parameters;
    double parameter_error;
    bool converged;
};

class NLoptBenchmark {
private:
    static int function_eval_count;
    
public:
    static void reset_eval_count() { function_eval_count = 0; }
    static int get_eval_count() { return function_eval_count; }
    
    template<typename Func>
    static double counting_wrapper(const std::vector<double>& x, std::vector<double>& grad, void* data) {
        function_eval_count++;
        return reinterpret_cast<Func*>(data)->operator()(x, grad, nullptr);
    }
    
    static BenchmarkResult benchmark_function(
        const std::string& name,
        nlopt::vfunc objective,
        const std::vector<double>& initial_guess,
        const std::vector<double>& expected_solution,
        void* data = nullptr) {
        
        BenchmarkResult result;
        result.test_name = name;
        result.algorithm = "NLopt_NelderMead";
        
        try {
            nlopt::opt opt(nlopt::LN_NELDERMEAD, initial_guess.size());
            opt.set_min_objective(objective, data);
            
            // Set tolerances to match our C# implementation
            opt.set_ftol_rel(1e-8);
            opt.set_xtol_rel(1e-8);
            opt.set_maxeval(10000);
            
            std::vector<double> x = initial_guess;
            double minf;
            
            reset_eval_count();
            auto start = std::chrono::high_resolution_clock::now();
            
            nlopt::result nlopt_result = opt.optimize(x, minf);
            
            auto end = std::chrono::high_resolution_clock::now();
            auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
            
            result.execution_time_ms = duration.count() / 1000.0;
            result.function_evaluations = get_eval_count();
            result.final_value = minf;
            result.final_parameters = x;
            result.converged = (nlopt_result > 0);
            
            // Calculate parameter error
            double max_error = 0.0;
            for (size_t i = 0; i < std::min(x.size(), expected_solution.size()); i++) {
                max_error = std::max(max_error, std::abs(x[i] - expected_solution[i]));
            }
            result.parameter_error = max_error;
            
        } catch (const std::exception& e) {
            result.execution_time_ms = -1;
            result.function_evaluations = -1;
            result.final_value = std::numeric_limits<double>::quiet_NaN();
            result.converged = false;
            result.parameter_error = std::numeric_limits<double>::quiet_NaN();
        }
        
        return result;
    }
    
    static void run_all_benchmarks() {
        std::vector<BenchmarkResult> results;
        
        // Standard mathematical functions
        std::cout << "=== NLopt Real Performance Benchmarks ===" << std::endl;
        std::cout << "Running standard optimization functions:" << std::endl;
        
        // Rosenbrock
        results.push_back(benchmark_function("Rosenbrock", TestFunctions::rosenbrock, 
            {-1.2, 1.0}, {1.0, 1.0}));
        
        // Sphere (5D)
        results.push_back(benchmark_function("Sphere5D", TestFunctions::sphere,
            {1.0, -2.0, 0.5, -1.5, 3.0}, {0.0, 0.0, 0.0, 0.0, 0.0}));
        
        // Booth
        results.push_back(benchmark_function("Booth", TestFunctions::booth,
            {0.0, 0.0}, {1.0, 3.0}));
        
        // Beale
        results.push_back(benchmark_function("Beale", TestFunctions::beale,
            {1.0, 1.0}, {3.0, 0.5}));
        
        // Himmelblau
        results.push_back(benchmark_function("Himmelblau", TestFunctions::himmelblau,
            {0.0, 0.0}, {3.0, 2.0}));
        
        // Powell
        results.push_back(benchmark_function("Powell", TestFunctions::powell,
            {3.0, -1.0, 0.0, 1.0}, {0.0, 0.0, 0.0, 0.0}));
        
        // Double Gaussian fitting
        std::cout << "Running Double Gaussian fitting benchmark:" << std::endl;
        DoubleGaussianData dgData;
        
        // Generate test data (same as C# version)
        std::vector<double> true_params = {1.5, -0.8, 0.6, 1.2, 1.0, 0.4};
        for (int i = 0; i < 500; i++) {
            double x = -3.0 + 6.0 * i / 499.0;
            dgData.x_data.push_back(x);
            
            double clean = DoubleGaussianData::evaluate(true_params, x);
            // Add small amount of noise
            double noise = 0.02 * clean * (((double)rand() / RAND_MAX) - 0.5);
            dgData.y_data.push_back(clean + noise);
        }
        
        std::vector<double> initial_guess = {1.0, 0.5, 0.8, 0.8, 1.5, 0.6};
        results.push_back(benchmark_function("DoubleGaussian", DoubleGaussianData::objective,
            initial_guess, true_params, &dgData));
        
        // Scalability tests
        std::cout << "Running scalability tests:" << std::endl;
        
        // 2D Sphere
        results.push_back(benchmark_function("Sphere2D", TestFunctions::sphere,
            {1.0, 1.0}, {0.0, 0.0}));
        
        // 10D Sphere
        std::vector<double> start_10d(10, 1.0);
        std::vector<double> expected_10d(10, 0.0);
        results.push_back(benchmark_function("Sphere10D", TestFunctions::sphere,
            start_10d, expected_10d));
        
        // 20D Sphere
        std::vector<double> start_20d(20, 1.0);
        std::vector<double> expected_20d(20, 0.0);
        results.push_back(benchmark_function("Sphere20D", TestFunctions::sphere,
            start_20d, expected_20d));
        
        // Print results
        print_results(results);
        save_results_csv(results);
    }
    
private:
    static void print_results(const std::vector<BenchmarkResult>& results) {
        std::cout << "\n=== NLopt Benchmark Results ===" << std::endl;
        std::cout << std::left << std::setw(15) << "Test" 
                  << std::setw(10) << "Time(ms)"
                  << std::setw(10) << "FuncEval"
                  << std::setw(12) << "FinalValue"
                  << std::setw(12) << "ParamError"
                  << std::setw(10) << "Converged" << std::endl;
        std::cout << std::string(80, '-') << std::endl;
        
        for (const auto& result : results) {
            std::cout << std::left << std::setw(15) << result.test_name
                      << std::setw(10) << std::fixed << std::setprecision(1) << result.execution_time_ms
                      << std::setw(10) << result.function_evaluations
                      << std::setw(12) << std::scientific << std::setprecision(2) << result.final_value
                      << std::setw(12) << std::scientific << std::setprecision(2) << result.parameter_error
                      << std::setw(10) << (result.converged ? "YES" : "NO") << std::endl;
        }
    }
    
    static void save_results_csv(const std::vector<BenchmarkResult>& results) {
        std::ofstream file("nlopt_benchmark_results.csv");
        file << "TestName,Algorithm,ExecutionTime_ms,FunctionEvaluations,FinalValue,ParameterError,Converged\n";
        
        for (const auto& result : results) {
            file << result.test_name << ","
                 << result.algorithm << ","
                 << result.execution_time_ms << ","
                 << result.function_evaluations << ","
                 << result.final_value << ","
                 << result.parameter_error << ","
                 << (result.converged ? "true" : "false") << "\n";
        }
        
        file.close();
        std::cout << "\nResults saved to nlopt_benchmark_results.csv" << std::endl;
    }
};

int NLoptBenchmark::function_eval_count = 0;

int main() {
    std::cout << "NLopt Real Performance Benchmark" << std::endl;
    std::cout << "=================================" << std::endl;
    
    // Set random seed for reproducible results
    srand(42);
    
    NLoptBenchmark::run_all_benchmarks();
    
    std::cout << "\nTo compare with C# implementation:" << std::endl;
    std::cout << "1. Run: dotnet run perf > csharp_results.txt" << std::endl;
    std::cout << "2. Compare nlopt_benchmark_results.csv with C# output" << std::endl;
    std::cout << "3. Use the analysis script to generate comparison charts" << std::endl;
    
    return 0;
}