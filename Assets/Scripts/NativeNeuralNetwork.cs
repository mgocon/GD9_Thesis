using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Native C# implementation of a feedforward neural network
/// Loads trained PyTorch weights from JSON and runs inference
/// No ONNX/Barracuda dependencies - pure C# implementation
/// </summary>
public class NativeNeuralNetwork
{
    private List<Layer> layers = new List<Layer>();
    private string modelType;
    private int actionDim = 6;
    
    public class Layer
    {
        public float[,] weights;
        public float[] bias;
        public bool useReLU;
        
        public Layer(JSONNode weightsData, JSONNode biasData, bool relu)
        {
            // weightsData is 2D array: [outputSize][inputSize]
            int outputSize = weightsData.Count;
            int inputSize = weightsData[0].Count;
            
            weights = new float[outputSize, inputSize];
            for (int i = 0; i < outputSize; i++)
            {
                for (int j = 0; j < inputSize; j++)
                {
                    weights[i, j] = weightsData[i][j].AsFloat;
                }
            }
            
            // biasData is 1D array
            bias = new float[biasData.Count];
            for (int i = 0; i < biasData.Count; i++)
            {
                bias[i] = biasData[i].AsFloat;
            }
            
            useReLU = relu;
        }
        
        public float[] Forward(float[] input)
        {
            int outputSize = weights.GetLength(0);
            float[] output = new float[outputSize];
            
            // Matrix multiplication: output = weights * input + bias
            for (int i = 0; i < outputSize; i++)
            {
                float sum = bias[i];
                for (int j = 0; j < input.Length; j++)
                {
                    sum += weights[i, j] * input[j];
                }
                
                // Apply ReLU activation if specified
                output[i] = useReLU ? Mathf.Max(0f, sum) : sum;
            }
            
            return output;
        }
    }
    
    /// <summary>
    /// Load neural network weights from JSON file
    /// </summary>
    public bool LoadFromJSON(string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"Model file not found: {jsonPath}");
                return false;
            }
            
            string jsonText = File.ReadAllText(jsonPath);
            JSONNode data = JSON.Parse(jsonText);
            
            if (data == null)
            {
                Debug.LogError($"Failed to parse JSON from {jsonPath}");
                return false;
            }
            
            modelType = data["model_type"];
            actionDim = data["action_dim"].AsInt;
            
            ParseWeights(data["weights"]);
            
            Debug.Log($"Loaded {modelType} model with {layers.Count} layers, {actionDim} actions");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load model: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }
    
    /// <summary>
    /// Parse weights from JSON structure and build layers
    /// </summary>
    private void ParseWeights(JSONNode weights)
    {
        layers.Clear();
        
        if (modelType == "DQN")
        {
            // DQN architecture: network.0, network.3, network.6, network.8
            // Layer 0: Input(30) -> Hidden1(256, ReLU)
            if (weights["network.0.weight"] != null && weights["network.0.bias"] != null)
                layers.Add(new Layer(weights["network.0.weight"], weights["network.0.bias"], true));
            
            // Layer 1: Hidden1(256) -> Hidden2(128, ReLU)
            if (weights["network.3.weight"] != null && weights["network.3.bias"] != null)
                layers.Add(new Layer(weights["network.3.weight"], weights["network.3.bias"], true));
            
            // Layer 2: Hidden2(128) -> Hidden3(64, ReLU)
            if (weights["network.6.weight"] != null && weights["network.6.bias"] != null)
                layers.Add(new Layer(weights["network.6.weight"], weights["network.6.bias"], true));
            
            // Layer 3: Hidden3(64) -> Output(6, no activation)
            if (weights["network.8.weight"] != null && weights["network.8.bias"] != null)
                layers.Add(new Layer(weights["network.8.weight"], weights["network.8.bias"], false));
        }
        else if (modelType == "PPO")
        {
            // PPO architecture: shared.0, shared.3, shared.6 (policy head might be missing)
            // Layer 0: Input(30) -> Hidden1(256, ReLU)
            if (weights["shared.0.weight"] != null && weights["shared.0.bias"] != null)
                layers.Add(new Layer(weights["shared.0.weight"], weights["shared.0.bias"], true));
            
            // Layer 1: Hidden1(256) -> Hidden2(128, ReLU)
            if (weights["shared.3.weight"] != null && weights["shared.3.bias"] != null)
                layers.Add(new Layer(weights["shared.3.weight"], weights["shared.3.bias"], true));
            
            // Layer 2: Hidden2(128) -> Hidden3(64, ReLU)
            if (weights["shared.6.weight"] != null && weights["shared.6.bias"] != null)
                layers.Add(new Layer(weights["shared.6.weight"], weights["shared.6.bias"], true));
            
            // Layer 3: Policy head (if available)
            if (weights["policy_head.weight"] != null && weights["policy_head.bias"] != null)
            {
                layers.Add(new Layer(weights["policy_head.weight"], weights["policy_head.bias"], false));
            }
            else
            {
                Debug.LogWarning("PPO policy_head not found in weights");
            }
        }
    }
    
    /// <summary>
    /// Run forward pass through the network
    /// </summary>
    public float[] Forward(float[] input)
    {
        float[] output = input;
        
        foreach (var layer in layers)
        {
            output = layer.Forward(output);
        }
        
        return output;
    }
    
    /// <summary>
    /// Get action from network output
    /// For DQN: returns argmax of Q-values
    /// For PPO: returns argmax of policy logits (after softmax)
    /// </summary>
    public int GetAction(float[] observation, out float confidence)
    {
        float[] output = Forward(observation);
        
        if (modelType == "DQN")
        {
            // DQN: Select action with highest Q-value
            int bestAction = 0;
            float maxQValue = output[0];
            
            for (int i = 1; i < output.Length; i++)
            {
                if (output[i] > maxQValue)
                {
                    maxQValue = output[i];
                    bestAction = i;
                }
            }
            
            // Normalize Q-value to [0,1] for confidence
            confidence = Mathf.Clamp01((maxQValue + 1f) / 2f);
            return bestAction;
        }
        else // PPO
        {
            // PPO: Apply softmax and take argmax
            float[] probabilities = Softmax(output);
            
            int bestAction = 0;
            float maxProb = probabilities[0];
            
            for (int i = 1; i < probabilities.Length; i++)
            {
                if (probabilities[i] > maxProb)
                {
                    maxProb = probabilities[i];
                    bestAction = i;
                }
            }
            
            confidence = maxProb;
            return bestAction;
        }
    }
    
    /// <summary>
    /// Apply softmax to convert logits to probabilities
    /// </summary>
    private float[] Softmax(float[] logits)
    {
        float[] probabilities = new float[logits.Length];
        float sumExp = 0f;
        
        // Find max for numerical stability
        float maxLogit = logits[0];
        for (int i = 1; i < logits.Length; i++)
        {
            if (logits[i] > maxLogit)
                maxLogit = logits[i];
        }
        
        // Compute exp and sum
        for (int i = 0; i < logits.Length; i++)
        {
            probabilities[i] = Mathf.Exp(logits[i] - maxLogit);
            sumExp += probabilities[i];
        }
        
        // Normalize
        for (int i = 0; i < probabilities.Length; i++)
        {
            probabilities[i] /= sumExp;
        }
        
        return probabilities;
    }
}
