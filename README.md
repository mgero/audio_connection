# audio_connection

This repository shows how to route audio between a Python program (sender) ad another Python/Unity program (receiver) on macOS using the
[BlackHole 16ch](https://existential.audio/blackhole/) virtual driver.
The **sender** generates white noise and writes it to BlackHole. The **receiver**
reads from the same device, applies a configurable low‑pass filter and plays the result on
the system output.

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
python receiver.py
```

#### Receiver Options

- `--input-device`: Specify the input device name (default: "BlackHole 16ch")
- `--output-device`: Specify the output device name (default: system default)
- `--cutoff`: Set the low-pass filter cutoff frequency in Hz (default: 1000.0)

#### Examples

Use default settings (BlackHole 16ch input, 1000 Hz cutoff):
```bash
python receiver.py
```

Specify a custom cutoff frequency:
```bash
python receiver.py --cutoff 500
```

Use different input/output devices with custom filter:
```bash
python receiver.py --input-device "My Input" --output-device "My Output" --cutoff 2000
```

## Using the C# Receiver in Unity

1. Create an Audio Object in Unity and add an audio component.
2. Associate the C# `Receiver` class with the GameObject.
3. Configure the `Receiver` to listen and direct the audio stream to the GameObject's audio component.

## Configuring Audio MIDI Setup (macOS)

These scripts assume the **BlackHole 16ch** driver is installed. If you already
have it, you can skip installation. Otherwise you can download it from
[existential.audio](https://existential.audio/blackhole/).

1. Open **Audio MIDI Setup** from `Applications → Utilities`.
2. Select `BlackHole 16ch` in the device list.
3. Set the format to **44100.0 Hz** and **2ch-16 bit Integer**.
4. Optionally create a multi-output device if you also want to monitor the audio output.
