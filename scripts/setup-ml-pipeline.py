#!/usr/bin/env python3
"""
Digital Twin Emotional Companion - ML Model Development Pipeline
Phase 1: Enhanced Emotional Intelligence - Voice Emotion Detection

This script sets up the complete ML development environment and pipeline
for training voice emotion detection models for the Digital Twin emotional companion.
"""

import os
import sys
import subprocess
import logging
from pathlib import Path

# Configure logging
logging.basicConfig(
    level=logging.INFO, format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)


class EmotionalMLPipeline:
    """Complete ML pipeline setup for emotion detection models"""

    def __init__(self):
        self.project_root = Path(__file__).parent.parent
        self.models_dir = self.project_root / "models"
        self.data_dir = self.project_root / "data"
        self.training_dir = self.project_root / "training"

    def setup_python_environment(self):
        """Setup Python environment with all required dependencies"""
        logger.info("Setting up Python environment...")

        requirements = [
            "tensorflow==2.15.0",
            "torch==2.1.0",
            "torchaudio==2.1.0",
            "transformers==4.36.0",
            "scikit-learn==1.3.0",
            "librosa==0.10.0",
            "scipy==1.11.0",
            "numpy==1.24.0",
            "pandas==2.1.0",
            "matplotlib==3.7.0",
            "seaborn==0.12.0",
            "mlflow==2.8.0",
            "optuna==3.3.0",
            "hydra-core==1.3.0",
            "redis==5.0.0",
            "psycopg2-binary==2.9.0",
            "asyncio==3.4.3",
            "fastapi==0.104.0",
            "pydantic==2.5.0",
            "uvicorn==0.24.0",
        ]

        for requirement in requirements:
            try:
                subprocess.run(
                    [sys.executable, "-m", "pip", "install", requirement],
                    check=True,
                    capture_output=True,
                )
                logger.info(f"Installed {requirement}")
            except subprocess.CalledProcessError as e:
                logger.error(f"Failed to install {requirement}: {e}")
                sys.exit(1)

    def create_project_structure(self):
        """Create the complete project structure for ML development"""
        logger.info("Creating ML project structure...")

        directories = [
            "models/emotion-recognition",
            "models/personality-models",
            "models/conversation-models",
            "data/raw/emotions",
            "data/processed/emotions",
            "data/validation/emotions",
            "training/pipelines",
            "training/configs",
            "training/logs",
            "training/checkpoints",
            "tests/ml-model-tests",
            "scripts/data-collection",
            "scripts/model-training",
            "monitoring/model-monitoring",
        ]

        for directory in directories:
            full_path = self.project_root / directory
            full_path.mkdir(parents=True, exist_ok=True)
            logger.info(f"Created directory: {full_path}")

    def setup_voice_emotion_training(self):
        """Setup voice emotion detection training pipeline"""
        logger.info("Setting up voice emotion detection training...")

        # Voice emotion detection training script
        voice_script = self.training_dir / "pipelines" / "train_voice_emotion.py"

        voice_training_code = '''#!/usr/bin/env python3
"""
Voice Emotion Detection Training Pipeline
Trains CNN+LSTM model for real-time voice emotion recognition
"""

import os
import torch
import torch.nn as nn
import torchaudio
import librosa
import numpy as np
import pandas as pd
from torch.utils.data import Dataset, DataLoader
from transformers import Wav2Vec2Model, Wav2Vec2Processor
import mlflow
import optuna

class VoiceEmotionDataset(Dataset):
    def __init__(self, audio_files, labels, sample_rate=16000):
        self.audio_files = audio_files
        self.labels = labels
        self.sample_rate = sample_rate
        self.processor = Wav2Vec2Processor.from_pretrained("facebook/wav2vec2-base")
        
    def __len__(self):
        return len(self.audio_files)
    
    def __getitem__(self, idx):
        audio_path = self.audio_files[idx]
        label = self.labels[idx]
        
        # Load and process audio
        speech_array, sampling_rate = torchaudio.load(audio_path)
        if sampling_rate != self.sample_rate:
            resampler = torchaudio.transforms.Resample(sampling_rate, self.sample_rate)
            speech_array = resampler(speech_array)
        
        # Extract features
        inputs = self.processor(speech_array.squeeze().numpy(), 
                               sampling_rate=self.sample_rate, 
                               return_tensors="pt")
        
        return {
            'input_values': inputs['input_values'].squeeze(),
            'attention_mask': inputs['attention_mask'].squeeze(),
            'labels': torch.tensor(label, dtype=torch.long)
        }

class VoiceEmotionModel(nn.Module):
    def __init__(self, num_classes=7, hidden_size=256):
        super().__init__()
        self.wav2vec = Wav2Vec2Model.from_pretrained("facebook/wav2vec2-base")
        self.lstm = nn.LSTM(self.wav2vec.config.hidden_size, hidden_size, 
                           batch_first=True, bidirectional=True)
        self.classifier = nn.Sequential(
            nn.Linear(hidden_size * 2, 128),
            nn.ReLU(),
            nn.Dropout(0.3),
            nn.Linear(128, num_classes)
        )
        
    def forward(self, input_values, attention_mask):
        # Extract features
        with torch.no_grad():
            features = self.wav2vec(input_values, attention_mask=attention_mask).last_hidden_state
        
        # LSTM processing
        lstm_out, _ = self.lstm(features)
        
        # Classification
        logits = self.classifier(lstm_out[:, -1, :])  # Use last time step
        return logits

def train_model(model, train_loader, val_loader, num_epochs=50, learning_rate=1e-4):
    """Train the voice emotion model"""
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model = model.to(device)
    
    criterion = nn.CrossEntropyLoss()
    optimizer = torch.optim.AdamW(model.parameters(), lr=learning_rate)
    scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(optimizer, patience=5)
    
    best_val_accuracy = 0.0
    
    for epoch in range(num_epochs):
        model.train()
        train_loss = 0.0
        train_correct = 0
        train_total = 0
        
        for batch in train_loader:
            input_values = batch['input_values'].to(device)
            attention_mask = batch['attention_mask'].to(device)
            labels = batch['labels'].to(device)
            
            optimizer.zero_grad()
            outputs = model(input_values, attention_mask)
            loss = criterion(outputs, labels)
            loss.backward()
            optimizer.step()
            
            train_loss += loss.item()
            _, predicted = torch.max(outputs.data, 1)
            train_total += labels.size(0)
            train_correct += (predicted == labels).sum().item()
        
        # Validation
        model.eval()
        val_loss = 0.0
        val_correct = 0
        val_total = 0
        
        with torch.no_grad():
            for batch in val_loader:
                input_values = batch['input_values'].to(device)
                attention_mask = batch['attention_mask'].to(device)
                labels = batch['labels'].to(device)
                
                outputs = model(input_values, attention_mask)
                loss = criterion(outputs, labels)
                val_loss += loss.item()
                _, predicted = torch.max(outputs.data, 1)
                val_total += labels.size(0)
                val_correct += (predicted == labels).sum().item()
        
        # Calculate metrics
        train_accuracy = 100 * train_correct / train_total
        val_accuracy = 100 * val_correct / val_total
        
        # Log metrics
        mlflow.log_metrics({
            'epoch': epoch + 1,
            'train_loss': train_loss / len(train_loader),
            'train_accuracy': train_accuracy,
            'val_loss': val_loss / len(val_loader),
            'val_accuracy': val_accuracy
        })
        
        # Save best model
        if val_accuracy > best_val_accuracy:
            best_val_accuracy = val_accuracy
            torch.save(model.state_dict(), 'best_voice_emotion_model.pth')
            mlflow.log_artifact('best_voice_emotion_model.pth')
        
        scheduler.step(val_loss)
        
        print(f'Epoch {epoch+1}/{num_epochs}: '
              f'Train Loss: {train_loss/len(train_loader):.4f}, '
              f'Train Acc: {train_accuracy:.2f}%, '
              f'Val Loss: {val_loss/len(val_loader):.4f}, '
              f'Val Acc: {val_accuracy:.2f}%')

def objective(trial):
    """Optuna objective for hyperparameter optimization"""
    # Suggest hyperparameters
    lr = trial.suggest_float('lr', 1e-5, 1e-3, log=True)
    batch_size = trial.suggest_categorical('batch_size', [16, 32, 64])
    hidden_size = trial.suggest_categorical('hidden_size', [128, 256, 512])
    
    # Create model with suggested parameters
    model = VoiceEmotionModel(hidden_size=hidden_size)
    
    # Train with cross-validation
    # ... (implementation details omitted for brevity)
    
    return val_accuracy

if __name__ == "__main__":
    # Setup MLflow experiment
    mlflow.set_experiment("voice_emotion_detection")
    
    # Hyperparameter optimization
    study = optuna.create_study(direction='maximize')
    study.optimize(objective, n_trials=50)
    
    print(f"Best trial: {study.best_trial.params}")
    print(f"Best accuracy: {study.best_value:.2f}%")
'''

        with open(voice_script, "w") as f:
            f.write(voice_training_code)
        os.chmod(voice_script, 0o755)
        logger.info(f"Created voice emotion training script: {voice_script}")

    def setup_data_collection(self):
        """Setup data collection scripts for training data"""
        logger.info("Setting up data collection pipeline...")

        data_collection_script = (
            self.project_root
            / "scripts"
            / "data-collection"
            / "collect_emotion_data.py"
        )

        data_collection_code = '''#!/usr/bin/env python3
"""
Emotion Data Collection Pipeline
Collects and preprocesses emotion training data from multiple sources
"""

import os
import json
import librosa
import numpy as np
import pandas as pd
from pathlib import Path
import requests
import zipfile

class EmotionDataCollector:
    def __init__(self, output_dir):
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        
    def download_iemocap_dataset(self):
        """Download IEMOCAP dataset for emotion training"""
        print("Downloading IEMOCAP dataset...")
        
        # IEMOCAP download URLs (simplified for example)
        iemocap_urls = {
            "audio": "https://datasets.server.com/iemocap/audio.zip",
            "labels": "https://datasets.server.com/iemocap/labels.zip"
        }
        
        for name, url in iemocap_urls.items():
            response = requests.get(url, stream=True)
            file_path = self.output_dir / f"{name}.zip"
            
            with open(file_path, 'wb') as f:
                for chunk in response.iter_content(chunk_size=8192):
                    f.write(chunk)
            
            # Extract
            with zipfile.ZipFile(file_path, 'r') as zip_ref:
                zip_ref.extractall(self.output_dir / name.replace('.zip', ''))
            
            print(f"Downloaded and extracted {name}")
    
    def preprocess_audio_files(self, audio_dir, labels_df):
        """Preprocess audio files for training"""
        processed_data = []
        
        for idx, row in labels_df.iterrows():
            audio_path = audio_dir / row['filename']
            
            if audio_path.exists():
                # Load audio
                y, sr = librosa.load(str(audio_path), sr=16000)
                
                # Extract features
                mfccs = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=13)
                chroma = librosa.feature.chroma(y=y, sr=sr)
                spectral_contrast = librosa.feature.spectral_contrast(y=y, sr=sr)
                tonnetz = librosa.feature.tonnetz(y=y, sr=sr)
                
                # Combine features
                features = np.hstack([
                    np.mean(mfccs, axis=1),
                    np.mean(chroma, axis=1),
                    np.mean(spectral_contrast, axis=1),
                    np.mean(tonnetz, axis=1)
                ])
                
                processed_data.append({
                    'features': features.tolist(),
                    'emotion': row['emotion'],
                    'intensity': row['intensity'],
                    'filename': row['filename']
                })
        
        return processed_data
    
    def create_training_dataset(self, processed_data, output_path):
        """Create training dataset in ML-ready format"""
        df = pd.DataFrame(processed_data)
        
        # Map emotions to numerical labels
        emotion_map = {
            'happy': 0, 'sad': 1, 'angry': 2, 'fear': 3,
            'disgust': 4, 'surprise': 5, 'neutral': 6
        }
        df['label'] = df['emotion'].map(emotion_map)
        
        # Save processed dataset
        df.to_json(output_path, orient='records', indent=2)
        print(f"Saved training dataset to {output_path}")

if __name__ == "__main__":
    collector = EmotionDataCollector("data/processed/emotions")
    
    # Download datasets
    collector.download_iemocap_dataset()
    
    # Process data
    # (Implementation would load actual data and process it)
    print("Data collection pipeline completed")
'''

        with open(data_collection_script, "w") as f:
            f.write(data_collection_code)
        os.chmod(data_collection_script, 0o755)
        logger.info(f"Created data collection script: {data_collection_script}")

    def setup_model_monitoring(self):
        """Setup model monitoring and drift detection"""
        logger.info("Setting up model monitoring...")

        monitoring_script = (
            self.project_root
            / "monitoring"
            / "model-monitoring"
            / "monitor_emotion_models.py"
        )

        monitoring_code = '''#!/usr/bin/env python3
"""
Model Monitoring and Drift Detection
Real-time monitoring of ML model performance and data drift
"""

import redis
import json
import numpy as np
import pandas as pd
from datetime import datetime, timedelta
import mlflow
import logging

class ModelMonitor:
    def __init__(self, redis_host='localhost', redis_port=6379):
        self.redis_client = redis.Redis(host=redis_host, port=redis_port, decode_responses=True)
        self.logger = logging.getLogger(__name__)
        
    def log_prediction(self, model_version, prediction_confidence, actual_label=None):
        """Log model prediction for monitoring"""
        timestamp = datetime.utcnow().isoformat()
        
        log_entry = {
            'timestamp': timestamp,
            'model_version': model_version,
            'prediction_confidence': prediction_confidence,
            'actual_label': actual_label
        }
        
        # Store in Redis for real-time monitoring
        self.redis_client.lpush('model_predictions', json.dumps(log_entry))
        
        # Keep only last 10000 predictions
        self.redis_client.ltrim('model_predictions', 0, 9999)
        
        # Log to MLflow for long-term tracking
        mlflow.log_metrics({
            'prediction_confidence': prediction_confidence,
            'prediction_accuracy': 1.0 if actual_label else None
        })
    
    def calculate_drift_metrics(self, window_size=1000):
        """Calculate data drift metrics"""
        # Get recent predictions
        recent_predictions = self.redis_client.lrange('model_predictions', 0, window_size-1)
        recent_predictions = [json.loads(p) for p in recent_predictions]
        
        if len(recent_predictions) < 100:
            return None
        
        # Calculate confidence distribution
        confidences = [p['prediction_confidence'] for p in recent_predictions]
        avg_confidence = np.mean(confidences)
        confidence_std = np.std(confidences)
        
        # Calculate prediction entropy (measure of uncertainty)
        confidence_hist = np.histogram(confidences, bins=10)[0]
        entropy = -np.sum((confidence_hist / len(confidences)) * 
                         np.log2(confidence_hist / len(confidences) + 1e-10))
        
        drift_metrics = {
            'avg_confidence': avg_confidence,
            'confidence_std': confidence_std,
            'entropy': entropy,
            'sample_size': len(recent_predictions)
        }
        
        # Alert on significant drift
        if confidence_std > 0.3 or entropy > 2.0:
            self.logger.warning(f"Model drift detected: {drift_metrics}")
            
        return drift_metrics
    
    def check_model_performance(self, time_window_hours=1):
        """Check model performance over time window"""
        cutoff_time = datetime.utcnow() - timedelta(hours=time_window_hours)
        
        recent_predictions = self.redis_client.lrange('model_predictions', 0, -1)
        recent_predictions = [json.loads(p) for p in recent_predictions 
                          if datetime.fromisoformat(p['timestamp']) > cutoff_time]
        
        if len(recent_predictions) < 10:
            return None
        
        # Calculate accuracy if we have actual labels
        predictions_with_labels = [p for p in recent_predictions if p.get('actual_label')]
        
        if len(predictions_with_labels) > 0:
            correct_predictions = sum(1 for p in predictions_with_labels 
                                if p.get('predicted_correct', False))
            accuracy = correct_predictions / len(predictions_with_labels)
            
            performance_metrics = {
                'accuracy': accuracy,
                'total_predictions': len(predictions_with_labels),
                'time_window_hours': time_window_hours
            }
            
            # Log performance to MLflow
            mlflow.log_metrics(performance_metrics)
            
            return performance_metrics
        
        return None

if __name__ == "__main__":
    monitor = ModelMonitor()
    
    # Example monitoring loop
    import time
    while True:
        drift_metrics = monitor.calculate_drift_metrics()
        performance_metrics = monitor.check_model_performance()
        
        if drift_metrics:
            print(f"Drift metrics: {drift_metrics}")
        
        if performance_metrics:
            print(f"Performance metrics: {performance_metrics}")
        
        time.sleep(300)  # Check every 5 minutes
'''

        with open(monitoring_script, "w") as f:
            f.write(monitoring_code)
        os.chmod(monitoring_script, 0o755)
        logger.info(f"Created model monitoring script: {monitoring_script}")

    def setup_ci_cd_configs(self):
        """Setup CI/CD configurations for ML pipeline"""
        logger.info("Setting up CI/CD configurations...")

        # GitHub Actions workflow for ML training
        github_workflow = """name: ML Model Training

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  MLFLOW_TRACKING_URI: http://localhost:5000
  AWS_DEFAULT_REGION: us-west-2

jobs:
  train-models:
    runs-on: ubuntu-latest
    
    services:
      redis:
        image: redis:7
        ports:
          - 6379:6379
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: testpass
          POSTGRES_DB: ml_test
        ports:
          - 5432:5432
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v3
    
    - name: Setup Python
      uses: actions/setup-python@v4
      with:
        python-version: '3.11'
    
    - name: Install dependencies
      run: |
        pip install -r requirements.txt
    
    - name: Download datasets
      run: |
        python scripts/data-collection/collect_emotion_data.py
    
    - name: Train voice emotion model
      run: |
        python training/pipelines/train_voice_emotion.py
    
    - name: Evaluate model
      run: |
        python tests/ml-model-tests/test_voice_emotion.py
    
    - name: Log metrics
      run: |
        mlflow ui --port 5000 &
        sleep 10
    
    - name: Upload model artifacts
      uses: actions/upload-artifact@v3
      with:
        name: emotion-models
        path: models/
"""

        workflow_dir = self.project_root / ".github" / "workflows"
        workflow_dir.mkdir(parents=True, exist_ok=True)

        workflow_file = workflow_dir / "ml-training.yml"
        with open(workflow_file, "w") as f:
            f.write(github_workflow)
        logger.info(f"Created GitHub Actions workflow: {workflow_file}")

    def run_complete_setup(self):
        """Execute the complete ML pipeline setup"""
        logger.info("Starting complete ML pipeline setup...")

        try:
            self.setup_python_environment()
            self.create_project_structure()
            self.setup_voice_emotion_training()
            self.setup_data_collection()
            self.setup_model_monitoring()
            self.setup_ci_cd_configs()

            logger.info("✅ ML pipeline setup completed successfully!")
            logger.info("🚀 Ready to start training voice emotion models")

            print("\\n" + "=" * 60)
            print("🎯 NEXT STEPS:")
            print("1. Run: python scripts/data-collection/collect_emotion_data.py")
            print("2. Train: python training/pipelines/train_voice_emotion.py")
            print(
                "3. Monitor: python monitoring/model-monitoring/monitor_emotion_models.py"
            )
            print("4. Deploy: Use CI/CD pipeline or manual deployment")
            print("=" * 60)

        except Exception as e:
            logger.error(f"❌ Setup failed: {e}")
            sys.exit(1)


if __name__ == "__main__":
    pipeline = EmotionalMLPipeline()
    pipeline.run_complete_setup()
