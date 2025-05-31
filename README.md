# audio_connection

This repository contains two small Python applications for streaming audio noise using the `pyaudio` library. The noise is generated on the sender side and transmitted via UDP. The receiver plays the audio back, optionally using the **BlackHole 16ch** virtual device.

## Requirements

- Python 3
- `pyaudio` installed (`pip install pyaudio`)

## Usage

### Sender

Run the sender to generate white noise and transmit it to a receiver:

```bash
python sender.py --host <receiver_host> --port 50007
```

The sender automatically looks for an output device named `BlackHole 16ch` and
opens a two-channel 44.1 kHz stream on it. If that device is not present, the
default output device is used.

The sender also writes the noise to the output device named `BlackHole 16ch` if available.

### Receiver

Start the receiver to listen for the incoming audio stream and play it:

```bash
python receiver.py --port 50007 --device "BlackHole 16ch"
```

Specify another device name with `--device` to use a different output device.

## Configuring Audio MIDI Setup (macOS)

These scripts assume the **BlackHole 16ch** driver is installed. If you already
have it, you can skip installation. Otherwise you can download it from
[existential.audio](https://existential.audio/blackhole/).

1. Open **Audio MIDI Setup** from `Applications → Utilities`.
2. Select `BlackHole 16ch` in the device list.
3. Set the format to **44100.0 Hz** and **2ch-16 bit Integer**.
4. Optionally create a multi-output device if you also want to monitor the
   stream through your speakers.

