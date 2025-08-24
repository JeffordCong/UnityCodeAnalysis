using UnityEngine;

namespace MyEditor.MaterialSystem
{
    [System.Serializable]
    public class ComNormalMap : MaterialCom
    {
        public Texture2D normalMap;

        public override void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;

            MaterialHelper.SetTexture(mat, "_NormalMap", normalMap);
            MaterialHelper.SetPropKeywordByTex(mat, "_NORMALMAP", normalMap);
        }

        public override void ApplyImportSettings(int maxSize)
        {
            var preset = TexturePlatformSettings.BaseMap;
            var config = TexturePlatformSettings.OverrideAllPlatformMaxSize(preset, maxSize);
            string suffix = ComTextureSuffixPresets.GetComSuffix(this.GetType());
            TextureHelper.ApplyImportSettings(normalMap, config, suffix, autoRename: true);
        }
    }
}