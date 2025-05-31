using UnityEngine;
using System.Collections;

public class Receiver : MonoBehaviour
{
    [Header("PyAudio Configuration (matching Python script)")]
    [SerializeField] private string inputDevice = "BlackHole 16ch";
    [SerializeField] private int CHUNK = 1024;      // Exact same as Python
    [SerializeField] private int CHANNELS = 2;      // Exact same as Python  
    [SerializeField] private int RATE = 44100;      // Exact same as Python
    
    [Header("Low-Pass Filter (exact Python implementation)")]
    [SerializeField] private float CUTOFF = 1000.0f;
    [SerializeField] private bool enableFilter = true;
    [Range(0.1f, 10000.0f)]
    [SerializeField] private float cutoffSlider = 1000.0f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private AudioSource audioSource;
    private AudioClip microphoneClip;
    private float[] filterState;        // Equivalent to Python's state
    private float alpha;
    private bool isProcessing = false;
    private string selectedInputDevice;
    private int lastReadPosition = 0;
    
    // Continuous circular buffer approach (like PyAudio internal buffer)
    private float[] circularBuffer;
    private int writePosition = 0;
    private int readPosition = 0;
    private int bufferSize = 16384; // Large buffer to prevent underruns
    private readonly object bufferLock = new object();
    
    // Processing buffers
    private float[] tempInputBuffer;
    private float[] tempOutputBuffer;

    void Start()
    {
        InitializeAudio();
    }

    private void InitializeAudio()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.volume = 1.0f;
        audioSource.spatialBlend = 0f;

        // Find input device
        selectedInputDevice = FindInputDevice(inputDevice);
        if (selectedInputDevice == null)
        {
            Debug.LogError($"Input device '{inputDevice}' not found");
            return;
        }

        Debug.Log($"Capturing from '{selectedInputDevice}' and playing with low-pass filter (cutoff: {CUTOFF} Hz)");

        // Initialize filter
        InitializeFilter();

        // Initialize continuous buffer
        circularBuffer = new float[bufferSize * CHANNELS];
        tempInputBuffer = new float[CHUNK * CHANNELS];
        tempOutputBuffer = new float[CHUNK * CHANNELS];

        // Start microphone
        if (StartMicrophoneCapture())
        {
            // Use OnAudioFilterRead instead of OnAudioRead for direct processing
            audioSource.clip = AudioClip.Create("DummyClip", 1024, CHANNELS, RATE, false);
            audioSource.Play();
            
            // Start continuous processing
            StartCoroutine(ContinuousAudioProcessing());
            
            Debug.Log("Continuous audio processing started");
        }
    }

    private string FindInputDevice(string deviceName)
    {
        foreach (string device in Microphone.devices)
        {
            Debug.Log($"Available device: {device}");
            if (device.ToLower().Contains(deviceName.ToLower()))
            {
                return device;
            }
        }
        
        if (Microphone.devices.Length > 0)
        {
            Debug.LogWarning($"Device '{deviceName}' not found, using: {Microphone.devices[0]}");
            return Microphone.devices[0];
        }
        
        return null;
    }

    private void InitializeFilter()
    {
        // Exact same formula as Python script
        float dt = 1.0f / RATE;
        float rc = 1.0f / (2.0f * Mathf.PI * CUTOFF);
        alpha = dt / (rc + dt);
        
        // Equivalent to Python's state = np.zeros(CHANNELS, dtype=np.float32)
        filterState = new float[CHANNELS];
        for (int i = 0; i < CHANNELS; i++)
        {
            filterState[i] = 0.0f;
        }
        
        Debug.Log($"Filter initialized - Alpha: {alpha}");
    }

    private bool StartMicrophoneCapture()
    {
        Debug.Log($"Starting microphone: device='{selectedInputDevice}', rate={RATE}");
        
        // Start microphone with 1 second buffer
        microphoneClip = Microphone.Start(selectedInputDevice, true, 1, RATE);
        
        if (microphoneClip == null)
        {
            Debug.LogError("Failed to start microphone");
            return false;
        }

        // Wait for microphone to start
        int timeout = 0;
        while (Microphone.GetPosition(selectedInputDevice) <= 0 && timeout < 1000)
        {
            System.Threading.Thread.Sleep(10);
            timeout += 10;
        }
        
        if (Microphone.GetPosition(selectedInputDevice) <= 0)
        {
            Debug.LogError("Microphone failed to start");
            return false;
        }

        Debug.Log($"Microphone started: {microphoneClip.channels}ch, {microphoneClip.frequency}Hz");
        isProcessing = true;
        return true;
    }

    private IEnumerator ContinuousAudioProcessing()
    {
        // Process as fast as possible to keep buffer filled
        while (isProcessing)
        {
            try
            {
                ProcessMicrophoneDataContinuous();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Audio processing error: {e.Message}");
            }
            
            // Very short yield to maintain continuous processing
            yield return null;
        }
        
        Debug.Log("Continuous audio processing stopped");
    }

    private void ProcessMicrophoneDataContinuous()
    {
        if (microphoneClip == null) return;

        int currentPosition = Microphone.GetPosition(selectedInputDevice);
        if (currentPosition < 0) return;

        // Calculate available samples
        int samplesAvailable;
        if (currentPosition < lastReadPosition)
        {
            samplesAvailable = (microphoneClip.samples - lastReadPosition) + currentPosition;
        }
        else
        {
            samplesAvailable = currentPosition - lastReadPosition;
        }

        // Process all available samples in chunks
        while (samplesAvailable > 0)
        {
            // Determine how many samples to process this iteration
            int samplesToProcess = Mathf.Min(samplesAvailable, CHUNK);
            if (microphoneClip.channels == 1 && CHANNELS == 2)
            {
                samplesToProcess = Mathf.Min(samplesAvailable, CHUNK / 2);
            }

            if (samplesToProcess == 0) break;

            // Read samples
            float[] inputSamples = new float[samplesToProcess * microphoneClip.channels];
            
            if (lastReadPosition + samplesToProcess > microphoneClip.samples)
            {
                // Handle wrap-around
                int firstPart = microphoneClip.samples - lastReadPosition;
                int secondPart = samplesToProcess - firstPart;
                
                float[] temp1 = new float[firstPart * microphoneClip.channels];
                float[] temp2 = new float[secondPart * microphoneClip.channels];
                
                microphoneClip.GetData(temp1, lastReadPosition);
                microphoneClip.GetData(temp2, 0);
                
                System.Array.Copy(temp1, 0, inputSamples, 0, temp1.Length);
                System.Array.Copy(temp2, 0, inputSamples, temp1.Length, temp2.Length);
                
                lastReadPosition = secondPart;
            }
            else
            {
                microphoneClip.GetData(inputSamples, lastReadPosition);
                lastReadPosition += samplesToProcess;
            }

            // Convert and process
            ProcessSamples(inputSamples, samplesToProcess);
            
            // Update counters
            samplesAvailable -= samplesToProcess;
        }
    }

    private void ProcessSamples(float[] inputSamples, int sampleCount)
    {
        // Convert mono to stereo if needed
        int outputSampleCount;
        if (microphoneClip.channels == 1 && CHANNELS == 2)
        {
            outputSampleCount = sampleCount * 2;
            for (int i = 0; i < sampleCount; i++)
            {
                tempInputBuffer[i * 2] = inputSamples[i];     // Left
                tempInputBuffer[i * 2 + 1] = inputSamples[i]; // Right
            }
        }
        else
        {
            outputSampleCount = inputSamples.Length;
            System.Array.Copy(inputSamples, 0, tempInputBuffer, 0, outputSampleCount);
        }

        // Apply low-pass filter (exact Python implementation)
        ApplyLowPassFilter(tempInputBuffer, tempOutputBuffer, outputSampleCount);

        // Add to circular buffer
        lock (bufferLock)
        {
            for (int i = 0; i < outputSampleCount; i++)
            {
                circularBuffer[writePosition] = tempOutputBuffer[i];
                writePosition = (writePosition + 1) % circularBuffer.Length;
                
                // Prevent overflow by advancing read position if needed
                if (writePosition == readPosition)
                {
                    readPosition = (readPosition + 1) % circularBuffer.Length;
                }
            }
        }
    }

    private void ApplyLowPassFilter(float[] input, float[] output, int sampleCount)
    {
        // Exact same implementation as Python lowpass_filter function
        float dt = 1.0f / RATE;
        float rc = 1.0f / (2.0f * Mathf.PI * CUTOFF);
        float currentAlpha = dt / (rc + dt);
        
        if (!enableFilter)
        {
            // If filter disabled, just copy input to output
            System.Array.Copy(input, 0, output, 0, sampleCount);
            return;
        }
        
        // Process each channel separately (exact Python implementation)
        for (int ch = 0; ch < CHANNELS; ch++)
        {
            float prev = filterState[ch];
            
            // Apply filter to each sample in this channel
            for (int i = ch; i < sampleCount; i += CHANNELS)
            {
                // Exact Python formula: prev = prev + alpha * (sample - prev)
                prev = prev + currentAlpha * (input[i] - prev);
                output[i] = prev;
            }
            
            // Update filter state (exact Python: state[ch] = prev)
            filterState[ch] = prev;
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        // This is called by Unity's audio system for real-time processing
        // Much more reliable than OnAudioRead for continuous streaming
        
        lock (bufferLock)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (readPosition != writePosition)
                {
                    data[i] = circularBuffer[readPosition];
                    readPosition = (readPosition + 1) % circularBuffer.Length;
                }
                else
                {
                    data[i] = 0f; // Output silence if buffer empty
                }
            }
        }
    }

    // Public methods for runtime control
    public void SetCutoffFrequency(float newCutoff)
    {
        CUTOFF = Mathf.Clamp(newCutoff, 0.1f, 10000.0f);
        cutoffSlider = CUTOFF;
        
        // Recalculate alpha
        float dt = 1.0f / RATE;
        float rc = 1.0f / (2.0f * Mathf.PI * CUTOFF);
        alpha = dt / (rc + dt);
        
        if (showDebugInfo)
        {
            Debug.Log($"Cutoff frequency set to: {CUTOFF:F1} Hz (Alpha: {alpha:F4})");
        }
    }

    [ContextMenu("Set Low Cutoff (200 Hz)")]
    public void SetLowCutoff() { SetCutoffFrequency(200f); }

    [ContextMenu("Set Mid Cutoff (1000 Hz)")]
    public void SetMidCutoff() { SetCutoffFrequency(1000f); }

    [ContextMenu("Set High Cutoff (5000 Hz)")]
    public void SetHighCutoff() { SetCutoffFrequency(5000f); }

    [ContextMenu("Toggle Filter")]
    public void ToggleFilter() { enableFilter = !enableFilter; }

    void Update()
    {
        if (Mathf.Abs(cutoffSlider - CUTOFF) > 0.1f)
        {
            SetCutoffFrequency(cutoffSlider);
        }
        
        // Show buffer status
        if (showDebugInfo && Time.frameCount % 300 == 0) // Every 5 seconds
        {
            lock (bufferLock)
            {
                int bufferedSamples = writePosition >= readPosition ? 
                    writePosition - readPosition : 
                    (circularBuffer.Length - readPosition) + writePosition;
                    
                float bufferSeconds = (float)bufferedSamples / (RATE * CHANNELS);
                Debug.Log($"Buffer status: {bufferedSamples} samples ({bufferSeconds:F3}s buffered)");
            }
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && isProcessing)
        {
            if (cutoffSlider != CUTOFF)
            {
                SetCutoffFrequency(cutoffSlider);
            }
        }
    }

    void OnDestroy()
    {
        StopProcessing();
    }

    void OnApplicationQuit()
    {
        StopProcessing();
    }

    private void StopProcessing()
    {
        Debug.Log("Stopping continuous audio processing...");
        isProcessing = false;
        
        if (selectedInputDevice != null)
        {
            Microphone.End(selectedInputDevice);
        }
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}