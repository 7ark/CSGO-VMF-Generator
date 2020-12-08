using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text;
using VMFGenerator;

namespace VMFGenerator
{
    public static class EasyInputLayer
    {
        public static void GetInput(out List<GenerationMethod> generationMethods, out List<string> entities)
        {
            //If enabled, will draw images to a created debug folder
            VMFDebug.DebugMode = true;

            //Generation Methods
            generationMethods = new List<GenerationMethod>();
            //generationMethods.Add(new AimMapGenerationMethod()
            //{
            //    mapSize = 1024,
            //    overrideMinStairsCount = 7
            //});
            //generationMethods.Add(new ImageGenerationMethod()
            //{
            //    InputFilePath = Directory.GetCurrentDirectory() + @"\Input\InputImage.png"
            //});
            //generationMethods.Add(new HollowCubeGenerationMethod()
            //{
            //    Position = new Vector3(0, 0, 5f),
            //    Texture = Textures.SKYBOX,
            //    Scalar = 64,
            //    Size = new Vector3(30, 30, 10),
            //    Thickness = 0.5f
            //});
            generationMethods.Add(new GridGenerationMethod()
            {
                displacementSidesPerBlock = { SideFacing.Top },
                blockDepth = 256
            });
            //generationMethods.Add(new BasicSpawnsGenerationMethod());

            //Entities
            entities = new List<string>();
            entities.Add(EntityTemplates.LightEnvironment(
                lightColor: Color.FromArgb(255, 240, 240),
                ambientLightColor: Color.FromArgb(240, 240, 255),
                origin: new Vector3(0, 0, 256),
                brightness: 400,
                angles: new Vector3(-40, -60, 0),
                pitch: -60));
        }
    }
}
