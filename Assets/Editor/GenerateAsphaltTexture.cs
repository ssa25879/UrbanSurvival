#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// 전쟁으로 마모된 아스팔트 느낌의 절차적 텍스처를 생성하는 에디터 전용 유틸리티.
// 외부 텍스처 에셋이 없어 노이즈 기반으로 균열/얼룩/색 변화를 합성한다.
public static class GenerateAsphaltTexture {
    [MenuItem("Tools/Urban Survival/Generate War-Torn Asphalt Texture")]
    public static void Generate() {
        int size = 1024;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        var rnd = new System.Random(20260923);

        Color baseDark = new Color(0.10f, 0.10f, 0.11f);
        Color baseLight = new Color(0.22f, 0.22f, 0.23f);
        Color stain = new Color(0.06f, 0.05f, 0.04f);
        Color dust = new Color(0.30f, 0.28f, 0.24f);

        float[,] noiseA = new float[size, size];
        float[,] noiseB = new float[size, size];
        float offsetAx = (float)rnd.NextDouble() * 1000f, offsetAy = (float)rnd.NextDouble() * 1000f;
        float offsetBx = (float)rnd.NextDouble() * 1000f, offsetBy = (float)rnd.NextDouble() * 1000f;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float u = x / (float)size;
                float v = y / (float)size;
                // 큰 얼룩(저주파) + 미세 알갱이(고주파) 노이즈를 합성
                float coarse = Mathf.PerlinNoise(u * 6f + offsetAx, v * 6f + offsetAy);
                float fine = Mathf.PerlinNoise(u * 40f + offsetBx, v * 40f + offsetBy);
                noiseA[x, y] = coarse;
                noiseB[x, y] = fine;
            }
        }

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float coarse = noiseA[x, y];
                float fine = noiseB[x, y];
                Color c = Color.Lerp(baseDark, baseLight, coarse);
                c = Color.Lerp(c, c * 1.15f, fine * 0.3f);

                // 얼룩(기름/그을음 자국) - 저주파 노이즈가 임계값보다 낮은 곳
                if (coarse < 0.28f) {
                    float t = Mathf.InverseLerp(0.28f, 0.05f, coarse);
                    c = Color.Lerp(c, stain, t * 0.8f);
                }
                // 먼지/잔해 얼룩 - 다른 위상의 저주파 임계값
                if (coarse > 0.74f) {
                    float t = Mathf.InverseLerp(0.74f, 0.95f, coarse);
                    c = Color.Lerp(c, dust, t * 0.5f);
                }
                tex.SetPixel(x, y, c);
            }
        }

        // 균열 라인: 무작위 시작점에서 짧은 선분을 이어 붙여 갈라진 자국을 그린다
        int crackCount = 26;
        for (int i = 0; i < crackCount; i++) {
            float px = (float)rnd.NextDouble() * size;
            float py = (float)rnd.NextDouble() * size;
            float angle = (float)rnd.NextDouble() * Mathf.PI * 2f;
            int segments = 14 + rnd.Next(0, 18);
            float crackWidth = 1.2f + (float)rnd.NextDouble() * 1.6f;
            for (int s = 0; s < segments; s++) {
                angle += ((float)rnd.NextDouble() - 0.5f) * 0.9f;
                float len = 6f + (float)rnd.NextDouble() * 10f;
                float nx = px + Mathf.Cos(angle) * len;
                float ny = py + Mathf.Sin(angle) * len;
                DrawLine(tex, px, py, nx, ny, crackWidth, new Color(0.02f, 0.02f, 0.02f));
                px = nx; py = ny;
                if (px < 0 || px >= size || py < 0 || py >= size) break;
            }
        }

        tex.Apply();

        string dir = "Assets/Game/Textures";
        if (!AssetDatabase.IsValidFolder(dir)) {
            if (!AssetDatabase.IsValidFolder("Assets/Game")) AssetDatabase.CreateFolder("Assets", "Game");
            AssetDatabase.CreateFolder("Assets/Game", "Textures");
        }
        string path = dir + "/WarTornAsphalt_Albedo.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.sRGBTexture = true;
        importer.SaveAndReimport();

        Debug.Log("War-torn asphalt 텍스처 생성 완료: " + path);
    }

    private static void DrawLine(Texture2D tex, float x0, float y0, float x1, float y1, float width, Color color) {
        int size = tex.width;
        float dist = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist));
        int halfW = Mathf.CeilToInt(width);
        for (int i = 0; i <= steps; i++) {
            float t = i / (float)steps;
            int cx = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int cy = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            for (int ox = -halfW; ox <= halfW; ox++) {
                for (int oy = -halfW; oy <= halfW; oy++) {
                    int px = cx + ox, py = cy + oy;
                    if (px < 0 || px >= size || py < 0 || py >= size) continue;
                    float d = Mathf.Sqrt(ox * ox + oy * oy);
                    if (d > width) continue;
                    float a = 1f - (d / width);
                    Color existing = tex.GetPixel(px, py);
                    tex.SetPixel(px, py, Color.Lerp(existing, color, a * 0.85f));
                }
            }
        }
    }
}
#endif
