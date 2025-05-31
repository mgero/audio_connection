import argparse
import logging
import numpy as np
import pyaudio

CHUNK = 1024
CHANNELS = 2
RATE = 44100
FORMAT = pyaudio.paInt16
DEVICE_NAME = "BlackHole 16ch"


def find_device_index(pa, name):
    """Return the index of the output device matching name."""
    for i in range(pa.get_device_count()):
        info = pa.get_device_info_by_index(i)
        if name.lower() in info.get('name', '').lower() and info.get('maxOutputChannels', 0) >= CHANNELS:
            return i
    return None


def main():
    parser = argparse.ArgumentParser(description="Stream white noise to BlackHole")
    parser.add_argument("--device", default=DEVICE_NAME, help="Output device name")
    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO, format='%(asctime)s [%(levelname)s] %(message)s')

    pa = pyaudio.PyAudio()
    device_index = find_device_index(pa, args.device)
    if device_index is None:
        logging.warning("Device '%s' not found, using default output", args.device)

    stream = pa.open(
        format=FORMAT,
        channels=CHANNELS,
        rate=RATE,
        output=True,
        output_device_index=device_index,
        frames_per_buffer=CHUNK,
    )

    logging.info("Streaming white noise to device '%s'", args.device)
    try:
        while True:
            noise = (np.random.randint(-32768, 32767, CHUNK * CHANNELS, dtype=np.int16)).tobytes()
            stream.write(noise)
    except KeyboardInterrupt:
        logging.info("Interrupted by user")
    finally:
        stream.stop_stream()
        stream.close()
        pa.terminate()


if __name__ == "__main__":
    main()
