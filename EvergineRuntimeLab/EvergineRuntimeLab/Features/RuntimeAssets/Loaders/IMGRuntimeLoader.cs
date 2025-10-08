using Evergine.Common.Graphics;
using Evergine.Components.Animation;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Mathematics;
using Evergine.Runtimes.Images;
using Evergine.Runtimes.STL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvergineRuntimeLab.Features.RuntimeAssets.Loaders
{
    public class IMGRuntimeLoader : BaseRuntimeLoader
    {
        public override RuntimeLoaderType LoaderType { get; } = RuntimeLoaderType.Image;

        public override string[] SupportedExtensions { get; } = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".ktx", ".ktx2", ".webp", ".gif", ".ico" };

        private StandardMaterial material;

        public IMGRuntimeLoader(RuntimeAssetManager runtimeAssetManager)
            : base(runtimeAssetManager)
        {
            var effect = runtimeAssetManager.AssetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);
            this.material = new StandardMaterial(effect);
            this.material.IBLEnabled = false;
            this.material.LightingEnabled = false;
            this.material.AlphaCutout = 0.01f;
            this.material.LayerDescription = runtimeAssetManager.AssetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
            this.material.BaseColorSampler = runtimeAssetManager.AssetsService.Load<SamplerState>(DefaultResourcesIDs.LinearClampSamplerID);
        }

        public override async Task<RuntimeLoadResult> LoadAsset(string path)
        {
            RuntimeLoadResult result = new RuntimeLoadResult();

            using var fileStream = File.OpenRead(path);
            if (fileStream != null)
            {
                var texture = await ImageRuntime.Instance.Read(fileStream);

                if (texture != null)
                {
                    var aspectRatio = (float)texture.Description.Width / texture.Description.Height;

                    this.material.BaseColorTexture = texture;

                    var modelEntity = new Entity()
                        .AddComponent(new Transform3D() { LocalRotation = new Vector3(MathHelper.ToRadians(90), 0, 0) })
                        .AddComponent(new MaterialComponent() { Material = this.material.Material })
                        .AddComponent(new PlaneMesh() { Width = aspectRatio, Height = 1 })
                        .AddComponent(new MeshRenderer());

                    if (modelEntity != null)
                    {
                        result.IsValid = true;
                        result.Entity = modelEntity;
                        result.BoundingBox = new BoundingBox(new Vector3(aspectRatio * -0.5f, 0, -0.5f), new Vector3(aspectRatio * 0.5f, 0, 0.5f));
                    }
                }
            }

            return result;
        }
    }
}
