using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text;
using VMFConverter;

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
            generationMethods.Add(new MiscGenerationMethod());
            //generationMethods.Add(new ImageGenerationMethod()
            //{
            //    InputFilePath = Directory.GetCurrentDirectory() + @"\Input\InputImage.png"
            //});
            generationMethods.Add(new HollowCubeGenerationMethod()
            {
                Position = new Vector3(0, 0, 5f),
                Texture = Textures.SKYBOX,
                Scalar = 64,
                Size = new Vector3(30, 50, 10),
                Thickness = 0.5f
            });

            //Entities
            entities = new List<string>();
            entities.Add(EntityTemplates.LightEnvironment(
                lightColor: Color.FromArgb(255, 240, 240),
                ambientLightColor: Color.FromArgb(240, 240, 255),
                origin: new Vector3(0, 0, 256),
                brightness: 400,
                angles: new Vector3(-40, -60, 0),
                pitch: -60));
            entities.Add(EntityTemplates.InfoPlayerTerrorist(
                origin: new Vector3(0, 64, 0))
                );
            entities.Add(EntityTemplates.InfoPlayerCounterTerrorist(
                origin: new Vector3(128, 64, 0))
                );
        }
    }
}
