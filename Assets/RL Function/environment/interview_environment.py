try:
    import gymnasium as gym
    from gymnasium import spaces
except ImportError:
    import gym
    from gym import spaces

import numpy as np
import sys
import os

# Add the data preprocessing path
sys.path.append(os.path.join(os.path.dirname(__file__), '..', 'data_preprocessing'))

class InterviewEnvironment(gym.Env):
    def __init__(self, features_dim=30):
        super(InterviewEnvironment, self).__init__()
        
        # Define action space (feedback types for interview coaching)
        self.action_space = spaces.Discrete(6)
        self.feedback_types = [
            "encourage_confidence",      # 0 - boost confidence
            "improve_speech_pace",       # 1 - adjust speaking speed
            "enhance_clarity",           # 2 - improve articulation
            "optimize_tone",             # 3 - better emotional tone
            "reduce_nervousness",        # 4 - calm anxiety
            "maintain_current_approach"  # 5 - no change needed
        ]
        
        # Define observation space
        # Speech features (25) + Performance metrics (5) = 30 total
        self.observation_space = spaces.Box(
            low=-np.inf, high=np.inf, 
            shape=(features_dim,),
            dtype=np.float32
        )
        
        # Interview simulation state
        self.max_questions = 8  # Questions per interview session
        self.current_question = 0
        self.performance_history = []
        self.current_speech_features = None
        self.session_improvement = 0
        
        # Load real IEMOCAP data for more realistic simulation
        self.load_background_data()
    
    def load_background_data(self):
        """Load IEMOCAP data for realistic feature generation"""
        try:
            from iemocap_loader import IEMOCAPLoader
            loader = IEMOCAPLoader()
            
            # Try to load real data first
            features, labels, filenames = loader.process_dataset()
            
            if features is not None and len(features) > 0:
                self.background_features = features
                self.background_labels = labels
                print(f"Loaded {len(features)} real IEMOCAP samples for simulation")
            else:
                # Fall back to simulated data
                print("Real IEMOCAP data not available, generating simulated data...")
                features, labels, filenames = loader.generate_simulated_data(1000)
                self.background_features = features
                self.background_labels = labels
                print(f"Generated {len(features)} simulated samples for training")
                
        except Exception as e:
            print(f"Error loading background data: {e}")
            print("Using fallback random features...")
            self.background_features = None
    
    def reset(self):
        """Reset environment for new interview session"""
        self.current_question = 0
        self.performance_history = []
        self.session_improvement = 0
        
        # Initialize with realistic speech features
        self.current_speech_features = self._generate_initial_features()
        
        # Initial performance metrics [confidence, clarity, pace, tone, overall]
        initial_performance = np.array([0.5, 0.5, 0.5, 0.5, 0.5])
        self.performance_history.append(initial_performance)
        
        # Combine speech features with performance metrics
        observation = np.concatenate([self.current_speech_features, initial_performance])
        return observation.astype(np.float32)
    
    def step(self, action):
        """Execute feedback action and return new state"""
        self.current_question += 1
        
        # Apply feedback effect based on action
        performance_change = self._apply_feedback_effect(action)
        
        # Get current performance
        current_performance = self.performance_history[-1].copy()
        
        # Update performance based on feedback
        new_performance = np.clip(current_performance + performance_change, 0, 1)
        self.performance_history.append(new_performance)
        
        # Calculate reward
        reward = self._calculate_reward(current_performance, new_performance, action)
        
        # Update session improvement tracking
        improvement = np.sum(new_performance - self.performance_history[0])
        self.session_improvement = improvement
        
        # Check if interview session is complete
        done = self.current_question >= self.max_questions
        
        # Generate new speech features (simulate next answer)
        self.current_speech_features = self._generate_next_features(new_performance)
        
        # Create new observation
        observation = np.concatenate([self.current_speech_features, new_performance])
        
        # Info for analysis
        info = {
            'feedback_action': self.feedback_types[action],
            'performance_change': performance_change,
            'question_number': self.current_question,
            'session_improvement': self.session_improvement,
            'current_performance': new_performance
        }
        
        return observation.astype(np.float32), reward, done, info
    
    def _generate_initial_features(self):
        """Generate realistic initial speech features"""
        if self.background_features is not None and len(self.background_features) > 0:
            # Use a random sample from IEMOCAP as base
            idx = np.random.randint(0, len(self.background_features))
            base_features = self.background_features[idx].copy()
            
            # Add some variation
            noise = np.random.normal(0, 0.1, len(base_features))
            return base_features + noise
        else:
            # Fallback to random features if no background data
            return np.random.randn(25)  # 25 speech features
    
    def _generate_next_features(self, performance):
        """Generate speech features for next response based on current performance"""
        base_features = self.current_speech_features.copy()
        
        # Performance influences feature changes
        performance_influence = (performance[4] - 0.5) * 0.2  # Overall performance effect
        
        # Add realistic variation
        variation = np.random.normal(performance_influence, 0.15, len(base_features))
        
        return base_features + variation
    
    def _apply_feedback_effect(self, action):
        """Simulate how different feedback types affect performance"""
        # Base improvement amount
        base_effect = 0.08
        
        # Random variation in feedback effectiveness
        effectiveness = np.random.uniform(0.7, 1.3)
        actual_effect = base_effect * effectiveness
        
        # Different feedback types affect different metrics
        effect = np.zeros(5)  # [confidence, clarity, pace, tone, overall]
        
        if action == 0:  # encourage_confidence
            effect[0] = actual_effect * 1.2  # Strong confidence boost
            effect[4] = actual_effect * 0.6  # Moderate overall improvement
            
        elif action == 1:  # improve_speech_pace
            effect[2] = actual_effect * 1.0  # Pace improvement
            effect[1] = actual_effect * 0.4  # Slight clarity improvement
            effect[4] = actual_effect * 0.5  # Overall improvement
            
        elif action == 2:  # enhance_clarity
            effect[1] = actual_effect * 1.1  # Strong clarity boost
            effect[4] = actual_effect * 0.7  # Good overall improvement
            
        elif action == 3:  # optimize_tone
            effect[3] = actual_effect * 1.0  # Tone improvement
            effect[0] = actual_effect * 0.3  # Slight confidence boost
            effect[4] = actual_effect * 0.5  # Overall improvement
            
        elif action == 4:  # reduce_nervousness
            effect[0] = actual_effect * 0.9  # Confidence boost
            effect[3] = actual_effect * 0.6  # Better tone
            effect[2] = actual_effect * 0.4  # Steadier pace
            effect[4] = actual_effect * 0.6  # Overall improvement
            
        else:  # maintain_current_approach (action == 5)
            # Small random change, could be positive or negative
            effect = np.random.normal(0, 0.02, 5)
        
        # Add some noise to make it realistic
        noise = np.random.normal(0, 0.03, 5)
        return effect + noise
    
    def _calculate_reward(self, old_performance, new_performance, action):
        """Calculate reward based on performance improvement"""
        # Calculate improvement for each metric
        improvements = new_performance - old_performance
        
        # Overall improvement reward
        overall_improvement = improvements[4]  # Overall performance metric
        
        # Specific metric improvements
        confidence_improvement = improvements[0]
        clarity_improvement = improvements[1]
        
        # Base reward from overall improvement
        reward = overall_improvement * 10
        
        # Bonus for improving key metrics
        reward += confidence_improvement * 5
        reward += clarity_improvement * 5
        
        # Penalty for declining performance
        decline_penalty = np.sum(np.minimum(0, improvements)) * 8
        reward += decline_penalty
        
        # Small bonus for consistent improvement across all metrics
        if np.all(improvements >= -0.02):  # Allow small negative changes
            reward += 1.0
        
        # Penalize inappropriate actions (if overall performance is already high)
        if new_performance[4] > 0.85 and action != 5:  # Should use "maintain" when performance is high
            reward -= 0.5
        
        return reward
    
    def get_action_meanings(self):
        """Return the meaning of each action for analysis"""
        return dict(enumerate(self.feedback_types))