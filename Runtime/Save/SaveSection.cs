using System;
using UnityEngine;

namespace RippleGame.Save
{
    internal interface ISaveSectionBinding
    {
        Type DataType { get; }
        int Version { get; }
        string ToJson();
    }

    /// <summary>One typed part of a save file. Change it through <see cref="Update"/> so the file knows to write.</summary>
    public sealed class SaveSection<T> : ISaveSectionBinding where T : class, new()
    {
        private readonly SaveFile owner;

        public T Value { get; }

        Type ISaveSectionBinding.DataType => typeof(T);
        int ISaveSectionBinding.Version => version;
        private readonly int version;

        internal SaveSection(SaveFile owner, T value, int version)
        {
            this.owner = owner;
            this.version = version;
            Value = value;
        }

        public void Update(Action<T> change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            change(Value);
            owner.MarkDirty();
        }

        string ISaveSectionBinding.ToJson() => JsonUtility.ToJson(Value);
    }
}
