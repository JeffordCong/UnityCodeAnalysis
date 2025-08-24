using UnityEngine;

namespace MyEditor.MaterialSystem
{
    [System.Serializable]
    public abstract class MaterialCom
    {
        public abstract void ApplyToMaterial(Material mat);

        public abstract void ApplyImportSettings(int maxSize = 512);

    }
    /*
        /// <summary>
        /// 标准贴图集合，用于Config常用的贴图。
        /// </summary>
        [System.Serializable]
        public class StandardTextureGroup
        {
            public ComBaseMap baseMap = new ComBaseMap();
            public ComNormalMap normalMap = new ComNormalMap();

            public void ApplyToMaterial(Material mat)
            {
                baseMap.ApplyToMaterial(mat);
                normalMap.ApplyToMaterial(mat);
            }

            public void ApplyImportSettings(int baseSize, int normalSize)
            {
                baseMap.ApplyImportSettings(baseSize);
                normalMap.ApplyImportSettings(normalSize);
            }

        }
        */
}