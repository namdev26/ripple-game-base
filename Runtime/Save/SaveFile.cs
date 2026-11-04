using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace RippleGame.Save
{
    public enum SaveProblem
    {
        /// <summary>The main file was unusable and an older copy was loaded.</summary>
        RecoveredFromBackup,
        /// <summary>No readable copy exists. Defaults are used and every bad file is kept as ".corrupt", ".corrupt.1" and so on.</summary>
        DataLost,
        /// <summary>Writing failed, for example because storage is full. The write is retried later.</summary>
        WriteFailed,
        /// <summary>A section was written by a newer build. This session will not write.</summary>
        NewerVersion,
        /// <summary>Files exist but could not be read right now. Defaults are used, files are untouched and this session will not write.</summary>
        ReadFailed
    }

    public sealed class SaveOptions
    {
        /// <summary>Longest time a change waits in memory. Zero disables timed writes.</summary>
        public float AutoFlushSeconds = 5f;
        /// <summary>Defaults to files in Application.persistentDataPath.</summary>
        public ISaveStorage Storage;
    }

    [Serializable]
    internal sealed class SaveEntry
    {
        public string key;
        public int version;
        public string json;
    }

    [Serializable]
    internal sealed class SaveDocument
    {
        public List<SaveEntry> sections = new List<SaveEntry>();
    }

    /// <summary>An encrypted save file holding any number of typed sections. All sections are written together.</summary>
    /// <remarks>Subscribe to <see cref="ProblemRaised"/> before the first <see cref="Section{T}"/> call, which is when loading happens.</remarks>
    public sealed class SaveFile : IDisposable
    {
        private const int MaxCorruptCopies = 5;
        private static readonly HashSet<string> OpenNames = new HashSet<string>();

        private readonly string name;
        private readonly byte[] key;
        private readonly ISaveStorage storage;
        private readonly float autoFlushSeconds;
        private readonly Dictionary<string, ISaveSectionBinding> sections = new Dictionary<string, ISaveSectionBinding>();
        private SaveDocument document = new SaveDocument();
        private SaveRunner runner;
        private string lastWrittenJson;
        private bool loaded;
        private bool dirty;
        private bool disposed;
        private float secondsSinceFlush;

        public event Action<SaveProblem> ProblemRaised;

        /// <summary>True after <see cref="SaveProblem.NewerVersion"/> or <see cref="SaveProblem.ReadFailed"/>. Writes are refused to protect existing data.</summary>
        public bool IsReadOnly { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOpenNames()
        {
            OpenNames.Clear();
            SaveRunner.ApplicationQuitting = false;
        }

        private string MainFile => name + ".sav";
        private string TemporaryFile => name + ".tmp";
        private string BackupFile => name + ".bak";

        private string CorruptCopy(int index) => index == 0 ? name + ".corrupt" : name + ".corrupt." + index;

        /// <param name="name">File name without extension, unique within the running game.</param>
        /// <param name="base64Key">A 32-byte key from TOOL Hub. Changing or losing it makes existing saves unreadable.</param>
        public SaveFile(string name, string base64Key, SaveOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                name.IndexOfAny(new[] { '/', '\\' }) >= 0 || name.Contains(".."))
                throw new ArgumentException("The save name must be a simple file name.", nameof(name));

            key = SaveCrypto.DecodeKey(base64Key);
            options = options ?? new SaveOptions();
            storage = options.Storage ?? new FileSaveStorage();
            autoFlushSeconds = options.AutoFlushSeconds;
            this.name = name;

            if (!OpenNames.Add(name))
                throw new InvalidOperationException($"Save file '{name}' is already open. Share the existing instance or dispose it first.");

            EnsureRunner();
        }

        /// <param name="version">Raise this when the data shape changes.</param>
        /// <param name="migrate">Receives the stored version and JSON, returns JSON for the current version.</param>
        public SaveSection<T> Section<T>(string key, int version = 1, Func<int, string, string> migrate = null) where T : class, new()
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("A section key is required.", nameof(key));
            EnsureRunner();
            EnsureLoaded();

            if (sections.TryGetValue(key, out var existing))
            {
                if (existing is SaveSection<T> typed) return typed;
                throw new InvalidOperationException($"Save section '{key}' already uses type {existing.DataType.Name}.");
            }

            var section = new SaveSection<T>(this, LoadValue<T>(key, version, migrate), version);
            sections.Add(key, section);
            return section;
        }

        /// <summary>Writes now if anything changed. Returns false if the data could not be written.</summary>
        public bool Flush()
        {
            if (disposed) return false;
            EnsureRunner();
            return WriteIfChanged();
        }

        private bool WriteIfChanged()
        {
            EnsureLoaded();
            if (IsReadOnly) return false;

            var json = SerializeDocument();
            if (json == lastWrittenJson)
            {
                dirty = false;
                return true;
            }

            try
            {
                WriteAtomically(SaveCrypto.Seal(Encoding.UTF8.GetBytes(json), key));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                dirty = true;
                secondsSinceFlush = 0f;
                Debug.LogWarning($"[RippleGame Save] Could not write '{name}': {exception.Message}");
                Raise(SaveProblem.WriteFailed);
                return false;
            }

            lastWrittenJson = json;
            dirty = false;
            secondsSinceFlush = 0f;
            return true;
        }

        /// <summary>Advances the timed write. Called for you while the game is playing.</summary>
        public void Tick(float deltaSeconds)
        {
            if (disposed || !dirty || IsReadOnly || autoFlushSeconds <= 0f) return;
            secondsSinceFlush += deltaSeconds;
            if (secondsSinceFlush >= autoFlushSeconds) WriteIfChanged();
        }

        public void Dispose()
        {
            if (disposed) return;
            WriteIfChanged();
            disposed = true;
            OpenNames.Remove(name);
            if (runner != null) runner.Release();
            runner = null;
        }

        internal void MarkDirty() => dirty = true;

        private T LoadValue<T>(string sectionKey, int version, Func<int, string, string> migrate) where T : class, new()
        {
            var entry = document.sections.Find(item => item.key == sectionKey);
            if (entry == null) return new T();

            if (entry.version > version)
            {
                IsReadOnly = true;
                Raise(SaveProblem.NewerVersion);
                return new T();
            }

            var json = entry.json;
            if (entry.version < version)
            {
                if (migrate == null)
                    throw new InvalidOperationException($"Save section '{sectionKey}' is version {entry.version}, code expects {version}, and no migrate function was given.");
                json = migrate(entry.version, entry.json);
                dirty = true;
            }

            return JsonUtility.FromJson<T>(json) ?? new T();
        }

        private string SerializeDocument()
        {
            foreach (var pair in sections)
            {
                var entry = document.sections.Find(item => item.key == pair.Key);
                if (entry == null)
                {
                    entry = new SaveEntry { key = pair.Key };
                    document.sections.Add(entry);
                }

                entry.version = pair.Value.Version;
                entry.json = pair.Value.ToJson();
            }

            return JsonUtility.ToJson(document);
        }

        private void WriteAtomically(byte[] data)
        {
            storage.Write(TemporaryFile, data);
            // With no main file to rotate, the existing backup is the newest good copy and stays.
            if (storage.Exists(MainFile))
            {
                storage.Delete(BackupFile);
                storage.Move(MainFile, BackupFile);
            }
            storage.Move(TemporaryFile, MainFile);
        }

        private void EnsureRunner()
        {
            // Without a domain reload a static SaveFile outlives the runner from the previous play session.
            if (Application.isPlaying && !SaveRunner.ApplicationQuitting && runner == null)
                runner = SaveRunner.Create(this);
        }

        private void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            try
            {
                LoadFromStorage();
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // The files may be perfectly good, so nothing is moved and nothing may overwrite them.
                document = new SaveDocument();
                IsReadOnly = true;
                Debug.LogWarning($"[RippleGame Save] Could not read '{name}': {exception.Message}");
                Raise(SaveProblem.ReadFailed);
            }
        }

        private void LoadFromStorage()
        {
            var damaged = new List<string>();
            foreach (var file in new[] { MainFile, TemporaryFile, BackupFile })
            {
                if (!storage.Exists(file)) continue;
                if (TryParse(storage.ReadOrNull(file), out var found))
                {
                    Adopt(file, found, damaged);
                    return;
                }
                damaged.Add(file);
            }

            // A kept copy only opens if the key changed and has now been restored.
            for (var index = 0; index < MaxCorruptCopies; index++)
            {
                var file = CorruptCopy(index);
                if (storage.Exists(file) && TryParse(storage.ReadOrNull(file), out var found))
                {
                    Adopt(file, found, damaged);
                    return;
                }
            }

            lastWrittenJson = JsonUtility.ToJson(document);
            if (damaged.Count == 0) return;

            foreach (var file in damaged) KeepCorruptCopy(file);
            Raise(SaveProblem.DataLost);
        }

        private void Adopt(string file, SaveDocument found, List<string> damaged)
        {
            document = found;
            if (file == MainFile)
            {
                lastWrittenJson = JsonUtility.ToJson(document);
                return;
            }

            foreach (var bad in damaged) KeepCorruptCopy(bad);

            if (file != BackupFile && TryPromote(file))
            {
                lastWrittenJson = JsonUtility.ToJson(document);
            }
            else
            {
                // lastWrittenJson stays null so the next write recreates the main file. A recovered backup stays as it is.
                dirty = true;
            }

            Raise(SaveProblem.RecoveredFromBackup);
        }

        private bool TryPromote(string file)
        {
            try
            {
                if (!storage.Exists(file) || storage.Exists(MainFile)) return false;
                storage.Move(file, MainFile);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private bool TryParse(byte[] data, out SaveDocument result)
        {
            result = null;
            if (!SaveCrypto.TryOpen(data, key, out var plain)) return false;
            try
            {
                result = JsonUtility.FromJson<SaveDocument>(Encoding.UTF8.GetString(plain));
                return result?.sections != null;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>Copies are never overwritten, except the last slot once all are in use, so the oldest always survive.</summary>
        private void KeepCorruptCopy(string file)
        {
            try
            {
                var index = 0;
                while (index < MaxCorruptCopies - 1 && storage.Exists(CorruptCopy(index))) index++;
                var target = CorruptCopy(index);
                storage.Delete(target);
                storage.Move(file, target);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[RippleGame Save] Could not keep a copy of damaged file '{file}': {exception.Message}");
            }
        }

        private void Raise(SaveProblem problem)
        {
            // Logged as well, because static initializers can load before anyone subscribes.
            Debug.LogWarning($"[RippleGame Save] '{name}': {problem}");
            ProblemRaised?.Invoke(problem);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(SaveFile));
        }
    }

    /// <summary>Editor and debugging helpers. Not needed by normal game code.</summary>
    public static class SaveInspector
    {
        public static string GenerateKey() => SaveCrypto.GenerateKey();

        public static bool TryDecrypt(byte[] file, string base64Key, out string json)
        {
            json = null;
            byte[] key;
            try
            {
                key = SaveCrypto.DecodeKey(base64Key);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (!SaveCrypto.TryOpen(file, key, out var plain)) return false;
            json = Encoding.UTF8.GetString(plain);
            return true;
        }
    }
}
