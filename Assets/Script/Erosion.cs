using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public class Erosion : MonoBehaviour
{
    // terrain à modifier et heightmap à appliquer
    public TerrainData terrain;
    public Texture2D heightMap;
    int resolution;


    // itération d'érosion à modéliser
    public int cycles = 300000;

    // paramètres influant sur l'érosion
    public float gravite = 4;
    public float inertie = 0.05f;
    public float tauxSediment = 4;
    public int surface = 6;
    public float largeurErosion = 0.035f;

    // Pinceaux érosion 
    int[][] surfaceErosion;

    System.Random random = new System.Random();

    float[] initialisation()
    {
        // récupération de la résolution du terrain
        resolution = terrain.heightmapResolution;


        // le tableau de tableau référence les coordonnées de tous les points dans la surface d'action centrée à une coordonnée précise.
        surfaceErosion = new int[resolution * resolution][];

        // pour chacun des m² de la carte
        for (int point = 0; point < resolution * resolution; point++)
        {
            // récupération des coordonnées du point
            int X = point % resolution;
            int Y = point / resolution;

            // création d'un tableau pour le m² "point" considéré 
            surfaceErosion[point] = new int[surface * surface];
            int index = 0;

            // pour toute la surface "surface" centrée sur le m² "point"
            for (int y = -surface / 2; y < surface / 2; y++)
            {
                for (int x = -surface / 2; x < surface / 2; x++)
                {
                    // ajout de toutes les coordonnées des points autour du m² contenus dans la surface "surface"
                    surfaceErosion[point][index] = (y + Y) * resolution + x + X;

                    // nombre de points dans la surface centrée sur "point"
                    index++;
                }
            }
        }


        // renvoie le terrain brut généré par le jpg via un script externe 
        return Generateur.ConvertisseurTerrainJPG(heightMap, terrain);
    }


    public void Eroder()
    {

        float[] dataTerrain = initialisation();

        // boucle itérative pour chaque cycle d'érosion
        for (int c = 0; c < cycles; c++) 
        {
            // création de la goutte
            Goutte g = CreerGoutte();

            // pour toute la durée de vie de la goutte 
            for (int i = 0; i < 30; i++)
            {

                int coordX = (int)g.X;
                int coordY = (int)g.Y;
                
                // DEPLACEMENT DE LA GOUTTE 

                // position  arrondie sur le terrain de la goutte
                int position = coordX + resolution * coordY;

                // position exacte de la goutte dans le m² de coordonnée (X,Y)
                float x = g.X - coordX;
                float y = g.Y - coordY;

                // calcul de la hauteur et des gradients directionnels de la goutte 
                HauteurGradient hg = CalculerHauteurGradient(dataTerrain, g.X, g.Y);

                // mis à jour de la direction prise par la goutte en fonction de sa direction précédente (inertie) et le terrain autour d'elle (gradientX & gradientY) 
                g.dirX = g.dirX * inertie - hg.gradientX*(1-inertie);
                g.dirY = g.dirY * inertie - hg.gradientY*(1-inertie);

                // normalisation du vecteur directionnel de la goutte 
                float dirXY = Mathf.Sqrt(g.dirX * g.dirX + g.dirY * g.dirY);
                g.dirX /= (dirXY != 0) ? dirXY : 1;
                g.dirY /= (dirXY != 0) ? dirXY : 1;

                // mis à jour de la position de la goutte en fonction de ses directions
                g.X += g.dirX;
                g.Y += g.dirY;

                // si la goutte ne bouge plus ou si elle dépasse les limites du terrains, on stoppe le programme
                if ((g.dirX == 0 && g.dirY == 0) || g.X < 0 || g.X >= resolution - 1 || g.Y < 0 || g.Y >= resolution - 1)
                {
                    break;
                }

                // calcul de la nouvelle hauteur de la goutte en fonction de sa nouvelle position
                float nouvelleHauteur = CalculerHauteurGradient(dataTerrain, g.X, g.Y).hauteur;

                // différence d'hauteur parcourue par la goutte
                float deltaHauteur = hg.hauteur-nouvelleHauteur;

                // SEDIMENTATION DE LA GOUTTE
                // quantité maximale de sédiment que peut contenir la goutte
                float maxSediment = Mathf.Max(deltaHauteur * g.vitesse * g.volume * tauxSediment,0.01f);

                // Important, peut importe la situation la goutte ne peut pas éroder ou déposer plus de sédiments que la hauteur de terrain qu'elle a parcourue
                // sinon l'érosion produit des crevasses multiples très disgracieuses


                float depot = 0;
                // si la goutte contient plus de sédiment qu'elle ne peut normalement en contenir on dépose une fraction de l'excedent
                if (g.sediment > maxSediment)
                    depot = (g.sediment - maxSediment)*0.3f;

                // si la goutte est sur une pente montante on dépose les sédiments
                if (deltaHauteur < 0)
                    depot = Mathf.Min(-deltaHauteur,g.sediment);

                // si dépot il y a on update les données du terrain en rajoutant le dépot 
                dataTerrain[position] += depot * (1 - x) * (1 - y);
                dataTerrain[position + 1] += depot * x * (1 - y);
                dataTerrain[position + resolution] += depot * (1 - x) * y;
                dataTerrain[position + resolution + 1] += depot * x * y;

                // on elève des sédiments portés par l'eau le dépot
                g.sediment -= depot;

                // si l'eau ne dépose aucun sédiment, cela signifique qu'elle en retire au terrain, l'érosion a lieu
                if (depot == 0)
                {
                    // on ne doit pas éroder plus que la hauteur de terrain parcourue 
                    float erosion = Mathf.Min((maxSediment - g.sediment) * 0.3f ,deltaHauteur)*largeurErosion;

                    // pour chaque point dans le rayon d'action de la goutte à "position"
                    for (int j = 0; j < surfaceErosion[position].Length; j++)
                    {
                        // méthode pour éviter des erreurs Out Of Array
                        int index = Mathf.Min(surfaceErosion[position][j], dataTerrain.Length - 1);
                        index = Mathf.Max(0, index);

                        // determination de la quantité de sédiment à enlever, pour ne pas retirer plus de sédiment que le terrain est haut (sinon la hauteur devient négative)
                        float deltaSediment = Mathf.Min(dataTerrain[index], erosion);

                        // mise à jour des données du terrain et de la quantité de sédiment portée par la goutte
                        dataTerrain[index] -= deltaSediment;
                        g.sediment += deltaSediment;
                    }

                }

                // mise à jour de la vitesse de la goutte et simulation du processus d'évaporation
                g.vitesse = Mathf.Sqrt(g.vitesse*g.vitesse + deltaHauteur * gravite);
                g.volume *= 0.99f;

            }
        }

        // une fois le processus d'erosion terminé, on récupère les données du terrain et on y intègre les données calculées 
        float[,] heightmapData = terrain.GetHeights(0,0,resolution,resolution);

        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                heightmapData[y, x] = dataTerrain[y * resolution + x];

        terrain.SetHeights(0, 0, heightmapData);
    }

    Goutte CreerGoutte()
    {
        float X = random.Next(0, resolution - 1);
        float Y = random.Next(0, resolution - 1);
        return new Goutte() { X = X, Y = Y, dirX = 0, dirY = 0, vitesse = 1, volume = 1, sediment = 0  };
    }

    HauteurGradient CalculerHauteurGradient(float[] data, float X, float Y)
    {
        // on récupère le m² dans lequel se trouve la hauteur à déterminer
        int posX = (int)X;
        int posY = (int)Y;

        // correspond à la position exacte dans la cellule (m²) où se trouve la goutte
        float x = X - posX;
        float y = Y - posY;

        // correspond à la position dans la data du terrain où l'on se trouve 
        int position = posX + resolution * posY;

        // determination de la hauteur des 4 points les plus proches aux 4 points cardinaux de la hauteur à considérer
        float h1 = data[position];
        float h2 = data[position + 1];
        float h3 = data[position + resolution];
        float h4 = data[position + resolution + 1];

        // calcul en fonction des 4 points cardinaux du gradient directionnel X et Y
        float gradientX = (h2 - h1) * (1 - y) + (h4 - h3) * y;
        float gradientY = (h3 - h1) * (1 - x) + (h4 - h2) * x;

        // determination de la hauteur du point à considérer par interpolation bilinéaire
        float h = h1 * (1 - x) * (1 - y) + h2 * x * (1 - y) + h3 * (1 - x) * y + h4 * x * y;

        return new HauteurGradient() { hauteur = h, gradientX = gradientX, gradientY = gradientY };
    }


    struct HauteurGradient
    {
        public float hauteur;
        public float gradientX;
        public float gradientY;
    }

    struct Goutte
    {
        public float X;
        public float Y;
        public float dirX;
        public float dirY;
        public float vitesse;
        public float volume;
        public float sediment;
    }

    
}