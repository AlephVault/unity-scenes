using AlephVault.Unity.Support.Utils;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlephVault.Unity.Scenes
{
    namespace Authoring
    {
        namespace Types
        {
            /// <summary>
            ///   A scene config layout wraps the whole list of
            ///   configured scenes and also provides a mean to
            ///   preload (singleton scenes), load (return a
            ///   singleton scene or instantiate a template
            ///   scene), unload a scene, and clear all the
            ///   loaded scenes.
            /// </summary>
            [System.Serializable]
            public class SceneConfigLayout
            {
                /// <summary>
                ///   The status of this layout regarding the maps.
                ///   Tells whether the main maps (which are only
                ///   the singleton maps) are loaded or not.
                /// </summary>
                public enum LoadStatus
                {
                    Unloaded, Loading, Loaded, Unloading
                }

                /// <summary>
                ///   Contains all the scenes to be loaded, be
                ///   them singleton and/or template scenes.
                /// </summary>
                [SerializeField]
                private SceneConfigDictionary scenesMap = new SceneConfigDictionary();

                // Keeps a track of the scenes instantiated from
                // "template" scene configurations.
                private HashSet<Scene> instantiatedTemplateScenes = new HashSet<Scene>();

                // Using a mutex, so two threads cannot touch the
                // state simultaneously.
                private Mutex mutex = new Mutex();

                /// <summary>
                ///   The layout's current load status.
                /// </summary>
                public LoadStatus Status { get; private set; }

                public SceneConfigLayout()
                {
                    Status = LoadStatus.Unloaded;
                    SceneManager.sceneUnloaded += OnSceneUnloaded;
                }

                ~SceneConfigLayout()
                {
                    SceneManager.sceneUnloaded -= OnSceneUnloaded;
                    try
                    {
                        Task.Run(Teardown);
                    }
                    catch {}
                }

                // When a scene is unloaded, it is removed from
                // the hash set. This will not cause any collision
                // in the case of two SceneConfigLayout elements
                // coexist.
                private void OnSceneUnloaded(Scene unloaded)
                {
                    instantiatedTemplateScenes.Remove(unloaded);
                }

                // Expects a particular status to be in this layout
                // and, if everything is ok, moves the status to a
                // new one.
                private void ExpectAndThen(LoadStatus require, LoadStatus next)
                {
                    using(mutex)
                    {
                        if (Status != require)
                        {
                            throw new Scenes.Types.Exception(string.Format("The {0} status is expected, but the current status is {1} instead", require, Status));
                        }
                        Status = next;
                    }
                }

                /// <summary>
                ///   (Pre-)Loads all the singleton scenes in the layout.
                ///   Only available when the layout is not loaded.
                /// </summary>
                public async Task Initialize()
                {
                    ExpectAndThen(LoadStatus.Unloaded, LoadStatus.Loading);
                    foreach(KeyValuePair<string, SceneConfig> pair in scenesMap)
                    {
                        if (pair.Value.LoadMode == SceneLoadMode.Singleton)
                        {
                            await pair.Value.Load();
                        }
                    }
                    Status = LoadStatus.Loaded;
                }

                /// <summary>
                ///   Loads a scene by the given key. On singleton, it may
                ///   return a scene already loaded. On template, it will
                ///   always instantiate and keep the reference.
                /// </summary>
                /// <param name="key">The key of the scene to load</param>
                /// <returns>The loaded scene</returns>
                public async Task<Scene> Load(string key)
                {
                    ExpectAndThen(LoadStatus.Loaded, LoadStatus.Loading);
                    try
                    {
                        SceneConfig config = scenesMap[key];
                        Scene loaded = await config.Load();
                        if (config.LoadMode == SceneLoadMode.Template)
                        {
                            instantiatedTemplateScenes.Add(loaded);
                        }
                        return loaded;
                    }
                    catch(KeyNotFoundException)
                    {
                        throw new Scenes.Types.Exception("No scene to load with key: " + key);
                    }
                    finally
                    {
                        Status = LoadStatus.Loaded;
                    }
                }

                /// <summary>
                ///   Unloads all the scenes, including the instantiated ones,
                ///   from the layout. Only available when the layout is loaded.
                /// </summary>
                public async Task Teardown()
                {
                    ExpectAndThen(LoadStatus.Loaded, LoadStatus.Unloading);
                    // Clearing all the singleton scenes.
                    foreach (KeyValuePair<string, SceneConfig> pair in scenesMap)
                    {
                        if (pair.Value.LoadMode == SceneLoadMode.Singleton)
                        {
                            await pair.Value.Unload();
                        }
                    }
                    // Also clearing all the instantiated ones.
                    foreach (Scene scene in new HashSet<Scene>(instantiatedTemplateScenes))
                    {
                        AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
                        while (!operation.isDone) await Tasks.Blink();
                    }
                    Status = LoadStatus.Unloaded;
                }
            }
        }
    }
}
