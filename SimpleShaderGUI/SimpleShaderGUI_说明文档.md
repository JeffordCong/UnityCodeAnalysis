# SimpleShaderGUI 使用说明文档

## 概述

**SimpleShaderGUI** 是一个为 Unity Shader 提供的自定义材质编辑器框架，旨在简化 Shader 属性的组织和展示。它提供了以下核心功能：

- **折叠页系统**：支持多级嵌套的折叠页，类似 Word 文档的多级标题
- **条件显示**：根据开关或枚举值动态显示/隐藏属性
- **渲染状态管理**：内置的混合模式、剔除模式等渲染状态控制
- **Pass 切换**：方便地启用/禁用 Shader Pass
- **增强的属性绘制**：纹理、向量等属性的自定义绘制器

---

## 快速开始

### 1. 在 Shader 中启用 SimpleShaderGUI

在你的 Shader 文件底部添加以下代码：

```shaderlab
CustomEditor "Scarecrow.SimpleShaderGUI"
```

完整示例：

```shaderlab
Shader "Custom/MyShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        // ... 你的 SubShader 代码
    }
    
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
```

---

## 渲染状态管理

SimpleShaderGUI 提供了内置的渲染状态管理功能，可以方便地控制混合模式、剔除模式、渲染队列等。

### 启用渲染状态管理

在 Shader 的 `Properties` 块中添加以下属性：

```shaderlab
Properties
{
    [HideInInspector] _EnableRenderStates ("Enable Render States", Float) = 0
    [HideInInspector] _BlendModeIndex ("Blend Mode", Float) = 0
    [HideInInspector] _CullIndex ("Cull Mode", Float) = 0
    [HideInInspector] _QueueOffsetValue ("Queue Offset", Float) = 0
}
```

在材质编辑器中会显示一个 **Enable Render States** 按钮，点击后可以配置：

- **Blend Mode**（混合模式）：
  - `Opaque`：不透明
  - `Cutout`：镂空（需配合 Alpha Test）
  - `Transparent`：透明
  - `Additive`：叠加
  - `Overlay`：覆盖（总是绘制）
  - `AdditiveOverlay`：叠加覆盖
  - `Background`：背景
  - `Decal`：贴花

- **Render Face**（渲染面）：
  - `Front`：正面
  - `Both`：双面
  - `Back`：背面

- **Queue Offset**（队列偏移）：-50 到 50 的整数值

### 在 SubShader 中应用

```shaderlab
SubShader
{
    Tags { "RenderType" = "Opaque" }
    
    Blend [_SrcBlend] [_DstBlend]
    ZWrite [_ZWrite]
    ZTest [_ZTest]
    Cull [_Cull]
    
    // ... Pass 定义
}
```

---

## 折叠页系统

折叠页是 SimpleShaderGUI 的核心功能，用于组织和分组 Shader 属性。

### 基础用法

#### 1. 定义折叠页属性

在属性的 **显示名称** 后添加 `_Foldout` 后缀：

```shaderlab
Properties
{
    [Foldout(1)] _Group1 ("Main Settings_Foldout", Float) = 1
    _MainTex ("Main Texture", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1,1)
}
```

> **注意**：`_Foldout` 后缀必须添加在显示名称中，而不是属性名。

#### 2. `[Foldout]` 参数说明

```shaderlab
[Foldout(level, style, drawToggle, isOpen, showList...)]
```

| 参数 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| `level` | float | 折叠页等级（1, 2, 3...），数字越小等级越高 | 1 |
| `style` | float | 样式：1=大型，2=中型，3=小型 | 1 |
| `drawToggle` | float | 是否绘制复选框：0=否，1=是 | 0 |
| `isOpen` | float | 初始展开状态：0=折叠，1=展开 | 1 |
| `showList` | string[] | 条件显示列表（可选） | 空 |

#### 3. 折叠页样式

- **大型（Big）**：用于一级标题，字体最大，带背景色
- **中型（Median）**：用于二级标题，字体中等
- **小型（Small）**：用于三级及以下标题，使用默认折叠样式

示例：

```shaderlab
Properties
{
    // 一级折叠页（大型）
    [Foldout(1, 1)] _MainGroup ("主要设置_Foldout", Float) = 1
    _MainTex ("主纹理", 2D) = "white" {}
    
    // 二级折叠页（中型）
    [Foldout(2, 2)] _ColorGroup ("颜色设置_Foldout", Float) = 1
    _Color ("颜色", Color) = (1,1,1,1)
    _Tint ("色调", Color) = (1,1,1,1)
    
    // 三级折叠页（小型）
    [Foldout(3, 3)] _AdvancedColor ("高级颜色_Foldout", Float) = 1
    _ColorMultiplier ("颜色倍增", Float) = 1
}
```

#### 4. 折叠页权限控制

通过 `drawToggle` 参数可以为折叠页添加一个复选框，控制内部属性是否可编辑：

```shaderlab
Properties
{
    // 带复选框的折叠页
    [Foldout(1, 1, 1)] _SpecularGroup ("高光设置_Foldout", Float) = 1
    _SpecularColor ("高光颜色", Color) = (1,1,1,1)
    _Glossiness ("光泽度", Range(0, 1)) = 0.5
}
```

- 复选框勾选时：内部属性可编辑，属性值为 1
- 复选框取消时：内部属性禁用（灰色），属性值为 0

### 嵌套折叠页

折叠页支持多级嵌套，通过 `level` 参数控制层级关系：

```shaderlab
Properties
{
    // 一级折叠页
    [Foldout(1)] _Level1 ("一级标题_Foldout", Float) = 1
    _Prop1 ("属性1", Float) = 0
    
    // 二级折叠页（嵌套在一级内）
    [Foldout(2)] _Level2 ("二级标题_Foldout", Float) = 1
    _Prop2 ("属性2", Float) = 0
    
    // 三级折叠页（嵌套在二级内）
    [Foldout(3)] _Level3 ("三级标题_Foldout", Float) = 1
    _Prop3 ("属性3", Float) = 0
    
    // 跳出到一级
    [Foldout_Out(1)] _OutMarker ("跳出标记_Foldout", Float) = 0
    
    // 新的一级折叠页
    [Foldout(1)] _AnotherLevel1 ("另一个一级标题_Foldout", Float) = 1
    _Prop4 ("属性4", Float) = 0
}
```

### `[Foldout_Out]` 跳出器

用于在嵌套折叠页中跳出到指定等级：

```shaderlab
[Foldout_Out(targetLevel)]
```

- `targetLevel`：跳出到的目标等级（跳出后的等级为 `targetLevel - 1`）

示例：

```shaderlab
Properties
{
    [Foldout(1)] _Group1 ("组1_Foldout", Float) = 1
    [Foldout(2)] _SubGroup1 ("子组1_Foldout", Float) = 1
    _Prop1 ("属性1", Float) = 0
    
    // 跳出到等级 1（实际跳到等级 0，即顶层）
    [Foldout_Out(1)] _Out1 ("_Foldout", Float) = 0
    
    [Foldout(1)] _Group2 ("组2_Foldout", Float) = 1
    _Prop2 ("属性2", Float) = 0
}
```

### Pass 切换功能

SimpleShaderGUI 支持通过折叠页控制 Shader Pass 的启用/禁用。

#### 命名规则

属性名必须以 `_Enable` 开头，以 `Pass` 结尾：

```shaderlab
_Enable<PassName>Pass
```

例如：
- `_EnableOutlinePass` → Pass 名称为 `Outline`
- `_EnableShadowPass` → Pass 名称为 `Shadow`

#### 使用示例

```shaderlab
Shader "Custom/OutlineShader"
{
    Properties
    {
        // Pass 切换折叠页
        [Foldout(1)] _EnableOutlinePass ("描边 Pass_Foldout", Float) = 1
        _OutlineColor ("描边颜色", Color) = (0,0,0,1)
        _OutlineWidth ("描边宽度", Range(0, 0.1)) = 0.01
    }
    
    SubShader
    {
        // 主 Pass
        Pass
        {
            Name "MainPass"
            // ... 主 Pass 代码
        }
        
        // 描边 Pass
        Pass
        {
            Name "Outline"  // 必须与属性名匹配
            // ... 描边 Pass 代码
        }
    }
    
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
```

#### 行为说明

- 复选框勾选时：Pass 启用，内部属性可编辑
- 复选框取消时：Pass 禁用，内部属性变灰
- 自动调用 `Material.SetShaderPassEnabled(passName, enabled)`

---

## 条件显示（Switch）

条件显示功能允许根据开关或枚举值动态显示/隐藏属性。

### 1. Toggle 开关

#### `[Toggle_Switch]`

创建一个开关控制器：

```shaderlab
Properties
{
    [Toggle_Switch] _UseNormalMap ("使用法线贴图", Float) = 0
    [Switch(_UseNormalMap)] _NormalMap ("法线贴图", 2D) = "bump" {}
    [Switch(_UseNormalMap)] _NormalScale ("法线强度", Range(0, 2)) = 1
}
```

- `[Toggle_Switch]`：定义开关控制器
- `[Switch(_UseNormalMap)]`：受控属性，仅在 `_UseNormalMap` 启用时显示

#### 关键字生成

`[Toggle_Switch]` 会自动生成 Shader 关键字：

```shaderlab
_UseNormalMap → _USENORMALMAP_ON
```

在 Shader 中使用：

```shaderlab
#pragma shader_feature _USENORMALMAP_ON

// ...

#ifdef _USENORMALMAP_ON
    // 使用法线贴图的代码
#endif
```

### 2. Enum 枚举

#### `[Enum_Switch]`

创建一个枚举控制器：

```shaderlab
Properties
{
    [Enum_Switch(Off, Multiply, Add)] _BlendMode ("混合模式", Float) = 0
    [Switch(Multiply, Add)] _BlendTex ("混合纹理", 2D) = "white" {}
    [Switch(Multiply)] _MultiplyColor ("乘法颜色", Color) = (1,1,1,1)
    [Switch(Add)] _AddColor ("叠加颜色", Color) = (1,1,1,1)
}
```

- `[Enum_Switch(选项1, 选项2, ...)]`：定义枚举控制器
- `[Switch(选项1, 选项2)]`：受控属性，仅在指定选项激活时显示

#### 关键字生成

`[Enum_Switch]` 会为每个选项生成关键字：

```shaderlab
_BlendMode + Multiply → _BLENDMODE_MULTIPLY
_BlendMode + Add → _BLENDMODE_ADD
```

在 Shader 中使用：

```shaderlab
#pragma shader_feature _ _BLENDMODE_MULTIPLY _BLENDMODE_ADD

// ...

#if defined(_BLENDMODE_MULTIPLY)
    // Multiply 模式代码
#elif defined(_BLENDMODE_ADD)
    // Add 模式代码
#endif
```

### 3. `[Switch]` 受控属性

```shaderlab
[Switch(条件1, 条件2, ...)]
```

- 可以指定多个条件，满足任意一个即显示
- 条件可以是 Toggle 属性名或 Enum 选项名

示例：

```shaderlab
Properties
{
    [Toggle_Switch] _UseTexture ("使用纹理", Float) = 0
    [Enum_Switch(Color, Texture, Both)] _Mode ("模式", Float) = 0
    
    // 在 _UseTexture 启用 或 _Mode 为 Texture/Both 时显示
    [Switch(_UseTexture, Texture, Both)] _MainTex ("主纹理", 2D) = "white" {}
}
```

---

## 纹理绘制器

### `[Tex]` 属性

用于绘制纹理属性，支持在同一行显示额外属性（如颜色）。

#### 基础用法

```shaderlab
[Tex]
```

仅绘制纹理，带缩放和偏移控制。

#### 带额外属性

```shaderlab
[Tex(_PropertyName)]
```

在纹理旁边绘制指定属性（通常是颜色）。

示例：

```shaderlab
Properties
{
    // 纹理 + 颜色在同一行
    [Tex(_Color)] _MainTex ("主纹理", 2D) = "white" {}
    _Color ("颜色", Color) = (1,1,1,1)
    
    // 仅纹理
    [Tex] _NormalMap ("法线贴图", 2D) = "bump" {}
}
```

#### 条件显示

```shaderlab
[Tex(_PropertyName, 条件1, 条件2, ...)]
```

示例：

```shaderlab
Properties
{
    [Toggle_Switch] _UseAlbedo ("使用反照率", Float) = 0
    [Tex(_AlbedoColor, _UseAlbedo)] _AlbedoTex ("反照率纹理", 2D) = "white" {}
    _AlbedoColor ("反照率颜色", Color) = (1,1,1,1)
}
```

---

## 完整示例

以下是一个综合使用各种功能的完整 Shader 示例：

```shaderlab
Shader "Custom/CompleteExample"
{
    Properties
    {
        // ========== 渲染状态 ==========
        [HideInInspector] _EnableRenderStates ("Enable Render States", Float) = 0
        [HideInInspector] _BlendModeIndex ("Blend Mode", Float) = 0
        [HideInInspector] _CullIndex ("Cull Mode", Float) = 0
        [HideInInspector] _QueueOffsetValue ("Queue Offset", Float) = 0
        
        // ========== 主要设置 ==========
        [Foldout(1, 1)] _MainSettings ("主要设置_Foldout", Float) = 1
        [Tex(_Color)] _MainTex ("主纹理", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        _Brightness ("亮度", Range(0, 2)) = 1
        
        // ========== 法线贴图（条件显示）==========
        [Foldout(2, 2)] _NormalSettings ("法线设置_Foldout", Float) = 1
        [Toggle_Switch] _UseNormalMap ("使用法线贴图", Float) = 0
        [Switch(_UseNormalMap)][Tex] _NormalMap ("法线贴图", 2D) = "bump" {}
        [Switch(_UseNormalMap)] _NormalScale ("法线强度", Range(0, 2)) = 1
        
        // 跳出到一级
        [Foldout_Out(1)] _Out1 ("_Foldout", Float) = 0
        
        // ========== 高光设置（带权限控制）==========
        [Foldout(1, 1, 1)] _SpecularSettings ("高光设置_Foldout", Float) = 1
        _SpecularColor ("高光颜色", Color) = (1,1,1,1)
        _Glossiness ("光泽度", Range(0, 1)) = 0.5
        
        // ========== 混合模式（枚举）==========
        [Foldout(2, 2)] _BlendSettings ("混合设置_Foldout", Float) = 1
        [Enum_Switch(Off, Multiply, Add)] _BlendMode ("混合模式", Float) = 0
        [Switch(Multiply, Add)][Tex(_BlendColor)] _BlendTex ("混合纹理", 2D) = "white" {}
        _BlendColor ("混合颜色", Color) = (1,1,1,1)
        [Switch(Multiply)] _MultiplyStrength ("乘法强度", Range(0, 1)) = 0.5
        [Switch(Add)] _AddStrength ("叠加强度", Range(0, 1)) = 0.5
        
        [Foldout_Out(1)] _Out2 ("_Foldout", Float) = 0
        
        // ========== 描边 Pass ==========
        [Foldout(1, 1)] _EnableOutlinePass ("描边 Pass_Foldout", Float) = 0
        _OutlineColor ("描边颜色", Color) = (0,0,0,1)
        _OutlineWidth ("描边宽度", Range(0, 0.1)) = 0.01
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Cull [_Cull]
        
        Pass
        {
            Name "MainPass"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _USENORMALMAP_ON
            #pragma shader_feature _ _BLENDMODE_MULTIPLY _BLENDMODE_ADD
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                #ifdef _USENORMALMAP_ON
                float4 tangent : TANGENT;
                #endif
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Brightness;
            
            #ifdef _USENORMALMAP_ON
            sampler2D _NormalMap;
            float _NormalScale;
            #endif
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color * _Brightness;
                return col;
            }
            ENDCG
        }
        
        Pass
        {
            Name "Outline"
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            
            float _OutlineWidth;
            float4 _OutlineColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                float3 norm = normalize(v.normal);
                float3 outlinePos = v.vertex.xyz + norm * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(float4(outlinePos, 1));
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
```

---

## 注意事项

1. **`_Foldout` 后缀**：必须添加在属性的**显示名称**中，不是属性名
   ```shaderlab
   ✅ 正确：[Foldout(1)] _Group ("设置_Foldout", Float) = 1
   ❌ 错误：[Foldout(1)] _Group_Foldout ("设置", Float) = 1
   ```

2. **折叠页属性类型**：折叠页属性必须是 `Float` 类型

3. **Pass 名称匹配**：使用 Pass 切换时，Pass 的 `Name` 必须与属性名中提取的名称一致
   ```shaderlab
   _EnableOutlinePass → Pass Name "Outline"
   ```

4. **关键字命名**：
   - Toggle：`_<PropertyName>_ON`（大写）
   - Enum：`_<PropertyName>_<OptionName>`（大写）

5. **嵌套层级**：理论上支持无限层级，但建议不超过 3 层以保持界面简洁

6. **性能考虑**：使用 `shader_feature` 而非 `multi_compile` 可以减少变体数量

---

## 总结

SimpleShaderGUI 提供了一套完整的材质编辑器解决方案，主要优势包括：

- ✅ **组织清晰**：通过折叠页系统组织大量属性
- ✅ **动态界面**：根据条件显示/隐藏属性，减少视觉混乱
- ✅ **易于使用**：通过 Attribute 即可配置，无需编写 C# 代码
- ✅ **功能丰富**：支持渲染状态、Pass 切换、权限控制等高级功能
- ✅ **可扩展**：基于 `PropertyDrawerBase` 可以轻松扩展自定义绘制器

通过合理使用这些功能，可以大幅提升 Shader 开发的效率和用户体验。
