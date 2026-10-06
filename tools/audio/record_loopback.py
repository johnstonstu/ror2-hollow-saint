"""Record the default output device (WASAPI loopback) to a 32-bit float WAV until a stop file
appears. Writes <out>.json with the wall-clock UTC ms of the first captured frame so the
recording can be aligned with DevAutopilot's audio-events.txt.
Usage: python record_loopback.py out.wav stopfile [max_seconds]
Needs: pip install pyaudiowpatch numpy (separate venv is fine)."""
import json, os, sys, time, wave
import numpy as np
import pyaudiowpatch as pyaudio

out, stop = sys.argv[1], sys.argv[2]
limit = float(sys.argv[3]) if len(sys.argv) > 3 else 1200.0
pa = pyaudio.PyAudio()
dev = pa.get_default_wasapi_loopback()
rate, ch = int(dev['defaultSampleRate']), int(dev['maxInputChannels'])
chunks, first = [], None

def cb(data, frames, info, status):
    global first
    if first is None:
        first = time.time() * 1000.0 - frames / rate * 1000.0
    chunks.append(data)
    return (None, pyaudio.paContinue)

stream = pa.open(format=pyaudio.paFloat32, channels=ch, rate=rate, input=True,
                 input_device_index=dev['index'], frames_per_buffer=1024, stream_callback=cb)
began = time.time()
while not os.path.exists(stop) and time.time() - began < limit:
    time.sleep(0.2)
stream.stop_stream(); stream.close(); pa.terminate()
audio = np.frombuffer(b''.join(chunks), dtype=np.float32)
# Float WAV (format 3) keeps >0 dBFS overs visible.
import struct
with open(out, 'wb') as f:
    data = audio.astype('<f4').tobytes()
    f.write(b'RIFF' + struct.pack('<I', 36 + len(data)) + b'WAVE')
    f.write(b'fmt ' + struct.pack('<IHHIIHH', 16, 3, ch, rate, rate * ch * 4, ch * 4, 32))
    f.write(b'data' + struct.pack('<I', len(data)) + data)
json.dump({'device': dev['name'], 'rate': rate, 'channels': ch, 'first_ms': first,
           'seconds': len(audio) / ch / rate}, open(out[:-4] + '.json', 'w'), indent=1)
print('recorded', round(len(audio) / ch / rate, 1), 's from', dev['name'])
