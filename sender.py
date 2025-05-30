import os
import socket
import argparse
import pyaudio

CHUNK = 1024
CHANNELS = 2
RATE = 44100
FORMAT = pyaudio.paInt16
DEVICE_NAME = "BlackHole 16ch"

def find_device_index(p, name):
    for i in range(p.get_device_count()):
        info = p.get_device_info_by_index(i)
        if name.lower() in info['name'].lower() and info['maxOutputChannels'] >= CHANNELS:
            return i
    return None

def main(host, port):
    pa = pyaudio.PyAudio()
    device_index = find_device_index(pa, DEVICE_NAME)

    stream = pa.open(format=FORMAT,
                     channels=CHANNELS,
                     rate=RATE,
                     output=True,
                     output_device_index=device_index,
                     frames_per_buffer=CHUNK)

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    print(f"Sending noise to {host}:{port}")
    try:
        while True:
            noise = os.urandom(CHUNK * CHANNELS * 2)
            stream.write(noise)
            sock.sendto(noise, (host, port))
    except KeyboardInterrupt:
        pass
    finally:
        stream.stop_stream()
        stream.close()
        pa.terminate()
        sock.close()

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Send noise via UDP using PyAudio.")
    parser.add_argument("--host", default="localhost", help="Receiver host")
    parser.add_argument("--port", type=int, default=50007, help="Receiver port")
    args = parser.parse_args()
    main(args.host, args.port)
