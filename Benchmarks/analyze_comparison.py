#!/usr/bin/env python3
"""
Real performance comparison analysis between NLopt and C# implementation
"""

import csv
import re
import json
from pathlib import Path
import matplotlib.pyplot as plt
import numpy as np
from typing import Dict, List, Tuple

class PerformanceAnalyzer:
    def __init__(self):
        self.nlopt_results = {}
        self.csharp_results = {}
        
    def parse_nlopt_csv(self, csv_file: str = "nlopt_benchmark_results.csv"):
        """Parse NLopt benchmark results from CSV"""
        if not Path(csv_file).exists():
            print(f"Warning: {csv_file} not found. Run NLopt benchmarks first.")
            return
            
        with open(csv_file, 'r') as f:
            reader = csv.DictReader(f)
            for row in reader:
                test_name = row['TestName']
                self.nlopt_results[test_name] = {
                    'time_ms': float(row['ExecutionTime_ms']),
                    'func_evals': int(row['FunctionEvaluations']),
                    'final_value': float(row['FinalValue']),
                    'param_error': float(row['ParameterError']),
                    'converged': row['Converged'].lower() == 'true',
                    'algorithm': row['Algorithm']
                }
    
    def parse_csharp_output(self, output_file: str = "csharp_results.txt"):
        """Parse C# benchmark output"""
        if not Path(output_file).exists():
            print(f"Warning: {output_file} not found. Run C# benchmarks first.")
            return
            
        with open(output_file, 'r') as f:
            content = f.read()
        
        # Parse the detailed results table
        pattern = r'(\w+)\s+(Ours \(\w+\)|\w+ \(\w+\))\s+([\d.]+)\s+(\d+)\s+(\d+)\s+([\d.E+-]+)\s+(\w+)'
        matches = re.findall(pattern, content)
        
        for match in matches:
            test_name, implementation, time_ms, iterations, func_evals, param_error, status = match
            
            if implementation.startswith('Ours'):
                variant = 'Standard' if 'Standard' in implementation else 'Optimized'
                key = f"{test_name}_{variant}"
                
                self.csharp_results[key] = {
                    'time_ms': float(time_ms),
                    'func_evals': int(func_evals),
                    'iterations': int(iterations),
                    'param_error': float(param_error),
                    'converged': status == 'CONVERGED',
                    'implementation': implementation
                }
    
    def generate_comparison_report(self):
        """Generate comprehensive comparison report"""
        report = []
        report.append("# Real NLopt vs C# Performance Comparison")
        report.append("=" * 50)
        report.append("")
        
        # Check if we have data
        if not self.nlopt_results or not self.csharp_results:
            report.append("⚠️  **Incomplete Data**")
            report.append("")
            if not self.nlopt_results:
                report.append("- NLopt results missing. Install NLopt and run: `make nlopt_benchmark && ./nlopt_benchmark`")
            if not self.csharp_results:
                report.append("- C# results missing. Run: `dotnet run perf > Benchmarks/csharp_results.txt`")
            report.append("")
            return "\n".join(report)
        
        # Performance comparison table
        report.append("## Performance Comparison Results")
        report.append("")
        report.append("| Test Function | NLopt Time(ms) | C# Std Time(ms) | C# Opt Time(ms) | Speedup Ratio | Accuracy Comparison |")
        report.append("|---------------|----------------|-----------------|-----------------|---------------|---------------------|")
        
        common_tests = set()
        for nlopt_test in self.nlopt_results.keys():
            if f"{nlopt_test}_Standard" in self.csharp_results:
                common_tests.add(nlopt_test)
        
        total_nlopt_time = 0
        total_csharp_std_time = 0
        total_csharp_opt_time = 0
        
        for test in sorted(common_tests):
            nlopt = self.nlopt_results[test]
            csharp_std = self.csharp_results.get(f"{test}_Standard", {})
            csharp_opt = self.csharp_results.get(f"{test}_Optimized", {})
            
            nlopt_time = nlopt['time_ms']
            csharp_std_time = csharp_std.get('time_ms', float('nan'))
            csharp_opt_time = csharp_opt.get('time_ms', float('nan'))
            
            total_nlopt_time += nlopt_time
            total_csharp_std_time += csharp_std_time if not np.isnan(csharp_std_time) else 0
            total_csharp_opt_time += csharp_opt_time if not np.isnan(csharp_opt_time) else 0
            
            # Calculate speedup ratios
            std_ratio = nlopt_time / csharp_std_time if not np.isnan(csharp_std_time) and csharp_std_time > 0 else float('nan')
            opt_ratio = nlopt_time / csharp_opt_time if not np.isnan(csharp_opt_time) and csharp_opt_time > 0 else float('nan')
            
            # Accuracy comparison
            nlopt_error = nlopt['param_error']
            csharp_error = csharp_std.get('param_error', float('nan'))
            acc_ratio = nlopt_error / csharp_error if not np.isnan(csharp_error) and csharp_error > 0 else float('nan')
            
            report.append(f"| {test:<13} | {nlopt_time:>13.1f} | {csharp_std_time:>14.1f} | {csharp_opt_time:>14.1f} | {opt_ratio:>12.2f}x | {acc_ratio:>18.2f}x |")
        
        # Summary statistics
        overall_std_ratio = total_nlopt_time / total_csharp_std_time if total_csharp_std_time > 0 else float('nan')
        overall_opt_ratio = total_nlopt_time / total_csharp_opt_time if total_csharp_opt_time > 0 else float('nan')
        
        report.append("")
        report.append("## Summary Statistics")
        report.append("")
        report.append(f"**Overall Performance Ratios** (NLopt time / C# time):")
        report.append(f"- C# Standard: {overall_std_ratio:.2f}x (NLopt is {1/overall_std_ratio:.1f}x faster)" if not np.isnan(overall_std_ratio) else "- C# Standard: Data unavailable")
        report.append(f"- C# Optimized: {overall_opt_ratio:.2f}x (NLopt is {1/overall_opt_ratio:.1f}x faster)" if not np.isnan(overall_opt_ratio) else "- C# Optimized: Data unavailable")
        report.append("")
        
        # Function evaluation efficiency
        report.append("## Function Evaluation Efficiency")
        report.append("")
        nlopt_total_evals = sum(r['func_evals'] for r in self.nlopt_results.values())
        csharp_total_evals = sum(r['func_evals'] for r in self.csharp_results.values() if '_Standard' in r)
        
        if csharp_total_evals > 0:
            eval_ratio = nlopt_total_evals / csharp_total_evals
            report.append(f"- NLopt total function evaluations: {nlopt_total_evals:,}")
            report.append(f"- C# total function evaluations: {csharp_total_evals:,}")
            report.append(f"- Evaluation efficiency ratio: {eval_ratio:.2f}x")
            if eval_ratio < 1.0:
                report.append(f"  * ✅ C# uses {(1-eval_ratio)*100:.1f}% fewer function evaluations")
            else:
                report.append(f"  * ❌ C# uses {(eval_ratio-1)*100:.1f}% more function evaluations")
        
        report.append("")
        
        # Detailed analysis
        report.append("## Detailed Analysis")
        report.append("")
        
        fastest_nlopt = []
        fastest_csharp = []
        
        for test in sorted(common_tests):
            nlopt = self.nlopt_results[test]
            csharp_opt = self.csharp_results.get(f"{test}_Optimized", {})
            
            if csharp_opt and nlopt['time_ms'] < csharp_opt['time_ms']:
                fastest_nlopt.append(test)
            elif csharp_opt and csharp_opt['time_ms'] < nlopt['time_ms']:
                fastest_csharp.append(test)
        
        if fastest_nlopt:
            report.append(f"**NLopt Faster On**: {', '.join(fastest_nlopt)}")
        if fastest_csharp:
            report.append(f"**C# Faster On**: {', '.join(fastest_csharp)}")
        
        report.append("")
        report.append("## Key Findings")
        report.append("")
        
        if not np.isnan(overall_opt_ratio):
            if overall_opt_ratio > 0.8:
                report.append("✅ **Competitive Performance**: C# optimized version achieves >80% of NLopt performance")
            elif overall_opt_ratio > 0.5:
                report.append("⚠️  **Reasonable Performance**: C# achieves 50-80% of NLopt performance")
            else:
                report.append("❌ **Performance Gap**: C# significantly slower than NLopt")
        
        # Hardware and environment info
        report.append("")
        report.append("## Test Environment")
        report.append("")
        report.append("- **Hardware**: Same system for both benchmarks")
        report.append("- **NLopt**: Native C++ implementation with Nelder-Mead")
        report.append("- **C#**: .NET 9.0 JIT-compiled implementation")
        report.append("- **Compiler**: g++ -O3 -march=native for NLopt")
        report.append("- **Convergence**: Same tolerance settings (1e-8)")
        
        return "\n".join(report)
    
    def create_performance_chart(self):
        """Create performance comparison charts"""
        try:
            import matplotlib.pyplot as plt
            
            # Find common tests
            common_tests = []
            nlopt_times = []
            csharp_std_times = []
            csharp_opt_times = []
            
            for nlopt_test in self.nlopt_results.keys():
                if f"{nlopt_test}_Standard" in self.csharp_results:
                    common_tests.append(nlopt_test)
                    nlopt_times.append(self.nlopt_results[nlopt_test]['time_ms'])
                    csharp_std_times.append(self.csharp_results[f"{nlopt_test}_Standard"]['time_ms'])
                    
                    opt_key = f"{nlopt_test}_Optimized"
                    if opt_key in self.csharp_results:
                        csharp_opt_times.append(self.csharp_results[opt_key]['time_ms'])
                    else:
                        csharp_opt_times.append(csharp_std_times[-1])
            
            if not common_tests:
                print("No common tests found for charting")
                return
            
            # Create bar chart
            x = np.arange(len(common_tests))
            width = 0.25
            
            fig, ax = plt.subplots(figsize=(12, 8))
            
            bars1 = ax.bar(x - width, nlopt_times, width, label='NLopt (C++)', color='blue', alpha=0.7)
            bars2 = ax.bar(x, csharp_std_times, width, label='C# Standard', color='orange', alpha=0.7)
            bars3 = ax.bar(x + width, csharp_opt_times, width, label='C# Optimized', color='green', alpha=0.7)
            
            ax.set_xlabel('Test Functions')
            ax.set_ylabel('Execution Time (ms)')
            ax.set_title('Performance Comparison: NLopt vs C# Implementation')
            ax.set_xticks(x)
            ax.set_xticklabels(common_tests, rotation=45, ha='right')
            ax.legend()
            ax.set_yscale('log')  # Log scale for better visualization
            
            # Add value labels on bars
            def add_labels(bars):
                for bar in bars:
                    height = bar.get_height()
                    ax.annotate(f'{height:.1f}',
                               xy=(bar.get_x() + bar.get_width() / 2, height),
                               xytext=(0, 3),
                               textcoords="offset points",
                               ha='center', va='bottom', fontsize=8)
            
            add_labels(bars1)
            add_labels(bars2)
            add_labels(bars3)
            
            plt.tight_layout()
            plt.savefig('performance_comparison.png', dpi=300, bbox_inches='tight')
            plt.close()
            
            print("📊 Performance chart saved as performance_comparison.png")
            
        except ImportError:
            print("Matplotlib not available. Install with: pip install matplotlib")
    
    def run_analysis(self):
        """Run complete analysis"""
        print("🔍 Analyzing benchmark results...")
        
        self.parse_nlopt_csv()
        self.parse_csharp_output()
        
        # Generate report
        report = self.generate_comparison_report()
        
        # Save report
        with open('REAL_PERFORMANCE_COMPARISON.md', 'w') as f:
            f.write(report)
        
        print("📝 Analysis complete!")
        print("📄 Report saved as: REAL_PERFORMANCE_COMPARISON.md")
        
        # Create chart if possible
        self.create_performance_chart()
        
        # Print summary to console
        print("\n" + "="*60)
        print("QUICK SUMMARY")
        print("="*60)
        
        if self.nlopt_results and self.csharp_results:
            # Calculate quick stats
            common_tests = set()
            for nlopt_test in self.nlopt_results.keys():
                if f"{nlopt_test}_Optimized" in self.csharp_results:
                    common_tests.add(nlopt_test)
            
            if common_tests:
                ratios = []
                for test in common_tests:
                    nlopt_time = self.nlopt_results[test]['time_ms']
                    csharp_time = self.csharp_results[f"{test}_Optimized"]['time_ms']
                    if csharp_time > 0:
                        ratios.append(nlopt_time / csharp_time)
                
                if ratios:
                    avg_ratio = np.mean(ratios)
                    print(f"✅ Tested {len(common_tests)} functions on same hardware")
                    print(f"📊 C# Optimized achieves {avg_ratio:.1%} of NLopt performance")
                    print(f"⚡ NLopt is {1/avg_ratio:.1f}x faster on average")
                else:
                    print("❌ Unable to calculate performance ratios")
            else:
                print("❌ No matching test cases found")
        else:
            print("❌ Incomplete benchmark data")
            if not self.nlopt_results:
                print("   Missing NLopt results - run: make install_nlopt && make nlopt_benchmark && ./nlopt_benchmark")
            if not self.csharp_results:
                print("   Missing C# results - run: dotnet run perf > Benchmarks/csharp_results.txt")

if __name__ == "__main__":
    analyzer = PerformanceAnalyzer()
    analyzer.run_analysis()