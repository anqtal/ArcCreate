using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ArcCreate.Gameplay.Auth;
using ArcCreate.Gameplay.Score;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ArcCreate.Gameplay
{
    public static class DxResource
    {
        public enum FileType
        {
            SongAudio,
            SongImage,
            SongChart,
            PackImage,
            PreviewAudio,
            BackgroundImage
        }

        private const int MaxDownloadAttempts = 3;
        private const int MaxConcurrentDownloads = 8;
        private static readonly string ResPath = Path.Combine(Application.persistentDataPath, "res");
        private static readonly Dictionary<string, string> ManifestFileToHash = new(StringComparer.OrdinalIgnoreCase);
        private static UniTaskCompletionSource initTcs;

        public static async UniTask<byte[]> ReadFile(string id, FileType type, string chartFileName = null)
        {
            var fullPath = GetFilePath(id, type, chartFileName);
            if (!File.Exists(fullPath)) return null;
            var data = await File.ReadAllBytesAsync(fullPath);
            return ShouldXor(type) ? XorBytes(data, "erc") : data;
        }

        public static byte[] ReadFileSync(string id, FileType type, string chartFileName = null)
        {
            var fullPath = GetFilePath(id, type, chartFileName);
            if (!File.Exists(fullPath)) return null;

            var data = File.ReadAllBytes(fullPath);
            return ShouldXor(type) ? XorBytes(data, "erc") : data;
        }

        public static string GetLocalPath(string id, FileType type, string chartFileName = null)
        {
            return GetFilePath(id, type, chartFileName);
        }

        private static string GetFilePath(string id, FileType type, string chartFileName)
        {
            return type switch
            {
                FileType.SongAudio => Path.Combine(ResPath, "songs", $"dl_{id}", "base.audio"),
                FileType.SongImage => GetManifestPath($"{id}_jacket"),
                FileType.SongChart => Path.Combine(ResPath, "songs", $"dl_{id}", chartFileName ?? "2.chart"),
                FileType.PackImage => Path.Combine(ResPath, "pack", $"1080_select_{id}.png"),
                FileType.PreviewAudio => GetManifestPath($"{id}_preview"),
                FileType.BackgroundImage => GetManifestPathForBackground(id),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private static string GetManifestPath(string fileKey)
        {
            if (string.IsNullOrWhiteSpace(fileKey)) return string.Empty;

            if (!ManifestFileToHash.TryGetValue(fileKey, out var hash) || string.IsNullOrWhiteSpace(hash))
                return string.Empty;

            return Path.Combine(Application.persistentDataPath, "dl", hash);
        }

        private static string GetManifestPathForBackground(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;

            if (Path.HasExtension(id)) return GetManifestPath(id);

            var jpg = GetManifestPath($"{id}.jpg");
            if (!string.IsNullOrWhiteSpace(jpg)) return jpg;

            return GetManifestPath($"{id}.png");
        }

        private static bool ShouldXor(FileType type)
        {
            return type != FileType.SongImage && type != FileType.PreviewAudio && type != FileType.BackgroundImage;
        }

        // XOR 处理函数，key 以循环方式应用到 bytes 上
        private static byte[] XorBytes(byte[] data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var result = new byte[data.Length];

            for (var i = 0; i < data.Length; i++) result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);

            return result;
        }

        public static UniTask Init()
        {
            if (initTcs != null) return initTcs.Task;

            initTcs = new UniTaskCompletionSource();
            InitInternal().Forget();
            return initTcs.Task;
        }

        private static async UniTaskVoid InitInternal()
        {
            try
            {
                var manifestHashes = await FetchManifestHashes();
                if (manifestHashes.Count > 0) CleanupDlByManifest(manifestHashes);

                var items = BuildManifestDownloadList();
                if (items.Count > 0)
                {
                    var dialog = await DxResourceDownloadDialog.Show(items.Count);
                    if (dialog == null)
                    {
                        initTcs.TrySetResult();
                        return;
                    }

                    var completed = 0;
                    using var throttler = new SemaphoreSlim(MaxConcurrentDownloads);
                    var tasks = new List<UniTask>();
                    foreach (var item in items)
                        tasks.Add(DownloadWithThrottle(item, throttler, () =>
                        {
                            var current = Interlocked.Increment(ref completed);
                            UniTask.Void(async () =>
                            {
                                await UniTask.SwitchToMainThread();
                                dialog.UpdateProgress(current, items.Count);
                            });
                        }));

                    await UniTask.WhenAll(tasks);

                    dialog.Close();
                }

                await RefreshScoreCacheFromLogin();
                initTcs.TrySetResult();
            }
            catch (Exception e)
            {
                initTcs.TrySetException(e);
                throw;
            }
        }

        private static async UniTask<HashSet<string>> FetchManifestHashes()
        {
            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const string url = "https://erc.osiom.cc/api/manifest";
            using var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", "ArcCreate");
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download manifest: {request.error}");
                return hashes;
            }

            ManifestFileToHash.Clear();
            try
            {
                var root = JObject.Parse(request.downloadHandler.text);
                var value = root["value"];
                CollectManifestHashes(value?["bg"], hashes, ManifestFileToHash);
                CollectManifestHashes(value?["songs"], hashes, ManifestFileToHash);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse manifest: {e.Message}");
            }

            return hashes;
        }

        private static void CollectManifestHashes(JToken token, HashSet<string> hashes,
            Dictionary<string, string> fileToHash)
        {
            if (token is not JArray list) return;

            foreach (var item in list)
            {
                if (item is not JObject obj) continue;

                foreach (var prop in obj.Properties())
                    if (prop.Value.Type == JTokenType.String)
                    {
                        var hash = prop.Value.ToString();
                        hashes.Add(hash);
                        fileToHash[prop.Name] = hash;
                    }
            }
        }

        private static void CleanupDlByManifest(HashSet<string> validHashes)
        {
            var dlDir = Path.Combine(Application.persistentDataPath, "dl");
            if (!Directory.Exists(dlDir)) return;

            foreach (var file in Directory.GetFiles(dlDir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!IsHashName(name) || !validHashes.Contains(name)) File.Delete(file);
            }

            var directories = Directory.GetDirectories(dlDir, "*", SearchOption.AllDirectories);
            Array.Sort(directories, (a, b) => b.Length.CompareTo(a.Length));
            foreach (var dir in directories)
                if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                    Directory.Delete(dir, false);
        }

        private static bool IsHashName(string name)
        {
            return Regex.IsMatch(name, "^[0-9a-fA-F]{32}$");
        }

        private static async UniTask RefreshScoreCacheFromLogin()
        {
            LoginState.LoadFromPrefs();
            if (string.IsNullOrWhiteSpace(LoginState.UserId)) return;

            await ScoreCache.RefreshFromServer(LoginState.UserId);
        }

        private static async UniTask DownloadWithThrottle(DownloadItem item, SemaphoreSlim throttler, Action onComplete)
        {
            await throttler.WaitAsync();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(item.TargetPath) ?? string.Empty);
                await DownloadToFile(item.Url, item.TargetPath, item.ExpectedHash);
            }
            finally
            {
                throttler.Release();
                onComplete?.Invoke();
            }
        }

        private static async UniTask DownloadToFile(string url, string targetPath, string expectedHash)
        {
            Debug.Log($"Downloading {url} -> {targetPath}");
            Exception lastError = null;
            for (var attempt = 1; attempt <= MaxDownloadAttempts; attempt++)
                try
                {
                    using var request = UnityWebRequest.Get(url);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    await request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success) throw new Exception(request.error);

                    await File.WriteAllBytesAsync(targetPath, request.downloadHandler.data);
                    if (!IsValidFile(targetPath)) throw new Exception("Downloaded file is empty.");

                    if (!IsHashMatch(targetPath, expectedHash)) throw new Exception("Hash mismatch.");

                    return;
                }
                catch (Exception e)
                {
                    lastError = e;
                    if (File.Exists(targetPath)) File.Delete(targetPath);

                    await UniTask.Delay(500 * attempt);
                }

            Debug.LogError($"Failed to download {url} after {MaxDownloadAttempts} attempts: {lastError?.Message}");
        }

        private static bool IsValidFile(string path)
        {
            if (!File.Exists(path)) return false;

            var info = new FileInfo(path);
            return info.Length > 0;
        }

        private static bool IsHashMatch(string path, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(expectedHash) || !File.Exists(path)) return false;

            using var md5 = MD5.Create();
            using var stream = File.OpenRead(path);
            var hashBytes = md5.ComputeHash(stream);
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        private static List<DownloadItem> BuildManifestDownloadList()
        {
            var items = new List<DownloadItem>();
            if (ManifestFileToHash.Count == 0) return items;

            var dlDir = Path.Combine(Application.persistentDataPath, "dl");
            Directory.CreateDirectory(dlDir);
            foreach (var pair in ManifestFileToHash)
            {
                var fileName = pair.Key;
                var hash = pair.Value;
                var targetPath = Path.Combine(dlDir, hash);
                if (IsValidFile(targetPath) && IsHashMatch(targetPath, hash)) continue;

                items.Add(new DownloadItem
                {
                    Url = $"https://erc.osiom.cc/dl/res/{hash}",
                    TargetPath = targetPath,
                    ExpectedHash = hash
                });
            }

            return items;
        }

        private class DownloadItem
        {
            public string ExpectedHash;
            public string TargetPath;
            public string Url;
        }
    }
}