using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static void Save(AudioClip clip, string filepath)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        byte[] wavData = ConvertAudioClipToWav(samples, clip.channels, clip.frequency);
        File.WriteAllBytes(filepath, wavData);
    }

    private static byte[] ConvertAudioClipToWav(float[] samples, int channels, int sampleRate)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int sampleCount = samples.Length;

        int byteRate = sampleRate * channels * 2;

        // ะด RIFF Header
        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + sampleCount * 2);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

        // fmt chunk
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16); // Subchunk1Size
        writer.Write((short)1); // AudioFormat (1 = PCM)
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * 2)); // BlockAlign
        writer.Write((short)16); // BitsPerSample

        // data chunk
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(sampleCount * 2);

        // ะด PCM สýพÝ
        foreach (var s in samples)
        {
            short value = (short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue);
            writer.Write(value);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
