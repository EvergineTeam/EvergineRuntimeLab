using Evergine.Bindings.Imgui;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.UI;
using EvergineRuntimeLab.Features.RuntimeAssets.Loaders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace EvergineRuntimeLab.Features.UI
{
    public class UIComponent : Behavior
    {
        [BindSceneManager]
        private ImGuiManager imGuiManager;

        private Texture evergineLogoTex;
        private ulong evergineLogo;

        public string Text;

        protected override bool OnAttached()
        {
            this.evergineLogoTex = this.Managers.AssetSceneManager.Load<Texture>(EvergineContent.Textures.EvergineLogo_png);

            this.evergineLogo = this.imGuiManager.CreateImGuiBinding(this.evergineLogoTex);


            return base.OnAttached();
        }

        protected unsafe override void Update(TimeSpan gameTime)
        {
            var io = ImguiNative.igGetIO_Nil();

            bool open = true;
            var bgColor = new Color(49, 49, 49);
            ImguiNative.igPushStyleColor_Vec4(ImGuiCol.WindowBg, bgColor.ToVector4());
            ImguiNative.igSetNextWindowPos(Vector2.Zero, ImGuiCond.Always, Vector2.Zero);
            ImguiNative.igSetNextWindowSize(io->DisplaySize, ImGuiCond.Always);

            ImguiNative.igBegin("MainWindow", open.Pointer(), ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoTitleBar);


            var textureSize = new Vector2(evergineLogoTex.Description.Width, evergineLogoTex.Description.Height);
            var position = new Vector2(
                (io->DisplaySize.X - textureSize.X) * 0.5f,
                (io->DisplaySize.Y - textureSize.Y) * 0.3f);

            ImguiNative.igSetCursorPos(position);
            ImguiNative.igImage(
                this.evergineLogo,
                textureSize,
                uv0: Vector2.Zero,
                uv1: Vector2.One);


            Vector2 size;
            ImguiNative.igCalcTextSize(&size, this.Text, null, false, -1.0f);

            position = new Vector2(
                (io->DisplaySize.X - size.X) * 0.5f,
                (io->DisplaySize.Y - size.Y) * 0.5f);


            ImguiNative.igSetCursorPos(position);
            ImguiNative.igTextWrapped(this.Text);

            ImguiNative.igEnd();

            ImguiNative.igPopStyleColor(1);
        }

        internal void SetSuportedFiles(Dictionary<RuntimeLoaderType, string[]> extensionsByType)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Drag and drop the following files:");
            foreach (var kvp in extensionsByType)
            {
                var description = GetDescription(kvp.Key) ?? kvp.Key.ToString();
                var extensions = string.Join(", ", kvp.Value);
                sb.AppendLine($"· {description}: {extensions}");
            }
            this.Text = sb.ToString();
        }



        public static string GetDescription(Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
    }
}
