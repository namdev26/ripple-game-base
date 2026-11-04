using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RippleGame.Save
{
    /// <summary>Where save bytes live. Implement this to add another backend, such as a cloud mirror.</summary>
    public interface ISaveStorage
    {
        bool Exists(string file);
        byte[] ReadOrNull(string file);
        void Write(string file, byte[] data);
        /// <summary>Renames a file. The destination is guaranteed not to exist.</summary>
        void Move(string from, string to);
        /// <summary>Removes a file. A missing file is not an error.</summary>
        void Delete(string file);
    }

    public sealed class MemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

        public bool Exists(string file) => files.ContainsKey(file);
        public byte[] ReadOrNull(string file) => files.TryGetValue(file, out var data) ? (byte[])data.Clone() : null;
        public void Write(string file, byte[] data) => files[file] = (byte[])data.Clone();
        public void Delete(string file) => files.Remove(file);

        public void Move(string from, string to)
        {
            files[to] = files[from];
            files.Remove(from);
        }
    }

    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string directory;

        /// <param name="directory">Defaults to Application.persistentDataPath.</param>
        public FileSaveStorage(string directory = null)
        {
            this.directory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
        }

        private string PathOf(string file) => Path.Combine(directory, file);

        public bool Exists(string file) => File.Exists(PathOf(file));

        public byte[] ReadOrNull(string file)
        {
            var path = PathOf(file);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        public void Write(string file, byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Directory.CreateDirectory(directory);
            using (var stream = new FileStream(PathOf(file), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(data, 0, data.Length);
                stream.Flush(true);
            }
        }

        public void Move(string from, string to) => File.Move(PathOf(from), PathOf(to));

        public void Delete(string file)
        {
            var path = PathOf(file);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
