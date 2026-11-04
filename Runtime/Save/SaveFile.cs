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
        /// <summary>No readable copy exists. Defaults are used and the bad file is kept as ".corrupt".</summary>
        DataLost,
        /// <summary>Writing failed, for example because storage is full. The write is retried later.</summary>
        WriteFailed,
        /// <summary>A section was written by a newer build. This session will not write.</summary>
        NewerVersion
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

        /// <summary>True when a section came from a newer build. Writes are refused to protect that data.</summary>
        public bool IsReadOnly { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOpenNames() => OpenNames.Clear();

        private string MainFile => name + ".sav";
        private string TemporaryFile => name + ".tmp";
        private string BackupFile => name + ".bak";
        private string CorruptFile => name + ".corrupt";

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

            if (Application.isPlaying) runner = SaveRunner.Create(this);
        }

        /// <param name="version">Raise this when the data shape changes.</param>
        /// <param name="migrate">Receives the stored version and JSON, returns JSON for the current version.</param>
        public SaveSection<T> Section<T>(string key, int version = 1, Func<int, string, string> migrate = null) where T : class, new()
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("A section key is required.", nameof(key));
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
                ProblemRaised?.Invoke(SaveProblem.WriteFailed);
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
            if (secondsSinceFlush >= autoFlushSeconds) Flush();
        }

        public void Dispose()
        {
            if (disposed) return;
            Flush();
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
                ProblemRaised?.Invoke(SaveProblem.NewerVersion);
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
            storage.Delete(BackupFile);
            if (storage.Exists(MainFile)) storage.Move(MainFile, BackupFile);
            storage.Move(TemporaryFile, MainFile);
        }

        private void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            string firstExisting = null;
            foreach (var file in new[] { MainFile, TemporaryFile, BackupFile })
            {
                if (!Exists(file)) continue;
                firstExisting = firstExisting ?? file;
                if (!TryRead(file, out var loadedDocument)) continue;

                document = loadedDocument;
                if (file == MainFile)
                {
                    lastWrittenJson = JsonUtility.ToJson(document);
                    return;
                }

                // lastWrittenJson stays null so the next write replaces the damaged main file.
                dirty = true;
                ProblemRaised?.Invoke(SaveProblem.RecoveredFromBackup);
                return;
            }

            lastWrittenJson = JsonUtility.ToJson(document);
            if (firstExisting == null) return;

            KeepCorruptCopy(firstExisting);
            ProblemRaised?.Invoke(SaveProblem.DataLost);
        }

        private bool Exists(string file)
        {
            try
            {
                return storage.Exists(file);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private bool TryRead(string file, out SaveDocument result)
        {
            result = null;
            try
            {
                if (!SaveCrypto.TryOpen(storage.ReadOrNull(file), key, out var plain)) return false;
                result = JsonUtility.FromJson<SaveDocument>(Encoding.UTF8.GetString(plain));
                return result?.sections != null;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                return false;
            }
        }

        private void KeepCorruptCopy(string file)
        {
            try
            {
                storage.Delete(CorruptFile);
                storage.Move(file, CorruptFile);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[RippleGame Save] Could not keep a copy of damaged file '{file}': {exception.Message}");
            }
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
