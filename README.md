# audio_connection

This repository shows how to route audio between two Python programs on macOS using the
[BlackHole 16ch](https://existential.audio/blackhole/) virtual driver.
The **sender** generates white noise and writes it to BlackHole. The **receiver**
reads from the same device, applies a 1 kHz low‑pass filter and plays the result on
the system output.

## Requirements

- Python 3
- `pyaudio` and `numpy` installed (`pip install pyaudio numpy`)

## Configuring Audio MIDI Setup (macOS)

1. Install **BlackHole 16ch** from the link above.
2. Open *Audio MIDI Setup* and ensure the BlackHole device runs at 44.1 kHz with
   at least two channels enabled.
3. Optionally create a Multi‑Output device that aggregates BlackHole with your
   speakers so the sender's output can be monitored.

## Usage

Run the sender in one terminal:

```bash
python sender.py
```

In another terminal run the receiver (which captures from BlackHole and plays
on the default speakers):

```bash
python receiver.py
```

Use `--input-device` or `--output-device` to override the device names if
necessary. Both scripts log basic information about the active devices so you
can verify the routing.

