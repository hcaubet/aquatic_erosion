using UnityEngine;
using UnityEditor;
using System;

public class Generateur : MonoBehaviour 
{
    public Texture2D image;
    public TerrainData terrain;

    public void Generer() {
        float[] map = ConvertisseurTerrainJPG(image,terrain);

        int resolution = terrain.heightmapResolution;
        float[,] heightmapData = terrain.GetHeights(0,0,resolution,resolution);


        // application des données sur le terrain
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heightmapData[y, x] = map[y * resolution + x];

            }
        }

        terrain.SetHeights(0, 0, heightmapData);
    }

    public static float[] ConvertisseurTerrainJPG(Texture2D image,TerrainData terrain)
    {
        int l = image.width;
        int h = image.height;

        int resolution = terrain.heightmapResolution;
        float[] map2 = new float[resolution * resolution];

        Color[] mapColors = image.GetPixels();
        Color[] map = new Color[resolution * resolution];

        if (resolution != l || h != l)
        {
            // Si l'image n'a pas de filtre on utilise la technique du nearest neighbor
            if (image.filterMode == FilterMode.Point)
                {
                // on récupere les ratio entre l'image et la taille du terrain
                float dx = (float)l / (float)resolution;
                float dy = (float)h / (float)resolution;
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        int px = Mathf.FloorToInt(x*dx);
                        int py = Mathf.FloorToInt(y*dy);
                        map[y*resolution + x] = mapColors[Mathf.FloorToInt(py* l + px)];
                    }
                }
            }
            // Sinon on met l'image à la bonne taille par bilinear scaling
            else
            {
                float ratioX = (float)(l-1) / (float)resolution;
                float ratioY = (float)(h-1) / (float)resolution;
                
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        int yy = Mathf.FloorToInt(y * ratioY);
                        int xx = Mathf.FloorToInt(x * ratioX);

                        float xd = ratioX * x - xx;
                        float yd = ratioY * y - yy;

                        int index = l*yy +xx;

                        Color pa = mapColors[index];
                        Color pb = mapColors[index + 1];
                        Color pc = mapColors[index + l];
                        Color pd = mapColors[index + l + 1];

                        map[y*resolution + x] = Color.Lerp(Color.Lerp(pa, pb, xd), Color.Lerp(pc, pd, xd), yd);
                    }
                }
            }
        }
        else
        {
            map = mapColors;      
        }


        // normalisation des données
        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                map2[y*resolution+x] = map[y * resolution + x].grayscale;

        return map2;

    }
}
