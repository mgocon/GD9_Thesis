import numpy as np
import matplotlib.pyplot as plt
import sys
import os
import time
from datetime import datetime

# Add paths for imports
sys.path.append(os.path.join(os.path.dirname(__file__), 'environment'))
sys.path.append(os.path.join(os.path.dirname(__file__), 'models'))
sys.path.append(os.path.join(os.path.dirname(__file__), 'data_preprocessing'))

from interview_environment import InterviewEnvironment
from dqn_model import DQNAgent
from ppo_model import PPOAgent

def train_dqn(env, episodes=1000, save_interval=200):
    """Train DQN agent with detailed logging"""
    state_dim = env.observation_space.shape[0]
    action_dim = env.action_space.n
    
    print(f"🤖 Training DQN - State dim: {state_dim}, Action dim: {action_dim}")
    
    agent = DQNAgent(state_dim, action_dim)
    scores = []
    best_score = float('-inf')
    episode_lengths = []
    action_counts = {i: 0 for i in range(action_dim)}
    
    start_time = time.time()
    
    for episode in range(episodes):
        state = env.reset()
        total_reward = 0
        done = False
        step_count = 0
        episode_actions = []
        
        while not done:
            action = agent.act(state)
            next_state, reward, done, info = env.step(action)
            agent.remember(state, action, reward, next_state, done)
            
            state = next_state
            total_reward += reward
            step_count += 1
            episode_actions.append(action)
            action_counts[action] += 1
            
            # Train the agent
            if len(agent.memory) > agent.batch_size:
                agent.replay()
        
        scores.append(total_reward)
        episode_lengths.append(step_count)
        
        # Update target network periodically
        if episode % 100 == 0:
            agent.update_target_network()
        
        # Save best model
        if total_reward > best_score:
            best_score = total_reward
            os.makedirs('saved_models', exist_ok=True)
            agent.save('saved_models/dqn_best.pth')
        
        # Progress reporting
        if episode % 100 == 0:
            avg_score = np.mean(scores[-100:]) if len(scores) >= 100 else np.mean(scores)
            avg_length = np.mean(episode_lengths[-100:]) if len(episode_lengths) >= 100 else np.mean(episode_lengths)
            elapsed_time = time.time() - start_time
            
            print(f"\nDQN Episode {episode}/{episodes}")
            print(f"  Score: {total_reward:.2f}, Avg Score: {avg_score:.2f}")
            print(f"  Epsilon: {agent.epsilon:.3f}, Avg Length: {avg_length:.1f}")
            print(f"  Best Score: {best_score:.2f}, Time: {elapsed_time:.1f}s")
            
            # Show action distribution
            total_actions = sum(action_counts.values())
            if total_actions > 0:
                print("  Action Distribution:")
                for action_id, count in action_counts.items():
                    action_name = env.feedback_types[action_id]
                    percentage = (count / total_actions) * 100
                    print(f"    {action_name}: {percentage:.1f}%")
    
    training_time = time.time() - start_time
    print(f"\n✅ DQN Training completed in {training_time:.1f} seconds")
    
    return agent, scores, episode_lengths, action_counts

def train_ppo(env, episodes=1000):
    """Train PPO agent with detailed logging"""
    state_dim = env.observation_space.shape[0]
    action_dim = env.action_space.n
    
    print(f"\n🧠 Training PPO - State dim: {state_dim}, Action dim: {action_dim}")
    
    agent = PPOAgent(state_dim, action_dim)
    scores = []
    best_score = float('-inf')
    episode_lengths = []
    action_counts = {i: 0 for i in range(action_dim)}
    
    start_time = time.time()
    
    for episode in range(episodes):
        state = env.reset()
        total_reward = 0
        done = False
        step_count = 0
        episode_actions = []
        
        while not done:
            action, action_prob = agent.select_action(state)
            next_state, reward, done, info = env.step(action)
            agent.store_transition(state, action, reward, next_state, done, action_prob)
            
            state = next_state
            total_reward += reward
            step_count += 1
            episode_actions.append(action)
            action_counts[action] += 1
        
        # Update policy after each episode
        agent.update()
        scores.append(total_reward)
        episode_lengths.append(step_count)
        
        # Save best model
        if total_reward > best_score:
            best_score = total_reward
            os.makedirs('saved_models', exist_ok=True)
            agent.save('saved_models/ppo_best.pth')
        
        # Progress reporting
        if episode % 100 == 0:
            avg_score = np.mean(scores[-100:]) if len(scores) >= 100 else np.mean(scores)
            avg_length = np.mean(episode_lengths[-100:]) if len(episode_lengths) >= 100 else np.mean(episode_lengths)
            elapsed_time = time.time() - start_time
            
            print(f"\nPPO Episode {episode}/{episodes}")
            print(f"  Score: {total_reward:.2f}, Avg Score: {avg_score:.2f}")
            print(f"  Avg Length: {avg_length:.1f}, Best Score: {best_score:.2f}")
            print(f"  Time: {elapsed_time:.1f}s")
            
            # Show training stats
            stats = agent.get_training_stats()
            print(f"  Policy Loss: {stats['avg_policy_loss']:.4f}")
            print(f"  Value Loss: {stats['avg_value_loss']:.4f}")
            print(f"  Entropy: {stats['avg_entropy']:.4f}")
            
            # Show action distribution
            total_actions = sum(action_counts.values())
            if total_actions > 0:
                print("  Action Distribution:")
                for action_id, count in action_counts.items():
                    action_name = env.feedback_types[action_id]
                    percentage = (count / total_actions) * 100
                    print(f"    {action_name}: {percentage:.1f}%")
    
    training_time = time.time() - start_time
    print(f"\n✅ PPO Training completed in {training_time:.1f} seconds")
    
    return agent, scores, episode_lengths, action_counts

def compare_models(dqn_scores, ppo_scores, dqn_actions, ppo_actions, env):
    """Compare the performance of DQN and PPO"""
    print("\n" + "="*60)
    print("MODEL COMPARISON RESULTS")
    print("="*60)
    
    # Calculate statistics
    dqn_final_avg = np.mean(dqn_scores[-100:])
    ppo_final_avg = np.mean(ppo_scores[-100:])
    
    dqn_max = np.max(dqn_scores)
    ppo_max = np.max(ppo_scores)
    
    dqn_std = np.std(dqn_scores[-100:])
    ppo_std = np.std(ppo_scores[-100:])
    
    dqn_median = np.median(dqn_scores[-100:])
    ppo_median = np.median(ppo_scores[-100:])
    
    print(f"DQN Performance:")
    print(f"  Final Average (last 100): {dqn_final_avg:.2f} ± {dqn_std:.2f}")
    print(f"  Median: {dqn_median:.2f}")
    print(f"  Maximum Score: {dqn_max:.2f}")
    
    print(f"\nPPO Performance:")
    print(f"  Final Average (last 100): {ppo_final_avg:.2f} ± {ppo_std:.2f}")
    print(f"  Median: {ppo_median:.2f}")
    print(f"  Maximum Score: {ppo_max:.2f}")
    
    print(f"\nComparison:")
    improvement = dqn_final_avg - ppo_final_avg
    improvement_pct = (improvement / ppo_final_avg) * 100 if ppo_final_avg != 0 else 0
    
    if dqn_final_avg > ppo_final_avg:
        print(f"🏆 DQN performs better by {improvement:.2f} points ({improvement_pct:.1f}%)")
    else:
        print(f"🏆 PPO performs better by {-improvement:.2f} points ({-improvement_pct:.1f}%)")
    
    # Action preference analysis
    print(f"\nAction Preference Analysis:")
    total_dqn_actions = sum(dqn_actions.values())
    total_ppo_actions = sum(ppo_actions.values())
    
    print(f"{'Action':<25} {'DQN %':<10} {'PPO %':<10} {'Difference':<12}")
    print("-" * 60)
    
    for action_id in range(len(env.feedback_types)):
        action_name = env.feedback_types[action_id]
        dqn_pct = (dqn_actions[action_id] / total_dqn_actions) * 100 if total_dqn_actions > 0 else 0
        ppo_pct = (ppo_actions[action_id] / total_ppo_actions) * 100 if total_ppo_actions > 0 else 0
        diff = dqn_pct - ppo_pct
        
        print(f"{action_name:<25} {dqn_pct:<10.1f} {ppo_pct:<10.1f} {diff:<+12.1f}")

def create_comprehensive_plots(dqn_scores, ppo_scores, dqn_actions, ppo_actions, env):
    """Create comprehensive visualization of training results"""
    plt.style.use('default')
    fig = plt.figure(figsize=(20, 12))
    
    # 1. Training Progress Comparison
    plt.subplot(2, 4, 1)
    window = 50
    dqn_smoothed = [np.mean(dqn_scores[max(0, i-window):i+1]) for i in range(len(dqn_scores))]
    ppo_smoothed = [np.mean(ppo_scores[max(0, i-window):i+1]) for i in range(len(ppo_scores))]
    
    plt.plot(dqn_scores, alpha=0.3, color='blue', label='DQN Raw')
    plt.plot(dqn_smoothed, color='blue', linewidth=2, label='DQN Smoothed')
    plt.plot(ppo_scores, alpha=0.3, color='red', label='PPO Raw')
    plt.plot(ppo_smoothed, color='red', linewidth=2, label='PPO Smoothed')
    
    plt.title('Training Progress Comparison')
    plt.xlabel('Episode')
    plt.ylabel('Total Reward')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    # 2. Score Distribution
    plt.subplot(2, 4, 2)
    plt.hist(dqn_scores[-200:], bins=20, alpha=0.7, label='DQN', color='blue', density=True)
    plt.hist(ppo_scores[-200:], bins=20, alpha=0.7, label='PPO', color='red', density=True)
    plt.title('Score Distribution (Last 200 Episodes)')
    plt.xlabel('Total Reward')
    plt.ylabel('Density')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    # 3. Action Preference Comparison
    plt.subplot(2, 4, 3)
    actions = list(range(len(env.feedback_types)))
    action_labels = [name.replace('_', '\n') for name in env.feedback_types]
    
    total_dqn = sum(dqn_actions.values())
    total_ppo = sum(ppo_actions.values())
    
    dqn_percentages = [dqn_actions[i]/total_dqn*100 for i in actions]
    ppo_percentages = [ppo_actions[i]/total_ppo*100 for i in actions]
    
    x = np.arange(len(actions))
    width = 0.35
    
    plt.bar(x - width/2, dqn_percentages, width, label='DQN', alpha=0.8, color='blue')
    plt.bar(x + width/2, ppo_percentages, width, label='PPO', alpha=0.8, color='red')
    
    plt.title('Action Preference Comparison')
    plt.xlabel('Feedback Actions')
    plt.ylabel('Usage Percentage')
    plt.xticks(x, action_labels, rotation=45, ha='right')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    # 4. Rolling Average Comparison
    plt.subplot(2, 4, 4)
    rolling_window = 100
    dqn_rolling = [np.mean(dqn_scores[max(0, i-rolling_window):i+1]) for i in range(len(dqn_scores))]
    ppo_rolling = [np.mean(ppo_scores[max(0, i-rolling_window):i+1]) for i in range(len(ppo_scores))]
    
    plt.plot(dqn_rolling, label='DQN', linewidth=2, color='blue')
    plt.plot(ppo_rolling, label='PPO', linewidth=2, color='red')
    plt.title(f'Rolling Average ({rolling_window} episodes)')
    plt.xlabel('Episode')
    plt.ylabel('Average Reward')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    # 5. Performance Metrics Summary
    plt.subplot(2, 4, 5)
    metrics = ['Mean', 'Median', 'Std Dev', 'Max']
    dqn_metrics = [
        np.mean(dqn_scores[-100:]),
        np.median(dqn_scores[-100:]),
        np.std(dqn_scores[-100:]),
        np.max(dqn_scores)
    ]
    ppo_metrics = [
        np.mean(ppo_scores[-100:]),
        np.median(ppo_scores[-100:]),
        np.std(ppo_scores[-100:]),
        np.max(ppo_scores)
    ]
    
    x = np.arange(len(metrics))
    width = 0.35
    
    plt.bar(x - width/2, dqn_metrics, width, label='DQN', alpha=0.8, color='blue')
    plt.bar(x + width/2, ppo_metrics, width, label='PPO', alpha=0.8, color='red')
    
    plt.title('Performance Metrics (Last 100 Episodes)')
    plt.xlabel('Metrics')
    plt.ylabel('Values')
    plt.xticks(x, metrics)
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    # 6. Learning Curve Smoothed
    plt.subplot(2, 4, 6)
    smooth_window = 25
    dqn_smooth = np.convolve(dqn_scores, np.ones(smooth_window)/smooth_window, mode='valid')
    ppo_smooth = np.convolve(ppo_scores, np.ones(smooth_window)/smooth_window, mode='valid')
    
    plt.plot(range(smooth_window-1, len(dqn_scores)), dqn_smooth, 
             label='DQN', linewidth=2, color='blue')
    plt.plot(range(smooth_window-1, len(ppo_scores)), ppo_smooth, 
             label='PPO', linewidth=2, color='red')
    
    plt.title('Learning Curves (Smoothed)')
    plt.xlabel('Episode')
    plt.ylabel('Smoothed Reward')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    # 7. Convergence Analysis
    plt.subplot(2, 4, 7)
    # Calculate variance over time
    variance_window = 50
    dqn_variance = [np.var(dqn_scores[max(0, i-variance_window):i+1]) 
                    for i in range(variance_window, len(dqn_scores))]
    ppo_variance = [np.var(ppo_scores[max(0, i-variance_window):i+1]) 
                    for i in range(variance_window, len(ppo_scores))]
    
    plt.plot(range(variance_window, len(dqn_scores)), dqn_variance, 
             label='DQN Variance', color='blue')
    plt.plot(range(variance_window, len(ppo_scores)), ppo_variance, 
             label='PPO Variance', color='red')
    
    plt.title('Training Stability (Variance)')
    plt.xlabel('Episode')
    plt.ylabel('Reward Variance')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    # 8. Final Performance Box Plot
    plt.subplot(2, 4, 8)
    final_episodes = 100
    data_to_plot = [dqn_scores[-final_episodes:], ppo_scores[-final_episodes:]]
    
    box_plot = plt.boxplot(data_to_plot, labels=['DQN', 'PPO'], patch_artist=True)
    box_plot['boxes'][0].set_facecolor('lightblue')
    box_plot['boxes'][1].set_facecolor('lightcoral')
    
    plt.title(f'Final Performance Distribution\n(Last {final_episodes} Episodes)')
    plt.ylabel('Total Reward')
    plt.grid(True, alpha=0.3)
    
    plt.tight_layout()
    
    # Save the plot
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f'comprehensive_training_results_{timestamp}.png'
    plt.savefig(filename, dpi=300, bbox_inches='tight')
    print(f"\n📊 Comprehensive results saved to '{filename}'")
    
    plt.show()

def main():
    """Main training function"""
    print("🚀 Starting Interview Training RL Experiment")
    print("="*60)
    print(f"Timestamp: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    
    # Create environment
    env = InterviewEnvironment(features_dim=30)
    print(f"\n🎮 Environment created")
    print(f"   Observation space: {env.observation_space.shape}")
    print(f"   Action space: {env.action_space.n}")
    print(f"   Feedback types: {env.feedback_types}")
    
    # Training parameters
    episodes = 800
    
    # Train both models
    print(f"\n📚 Training both models for {episodes} episodes each...")
    
    dqn_agent, dqn_scores, dqn_lengths, dqn_actions = train_dqn(env, episodes=episodes)
    ppo_agent, ppo_scores, ppo_lengths, ppo_actions = train_ppo(env, episodes=episodes)
    
    # Save final models
    os.makedirs('saved_models', exist_ok=True)
    dqn_agent.save('saved_models/dqn_final.pth')
    ppo_agent.save('saved_models/ppo_final.pth')
    
    print(f"\n💾 Models saved to 'saved_models/' directory")
    
    # Compare results
    compare_models(dqn_scores, ppo_scores, dqn_actions, ppo_actions, env)
    
    # Create comprehensive visualizations
    create_comprehensive_plots(dqn_scores, ppo_scores, dqn_actions, ppo_actions, env)
    
    # Save training data for further analysis
    training_data = {
        'dqn_scores': dqn_scores,
        'ppo_scores': ppo_scores,
        'dqn_actions': dqn_actions,
        'ppo_actions': ppo_actions,
        'episodes': episodes,
        'timestamp': datetime.now().isoformat()
    }
    
    import pickle
    with open('training_data.pkl', 'wb') as f:
        pickle.dump(training_data, f)
    
    print(f"\n📈 Training data saved to 'training_data.pkl'")
    print("\n🎉 Training experiment completed successfully!")
    
    # Summary
    print(f"\n📋 FINAL SUMMARY")
    print(f"   DQN Final Score: {np.mean(dqn_scores[-100:]):.2f} ± {np.std(dqn_scores[-100:]):.2f}")
    print(f"   PPO Final Score: {np.mean(ppo_scores[-100:]):.2f} ± {np.std(ppo_scores[-100:]):.2f}")
    difference = np.mean(dqn_scores[-100:]) - np.mean(ppo_scores[-100:])
    print(f"   Performance Difference: {difference:.2f} points")
    
    return dqn_agent, ppo_agent, training_data

if __name__ == "__main__":
    main()