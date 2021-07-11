using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using AlephVault.Unity.Support.Utils;

namespace AlephVault.Unity.Scenes
{
    namespace Authoring
    {
        namespace Types
        {
            /// <summary>
            ///   This works as a scene domain to load a scene one or
            ///     more times, according to the scene load mode. If it
            ///     is configured as a singleton, then only one scene
            ///     load will be performed (as long as the scene remains
            ///     loaded). If not a singleton, no check will be done,
            ///     and so no constraint will be applied.
            /// </summary>
            [System.Serializable]
            public class SceneConfig
            {
                /// <summary>
                ///   The source scene to load.
                /// </summary>
                /// <seealso cref="SceneReference"/>
                [SerializeField]
                private SceneReference sourceScene;

                /// <summary>
                ///   The source scene to load.
                /// </summary>
                public SceneReference SourceScene => sourceScene;

                /// <summary>
                ///   The load mode to use.
                /// </summary>
                /// <seealso cref="SceneLoadMode"/>
                [SerializeField]
                private SceneLoadMode loadMode;

                /// <summary>
                ///   The physics mode to use when loading this scene.
                /// </summary>
                [SerializeField]
                private LocalPhysicsMode localPhysicsMode;

                /// <summary>
                ///   The load mode to use.
                /// </summary>
                public SceneLoadMode LoadMode => loadMode;

                // The loaded scene - only for singletons.
                private Scene sceneInstance;

                /// <summary>
                ///   Attempts to load the scene and returns its
                ///     reference. If this config uses the singleton
                ///     mode, and an instance of the scene was already
                ///     loaded, then the same scene is returned and
                ///     not fetched again. This is an asynchronous
                ///     task that must be waited for. The scene is
                ///     loaded additively, and using a new 3D physics
                ///     scene (so objects do not collide with others
                ///     in other scenes).
                /// </summary>
                /// <returns>The loaded scene</returns>
                public async Task<Scene> Load()
                {
                    // For singletons, if the scene is already loaded, return it.
                    if (loadMode == SceneLoadMode.Singleton && sceneInstance.IsValid() && sceneInstance.isLoaded)
                    {
                        return sceneInstance;
                    }
                    // Load the scene.
                    Scene scene = await sourceScene.Load(localPhysicsMode);
                    // For singletons, if the loaded scene is valid, store it.
                    if (loadMode == SceneLoadMode.Singleton && scene.IsValid())
                    {
                        sceneInstance = scene;
                    }
                    return scene;
                }

                /// <summary>
                ///   Unloads the current scene. It only makes sense when
                ///     the configuration is for a singleton instance. This
                ///     is an asynchronous task that must be waited for.
                /// </summary>
                public async Task Unload()
                {
                    if (loadMode == SceneLoadMode.Singleton)
                    {
                        AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneInstance);
                        while (!operation.isDone)
                        {
                            await Tasks.Blink();
                        }
                    }
                }
            }
        }
    }
}
