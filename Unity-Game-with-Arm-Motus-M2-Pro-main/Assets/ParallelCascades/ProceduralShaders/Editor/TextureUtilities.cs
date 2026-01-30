using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ParallelCascades.ProceduralShaders.Editor
{
    public static class TextureUtilities
    {
        public static void SaveTextureAsset(Texture2D texture)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(assetPath))
            {
                byte[] textureData;
            
                if(GraphicsFormatUtility.IsHDRFormat(texture.graphicsFormat))
                {
                    textureData = texture.EncodeToEXR();                
                }
                else
                {
                    textureData = texture.EncodeToPNG();
                }
            
                System.IO.File.WriteAllBytes(assetPath, textureData);
                AssetDatabase.ImportAsset(assetPath);
            }
            else
            {
                Debug.LogError("The texture is not associated with an asset in the project. Please save it as an asset first.");
            }
        }
        
        public static Texture2D CreateGradientTexture(string projectRelativeFolderPath, string name)
        {
            Texture2D colorGradientTexture = new Texture2D(16, 1, TextureFormat.RGBA32, false);
        
            for(int x = 0; x < colorGradientTexture.width; x++)
            {
                float t = x / (float)(colorGradientTexture.width - 1);
                Color color = Color.HSVToRGB(t, 1, 1);
                colorGradientTexture.SetPixel(x, 0, color);
            }
        
            colorGradientTexture.Apply();
        
            string texturePath = $"{projectRelativeFolderPath}/{name}.png";
            texturePath = AssetDatabase.GenerateUniqueAssetPath(texturePath);
        
            File.WriteAllBytes(texturePath, colorGradientTexture.EncodeToPNG());
        
            AssetDatabase.ImportAsset(texturePath);
        
            Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if(textureImporter != null)
            {
                textureImporter.isReadable = true;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                textureImporter.mipmapEnabled = false;
                textureImporter.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            }

            return importedTexture;
        }
        
        public static Texture2D GetPreviewTexture(Material material, int textureSize)
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(textureSize, textureSize, 16);
            RenderTexture.active = null;

            GameObject tempCameraObj = new GameObject
            {
                transform =
                {
                    position = Vector3.back
                },
                hideFlags = HideFlags.HideAndDontSave
            };

            Camera tempCamera = tempCameraObj.AddComponent<Camera>();
            tempCamera.hideFlags = HideFlags.HideAndDontSave;
            tempCamera.enabled = false;
            tempCamera.cameraType = CameraType.Preview;
            tempCamera.orthographic = true;
            tempCamera.orthographicSize = 0.5f;
            tempCamera.farClipPlane = 10.0f;
            tempCamera.nearClipPlane = 0.1f;
            tempCamera.clearFlags = CameraClearFlags.Color;
            tempCamera.backgroundColor = Color.clear;
            tempCamera.renderingPath = RenderingPath.Forward;
            tempCamera.useOcclusionCulling = false;
            tempCamera.allowMSAA = false;
            tempCamera.allowHDR = true;
            
            int previewLayer = 31;
            tempCamera.cullingMask = 1 << previewLayer;

            tempCamera.targetTexture = renderTexture;
            
            var tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tempQuad.GetComponent<MeshRenderer>().sharedMaterial = material;
            tempQuad.layer = previewLayer;
            tempCamera.Render();

            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.width), 0, 0);
            texture.Apply();
            
            RenderTexture.active = null;
            Object.DestroyImmediate(tempCameraObj);
            Object.DestroyImmediate(tempQuad);

            return texture;
        }
    }
}
