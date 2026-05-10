using UnityEngine;
using UnityEditor;

public class MaterialConverter
{
    [MenuItem("Tools/Convert URP Materials to Standard")]
    public static void ConvertAllMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int convertedCount = 0;
        int skippedCount = 0;

        Debug.Log($"[MaterialConverter] 開始掃描... 找到 {guids.Length} 個材質檔案。");

        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
        {
            Debug.LogError("[MaterialConverter] 找不到 Standard Shader！請確認專案設定正確。");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
            {
                Debug.LogWarning($"[MaterialConverter] 無法載入材質: {path}");
                continue;
            }

            string shaderName = mat.shader.name;

            // 檢查是否為 URP / HDRP / 或已損壞的 Shader
            if (shaderName.Contains("Universal") ||
                shaderName.Contains("URP") ||
                shaderName.Contains("HDRP") ||
                shaderName.Contains("Hidden/InternalErrorShader"))
            {
                // 嘗試保存原始顏色
                Color originalColor = Color.white;
                if (mat.HasProperty("_BaseColor"))
                {
                    originalColor = mat.GetColor("_BaseColor");
                }
                else if (mat.HasProperty("_Color"))
                {
                    originalColor = mat.GetColor("_Color");
                }

                // 嘗試保存原始主貼圖
                Texture originalMainTex = null;
                if (mat.HasProperty("_BaseMap"))
                {
                    originalMainTex = mat.GetTexture("_BaseMap");
                }
                else if (mat.HasProperty("_MainTex"))
                {
                    originalMainTex = mat.GetTexture("_MainTex");
                }

                // 切換 Shader
                mat.shader = standardShader;

                // 還原顏色和貼圖到 Standard Shader 的屬性
                mat.SetColor("_Color", originalColor);
                if (originalMainTex != null)
                {
                    mat.SetTexture("_MainTex", originalMainTex);
                }

                EditorUtility.SetDirty(mat);
                Debug.Log($"[MaterialConverter] ✅ 已轉換: {path} (原 Shader: {shaderName})");
                convertedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MaterialConverter] 轉換完成！已轉換: {convertedCount} 個, 跳過: {skippedCount} 個。");
        EditorUtility.DisplayDialog("Material Converter",
            $"轉換完成！\n\n已轉換: {convertedCount} 個材質\n跳過: {skippedCount} 個材質",
            "確定");
    }
}
