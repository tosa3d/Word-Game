// ©2015 - 2025 Candy Smith
// All rights reserved
// Redistribution of this software is strictly not allowed.
// Copy of this software can be obtained from unity asset store only.

using System;
using WordsToolkit.Scripts.Levels;

namespace WordsToolkit.Scripts.Infrastructure.Service
{
    public interface ILevelLoaderService
    {
        event Action<Level> OnLevelLoaded;
        void NotifyBeforeLevelLoaded(Level level);
        void NotifyLevelLoaded(Level level);
    }
}
