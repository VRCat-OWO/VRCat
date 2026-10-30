# -*- coding: utf-8 -*-
"""
生成宣传片配乐：柔和的环境垫底音（无鼓），45 秒，44.1kHz 立体声 WAV。
纯 numpy 合成：低频三角波 + 多层正弦谐波 pad（各自慢起慢收）+ 极轻的噪声空气层 + 单音点缀。
"""
import os
import wave

import numpy as np

SR = 44100
DUR = 45.0
N = int(SR * DUR)
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "dist", "promo-audio.wav")

# 和弦进行（Hz），4 个和弦循环，每个 6 秒
CHORDS = [
    [110.00, 261.63, 329.63, 392.00],   # Am7  A2 C4 E4 G4
    [87.31, 220.00, 261.63, 329.63],    # Fmaj7 F2 A3 C4 E4
    [130.81, 261.63, 329.63, 392.00],   # C     C3 C4 E4 G4
    [98.00, 246.94, 293.66, 392.00],    # G     G2 B3 D4 G4
]
CHORD_LEN = 6.0


def env_adsr(n, attack, release, sr=SR):
    a = int(attack * sr)
    r = int(release * sr)
    e = np.ones(n)
    if a > 0:
        e[:a] = np.linspace(0, 1, a) ** 1.6
    if r > 0 and r < n:
        e[-r:] = np.linspace(1, 0, r) ** 1.8
    return e


def pad_note(freq, start, length, amp, detune=0.0, harm=6):
    """一个柔和音符：谐波按 1/k^1.7 递减，带轻微失谐做宽度"""
    i0 = int(start * SR)
    n = int(length * SR)
    if i0 >= N:
        return np.zeros((0, 2))
    n = min(n, N - i0)
    t = np.arange(n) / SR
    wav = np.zeros(n)
    for k in range(1, harm + 1):
        f = freq * k * (1.0 + detune * (1 if k % 2 else -1))
        if f > SR / 2.2:
            break
        wav += np.sin(2 * np.pi * f * t + k * 0.7) / (k ** 1.7)
    wav *= env_adsr(n, 1.1, 1.9) * amp
    return wav


def build():
    left = np.zeros(N)
    right = np.zeros(N)
    t = np.arange(N) / SR

    # 1) pad：每个和弦 4 个音，左右交替偏移
    for ci in range(int(np.ceil(DUR / CHORD_LEN))):
        start = ci * CHORD_LEN
        if start >= DUR:
            break
        chord = CHORDS[ci % len(CHORDS)]
        for ni, f in enumerate(chord):
            amp = 0.16 if ni == 0 else 0.11
            for side, det in ((-1, 0.0016), (1, -0.0016)):
                w = pad_note(f * 2 if ni == 0 else f, start, CHORD_LEN + 2.4, amp, det)
                if len(w) == 0:
                    continue
                i0 = int(start * SR)
                seg = w[: max(0, N - i0)]
                if side < 0:
                    left[i0:i0 + len(seg)] += seg * 0.9
                else:
                    right[i0:i0 + len(seg)] += seg * 0.9

    # 2) 低频 sub：只跟根音，很轻
    for ci in range(int(np.ceil(DUR / CHORD_LEN))):
        start = ci * CHORD_LEN
        if start >= DUR:
            break
        f = CHORDS[ci % len(CHORDS)][0] / 2
        i0 = int(start * SR)
        n = min(int((CHORD_LEN + 1.6) * SR), N - i0)
        if n <= 0:
            continue
        seg = np.sin(2 * np.pi * f * np.arange(n) / SR) * env_adsr(n, 1.4, 1.6) * 0.10
        left[i0:i0 + n] += seg
        right[i0:i0 + n] += seg

    # 3) 空气层：低通噪声
    rng = np.random.default_rng(7)
    noise = rng.standard_normal(N).astype(np.float64)
    k = 220
    kernel = np.ones(k) / k
    air = np.convolve(noise, kernel, mode="same") * 0.05
    air *= np.clip(np.minimum(t / 3.0, (DUR - t) / 4.0), 0, 1)
    left += air
    right += np.roll(air, 37) * 0.98

    # 4) 点缀：每 2 秒一个很轻的高音单点
    for blip in np.arange(2.0, DUR - 2.0, 2.0):
        ci = int(blip / CHORD_LEN) % len(CHORDS)
        f = CHORDS[ci][2] * 2
        i0 = int(blip * SR)
        n = min(int(0.9 * SR), N - i0)
        if n <= 0:
            continue
        seg = np.sin(2 * np.pi * f * np.arange(n) / SR) * np.exp(-np.arange(n) / (0.22 * SR)) * 0.05
        left[i0:i0 + n] += seg * 0.8
        right[i0:i0 + n] += seg

    # 5) 整体包络 + 归一化
    fade = np.clip(np.minimum(t / 1.6, (DUR - t) / 3.2), 0, 1)[:, None]
    st = np.stack([left, right], axis=1) * fade
    peak = np.max(np.abs(st))
    if peak > 0:
        st = st / peak * 0.72
    return (st * 32767).astype(np.int16)


def main():
    st = build()
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with wave.open(OUT, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(st.tobytes())
    print("audio:", OUT, "%.1f MB" % (os.path.getsize(OUT) / 1024 / 1024))


if __name__ == "__main__":
    main()
