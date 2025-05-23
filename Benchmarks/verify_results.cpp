#include <nlopt.hpp>
#include <iostream>
#include <chrono>
#include <vector>

// Quick verification of key results
double sphere5d(const std::vector<double>& x, std::vector<double>& grad, void* data) {
    double sum = 0;
    for (size_t i = 0; i < x.size(); i++) {
        sum += x[i] * x[i];
    }
    return sum;
}

double rosenbrock(const std::vector<double>& x, std::vector<double>& grad, void* data) {
    double a = 1.0, b = 100.0;
    return (a - x[0]) * (a - x[0]) + b * (x[1] - x[0] * x[0]) * (x[1] - x[0] * x[0]);
}

void benchmark_function(const std::string& name, nlopt::vfunc func, 
                       const std::vector<double>& start, int runs = 10) {
    
    std::vector<double> times;
    
    for (int i = 0; i < runs; i++) {
        nlopt::opt opt(nlopt::LN_NELDERMEAD, start.size());
        opt.set_min_objective(func, nullptr);
        opt.set_ftol_rel(1e-8);
        opt.set_xtol_rel(1e-8);
        opt.set_maxeval(10000);
        
        std::vector<double> x = start;
        double minf;
        
        auto start_time = std::chrono::high_resolution_clock::now();
        nlopt::result result = opt.optimize(x, minf);
        auto end_time = std::chrono::high_resolution_clock::now();
        
        auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end_time - start_time);
        times.push_back(duration.count() / 1000.0); // Convert to milliseconds
    }
    
    // Calculate average
    double avg = 0;
    for (double t : times) avg += t;
    avg /= times.size();
    
    std::cout << name << ": " << avg << " ms (average of " << runs << " runs)" << std::endl;
}

int main() {
    std::cout << "NLopt Verification Benchmark" << std::endl;
    std::cout << "============================" << std::endl;
    
    // Test the functions that showed surprising results
    benchmark_function("Rosenbrock", rosenbrock, {-1.2, 1.0});
    benchmark_function("Sphere5D", sphere5d, {1.0, -2.0, 0.5, -1.5, 3.0});
    
    return 0;
}