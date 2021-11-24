namespace AlephVault.Unity.Scenes
{
    namespace Authoring
    {
        namespace Types
        {
            /// <summary>
            ///   Tells whether a scene will be considered as a
            ///   singleton (i.e. to be loaded just once) or a
            ///   template (i.e. to be loaded per-request) when
            ///   loading.
            /// </summary>
            public enum SceneLoadMode
            {
                Singleton, Template
            }
        }
    }
}
