import torch
import torch.nn as nn
import torch.optim as optim
import torch.nn.functional as F
import numpy as np

class PPONetwork(nn.Module):
    def __init__(self, input_dim, hidden_dim, output_dim):
        super(PPONetwork, self).__init__()
        
        # Shared layers
        self.shared = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.ReLU(),
            nn.Dropout(0.2),
            nn.Linear(hidden_dim, hidden_dim),
            nn.ReLU(),
            nn.Dropout(0.2),
            nn.Linear(hidden_dim, hidden_dim//2),
            nn.ReLU()
        )
        
        # Policy head
        self.policy_head = nn.Linear(hidden_dim//2, output_dim)
        
        # Value head
        self.value_head = nn.Linear(hidden_dim//2, 1)
    
    def forward(self, x):
        shared_features = self.shared(x)
        policy_logits = self.policy_head(shared_features)
        value = self.value_head(shared_features)
        return policy_logits, value

class PPOAgent:
    def __init__(self, state_dim, action_dim, learning_rate=3e-4, gamma=0.99, 
                 eps_clip=0.2, K_epochs=4, value_coef=0.5, entropy_coef=0.01):
        self.state_dim = state_dim
        self.action_dim = action_dim
        self.learning_rate = learning_rate
        self.gamma = gamma
        self.eps_clip = eps_clip
        self.K_epochs = K_epochs
        self.value_coef = value_coef
        self.entropy_coef = entropy_coef
        
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        hidden_dim = max(256, state_dim * 8)  # Adaptive hidden layer size
        
        self.policy = PPONetwork(state_dim, hidden_dim, action_dim).to(self.device)
        self.optimizer = optim.Adam(self.policy.parameters(), lr=learning_rate)
        
        self.policy_old = PPONetwork(state_dim, hidden_dim, action_dim).to(self.device)
        self.policy_old.load_state_dict(self.policy.state_dict())
        
        self.memory = []
        
        # Training metrics
        self.policy_losses = []
        self.value_losses = []
        self.entropies = []
        
    def select_action(self, state):
        """Select action using current policy"""
        state_tensor = torch.FloatTensor(state).unsqueeze(0).to(self.device)
        
        with torch.no_grad():
            policy_logits, _ = self.policy_old(state_tensor)
            policy_dist = F.softmax(policy_logits, dim=-1)
            
            # Add small epsilon for numerical stability
            policy_dist = policy_dist + 1e-8
            policy_dist = policy_dist / policy_dist.sum()
            
            action = torch.multinomial(policy_dist, 1).item()
            action_prob = policy_dist[0][action].item()
        
        return action, action_prob
    
    def store_transition(self, state, action, reward, next_state, done, action_prob):
        """Store transition in memory"""
        self.memory.append({
            'state': state,
            'action': action,
            'reward': reward,
            'next_state': next_state,
            'done': done,
            'action_prob': action_prob
        })
    
    def update(self):
        """Update policy using PPO algorithm"""
        if len(self.memory) == 0:
            return
        
        # Convert memory to tensors
        states = np.array([m['state'] for m in self.memory])
        actions = np.array([m['action'] for m in self.memory])
        rewards = [m['reward'] for m in self.memory]
        old_action_probs = np.array([m['action_prob'] for m in self.memory])
        dones = [m['done'] for m in self.memory]
        
        states = torch.FloatTensor(states).to(self.device)
        actions = torch.LongTensor(actions).to(self.device)
        old_action_probs = torch.FloatTensor(old_action_probs).to(self.device)
        
        # Calculate discounted rewards (returns)
        returns = []
        discounted_reward = 0
        for reward, done in zip(reversed(rewards), reversed(dones)):
            if done:
                discounted_reward = 0
            discounted_reward = reward + (self.gamma * discounted_reward)
            returns.insert(0, discounted_reward)
        
        returns = torch.FloatTensor(returns).to(self.device)
        
        # Normalize returns
        if len(returns) > 1:
            returns = (returns - returns.mean()) / (returns.std() + 1e-8)
        
        # Optimize policy for K epochs
        for epoch in range(self.K_epochs):
            # Get current policy outputs
            policy_logits, state_values = self.policy(states)
            policy_dist = F.softmax(policy_logits, dim=-1)
            
            # Add small epsilon for numerical stability
            policy_dist = policy_dist + 1e-8
            
            action_probs = policy_dist.gather(1, actions.unsqueeze(1)).squeeze()
            
            # Calculate ratio (pi_new / pi_old)
            ratios = action_probs / (old_action_probs + 1e-8)
            
            # Calculate advantages
            advantages = returns - state_values.squeeze()
            
            # Calculate surrogate losses
            surr1 = ratios * advantages
            surr2 = torch.clamp(ratios, 1 - self.eps_clip, 1 + self.eps_clip) * advantages
            policy_loss = -torch.min(surr1, surr2).mean()
            
            # Calculate value loss
            value_loss = F.mse_loss(state_values.squeeze(), returns)
            
            # Calculate entropy bonus
            entropy = -(policy_dist * torch.log(policy_dist + 1e-8)).sum(dim=-1).mean()
            
            # Total loss
            total_loss = policy_loss + self.value_coef * value_loss - self.entropy_coef * entropy
            
            # Store metrics
            self.policy_losses.append(policy_loss.item())
            self.value_losses.append(value_loss.item())
            self.entropies.append(entropy.item())
            
            # Update
            self.optimizer.zero_grad()
            total_loss.backward()
            
            # Gradient clipping for stability
            torch.nn.utils.clip_grad_norm_(self.policy.parameters(), max_norm=1.0)
            
            self.optimizer.step()
        
        # Update old policy
        self.policy_old.load_state_dict(self.policy.state_dict())
        
        # Clear memory
        self.memory = []
    
    def save(self, filepath):
        """Save model and training state"""
        torch.save({
            'policy_state_dict': self.policy.state_dict(),
            'optimizer_state_dict': self.optimizer.state_dict(),
            'policy_losses': self.policy_losses,
            'value_losses': self.value_losses,
            'entropies': self.entropies
        }, filepath)
    
    def load(self, filepath):
        """Load model and training state"""
        checkpoint = torch.load(filepath, map_location=self.device)
        self.policy.load_state_dict(checkpoint['policy_state_dict'])
        self.policy_old.load_state_dict(checkpoint['policy_state_dict'])
        self.optimizer.load_state_dict(checkpoint['optimizer_state_dict'])
        self.policy_losses = checkpoint.get('policy_losses', [])
        self.value_losses = checkpoint.get('value_losses', [])
        self.entropies = checkpoint.get('entropies', [])
    
    def get_training_stats(self):
        """Get training statistics"""
        return {
            'avg_policy_loss': np.mean(self.policy_losses[-100:]) if self.policy_losses else 0,
            'avg_value_loss': np.mean(self.value_losses[-100:]) if self.value_losses else 0,
            'avg_entropy': np.mean(self.entropies[-100:]) if self.entropies else 0,
            'memory_size': len(self.memory)
        }