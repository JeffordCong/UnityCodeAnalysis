using UnityEngine;

namespace MyEditor.MaterialSystem
{
    [System.Serializable]
    public class ComMixMap : MaterialCom
    {
        public Texture2D mixMap;

        public override void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;

            MaterialHelper.SetTexture(mat, "_MixMap", mixMap);
            MaterialHelper.SetPropKeywordByTex(mat, "_MIXMAP", mixMap);
        }

        public override void ApplyImportSettings(int maxSize)
        {

            var preset = TexturePlatformSettings.BaseMap;
            var config = TexturePlatformSettings.OverrideAllPlatformMaxSize(preset, maxSize);
            string suffix = ComTextureSuffixPresets.GetComSuffix(this.GetType());
            TextureHelper.ApplyImportSettings(mixMap, config, suffix, autoRename: true);
        }
    }
}