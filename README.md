# audio_connection

This repository contains two small Python applications for streaming audio noise using the `pyaudio` library. The sender generates white noise and sends it via UDP while also playing it on the **BlackHole 16ch** device. The receiver plays the incoming data and processes it with a simple low-pass filter.

## Requirements

- Python 3
- `pyaudio` installed (`pip install pyaudio`)

## Usage

### Sender

Run the sender to generate white noise on two channels and transmit it to a receiver:

```bash
python sender.py --host <receiver_host> --port 50007
```

The sender also writes the noise to the output device named `BlackHole 16ch` if available.

### Receiver

Start the receiver to listen for the incoming audio stream, apply a low-pass filter, and play it:

```bash
python receiver.py --port 50007 --device "BlackHole 16ch" --cutoff 1000
```

Use `--device` to select the output device and `--cutoff` to change the filter cutoff frequency (Hz).
