using UnityEngine;


namespace MyEditor.MaterialSystem
{
    [System.Serializable]
    public class BasePBR_Monster : MaterialConfig
    {

        public ComBaseMap baseMap = new ComBaseMap();
        public ComNormalMap normalMap = new ComNormalMap();
        public ComMixMap mixMap = new ComMixMap();
        public override string DisplayName => "测试/角色/PBR_怪物";
        public override Shader GetShader() => Shader.Find("Universal Render Pipeline/Lit");

        public override void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;
            mat.shader = GetShader();
            baseMap.ApplyToMaterial(mat);
            normalMap.ApplyToMaterial(mat);
            mixMap.ApplyToMaterial(mat);

        }

        public override void ApplyAllImportSettings()
        {
            baseMap.ApplyImportSettings(1024);
            normalMap.ApplyImportSettings(512);
            mixMap.ApplyImportSettings(512);
        }
    }
}