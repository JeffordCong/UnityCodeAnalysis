using UnityEngine;

namespace MyEditor.MaterialSystem
{
    [System.Serializable]
    public class ComBaseMap : MaterialCom
    {
        public Texture2D baseMap;

        public override void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;

            MaterialHelper.SetTexture(mat, "_BaseMap", baseMap);
            MaterialHelper.SetPropKeywordByTex(mat, "_BASEMAP", baseMap);
        }

        public override void ApplyImportSettings(int maxSize)
        {

            var preset = TexturePlatformSettings.BaseMap;
            var config = TexturePlatformSettings.OverrideAllPlatformMaxSize(preset, maxSize);
            string suffix = ComTextureSuffixPresets.GetComSuffix(this.GetType());
            TextureHelper.ApplyImportSettings(baseMap, config, suffix, autoRename: true);
        }
    }
}