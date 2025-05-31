import socket
import argparse
import math
from array import array
import pyaudio

CHUNK = 1024
CHANNELS = 2
RATE = 44100
FORMAT = pyaudio.paInt16


def find_device_index(p, name):
    for i in range(p.get_device_count()):
        info = p.get_device_info_by_index(i)
        if name.lower() in info['name'].lower() and info['maxOutputChannels'] >= CHANNELS:
            return i
    return None


def main(port, device_name, cutoff):
    pa = pyaudio.PyAudio()
    device_index = None
    if device_name:
        device_index = find_device_index(pa, device_name)

    stream = pa.open(format=FORMAT,
                     channels=CHANNELS,
                     rate=RATE,
                     output=True,
                     output_device_index=device_index,
                     frames_per_buffer=CHUNK)

    dt = 1.0 / RATE
    rc = 1.0 / (2 * math.pi * cutoff)
    alpha = dt / (rc + dt)
    prev = [0] * CHANNELS

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind(("", port))

    print(f"Listening on port {port}")
    try:
        while True:
            data, _ = sock.recvfrom(CHUNK * CHANNELS * 2)
            samples = array('h', data)
            for i in range(0, len(samples), CHANNELS):
                for c in range(CHANNELS):
                    x = samples[i + c]
                    prev[c] = int(alpha * x + (1 - alpha) * prev[c])
                    samples[i + c] = prev[c]
            stream.write(samples.tobytes())
    except KeyboardInterrupt:
        pass
    finally:
        stream.stop_stream()
        stream.close()
        pa.terminate()
        sock.close()

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Receive audio via UDP using PyAudio.")
    parser.add_argument("--port", type=int, default=50007, help="Port to listen on")
    parser.add_argument("--device", default=None, help="Output device name, e.g. 'BlackHole 16ch'")
    parser.add_argument("--cutoff", type=float, default=1000.0,
                        help="Low-pass filter cutoff frequency in Hz")
    args = parser.parse_args()
    main(args.port, args.device, args.cutoff)
