using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DJRussoFXHub;

// Read-only download to a user-selected destination. Never installs or replaces a VST.
internal static class PluginDownload
{
    private const string Root = "https://raw.githubusercontent.com/djrusso1994/DJ-RUSSO-FX-HUB/";
    private const long MaxBytes = 64L * 1024 * 1024;
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false })
    { Timeout = TimeSpan.FromSeconds(90) };

    internal sealed record Package(string Id, Version Version, string FileName, long Size, string Sha256, string Commit);
    private static string? Name(string id) => id switch
    {
        "fx-shell" => "DJ RUSSO FX SHELL.vst3",
        "meter-x" => "DJ RUSSO METER X.exe",
        "reverb-x" => "DJ RUSSO REVERB X.vst3",
        "delay-x" => "DJ RUSSO DELAY X.vst3",
        "compressor-x" => "DJ RUSSO COMPRESSOR X.vst3",
        "eq-x" => "DJ RUSSO EQ X.vst3",
        _ => null
    };
    private static Package? ReadPackage(string json, string id)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.GetProperty("schemaVersion").GetInt32() != 1)
            throw new InvalidDataException("Unsupported catalog schema.");
        foreach (var entry in doc.RootElement.GetProperty("packages").EnumerateArray())
        {
            if (entry.GetProperty("id").GetString() != id || !entry.TryGetProperty("download", out var d)) continue;
            var name = d.GetProperty("fileName").GetString();
            var hash = d.GetProperty("sha256").GetString();
            var commit = d.GetProperty("commit").GetString();
            var size = d.GetProperty("sizeBytes").GetInt64();
            if (name == null || name != Name(id) || hash == null || !Regex.IsMatch(hash, @"\A[0-9a-f]{64}\z") ||
                commit == null || !Regex.IsMatch(commit, @"\A[0-9a-f]{40}\z") || size <= 0 || size > MaxBytes ||
                !Version.TryParse(entry.GetProperty("version").GetString(), out var version))
                throw new InvalidDataException("Invalid package metadata.");
            return new Package(id, version, name, size, hash, commit);
        }
        return null;
    }

    internal static async Task<Package> ResolveAsync(string id, CancellationToken token)
    {
        var local = ReadPackage(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "plugin-updates.json"), token), id)
            ?? throw new InvalidDataException("No bundled download metadata.");
        // The stable catalog may not yet contain the staged product. Only prefer a strictly newer version.
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(12));
            using var response = await Client.GetAsync(Root + "main/plugin-updates.json", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > 131072) throw new InvalidDataException("Catalog too large.");
            using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            byte[] chunk = new byte[8192];
            int n;
            while ((n = await stream.ReadAsync(chunk.AsMemory(), timeout.Token)) > 0)
            {
                if (buffer.Length + n > 131072) throw new InvalidDataException("Catalog too large.");
                buffer.Write(chunk, 0, n);
            }
            var online = ReadPackage(System.Text.Encoding.UTF8.GetString(buffer.ToArray()), id);
            if (online != null && online.Version > local.Version) return online;
        }
        catch (HttpRequestException) { }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
        token.ThrowIfCancellationRequested();
        return local;
    }

    internal static async Task DownloadAsync(Package package, string destination, IProgress<int> progress, CancellationToken token)
    {
        string path = Path.GetFullPath(destination);
        // No in-place updates; no overwrite of any existing file, even after a race.
        if (File.Exists(path) || Directory.Exists(path)) throw new IOException("Destination already exists.");
        string dir = Path.GetDirectoryName(path) ?? throw new IOException("Invalid destination.");
        string installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "VST3");
        string app = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        string meter = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DJRusso", "FXHub", "MeterX");
        if (dir.Equals(installed, StringComparison.OrdinalIgnoreCase) || dir.StartsWith(installed + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            dir.Equals(app, StringComparison.OrdinalIgnoreCase) || dir.StartsWith(app + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            dir.Equals(meter, StringComparison.OrdinalIgnoreCase) || dir.StartsWith(meter + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Choose a separate download folder, not the installation directory.");
        string temp = Path.Combine(dir, ".djrusso-download-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            string url = Root + package.Commit + "/Payload/" + Uri.EscapeDataString(package.FileName);
            using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long length && length != package.Size)
                throw new InvalidDataException("Unexpected package size.");
            using var source = await response.Content.ReadAsStreamAsync(token);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long total = 0;
            int previous = -1;
            byte[] bytes = new byte[65536];
            await using (var target = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, bytes.Length, true))
            {
                int n;
                while ((n = await source.ReadAsync(bytes.AsMemory(), token)) > 0)
                {
                    total += n;
                    if (total > package.Size) throw new InvalidDataException("Unexpected package size.");
                    hash.AppendData(bytes, 0, n);
                    await target.WriteAsync(bytes.AsMemory(0,n), token);
                    int percent = (int)(total * 100 / package.Size);
                    if (percent != previous) { progress.Report(percent); previous = percent; }
                }
                await target.FlushAsync(token);
            }
            token.ThrowIfCancellationRequested();
            if (total != package.Size || !Convert.ToHexString(hash.GetHashAndReset()).Equals(package.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("SHA-256 verification failed.");
            File.Move(temp, path, false);
        }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }
}
