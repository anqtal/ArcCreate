using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ArcCreate.Compose.Components;
using ArcCreate.SceneTransition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ArcCreate.Compose.Rendering
{
    public class FrameRenderer : IDisposable
    {
        public delegate void RenderStatusDelegate(TimeSpan passed, TimeSpan remaining);

        private readonly AudioRenderer audioRenderer;
        private readonly Camera[] cameras;
        private readonly RenderTexture[] defaultRenderTextures;
        private readonly float endRenderingTime;
        private readonly GameplayViewport gameplayViewport;
        private readonly string outputPath;
        private readonly RenderTexture renderTexture;
        private readonly RenderSetting settings;
        private readonly bool showShutter;
        private readonly float startRenderingTime;
        private readonly TransitionSequence transitionSequence;
        private byte[] cachedByteArray;

        public FrameRenderer(
            string outputPath,
            Camera[] cameras,
            RenderSetting settings,
            int from,
            int to,
            AudioRenderer audioRenderer,
            bool showShutter,
            GameplayViewport gameplayViewport,
            TransitionSequence transitionSequence)
        {
            this.audioRenderer = audioRenderer;
            this.settings = settings;
            this.cameras = cameras;
            this.outputPath = outputPath;
            this.showShutter = showShutter;
            this.gameplayViewport = gameplayViewport;
            this.transitionSequence = transitionSequence;

            startRenderingTime = from / 1000f;
            endRenderingTime = to / 1000f;

            renderTexture = new RenderTexture(settings.Width, settings.Height, 24, RenderTextureFormat.ARGB32);
            Texture2D = new Texture2D(settings.Width, settings.Height, TextureFormat.ARGB32, false, true);
            defaultRenderTextures = new RenderTexture[cameras.Length];
            for (var i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                defaultRenderTextures[i] = cam.targetTexture;
            }
        }

        public Texture2D Texture2D { get; }

        public void Dispose()
        {
            Object.Destroy(renderTexture);
            Object.Destroy(Texture2D);
        }

        public async UniTask RenderVideo(CancellationToken token, RenderStatusDelegate onETA)
        {
            if (!TestFfmpeg()) return;

            var activeRT = RenderTexture.active;
            gameplayViewport.enabled = false;
            Services.Gameplay.SetCameraViewportRect(new Rect(0, 0, 1, 1));
            Services.Gameplay.SetCameraEnabled(true);

            foreach (var cam in cameras) cam.targetTexture = renderTexture;

            Process ffmpegProcess = null;
            BinaryWriter ffmpegWriter = null;
            try
            {
                ffmpegProcess = GetFFmpegProcess(outputPath, audioRenderer.SfxAudioList);
                ffmpegWriter = new BinaryWriter(ffmpegProcess.StandardInput.BaseStream);
            }
            catch (Exception e)
            {
                Debug.LogError(I18n.S("Compose.Exception.Render.FFmpeg.Start", new Dictionary<string, object>
                {
                    { "Message", e.Message },
                    { "StackTrace", e.StackTrace }
                }));

                ffmpegProcess?.Dispose();
                ffmpegWriter?.Dispose();
            }

            try
            {
                var startAt = DateTime.Now;
                Time.captureFramerate = Mathf.RoundToInt(settings.Fps);
                Services.Gameplay.Audio.AudioTiming = Mathf.RoundToInt(startRenderingTime * 1000);
                Services.Gameplay.Audio.IsRendering = true;
                Services.Grid.IsGridEnabled = false;
                Services.Cursor.EnableLaneCursor = false;
                await Services.Gameplay.Audio.PrepareVideoPlayback();
                await UniTask.Delay(500);

                var shouldUpdateTiming = false;
                var unityStartTime = Time.time;
                float bonusDuration = 0;
                if (showShutter)
                {
                    transitionSequence
                        .Show()
                        .ContinueWith(() => UniTask.Delay(transitionSequence.WaitDurationMs))
                        .ContinueWith(transitionSequence.Hide)
                        .ContinueWith(() => { shouldUpdateTiming = true; }).AttachExternalCancellation(token).Forget();

                    unityStartTime += transitionSequence.FullSequenceSeconds;
                    bonusDuration = transitionSequence.FullSequenceSeconds;
                }
                else
                {
                    shouldUpdateTiming = true;
                }

                while (!token.IsCancellationRequested)
                {
                    if (ffmpegProcess.HasExited) break;

                    Time.timeScale = 1;
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                    foreach (var cam in cameras)
                    {
                        cam.targetTexture = renderTexture;
                        RenderTexture.active = cam.targetTexture;
                        cam.Render();
                    }

                    Texture2D.ReadPixels(new Rect(0, 0, settings.Width, settings.Height), 0, 0);
                    Texture2D.Apply();

                    Profiler.BeginSample("Renderer: Extract bytes");
                    var bytes = Texture2D.GetRawTextureData<byte>();
                    if (bytes.Length != cachedByteArray?.Length) cachedByteArray = new byte[bytes.Length];

                    bytes.CopyTo(cachedByteArray);
                    ffmpegWriter.Write(cachedByteArray);

                    Profiler.EndSample();

                    var time = Time.time - unityStartTime + startRenderingTime;
                    if ((time * 1000 > Services.Gameplay.Audio.AudioLength || time > endRenderingTime) &&
                        shouldUpdateTiming) break;

                    if (shouldUpdateTiming)
                        Services.Gameplay.Audio.SetAudioTimingSilent(
                            Mathf.RoundToInt(time * 1000 + Settings.GlobalAudioOffset.Value));

                    Time.timeScale = 0;

                    var elapsed = DateTime.Now - startAt;
                    var speed = (time - startRenderingTime + bonusDuration) / elapsed.TotalSeconds;
                    if (speed > Mathf.Epsilon)
                    {
                        var eta = TimeSpan.FromSeconds((endRenderingTime - time + bonusDuration) / speed);
                        onETA.Invoke(elapsed, eta);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(I18n.S("Compose.Exception.Render.FFmpeg.Write", new Dictionary<string, object>
                {
                    { "Message", e.Message },
                    { "StackTrace", e.StackTrace }
                }));
            }
            finally
            {
                ffmpegProcess?.StandardInput.BaseStream.Close();
                ffmpegProcess?.WaitForExit();
                ffmpegProcess?.Dispose();
                ffmpegWriter?.Dispose();

                // Weird bug occurs if you remove this. I encourage everyone reading this source code to try it out lol.
                await UniTask.DelayFrame(60);

                RenderTexture.active = activeRT;
                for (var i = 0; i < cameras.Length; i++)
                {
                    var cam = cameras[i];
                    cam.targetTexture = defaultRenderTextures[i];
                }

                Time.captureFramerate = 0;
                Time.timeScale = 1;
                transitionSequence.DisableGameObject();
                gameplayViewport.enabled = true;
                Services.Gameplay.Audio.IsRendering = false;
                Directory.Delete(GetPath(), true);
            }
        }

        private bool TestFfmpeg()
        {
            try
            {
                var testFFmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Settings.FFmpegPath.Value,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                testFFmpegProcess.Start();
                return true;
            }
            catch (Exception)
            {
                Debug.LogError(I18n.S("Compose.Exception.Render.FFmpeg.NotFound", Settings.FFmpegPath.Value));
            }

            return false;
        }

        private Process GetFFmpegProcess(string videoPath, List<string> sfxAudioList)
        {
            var path = GetPath();

            // libx264 doesn't believe in odd sizes
            var w = Mathf.Max(settings.Width - settings.Width % 2, 2);
            var h = Mathf.Max(settings.Height - settings.Height % 2, 2);
            videoPath = videoPath.Replace(@"""", @"\""");
            if (!videoPath.ToLower().EndsWith(".mp4")) videoPath += ".mp4";

            Debug.Log($"Writing to {videoPath}");

            var argsForSfxAudio = "";
            foreach (var key in sfxAudioList) argsForSfxAudio += $"-i \"{Path.Combine(path, $"sfx_{key}.wav")}\" ";
#pragma warning disable
            var args = ""
                       + $" -f rawvideo -pixel_format argb -video_size {settings.Width}x{settings.Height} -framerate {settings.Fps} -i pipe: "
                       + $"-i \"{Path.Combine(path, "sfx.wav")}\" " // First audio (sfx)
                       + $"-i \"{Path.Combine(path, "song.wav")}\" " // Second audio (sfx)
                       + argsForSfxAudio
                       + $"-filter_complex amix=inputs={2 + sfxAudioList.Count}:duration=longest " //Mix audio files
                       + "-c:v libx264 " //Video codec
                       + "-c:a aac " //Audio codec
                       + "-pix_fmt yuv420p " //Set pixel format for QuickTime
                       + $"-crf {settings.Crf} " //Video quality
                       + $"-vf vflip,scale={w}x{h} " //Video size, vflip because screenshot in byte array is upside down
                       + "-b:a 384k " //Audio quality
                       + "-bf 2 " //2 B-frames
                       + "-flags +cgop " //Closed GOP (as it should be)
                       + "-movflags +faststart " //Move stream info to the beginning of file
                       + "-preset ultrafast " //Creates slightly larger file but speeds up rendering drastically
                       + $"-y -- \"{videoPath}\"";
#pragma warning restore

            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Settings.FFmpegPath.Value,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            ffmpegProcess.Start();
            ffmpegProcess.ErrorDataReceived += (sender, eventArgs) => { Debug.Log(eventArgs.Data); };
            ffmpegProcess.BeginErrorReadLine();
            ffmpegProcess.OutputDataReceived += (sender, eventArgs) => { Debug.Log(eventArgs.Data); };
            ffmpegProcess.BeginOutputReadLine();

            return ffmpegProcess;
        }

        private string GetPath(string fileName = "")
        {
            var path = Path.Combine(Path.GetDirectoryName(Services.Project.CurrentProject.Path), ".rendering",
                fileName);

            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));

            return path;
        }
    }
}