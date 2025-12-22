using NVorbis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Sound
{
    public class AudioClipPipeline : IDataPipeline<IRegister<AudioClip>, AudioClip>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<AudioClipPipeline> logger;

        public AudioClipPipeline(PluginAtlas atlas, IModLogger<AudioClipPipeline> logger)
        {
            this.atlas = atlas;
            this.logger = logger;
        }

        public List<IDefinition<AudioClip>> Run(IRegister<AudioClip> service)
        {
            var definitions = new List<IDefinition<AudioClip>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                var key = config.Key;
                foreach (var soundConfig in config.Value.Configuration.GetSection("audio_clips").GetChildren())
                {
                    var id = soundConfig.GetSection("id").Value;
                    var path = soundConfig.GetSection("path").Value;
                    if (path == null || id == null)
                    {
                        continue;
                    }

                    if (!path.EndsWith(".wav") && !path.EndsWith(".ogg"))
                    {
                        continue;
                    }

                    var name = key.GetId(TemplateConstants.AudioClip, id);

                    foreach (var directory in config.Value.AssetDirectories)
                    {
                        var fullpath = Path.Combine(directory, path);
                        if (!File.Exists(fullpath))
                        {
                            logger.Log(LogLevel.Warning, $"Could not find asset at path: {fullpath}. Sprite will not exist.");
                            continue;
                        }

                        AudioClip? clip;
                        if (path.EndsWith(".wav"))
                            clip = LoadWavFile(fullpath);
                        else 
                            clip = LoadOggFile(fullpath);

                        if (clip == null)
                            continue;

                        clip.name = name;
                        service.Register(name, clip);
                        break;
                    }
                }
            }
            return definitions;
        }

        private AudioClip? LoadWavFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int pos = 0;

            // --- RIFF header ---
            if (ReadString(bytes, ref pos, 4) != "RIFF")
            {
                logger.Log(LogLevel.Error, $"Invalid WAV file {path}: RIFF header not found.");
                return null;
            }
                

            pos += 4; // skip file size

            if (ReadString(bytes, ref pos, 4) != "WAVE")
            {
                logger.Log(LogLevel.Error, $"Invalid WAV file {path}: WAVE header not found.");
                return null;
            }
                

            // --- Search chunks until fmt is found ---
            int channels = 0, sampleRate = 0, bitsPerSample = 0;
            int dataStart = 0, dataSize = 0;
            bool fmtFound = false, dataFound = false;

            while (pos < bytes.Length)
            {
                string chunkId = ReadString(bytes, ref pos, 4);
                int chunkSize = BitConverter.ToInt32(bytes, pos); pos += 4;

                if (chunkId == "fmt ")
                {
                    fmtFound = true;

                    int audioFormat = BitConverter.ToInt16(bytes, pos); pos += 2;
                    channels = BitConverter.ToInt16(bytes, pos); pos += 2;
                    sampleRate = BitConverter.ToInt32(bytes, pos); pos += 4;

                    pos += 6; // skip byteRate + blockAlign

                    bitsPerSample = BitConverter.ToInt16(bytes, pos); pos += 2;

                    if (audioFormat != 1)
                    {
                        logger.Log(LogLevel.Error, $"Invalid WAV file {path}: WAV format {audioFormat} not supported. Only PCM supported.");
                        return null;
                    }
                        

                    if (bitsPerSample != 16)
                    { 
                        logger.Log(LogLevel.Error, $"Invalid WAV file {path}: Only 16-bit PCM WAV supported. Got {bitsPerSample}-bit.");
                        return null;
                    }

                    pos = pos - 16 + chunkSize; // skip rest of fmt chunk
                }
                else if (chunkId == "data")
                {
                    dataFound = true;
                    dataSize = chunkSize;
                    dataStart = pos;
                    break;
                }
                else
                {
                    // Skip unneeded chunks
                    pos += chunkSize;
                }
            }

            if (!fmtFound)
            {
                logger.Log(LogLevel.Error, $"Invalid WAV file {path}: Missing fmt chunk.");
                return null;
            }
                

            if (!dataFound)
            {
                logger.Log(LogLevel.Error, $"Invalid WAV file {path}: Missing data chunk.");
                return null;
            }

            // --- Decode PCM 16-bit ---
            int sampleCount = dataSize / 2; // 2 bytes per sample
            float[] samples = new float[sampleCount];

            int offset = dataStart;
            for (int i = 0; i < sampleCount; i++)
                samples[i] = BitConverter.ToInt16(bytes, offset + i * 2) / 32768f;

            AudioClip clip = AudioClip.Create(
                Path.GetFileNameWithoutExtension(path),
                sampleCount / channels,
                channels,
                sampleRate,
                false
            );

            clip.SetData(samples, 0);
            return clip;
        }

        private static string ReadString(byte[] data, ref int pos, int length)
        {
            string s = Encoding.ASCII.GetString(data, pos, length);
            pos += length;
            return s;
        }

        public AudioClip? LoadOggFile(string path)
        {
            try
            {
                byte[] data = File.ReadAllBytes(path);
                using var ms = new MemoryStream(data);
                using var vorbis = new VorbisReader(ms, false);
                int channels = vorbis.Channels;
                int sampleRate = vorbis.SampleRate;
                int totalSamples = (int)vorbis.TotalSamples;
                float[] samples = new float[totalSamples * channels];
                vorbis.ReadSamples(samples, 0, samples.Length);
                AudioClip clip = AudioClip.Create(
                    Path.GetFileNameWithoutExtension(path),
                    totalSamples,
                    channels,
                    sampleRate,
                    false
                );

                clip.SetData(samples, 0);
                return clip;
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, $"Could not load ogg file {path}, {e.Message}. Only Vorbis ogg files are supported.");
                return null;
            }
        }
    }
}
