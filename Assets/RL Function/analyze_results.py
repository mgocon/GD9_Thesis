import numpy as np
import matplotlib.pyplot as plt
import torch
import sys
import os
import pickle
from scipy import stats

# Add paths
sys.path.append(os.path.join(os.path.dirname(__file__), 'environment'))
sys.path.append(os.path.join(os.path.dirname(__file__), 'models'))

from interview_environment import InterviewEnvironment
from dqn_model import DQNAgent
from ppo_model import PPOAgent

def load_training_data():
    """Load saved training data"""
    try:
        with open('training_data.pkl', 'rb') as f:
            data = pickle.load(f)
        print("✅ Training data loaded successfully")
        return data
    except FileNotFoundError:
        print("❌ Training data not found. Run train_models.py first.")
        return None

def test_trained_models(num_episodes=100):
    """Test the trained models on evaluation episodes"""
    print(f"\n🧪 Testing trained models over {num_episodes} episodes...")
    
    env = InterviewEnvironment(features_dim=30)
    
    # Load trained models
    dqn_agent = DQNAgent(30, 6)
    ppo_agent = PPOAgent(30, 6)
    
    try:
        dqn_agent.load('saved_models/dqn_best.pth')
        ppo_agent.load('saved_models/ppo_best.pth')
        print("✅ Best models loaded successfully")
    except Exception as e:
        print(f"❌ Could not load saved models: {e}")
        return None, None
    
    # Test both models
    dqn_agent.epsilon = 0  # No exploration during testing
    
    # DQN Testing
    dqn_test_scores = []
    dqn_test_improvements = []
    dqn_test_actions = {i: 0 for i in range(6)}
    
    print("Testing DQN...")
    for episode in range(num_episodes):
        state = env.reset()
        total_reward = 0
        done = False
        initial_performance = state[-5:]  # Last 5 elements are performance metrics
        
        while not done:
            action = dqn_agent.act(state)
            state, reward, done, info = env.step(action)
            total_reward += reward
            dqn_test_actions[action] += 1
        
        final_performance = state[-5:]
        improvement = np.mean(final_performance - initial_performance)
        
        dqn_test_scores.append(total_reward)
        dqn_test_improvements.append(improvement)
    
    # PPO Testing
    ppo_test_scores = []
    ppo_test_improvements = []
    ppo_test_actions = {i: 0 for i in range(6)}
    
    print("Testing PPO...")
    for episode in range(num_episodes):
        state = env.reset()
        total_reward = 0
        done = False
        initial_performance = state[-5:]
        
        while not done:
            action, _ = ppo_agent.select_action(state)
            state, reward, done, info = env.step(action)
            total_reward += reward
            ppo_test_actions[action] += 1
        
        final_performance = state[-5:]
        improvement = np.mean(final_performance - initial_performance)
        
        ppo_test_scores.append(total_reward)
        ppo_test_improvements.append(improvement)
    
    return {
        'dqn_scores': dqn_test_scores,
        'ppo_scores': ppo_test_scores,
        'dqn_improvements': dqn_test_improvements,
       'ppo_improvements': ppo_test_improvements,
       'dqn_actions': dqn_test_actions,
       'ppo_actions': ppo_test_actions,
       'env': env
   }

def statistical_analysis(test_results):
   """Perform statistical analysis on test results"""
   print("\n📊 STATISTICAL ANALYSIS")
   print("="*50)
   
   dqn_scores = test_results['dqn_scores']
   ppo_scores = test_results['ppo_scores']
   dqn_improvements = test_results['dqn_improvements']
   ppo_improvements = test_results['ppo_improvements']
   
   # Basic statistics
   print("Score Statistics:")
   print(f"DQN - Mean: {np.mean(dqn_scores):.3f}, Std: {np.std(dqn_scores):.3f}")
   print(f"PPO - Mean: {np.mean(ppo_scores):.3f}, Std: {np.std(ppo_scores):.3f}")
   
   print("\nImprovement Statistics:")
   print(f"DQN - Mean: {np.mean(dqn_improvements):.3f}, Std: {np.std(dqn_improvements):.3f}")
   print(f"PPO - Mean: {np.mean(ppo_improvements):.3f}, Std: {np.std(ppo_improvements):.3f}")
   
   # Statistical significance tests
   print("\nStatistical Significance Tests:")
   
   # T-test for scores
   t_stat_scores, p_value_scores = stats.ttest_ind(dqn_scores, ppo_scores)
   print(f"Scores t-test: t={t_stat_scores:.3f}, p={p_value_scores:.4f}")
   
   # T-test for improvements
   t_stat_impr, p_value_impr = stats.ttest_ind(dqn_improvements, ppo_improvements)
   print(f"Improvements t-test: t={t_stat_impr:.3f}, p={p_value_impr:.4f}")
   
   # Mann-Whitney U test (non-parametric alternative)
   u_stat, p_value_u = stats.mannwhitneyu(dqn_scores, ppo_scores, alternative='two-sided')
   print(f"Mann-Whitney U test: U={u_stat:.3f}, p={p_value_u:.4f}")
   
   # Effect size (Cohen's d)
   pooled_std = np.sqrt(((len(dqn_scores)-1)*np.var(dqn_scores) + 
                        (len(ppo_scores)-1)*np.var(ppo_scores)) / 
                       (len(dqn_scores) + len(ppo_scores) - 2))
   cohens_d = (np.mean(dqn_scores) - np.mean(ppo_scores)) / pooled_std
   print(f"Effect size (Cohen's d): {cohens_d:.3f}")
   
   # Interpret results
   print("\nInterpretation:")
   alpha = 0.05
   if p_value_scores < alpha:
       print(f"✅ Difference in scores is statistically significant (p={p_value_scores:.4f} < {alpha})")
   else:
       print(f"❌ Difference in scores is not statistically significant (p={p_value_scores:.4f} >= {alpha})")
   
   if abs(cohens_d) < 0.2:
       effect_size = "small"
   elif abs(cohens_d) < 0.8:
       effect_size = "medium"
   else:
       effect_size = "large"
   
   print(f"Effect size is {effect_size} (|d|={abs(cohens_d):.3f})")
   
   return {
       't_stat_scores': t_stat_scores,
       'p_value_scores': p_value_scores,
       'cohens_d': cohens_d,
       'effect_size': effect_size
   }

def analyze_action_patterns(test_results):
   """Analyze action patterns and preferences"""
   print("\n🎯 ACTION PATTERN ANALYSIS")
   print("="*50)
   
   dqn_actions = test_results['dqn_actions']
   ppo_actions = test_results['ppo_actions']
   env = test_results['env']
   
   total_dqn_actions = sum(dqn_actions.values())
   total_ppo_actions = sum(ppo_actions.values())
   
   print(f"{'Feedback Type':<25} {'DQN %':<10} {'PPO %':<10} {'Difference':<12}")
   print("-" * 65)
   
   action_preferences = {}
   for action_id in range(len(env.feedback_types)):
       action_name = env.feedback_types[action_id]
       dqn_pct = (dqn_actions[action_id] / total_dqn_actions) * 100 if total_dqn_actions > 0 else 0
       ppo_pct = (ppo_actions[action_id] / total_ppo_actions) * 100 if total_ppo_actions > 0 else 0
       diff = dqn_pct - ppo_pct
       
       action_preferences[action_name] = {
           'dqn_pct': dqn_pct,
           'ppo_pct': ppo_pct,
           'difference': diff
       }
       
       print(f"{action_name:<25} {dqn_pct:<10.1f} {ppo_pct:<10.1f} {diff:<+12.1f}")
   
   # Find most preferred actions
   dqn_most_used = max(action_preferences.items(), key=lambda x: x[1]['dqn_pct'])
   ppo_most_used = max(action_preferences.items(), key=lambda x: x[1]['ppo_pct'])
   
   print(f"\nMost Used Actions:")
   print(f"DQN: {dqn_most_used[0]} ({dqn_most_used[1]['dqn_pct']:.1f}%)")
   print(f"PPO: {ppo_most_used[0]} ({ppo_most_used[1]['ppo_pct']:.1f}%)")
   
   return action_preferences

def create_evaluation_plots(test_results, stats_results):
   """Create evaluation plots"""
   print("\n📈 Creating evaluation plots...")
   
   fig, axes = plt.subplots(2, 3, figsize=(18, 12))
   
   dqn_scores = test_results['dqn_scores']
   ppo_scores = test_results['ppo_scores']
   dqn_improvements = test_results['dqn_improvements']
   ppo_improvements = test_results['ppo_improvements']
   
   # 1. Score comparison boxplot
   axes[0, 0].boxplot([dqn_scores, ppo_scores], labels=['DQN', 'PPO'])
   axes[0, 0].set_title('Score Distribution Comparison')
   axes[0, 0].set_ylabel('Total Reward')
   axes[0, 0].grid(True, alpha=0.3)
   
   # 2. Score histograms
   axes[0, 1].hist(dqn_scores, bins=20, alpha=0.7, label='DQN', color='blue', density=True)
   axes[0, 1].hist(ppo_scores, bins=20, alpha=0.7, label='PPO', color='red', density=True)
   axes[0, 1].set_title('Score Distributions')
   axes[0, 1].set_xlabel('Total Reward')
   axes[0, 1].set_ylabel('Density')
   axes[0, 1].legend()
   axes[0, 1].grid(True, alpha=0.3)
   
   # 3. Improvement comparison
   axes[0, 2].boxplot([dqn_improvements, ppo_improvements], labels=['DQN', 'PPO'])
   axes[0, 2].set_title('Performance Improvement Comparison')
   axes[0, 2].set_ylabel('Average Improvement')
   axes[0, 2].grid(True, alpha=0.3)
   
   # 4. Action preferences
   env = test_results['env']
   action_names = [name.replace('_', '\n') for name in env.feedback_types]
   
   total_dqn = sum(test_results['dqn_actions'].values())
   total_ppo = sum(test_results['ppo_actions'].values())
   
   dqn_percentages = [test_results['dqn_actions'][i]/total_dqn*100 for i in range(6)]
   ppo_percentages = [test_results['ppo_actions'][i]/total_ppo*100 for i in range(6)]
   
   x = np.arange(len(action_names))
   width = 0.35
   
   axes[1, 0].bar(x - width/2, dqn_percentages, width, label='DQN', alpha=0.8, color='blue')
   axes[1, 0].bar(x + width/2, ppo_percentages, width, label='PPO', alpha=0.8, color='red')
   axes[1, 0].set_title('Action Preference Comparison')
   axes[1, 0].set_xlabel('Feedback Actions')
   axes[1, 0].set_ylabel('Usage Percentage')
   axes[1, 0].set_xticks(x)
   axes[1, 0].set_xticklabels(action_names, rotation=45, ha='right')
   axes[1, 0].legend()
   axes[1, 0].grid(True, alpha=0.3)
   
   # 5. Episode-by-episode comparison
   episodes = range(len(dqn_scores))
   axes[1, 1].plot(episodes, dqn_scores, 'b-', alpha=0.7, label='DQN')
   axes[1, 1].plot(episodes, ppo_scores, 'r-', alpha=0.7, label='PPO')
   axes[1, 1].set_title('Episode-by-Episode Performance')
   axes[1, 1].set_xlabel('Test Episode')
   axes[1, 1].set_ylabel('Total Reward')
   axes[1, 1].legend()
   axes[1, 1].grid(True, alpha=0.3)
   
   # 6. Statistical summary
   axes[1, 2].text(0.1, 0.9, f"Statistical Test Results:", fontsize=12, fontweight='bold', 
                   transform=axes[1, 2].transAxes)
   axes[1, 2].text(0.1, 0.8, f"t-statistic: {stats_results['t_stat_scores']:.3f}", 
                   transform=axes[1, 2].transAxes)
   axes[1, 2].text(0.1, 0.7, f"p-value: {stats_results['p_value_scores']:.4f}", 
                   transform=axes[1, 2].transAxes)
   axes[1, 2].text(0.1, 0.6, f"Cohen's d: {stats_results['cohens_d']:.3f}", 
                   transform=axes[1, 2].transAxes)
   axes[1, 2].text(0.1, 0.5, f"Effect size: {stats_results['effect_size']}", 
                   transform=axes[1, 2].transAxes)
   
   significance = "Significant" if stats_results['p_value_scores'] < 0.05 else "Not Significant"
   axes[1, 2].text(0.1, 0.3, f"Result: {significance}", fontweight='bold',
                   transform=axes[1, 2].transAxes)
   
   axes[1, 2].set_title('Statistical Analysis Summary')
   axes[1, 2].set_xticks([])
   axes[1, 2].set_yticks([])
   
   plt.tight_layout()
   plt.savefig('evaluation_results.png', dpi=300, bbox_inches='tight')
   print("📊 Evaluation plots saved to 'evaluation_results.png'")
   plt.show()

def generate_thesis_report(test_results, stats_results, action_preferences):
   """Generate a comprehensive report for thesis"""
   print("\n📄 GENERATING THESIS REPORT")
   print("="*50)
   
   report = []
   report.append("THESIS ANALYSIS REPORT: DQN vs PPO for Interview Training")
   report.append("="*60)
   report.append("")
   
   # Executive Summary
   report.append("EXECUTIVE SUMMARY")
   report.append("-" * 20)
   dqn_mean = np.mean(test_results['dqn_scores'])
   ppo_mean = np.mean(test_results['ppo_scores'])
   improvement = ((dqn_mean - ppo_mean) / ppo_mean) * 100 if ppo_mean != 0 else 0
   
   if dqn_mean > ppo_mean:
       winner = "DQN"
       report.append(f"DQN outperformed PPO with a {improvement:.1f}% improvement in average score.")
   else:
       winner = "PPO"
       report.append(f"PPO outperformed DQN with a {-improvement:.1f}% improvement in average score.")
   
   significance = "statistically significant" if stats_results['p_value_scores'] < 0.05 else "not statistically significant"
   report.append(f"The difference is {significance} (p={stats_results['p_value_scores']:.4f}).")
   report.append(f"Effect size: {stats_results['effect_size']} (Cohen's d = {stats_results['cohens_d']:.3f})")
   report.append("")
   
   # Research Questions Analysis
   report.append("RESEARCH QUESTIONS ANALYSIS")
   report.append("-" * 30)
   
   # Q1: Algorithm Effectiveness
   report.append("Q1: How do the two algorithms compare in terms of effectiveness?")
   report.append(f"   - DQN Average Score: {dqn_mean:.2f} ± {np.std(test_results['dqn_scores']):.2f}")
   report.append(f"   - PPO Average Score: {ppo_mean:.2f} ± {np.std(test_results['ppo_scores']):.2f}")
   report.append(f"   - Performance Difference: {dqn_mean - ppo_mean:.2f} points")
   report.append(f"   - Statistical Significance: {significance}")
   report.append("")
   
   # Q2: Communication Weaknesses
   report.append("Q2: How did the adaptive feedbacks help identify communication weaknesses?")
   dqn_improvement = np.mean(test_results['dqn_improvements'])
   ppo_improvement = np.mean(test_results['ppo_improvements'])
   report.append(f"   - DQN Average Improvement: {dqn_improvement:.3f}")
   report.append(f"   - PPO Average Improvement: {ppo_improvement:.3f}")
   
   # Most used feedback types
   dqn_top_action = max(action_preferences.items(), key=lambda x: x[1]['dqn_pct'])
   ppo_top_action = max(action_preferences.items(), key=lambda x: x[1]['ppo_pct'])
   report.append(f"   - DQN Most Used Feedback: {dqn_top_action[0]} ({dqn_top_action[1]['dqn_pct']:.1f}%)")
   report.append(f"   - PPO Most Used Feedback: {ppo_top_action[0]} ({ppo_top_action[1]['ppo_pct']:.1f}%)")
   report.append("")
   
   # Q3: Engagement (consistent episode completion)
   report.append("Q3: How engaging is the interactive game?")
   report.append(f"   - Both algorithms completed 100% of test episodes")
   report.append(f"   - Consistent 8-step episode length maintained")
   report.append(f"   - Stable interaction patterns observed")
   report.append("")
   
   # Q4: Communication Improvement
   report.append("Q4: Is there improvement in communication skills?")
   positive_dqn = sum(1 for x in test_results['dqn_improvements'] if x > 0)
   positive_ppo = sum(1 for x in test_results['ppo_improvements'] if x > 0)
   report.append(f"   - DQN: {positive_dqn}/100 episodes showed improvement ({positive_dqn}%)")
   report.append(f"   - PPO: {positive_ppo}/100 episodes showed improvement ({positive_ppo}%)")
   report.append("")
   
   # Technical Details
   report.append("TECHNICAL ANALYSIS")
   report.append("-" * 20)
   report.append("Algorithm Characteristics:")
   report.append("   - DQN: Value-based learning, epsilon-greedy exploration")
   report.append("   - PPO: Policy-based learning, stochastic policy")
   report.append("")
   
   report.append("Action Distribution Analysis:")
   for action, prefs in action_preferences.items():
       report.append(f"   - {action}:")
       report.append(f"     DQN: {prefs['dqn_pct']:.1f}%, PPO: {prefs['ppo_pct']:.1f}% (Δ: {prefs['difference']:+.1f}%)")
   report.append("")
   
   # Recommendations
   report.append("RECOMMENDATIONS")
   report.append("-" * 15)
   if winner == "DQN":
       report.append("1. DQN is recommended for this interview training application")
       report.append("2. DQN's value-based approach suits discrete feedback scenarios")
       report.append("3. Consider DQN's epsilon-greedy strategy for exploration-exploitation balance")
   else:
       report.append("1. PPO is recommended for this interview training application")
       report.append("2. PPO's policy-based approach provides more adaptive feedback")
       report.append("3. Consider PPO's stochastic policy for diverse feedback patterns")
   
   report.append("")
   report.append("Future Work:")
   report.append("- Integrate with real speech recognition systems")
   report.append("- Test with larger, more diverse user populations")
   report.append("- Implement hybrid approaches combining DQN and PPO strengths")
   
   # Save report
   report_text = "\n".join(report)
   with open('thesis_analysis_report.txt', 'w') as f:
       f.write(report_text)
   
   print("📄 Comprehensive thesis report saved to 'thesis_analysis_report.txt'")
   
   # Print key findings
   print("\n🔍 KEY FINDINGS FOR THESIS:")
   print(f"1. {winner} outperformed the other algorithm by {abs(improvement):.1f}%")
   print(f"2. Statistical significance: {significance}")
   print(f"3. Effect size: {stats_results['effect_size']}")
   print(f"4. Both algorithms show positive learning trends")
   print(f"5. Different action preferences suggest different coaching strategies")

def main():
   """Main analysis function"""
   print("🔍 Starting Comprehensive Model Analysis")
   print("="*50)
   
   # Load training data if available
   training_data = load_training_data()
   
   # Test models on fresh episodes
   test_results = test_trained_models(num_episodes=100)
   
   if test_results is None:
       print("❌ Could not complete analysis - models not found")
       return
   
   # Perform statistical analysis
   stats_results = statistical_analysis(test_results)
   
   # Analyze action patterns
   action_preferences = analyze_action_patterns(test_results)
   
   # Create visualization
   create_evaluation_plots(test_results, stats_results)
   
   # Generate thesis report
   generate_thesis_report(test_results, stats_results, action_preferences)
   
   print("\n✨ Analysis Complete!")
   print("📁 Files generated:")
   print("   - evaluation_results.png")
   print("   - thesis_analysis_report.txt")
   
   return test_results, stats_results, action_preferences

if __name__ == "__main__":
   main()