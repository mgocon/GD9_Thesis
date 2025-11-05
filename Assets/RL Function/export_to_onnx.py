"""
Export trained PyTorch models (DQN and PPO) to ONNX format for Unity inference
"""
import torch
import sys
import os

# Add paths for imports
sys.path.append(os.path.join(os.path.dirname(__file__), 'models'))

from dqn_model import DQNAgent
from ppo_model import PPOAgent

def export_dqn_to_onnx(model_path, onnx_path, state_dim=30, action_dim=6):
    """Export DQN model to ONNX format"""
    print(f"🔄 Exporting DQN model from {model_path} to {onnx_path}")
    
    # Create agent
    agent = DQNAgent(state_dim, action_dim)
    
    # Load checkpoint with weights_only=False for compatibility
    checkpoint = torch.load(model_path, map_location=agent.device, weights_only=False)
    agent.q_network.load_state_dict(checkpoint['q_network_state_dict'])
    agent.q_network.eval()
    
    # Create dummy input
    dummy_input = torch.randn(1, state_dim).to(agent.device)
    
    # Export to ONNX (opset 9 - balance between PyTorch support and Barracuda compatibility)
    torch.onnx.export(
        agent.q_network,
        dummy_input,
        onnx_path,
        export_params=True,
        opset_version=9,
        do_constant_folding=True,
        input_names=['observation'],
        output_names=['q_values'],
        dynamic_axes={
            'observation': {0: 'batch_size'},
            'q_values': {0: 'batch_size'}
        }
    )
    
    print(f"✅ DQN model exported successfully to {onnx_path}")
    return True

def export_ppo_to_onnx(model_path, onnx_path, state_dim=30, action_dim=6):
    """Export PPO model to ONNX format"""
    print(f"🔄 Exporting PPO model from {model_path} to {onnx_path}")
    
    # Create agent
    agent = PPOAgent(state_dim, action_dim)
    
    # Load checkpoint with weights_only=False for compatibility
    checkpoint = torch.load(model_path, map_location=agent.device, weights_only=False)
    agent.policy.load_state_dict(checkpoint['policy_state_dict'])
    agent.policy.eval()
    
    # Create dummy input
    dummy_input = torch.randn(1, state_dim).to(agent.device)
    
    # Export to ONNX (opset 9 - balance between PyTorch support and Barracuda compatibility)
    torch.onnx.export(
        agent.policy,
        dummy_input,
        onnx_path,
        export_params=True,
        opset_version=9,
        do_constant_folding=True,
        input_names=['observation'],
        output_names=['policy_logits', 'value'],
        dynamic_axes={
            'observation': {0: 'batch_size'},
            'policy_logits': {0: 'batch_size'},
            'value': {0: 'batch_size'}
        }
    )
    
    print(f"✅ PPO model exported successfully to {onnx_path}")
    return True

def main():
    """Export both models to ONNX"""
    print("🚀 Starting ONNX Export Process")
    print("=" * 60)
    
    # Configuration
    state_dim = 30  # 25 speech features + 5 performance metrics
    action_dim = 6  # 6 feedback types
    
    # Paths
    base_path = os.path.dirname(__file__)
    models_path = os.path.join(base_path, 'saved_models')
    onnx_output_path = os.path.join(base_path, 'onnx_models')
    
    # Create output directory
    os.makedirs(onnx_output_path, exist_ok=True)
    
    # Export DQN models
    try:
        dqn_best_path = os.path.join(models_path, 'dqn_best.pth')
        dqn_onnx_path = os.path.join(onnx_output_path, 'dqn_model.onnx')
        export_dqn_to_onnx(dqn_best_path, dqn_onnx_path, state_dim, action_dim)
    except Exception as e:
        print(f"❌ Failed to export DQN: {e}")
    
    # Export PPO models
    try:
        ppo_best_path = os.path.join(models_path, 'ppo_best.pth')
        ppo_onnx_path = os.path.join(onnx_output_path, 'ppo_model.onnx')
        export_ppo_to_onnx(ppo_best_path, ppo_onnx_path, state_dim, action_dim)
    except Exception as e:
        print(f"❌ Failed to export PPO: {e}")
    
    print("\n" + "=" * 60)
    print("✨ Export process completed!")
    print(f"📁 ONNX models saved in: {onnx_output_path}")
    print("\nNext steps:")
    print("1. Copy the .onnx files to Unity's StreamingAssets folder")
    print("2. Install ML-Agents package in Unity")
    print("3. Use the provided C# scripts for inference")

if __name__ == "__main__":
    main()
