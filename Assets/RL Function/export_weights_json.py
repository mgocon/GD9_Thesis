"""
Export trained PyTorch model weights to JSON format for C# implementation
This avoids ONNX/Barracuda compatibility issues by implementing the network natively in C#
"""
import torch
import json
import sys
import os

# Add paths for imports
sys.path.append(os.path.join(os.path.dirname(__file__), 'models'))

from dqn_model import DQNAgent
from ppo_model import PPOAgent

def export_dqn_weights(model_path, output_path, state_dim=30, action_dim=10):
    """Export DQN weights to JSON format"""
    print(f"🔄 Exporting DQN weights from {model_path}")
    
    # Create agent and load checkpoint
    agent = DQNAgent(state_dim, action_dim)
    checkpoint = torch.load(model_path, map_location=agent.device, weights_only=False)
    agent.q_network.load_state_dict(checkpoint['q_network_state_dict'])
    agent.q_network.eval()
    
    # Extract weights from the network
    weights = {}
    for name, param in agent.q_network.named_parameters():
        weights[name] = param.detach().cpu().numpy().tolist()
    
    # Save to JSON
    with open(output_path, 'w') as f:
        json.dump({
            'model_type': 'DQN',
            'state_dim': state_dim,
            'action_dim': action_dim,
            'weights': weights
        }, f, indent=2)
    
    print(f"✅ DQN weights exported to {output_path}")
    print(f"   Layers: {list(weights.keys())}")
    return True

def export_ppo_weights(model_path, output_path, state_dim=30, action_dim=10):
    """Export PPO weights to JSON format"""
    print(f"🔄 Exporting PPO weights from {model_path}")
    
    # Create agent and load checkpoint
    agent = PPOAgent(state_dim, action_dim)
    checkpoint = torch.load(model_path, map_location=agent.device, weights_only=False)
    agent.policy.load_state_dict(checkpoint['policy_state_dict'])
    agent.policy.eval()
    
    # Extract weights from the network
    weights = {}
    for name, param in agent.policy.named_parameters():
        # Export all layers (policy_head and shared layers)
        weights[name] = param.detach().cpu().numpy().tolist()
    
    print(f"   Available layers: {list(weights.keys())}")
    # Save to JSON
    with open(output_path, 'w') as f:
        json.dump({
            'model_type': 'PPO',
            'state_dim': state_dim,
            'action_dim': action_dim,
            'weights': weights
        }, f, indent=2)
    
    print(f"✅ PPO weights exported to {output_path}")
    print(f"   Layers: {list(weights.keys())}")
    return True

def main():
    """Export both models' weights to JSON"""
    print("🚀 Starting Weight Export to JSON")
    print("=" * 60)
    
    # Configuration (original trained models had 6 actions)
    state_dim = 30  # 25 speech features + 5 performance metrics
    action_dim = 6  # Original model had 6 actions
    
    # Paths
    base_path = os.path.dirname(__file__)
    models_path = os.path.join(base_path, 'saved_models')
    output_path = os.path.join(base_path, 'weights_json')
    
    # Create output directory
    os.makedirs(output_path, exist_ok=True)
    
    # Export DQN
    dqn_model_path = os.path.join(models_path, 'dqn_best.pth')
    dqn_output_path = os.path.join(output_path, 'dqn_weights.json')
    
    try:
        export_dqn_weights(dqn_model_path, dqn_output_path, state_dim, action_dim)
    except Exception as e:
        print(f"❌ Failed to export DQN: {e}")
    
    # Export PPO
    ppo_model_path = os.path.join(models_path, 'ppo_best.pth')
    ppo_output_path = os.path.join(output_path, 'ppo_weights.json')
    
    try:
        export_ppo_weights(ppo_model_path, ppo_output_path, state_dim, action_dim)
    except Exception as e:
        print(f"❌ Failed to export PPO: {e}")
    
    print("=" * 60)
    print("✨ Weight export completed!")
    print(f"📁 JSON files saved in: {output_path}")
    print("\nNext steps:")
    print("1. Copy .json files to Unity's StreamingAssets folder")
    print("2. Implement simple neural network forward pass in C#")
    print("3. Load weights and run inference natively")

if __name__ == "__main__":
    main()
