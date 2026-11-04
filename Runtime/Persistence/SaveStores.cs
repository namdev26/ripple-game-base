using System;
using RippleGame.Economy;
using RippleGame.Progression;
using RippleGame.Save;

namespace RippleGame.Persistence
{
    /// <summary>Lets wallets and level progress live in a <see cref="SaveFile"/>, so they are written together.</summary>
    public static class SaveStores
    {
        [Serializable]
        private sealed class IntValue
        {
            public int value;
        }

        public static ICurrencyBalanceStore BalanceStore(this SaveFile save, string key) =>
            new SectionBalanceStore(Section(save, key));

        public static ILevelProgressStore LevelStore(this SaveFile save, string key) =>
            new SectionLevelStore(Section(save, key));

        private static SaveSection<IntValue> Section(SaveFile save, string key)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            return save.Section<IntValue>(key);
        }

        private sealed class SectionBalanceStore : ICurrencyBalanceStore
        {
            private readonly SaveSection<IntValue> section;
            public SectionBalanceStore(SaveSection<IntValue> section) => this.section = section;

            public int Load() => Math.Max(0, section.Value.value);

            public void Save(int balance)
            {
                if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
                section.Update(data => data.value = balance);
            }
        }

        private sealed class SectionLevelStore : ILevelProgressStore
        {
            private readonly SaveSection<IntValue> section;
            public SectionLevelStore(SaveSection<IntValue> section) => this.section = section;

            public int LoadHighestCompleted() => section.Value.value;
            public void SaveHighestCompleted(int level) => section.Update(data => data.value = level);
        }
    }
}
