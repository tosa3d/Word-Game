// ©2015 - 2025 Candy Smith
// All rights reserved
// Redistribution of this software is strictly not allowed.
// Copy of this software can be obtained from unity asset store only.

using System;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Infrastructure.Service
{
    public class LevelLoaderService : ILevelLoaderService
    {
        public event Action<Level> OnLevelLoaded;
        public event Action<Level> OnBeforeLevelLoaded;

        public void NotifyBeforeLevelLoaded(Level level)
        {
            OnBeforeLevelLoaded?.Invoke(level);
        }

        public void NotifyLevelLoaded(Level level)
        {
            OnLevelLoaded?.Invoke(level);
            EventManager.GetEvent<Level>(EGameEvent.LevelLoaded).Invoke(level);
        }
    }
}
