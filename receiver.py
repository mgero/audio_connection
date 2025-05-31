import argparse
import logging
import numpy as np
import pyaudio

CHUNK = 1024
CHANNELS = 2
RATE = 44100
FORMAT = pyaudio.paInt16
DEFAULT_CUTOFF = 1000.0
INPUT_DEVICE = "BlackHole 16ch"
OUTPUT_DEVICE = None  # default system output


def find_device_index(pa, name, input=False):
    for i in range(pa.get_device_count()):
        info = pa.get_device_info_by_index(i)
        if name.lower() in info.get('name', '').lower():
            if input and info.get('maxInputChannels', 0) >= CHANNELS:
                return i
            if not input and info.get('maxOutputChannels', 0) >= CHANNELS:
                return i
    return None


def lowpass_filter(block, state, rate=RATE, cutoff=DEFAULT_CUTOFF):
    """Apply a simple first-order low-pass filter."""
    dt = 1.0 / rate
    rc = 1.0 / (2 * np.pi * cutoff)
    alpha = dt / (rc + dt)
    out = np.empty_like(block, dtype=np.float32)
    for ch in range(CHANNELS):
        prev = state[ch]
        x = block[:, ch]
        y = out[:, ch]
        for i, sample in enumerate(x):
            prev = prev + alpha * (sample - prev)
            y[i] = prev
        state[ch] = prev
    return out.astype(np.int16), state


def main():
    parser = argparse.ArgumentParser(description="Receive and filter audio from BlackHole")
    parser.add_argument("--input-device", default=INPUT_DEVICE, help="Input device name")
    parser.add_argument("--output-device", default=OUTPUT_DEVICE, help="Output device name (default system output)")
    parser.add_argument("--cutoff", type=float, default=DEFAULT_CUTOFF, help=f"Low-pass filter cutoff frequency in Hz (default: {DEFAULT_CUTOFF})")
    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO, format='%(asctime)s [%(levelname)s] %(message)s')

    pa = pyaudio.PyAudio()
    input_index = find_device_index(pa, args.input_device, input=True)
    if input_index is None:
        logging.error("Input device '%s' not found", args.input_device)
        return
    output_index = None
    if args.output_device:
        output_index = find_device_index(pa, args.output_device, input=False)
        if output_index is None:
            logging.warning("Output device '%s' not found, using default", args.output_device)

    in_stream = pa.open(
        format=FORMAT,
        channels=CHANNELS,
        rate=RATE,
        input=True,
        input_device_index=input_index,
        frames_per_buffer=CHUNK,
    )

    out_stream = pa.open(
        format=FORMAT,
        channels=CHANNELS,
        rate=RATE,
        output=True,
        output_device_index=output_index,
        frames_per_buffer=CHUNK,
    )

    state = np.zeros(CHANNELS, dtype=np.float32)
    logging.info("Capturing from '%s' and playing with low-pass filter (cutoff: %.1f Hz)", args.input_device, args.cutoff)
    try:
        while True:
            data = in_stream.read(CHUNK, exception_on_overflow=False)
            block = np.frombuffer(data, dtype=np.int16).reshape(-1, CHANNELS).astype(np.float32)
            filtered, state = lowpass_filter(block, state, cutoff=args.cutoff)
            out_stream.write(filtered.tobytes())
    except KeyboardInterrupt:
        logging.info("Interrupted by user")
    finally:
        in_stream.stop_stream()
        in_stream.close()
        out_stream.stop_stream()
        out_stream.close()
        pa.terminate()


if __name__ == "__main__":
    main()