using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace DJRussoFXHub;

/// <summary>A dedicated Reverb X VST3 file is installed independently from FX SHELL.
/// The elevated worker ONLY acts on the fixed path and on the packaged, pinned SHA256 binary.</summary>
internal static class ReverbInstaller
{
    public const string VersionLabel = "1.3"; // reported by the user-supplied PE strings; FL Studio test is independent
    public const string PluginFileName = "DJ RUSSO REVERB X.vst3";
    public const string ExpectedSha256 = "e806986b839dd61dfccbf00f9210d5c0bf3dc83a9cdd965ad4030ffc144aaf85";
    public const string InstallArgument = "--djrusso-install-reverb";
    public const string RemoveArgument = "--djrusso-remove-reverb";

    public enum State { Missing, Managed, ExternalSameBinary, Conflict, ModifiedManaged }
    private sealed record Receipt(string Target, string Sha256, string Version, DateTimeOffset WhenUtc);

    public static string Vst3Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "VST3");
    public static string InstalledPath => Path.Combine(Vst3Folder, PluginFileName);
    public static string PayloadPath => Path.Combine(AppContext.BaseDirectory, "Payload", PluginFileName);
    private static string ReceiptPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DJRusso", "FXHub", "reverb-install.json");

    public static bool IsWorkerArgument(string arg) => arg == InstallArgument || arg == RemoveArgument;

    public static string? Sha256OrNull(string file)
    {
        try
        {
            using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }
        catch { return null; }
    }

    public static bool HasExpectedPayload()
    {
        if (Sha256OrNull(PayloadPath) != ExpectedSha256) return false;
        try
        {
            using var file = File.Open(PayloadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(file);
            if (reader.ReadUInt16() != 0x5a4d) return false; // MZ
            file.Position = 0x3c;
            int peOffset = reader.ReadInt32();
            if (peOffset < 64 || peOffset > 1024 * 1024 || peOffset + 6 > file.Length) return false;
            file.Position = peOffset;
            return reader.ReadUInt32() == 0x00004550 && reader.ReadUInt16() == 0x8664; // PE + AMD64
        }
        catch { return false; }
    }

    private static bool IsManagedReceipt()
    {
        try
        {
            var receipt = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(ReceiptPath));
            return receipt is not null
                && string.Equals(Path.GetFullPath(receipt.Target), Path.GetFullPath(InstalledPath), StringComparison.OrdinalIgnoreCase)
                && string.Equals(receipt.Sha256, ExpectedSha256, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    // UI-only short-lived cache. Never used for elevated install/remove security checks.
    private static readonly object StateCacheLock = new();
    private static State? cachedState;
    private static DateTime cachedAtUtc;
    public static State GetState()
    {
        lock (StateCacheLock)
        {
            if (cachedState.HasValue && DateTime.UtcNow - cachedAtUtc < TimeSpan.FromSeconds(3))
                return cachedState.Value;
            var state = GetStateUncached();
            cachedState = state;
            cachedAtUtc = DateTime.UtcNow;
            return state;
        }
    }
    public static void InvalidateStateCache()
    {
        lock (StateCacheLock) cachedState = null;
    }

    private static State GetStateUncached()
    {
        if (Directory.Exists(InstalledPath)) return State.Conflict;
        bool wasManaged = IsManagedReceipt();
        if (!File.Exists(InstalledPath)) return State.Missing;
        var installedHash = Sha256OrNull(InstalledPath);
        if (installedHash == ExpectedSha256)
            return wasManaged ? State.Managed : State.ExternalSameBinary;
        return wasManaged ? State.ModifiedManaged : State.Conflict;
    }

    public static void SaveReceipt()
    {
        InvalidateStateCache();
        if (Sha256OrNull(InstalledPath) != ExpectedSha256)
            throw new IOException("The installed VST3 does not match the expected SHA-256.");
        Directory.CreateDirectory(Path.GetDirectoryName(ReceiptPath)!);
        string temporary = ReceiptPath + ".new";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new Receipt(InstalledPath, ExpectedSha256, VersionLabel, DateTimeOffset.UtcNow)));
        File.Move(temporary, ReceiptPath, true);
    }

    public static void DeleteReceipt()
    {
        InvalidateStateCache();
        if (File.Exists(ReceiptPath)) File.Delete(ReceiptPath);
    }

    /// <summary>0 success; 2 bad payload; 3 target exists; 4 unmanaged/changed target;
    /// 5 access denied; 6 I/O failure; 7 missing installed file; 8 general failure.</summary>
    public static int RunElevatedWorker(string command)
    {
        if (!IsWorkerArgument(command)) return 8;
        if (command == InstallArgument && !HasExpectedPayload()) return 2;
        try
        {
            string target = InstalledPath;
            if (command == InstallArgument)
            {
                if (File.Exists(target) || Directory.Exists(target)) return 3; // never overwrite unmanaged files
                Directory.CreateDirectory(Vst3Folder);
                string temporary = Path.Combine(Vst3Folder, ".djrusso-" + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    File.Copy(PayloadPath, temporary, false);
                    if (Sha256OrNull(temporary) != ExpectedSha256) return 2;
                    File.Move(temporary, target); // atomic rename within the destination volume
                    return 0;
                }
                finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
            }
            if (!File.Exists(target)) return 7;
            if ((File.GetAttributes(target) & FileAttributes.ReparsePoint) != 0) return 4;
            if (Sha256OrNull(target) != ExpectedSha256) return 4; // never remove changed or unrelated binaries
            File.Delete(target);
            return 0;
        }
        catch (UnauthorizedAccessException) { return 5; }
        catch (IOException) { return 6; }
        catch { return 8; }
    }
}
