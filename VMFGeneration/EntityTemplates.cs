using System;
using System.Drawing;
using System.Numerics;

namespace VMFGenerator
{
    public static class EntityTemplates
    {
        public static int BlockEntityID = 0;
        public static int LastID = -1;

        public enum BlockEntityType { func_detail, func_buyzone_terrorist, func_buyzone_counterterrorist, func_buyzone_all, trigger_hurt }

        public static string ToValue(this BlockEntityType type)
        {
            switch (type)
            {
                case BlockEntityType.func_buyzone_terrorist:
                    return "func_buyzone\"" + Environment.NewLine +
                        "\t\"TeamNum\" \"2";
                case BlockEntityType.func_buyzone_counterterrorist:
                    return "func_buyzone\"" + Environment.NewLine +
                        "\t\"TeamNum\" \"3";
                case BlockEntityType.func_buyzone_all:
                    return "func_buyzone\"" + Environment.NewLine +
                        "\t\"TeamNum\" \"0";
            }

            return type.ToString();
        }

        public static string LightEnvironment(Color lightColor = new Color(), float brightness = 20, Color ambientLightColor = new Color(), float ambientBrightness = 200, Vector3 angles = new Vector3(), float pitch = -90, float sunSpreadAngle = 0, Vector3 origin = new Vector3())
        {
            if (lightColor.A == 0)
            {
                lightColor = Color.White;
            }
            if (ambientLightColor.A == 0)
            {
                ambientLightColor = Color.White;
            }

            return
            "entity" + Environment.NewLine +
            "{" + Environment.NewLine +
            "\t\"id\" \"" + (++LastID) + "\"" + Environment.NewLine +
            "\t\"classname\" \"light_environment\"" + Environment.NewLine +
            "\t\"_ambient\" \"" + ambientLightColor.R + " " + ambientLightColor.G + " " + ambientLightColor.B + " " + ambientBrightness + "\"" + Environment.NewLine +
            "\t\"_ambientHDR\" \"-1 -1 -1 1\"" + Environment.NewLine +
            "\t\"_AmbientScaleHDR\" \"1\"" + Environment.NewLine +
            "\t\"_light\" \"" + lightColor.R + " " + lightColor.G + " " + lightColor.B + " " + brightness + "\"" + Environment.NewLine +
            "\t\"_lightHDR\" \"-1 -1 -1 1\"" + Environment.NewLine +
            "\t\"_lightscaleHDR\" \"1\"" + Environment.NewLine +
            "\t\"angles\" \"" + angles.X + " " + angles.Y + " " + angles.Z + "\"" + Environment.NewLine +
            "\t\"pitch\" \"" + pitch + "\"" + Environment.NewLine +
            "\t\"SunSpreadAngle\" \"" + sunSpreadAngle + "\"" + Environment.NewLine +
            "\t\"origin\" \"" + origin.X + " " + origin.Y + " " + origin.Z + "\"" + Environment.NewLine +
            "\teditor" + Environment.NewLine +
            "\t{" + Environment.NewLine +
            "\t\t\"color\" \"220 30 220\"" + Environment.NewLine +
            "\t\t\"visgroupshown\" \"1\"" + Environment.NewLine +
            "\t\t\"visgroupautoshown\" \"1\"" + Environment.NewLine +
            "\t\t\"logicalpos\" \"[0 0]\"" + Environment.NewLine +
            "\t}" + Environment.NewLine +
            "}";
        }

        public static string InfoPlayerTerrorist(Vector3 angles = new Vector3(), Vector3 origin = new Vector3())
        {
            return
            "entity" + Environment.NewLine +
            "{" + Environment.NewLine +
            "\t\"id\" \"" + (++LastID) + "\"" + Environment.NewLine +
            "\t\"classname\" \"info_player_terrorist\"" + Environment.NewLine +
            "\t\"angles\" \"" + angles.X + " " + angles.Y + " " + angles.Z + "\"" + Environment.NewLine +
            "\t\"enabled\" \"1\"" + Environment.NewLine +
            "\t\"origin\" \"" + origin.X + " " + origin.Y + " " + origin.Z + "\"" + Environment.NewLine +
            "\teditor" + Environment.NewLine +
            "\t{" + Environment.NewLine +
            "\t\t\"color\" \"220 30 220\"" + Environment.NewLine +
            "\t\t\"visgroupshown\" \"1\"" + Environment.NewLine +
            "\t\t\"visgroupautoshown\" \"1\"" + Environment.NewLine +
            "\t\t\"logicalpos\" \"[0 0]\"" + Environment.NewLine +
            "\t}" + Environment.NewLine +
            "}";
        }
        public static string InfoPlayerCounterTerrorist(Vector3 angles = new Vector3(), Vector3 origin = new Vector3())
        {
            return
            "entity" + Environment.NewLine +
            "{" + Environment.NewLine +
            "\t\"id\" \"" + (++LastID) + "\"" + Environment.NewLine +
            "\t\"classname\" \"info_player_counterterrorist\"" + Environment.NewLine +
            "\t\"angles\" \"" + angles.X + " " + angles.Y + " " + angles.Z + "\"" + Environment.NewLine +
            "\t\"enabled\" \"1\"" + Environment.NewLine +
            "\t\"origin\" \"" + origin.X + " " + origin.Y + " " + origin.Z + "\"" + Environment.NewLine +
            "\teditor" + Environment.NewLine +
            "\t{" + Environment.NewLine +
            "\t\t\"color\" \"220 30 220\"" + Environment.NewLine +
            "\t\t\"visgroupshown\" \"1\"" + Environment.NewLine +
            "\t\t\"visgroupautoshown\" \"1\"" + Environment.NewLine +
            "\t\t\"logicalpos\" \"[0 0]\"" + Environment.NewLine +
            "\t}" + Environment.NewLine +
            "}";
        }
        public static string PropStatic(string modelName, Vector3 angles = new Vector3(), Vector3 origin = new Vector3())
        {
            return
            "entity" + Environment.NewLine +
            "{" + Environment.NewLine +
            "\t\"id\" \"" + (++LastID) + "\"" + Environment.NewLine +
            "\t\"classname\" \"prop_static\"" + Environment.NewLine +
            "\t\"angles\" \"" + angles.X + " " + angles.Y + " " + angles.Z + "\"" + Environment.NewLine +
            "\t\"disableflashlight\" \"0\"" + Environment.NewLine +
            "\t\"disableselfshadowing\" \"0\"" + Environment.NewLine +
            "\t\"disableshadowdepth\" \"0\"" + Environment.NewLine +
            "\t\"disableshadows\" \"0\"" + Environment.NewLine +
            "\t\"disablevertexlighting\" \"0\"" + Environment.NewLine +
            "\t\"disableX360\" \"0\"" + Environment.NewLine +
            "\t\"drawinfastreflection\" \"0\"" + Environment.NewLine +
            "\t\"enablelightbounce\" \"0\"" + Environment.NewLine +
            "\t\"fademaxdist\" \"0\"" + Environment.NewLine +
            "\t\"fademindist\" \"-1\"" + Environment.NewLine +
            "\t\"fadescale\" \"1\"" + Environment.NewLine +
            "\t\"ignorenormals\" \"0\"" + Environment.NewLine +
            "\t\"maxcpulevel\" \"0\"" + Environment.NewLine +
            "\t\"maxgpulevel\" \"0\"" + Environment.NewLine +
            "\t\"mincpulevel\" \"0\"" + Environment.NewLine +
            "\t\"mingpulevel\" \"0\"" + Environment.NewLine +
            "\t\"model\" \"" + modelName + ".mdl\"" + Environment.NewLine +
            "\t\"preventpropcombine\" \"0\"" + Environment.NewLine +
            "\t\"renderamt\" \"255\"" + Environment.NewLine +
            "\t\"rendercolor\" \"255 255 255\"" + Environment.NewLine +
            "\t\"screenspacefade\" \"0\"" + Environment.NewLine +
            "\t\"shadowdepthnocache\" \"0\"" + Environment.NewLine +
            "\t\"skin\" \"0\"" + Environment.NewLine +
            "\t\"solid\" \"6\"" + Environment.NewLine +
            "\t\"uniformscale\" \"1\"" + Environment.NewLine +
            "\t\"origin\" \"" + origin.X + " " + origin.Y + " " + origin.Z + "\"" + Environment.NewLine +
            "\teditor" + Environment.NewLine +
            "\t{" + Environment.NewLine +
            "\t\t\"color\" \"220 30 220\"" + Environment.NewLine +
            "\t\t\"visgroupshown\" \"1\"" + Environment.NewLine +
            "\t\t\"visgroupautoshown\" \"1\"" + Environment.NewLine +
            "\t\t\"logicalpos\" \"[0 0]\"" + Environment.NewLine +
            "\t}" + Environment.NewLine +
            "}";
        }
    }
}
