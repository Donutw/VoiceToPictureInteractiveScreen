# whisper_watcher.py
import os
import time
import whisper
import re


def count_meaningful_words(text):
    chinese_chars = re.findall(r'[\u4e00-\u9fff]', text)
    english_words = re.findall(r'\b[a-zA-Z]{2,}\b', text)
    return len(chinese_chars) + len(english_words)


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
                if "开始检测" in text or "start calibration" in text.lower():
                    with open("Trigger_Calibration.flag", "w", encoding="utf-8") as flag:
                        flag.write("triggered")

                if text and count_meaningful_words(text) >= 3:
                    os.makedirs("Transcripts", exist_ok=True)
                    temp_path = os.path.join("Transcripts", f.replace(".wav", ".txt.temp"))
                    with open(temp_path, "w", encoding="utf-8") as out:
                        out.write(text)
                    txt_path = temp_path.replace(".txt.temp", ".txt")
                    os.rename(temp_path, txt_path)
                    print(f"Saved: {txt_path}")
                else:
                    print("Ignored empty or short result")

                os.remove(path)
                processed.add(path)

        time.sleep(1)
    except KeyboardInterrupt:
        break
