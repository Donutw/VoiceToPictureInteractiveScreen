# whisper_watcher.py
import os
import time
import whisper

model = whisper.load_model("small")
watched_dir = "AudioInput"
processed = set()

print("Whisper watcher running...")

while True:
    try:
        for f in os.listdir(watched_dir):
            if f.endswith(".wav"):
                path = os.path.join(watched_dir, f)
                if path in processed:
                    continue

                print(f"New file: {f}")
                result = model.transcribe(path)
                text = result["text"].strip()

                if text and len(text) >= 3:
                    txt_path = os.path.join("Transcripts", f.replace(".wav", ".txt"))
                    os.makedirs("Transcripts", exist_ok=True)
                    with open(txt_path, "w", encoding="utf-8") as out:
                        out.write(text)
                    print(f"Saved: {txt_path}")
                else:
                    print("Ignored empty or short result")

                os.remove(path)
                processed.add(path)

        time.sleep(1)
    except KeyboardInterrupt:
        break
