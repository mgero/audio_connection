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

### Receiver

Start the receiver to listen for the incoming audio stream and play it:

```bash
python receiver.py --port 50007
```

By default the receiver also searches for `BlackHole 16ch`. Use `--device` to
override the output device if needed.
