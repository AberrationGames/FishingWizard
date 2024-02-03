#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Grabbit
{
    public class ColliderMeshDeletionPostProcessor : UnityEditor.AssetModificationProcessor
    {
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (!asset)
                return AssetDeleteResult.DidNotDelete;

            ColliderMeshContainer container = null;
            if (GrabbitEditor.IsInstanceCreated && GrabbitEditor.Instance.ColliderMeshContainer)
                container = GrabbitEditor.Instance.ColliderMeshContainer;

            if (container == null)
            {
                var ids = AssetDatabase.FindAssets("t:ColliderMeshContainer");
                if (ids.Length == 0)
                {
                    return AssetDeleteResult.DidNotDelete;
                }

                container = AssetDatabase.LoadAssetAtPath<ColliderMeshContainer>(AssetDatabase.GUIDToAssetPath(ids[0]));
            }

            if (container)
                container.RemoveMesh(asset);
            
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
#endif