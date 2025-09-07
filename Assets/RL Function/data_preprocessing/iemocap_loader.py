import pandas as pd
import numpy as np
import librosa
import os
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import train_test_split

class IEMOCAPLoader:
    def __init__(self, csv_path="Assets/data/IEMOCAP/iemocap_full_dataset.csv"):
        self.emotions_map = {
            'neutral': 0, 'calm': 1, 'happy': 2, 'sad': 3, 
            'angry': 4, 'fearful': 5, 'disgust': 6, 'surprised': 7,
            'ang': 4, 'hap': 2, 'sad': 3, 'neu': 0, 'fru': 4,
            'exc': 2, 'fea': 5, 'sur': 7, 'dis': 6, 'oth': 0
        }
        
        # Try multiple possible paths
        possible_paths = [
            csv_path,
            "../../data/IEMOCAP/iemocap_full_dataset.csv",
            "../data/IEMOCAP/iemocap_full_dataset.csv", 
            "Assets/data/IEMOCAP/iemocap_full_dataset.csv",
            "data/IEMOCAP/iemocap_full_dataset.csv"
        ]
        
        self.csv_path = None
        for path in possible_paths:
            if os.path.exists(path):
                self.csv_path = path
                print(f"Found IEMOCAP data at: {path}")
                break
        
        if self.csv_path is None:
            print("IEMOCAP CSV not found, will use simulated data")
        
    def load_csv_data(self):
        """Load data from CSV file"""
        if self.csv_path is None:
            return None
            
        try:
            df = pd.read_csv(self.csv_path)
            print(f"Loaded CSV with columns: {df.columns.tolist()}")
            print(f"Dataset shape: {df.shape}")
            return df
        except Exception as e:
            print(f"Error loading CSV: {e}")
            return None
    
    def extract_features_from_audio_path(self, audio_path, sr=16000):
        """Extract speech features for interview assessment"""
        try:
            # Load audio file
            y, sr = librosa.load(audio_path, sr=sr)
            
            # Feature dictionary
            features = {}
            
            # 1. Speech rate estimation
            tempo, _ = librosa.beat.beat_track(y=y, sr=sr)
            features['speech_rate'] = tempo
            
            # 2. Pitch features (fundamental frequency)
            pitches, magnitudes = librosa.piptrack(y=y, sr=sr)
            pitch_values = pitches[pitches > 0]
            features['pitch_mean'] = np.mean(pitch_values) if len(pitch_values) > 0 else 0
            features['pitch_std'] = np.std(pitch_values) if len(pitch_values) > 0 else 0
            features['pitch_range'] = np.max(pitch_values) - np.min(pitch_values) if len(pitch_values) > 0 else 0
            
            # 3. Energy features (confidence indicators)
            rms = librosa.feature.rms(y=y)[0]
            features['energy_mean'] = np.mean(rms)
            features['energy_std'] = np.std(rms)
            features['energy_max'] = np.max(rms)
            
            # 4. Spectral features
            spectral_centroids = librosa.feature.spectral_centroid(y=y, sr=sr)[0]
            features['spectral_centroid_mean'] = np.mean(spectral_centroids)
            features['spectral_centroid_std'] = np.std(spectral_centroids)
            
            spectral_rolloff = librosa.feature.spectral_rolloff(y=y, sr=sr)[0]
            features['spectral_rolloff_mean'] = np.mean(spectral_rolloff)
            
            # 5. MFCC features (first 13 coefficients)
            mfccs = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=13)
            for i in range(13):
                features[f'mfcc_{i}'] = np.mean(mfccs[i])
            
            # 6. Zero crossing rate (speech clarity)
            zcr = librosa.feature.zero_crossing_rate(y)[0]
            features['zcr_mean'] = np.mean(zcr)
            features['zcr_std'] = np.std(zcr)
            
            # 7. Pause and silence analysis
            frame_length = 2048
            hop_length = 512
            silence_threshold = 0.01
            silent_frames = rms < silence_threshold
            pause_count = np.sum(np.diff(silent_frames.astype(int)) == 1)
            total_frames = len(rms)
            features['pause_count'] = pause_count
            features['silence_ratio'] = np.sum(silent_frames) / total_frames
            
            return np.array(list(features.values()))
            
        except Exception as e:
            print(f"Error processing audio {audio_path}: {e}")
            return None
    
    def process_dataset(self):
        """Process the CSV dataset and extract features"""
        df = self.load_csv_data()
        if df is None:
            return None, None, None
        
        all_features = []
        all_labels = []
        all_filenames = []
        
        print("Processing dataset...")
        
        for idx, row in df.iterrows():
            if idx % 100 == 0:
                print(f"Processed {idx}/{len(df)} samples")
            
            # Get emotion label
            emotion = row.get('emotion', row.get('Emotion', ''))
            if emotion in self.emotions_map:
                emotion_label = self.emotions_map[emotion]
                
                # Get audio file path (adjust column name as needed)
                audio_file = row.get('file_path', row.get('path', row.get('filename', '')))
                
                if audio_file and os.path.exists(audio_file):
                    features = self.extract_features_from_audio_path(audio_file)
                    if features is not None:
                        all_features.append(features)
                        all_labels.append(emotion_label)
                        all_filenames.append(audio_file)
        
        print(f"Successfully processed {len(all_features)} samples")
        return np.array(all_features), np.array(all_labels), all_filenames
    
    def create_interview_performance_data(self, features, labels, num_samples=1000):
        """Convert emotion data to interview performance simulation data"""
        # Map emotions to interview performance metrics
        emotion_to_performance = {
            0: [0.7, 0.8, 0.7, 0.7, 0.7],  # neutral - good baseline
            1: [0.8, 0.8, 0.8, 0.8, 0.8],  # calm - excellent
            2: [0.9, 0.7, 0.6, 0.8, 0.8],  # happy - confident but maybe too fast
            3: [0.4, 0.6, 0.8, 0.5, 0.5],  # sad - low confidence, slow
            4: [0.6, 0.5, 0.4, 0.3, 0.4],  # angry - poor interview performance
            5: [0.3, 0.5, 0.7, 0.4, 0.4],  # fearful - very low confidence
            6: [0.5, 0.6, 0.5, 0.4, 0.5],  # disgust - poor overall
            7: [0.7, 0.6, 0.5, 0.6, 0.6],  # surprised - inconsistent
        }
        
        interview_data = []
        for i in range(min(num_samples, len(features))):
            emotion_label = labels[i]
            base_performance = emotion_to_performance.get(emotion_label, [0.5, 0.5, 0.5, 0.5, 0.5])
            
            # Add some noise to make it more realistic
            noise = np.random.normal(0, 0.1, 5)
            performance = np.clip(np.array(base_performance) + noise, 0, 1)
            
            # Combine speech features with performance metrics
            combined_features = np.concatenate([features[i], performance])
            interview_data.append(combined_features)
        
        return np.array(interview_data)
    
    def generate_simulated_data(self, num_samples=1000):
        """Generate simulated data when real IEMOCAP is not available"""
        print(f"Generating {num_samples} simulated samples...")
        
        all_features = []
        all_labels = []
        
        for i in range(num_samples):
            # Generate realistic speech features
            features = {}
            
            # Speech rate (words per minute, typical range 120-180)
            features['speech_rate'] = np.random.normal(150, 25)
            
            # Pitch features (Hz, typical range 80-300 for adults)
            pitch_mean = np.random.normal(180, 40)
            features['pitch_mean'] = max(80, pitch_mean)
            features['pitch_std'] = np.random.normal(20, 5)
            features['pitch_range'] = np.random.normal(100, 30)
            
            # Energy features (normalized 0-1)
            features['energy_mean'] = np.random.beta(2, 2)  # Beta distribution for 0-1 range
            features['energy_std'] = np.random.exponential(0.1)
            features['energy_max'] = features['energy_mean'] + np.random.exponential(0.2)
            
            # Spectral features
            features['spectral_centroid_mean'] = np.random.normal(2000, 500)
            features['spectral_centroid_std'] = np.random.normal(300, 100)
            features['spectral_rolloff_mean'] = np.random.normal(4000, 1000)
            
            # MFCC features (typically range -50 to 50)
            for j in range(13):
                features[f'mfcc_{j}'] = np.random.normal(0, 15)
            
            # Zero crossing rate (typical range 0-0.5)
            features['zcr_mean'] = np.random.beta(1, 3) * 0.5
            features['zcr_std'] = np.random.exponential(0.05)
            
            # Pause analysis
            features['pause_count'] = np.random.poisson(3)  # Average 3 pauses
            features['silence_ratio'] = np.random.beta(1, 4)  # Low silence ratio
            
            # Random emotion label
            emotion_label = np.random.randint(0, 8)
            
            feature_array = np.array(list(features.values()))
            all_features.append(feature_array)
            all_labels.append(emotion_label)
        
        print(f"Generated {len(all_features)} simulated samples")
        return np.array(all_features), np.array(all_labels), [f"simulated_{i}" for i in range(len(all_features))]