using System;
using UnityEngine;

public static class WavUtility
{
    private const int HeaderSize = 44;

    public static AudioClip ToAudioClip(
        byte[] wavFile,
        string clipName = "wav"
    )
    {
        if (
            wavFile == null ||
            wavFile.Length < HeaderSize
        )
        {
            throw new ArgumentException(
                "Invalid WAV file."
            );
        }

        int channels =
            BitConverter.ToInt16(
                wavFile,
                22
            );

        int sampleRate =
            BitConverter.ToInt32(
                wavFile,
                24
            );

        int bitsPerSample =
            BitConverter.ToInt16(
                wavFile,
                34
            );

        int dataPosition =
            FindDataChunk(
                wavFile
            );

        int dataSize =
            BitConverter.ToInt32(
                wavFile,
                dataPosition + 4
            );

        int audioStart =
            dataPosition + 8;

        if (bitsPerSample != 16)
        {
            throw new NotSupportedException(
                $"Only 16-bit PCM WAV is supported. " +
                $"Received {bitsPerSample}-bit."
            );
        }

        int sampleCount =
            dataSize / 2;

        float[] samples =
            new float[sampleCount];

        int offset =
            audioStart;

        for (
            int index = 0;
            index < sampleCount;
            index++
        )
        {
            short sample =
                BitConverter.ToInt16(
                    wavFile,
                    offset
                );

            samples[index] =
                sample / 32768f;

            offset += 2;
        }

        int frameCount =
            sampleCount /
            Mathf.Max(
                channels,
                1
            );

        AudioClip clip =
            AudioClip.Create(
                clipName,
                frameCount,
                channels,
                sampleRate,
                false
            );

        clip.SetData(
            samples,
            0
        );

        return clip;
    }

    private static int FindDataChunk(
        byte[] wavFile
    )
    {
        for (
            int index = 12;
            index < wavFile.Length - 8;
            index++
        )
        {
            if (
                wavFile[index] == 'd' &&
                wavFile[index + 1] == 'a' &&
                wavFile[index + 2] == 't' &&
                wavFile[index + 3] == 'a'
            )
            {
                return index;
            }
        }

        throw new ArgumentException(
            "WAV data chunk was not found."
        );
    }
}