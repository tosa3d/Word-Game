using UnityEngine;
using VContainer;
using WordsToolkit.Scripts.Gameplay.WordValidator;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.Services.BannedWords;
using WordsToolkit.Scripts.Services;

namespace WordsToolkit.Scripts.Levels.Editor
{
    public static class EditorScope
    {
        private static IObjectResolver editorContainer;
        private static bool isDisposed = false;

        public static IObjectResolver Container
        {
            get
            {
                if (isDisposed || editorContainer == null)
                {
                    if (isDisposed)
                        return null;
                        
                    var builder = new ContainerBuilder();
                    Configure(builder);
                    editorContainer = builder.Build();
                }
                return editorContainer;
            }
        }

        public static void Configure(IContainerBuilder builder)
        {
            var languageConfig = Resources.Load<LanguageConfiguration>("Settings/LanguageConfiguration");
            var bannedWordsConfig = Resources.Load<BannedWordsConfiguration>("BannedWords/BannedWords");

            if (!languageConfig || !bannedWordsConfig)
            {
                Debug.LogError("Failed to load required configurations for editor container");
                return;
            }

            builder.RegisterInstance(languageConfig);
            builder.RegisterInstance(bannedWordsConfig);

            builder.Register<ILanguageService, LanguageService>(Lifetime.Singleton);
            builder.Register<IBannedWordsService, BannedWordsService>(Lifetime.Singleton);
            builder.Register<IModelController, ModelController>(Lifetime.Singleton);
            builder.Register<ICustomWordRepository, CustomWordRepository>(Lifetime.Singleton);
            builder.Register<IWordValidator, DefaultWordValidator>(Lifetime.Singleton);
        }

        public static T Resolve<T>() where T : class
        {
            var container = Container;
            if (container == null || isDisposed)
                return null;
            return container.Resolve<T>();
        }

        public static void Dispose()
        {
            isDisposed = true;
            if (editorContainer != null)
            {
                if (editorContainer is global::System.IDisposable disposableContainer)
                {
                    disposableContainer.Dispose();
                }
                editorContainer = null;
            }
        }

        // Called by Unity when domain is reloading
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            isDisposed = false;
            editorContainer = null;
        }
    }
}
