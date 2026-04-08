using Evergine.Common.Graphics;
using Evergine.Common.IO;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Mathematics;
using Evergine.Runtimes.Video;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EvergineRuntimeLab.Features.RuntimeAssets.Loaders
{
    public class VideoRuntimeLoader : BaseRuntimeLoader
    {
        public override RuntimeLoaderType LoaderType { get; } = RuntimeLoaderType.Video;

        public override string[] SupportedExtensions { get; } = new[] { ".mp4", ".avi", ".mov", ".mkv" };

        private StandardMaterial material;

        public VideoRuntimeLoader(RuntimeAssetManager runtimeAssetManager)
            : base(runtimeAssetManager)
        {
            this.CleanupTempDirectory();

            var effect = runtimeAssetManager.AssetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);
            this.material = new StandardMaterial(effect);
            this.material.IBLEnabled = false;
            this.material.LightingEnabled = false;
            this.material.AlphaCutout = 0.01f;
            this.material.LayerDescription = runtimeAssetManager.AssetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
            this.material.BaseColorSampler = runtimeAssetManager.AssetsService.Load<SamplerState>(DefaultResourcesIDs.LinearClampSamplerID);
        }

        private void CleanupTempDirectory()
        {
            var assetsDirectory = this.runtimeAssetManager.AssetsDirectory;
            if (assetsDirectory == null || string.IsNullOrWhiteSpace(assetsDirectory.RootPath))
            {
                return;
            }

            var tempDirectory = Path.Combine(assetsDirectory.RootPath, "../tmp");
            Directory.CreateDirectory(tempDirectory);

            foreach (var filePath in Directory.EnumerateFiles(tempDirectory))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore files that are still locked by the runtime.
                }
            }
        }

        public override async Task<RuntimeLoadResult> LoadAsset(string path)
        {
            RuntimeLoadResult result = new RuntimeLoadResult();

            // The video file needs to be copied to a location relative to the assets folder,
            // so we copy it to a temp directory inside the assets folder. This is a current
            // limitation of the VideoPlayer component that requires video paths to be relative to the assets directory.
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return result;
            }

            var targetVideoPath = await this.CopyVideoToContentAsync(path);
            if (string.IsNullOrWhiteSpace(targetVideoPath))
            {
                return result;
            }

            var modelEntity = new Entity()
                .AddComponent(new Transform3D() { LocalRotation = new Vector3(MathHelper.ToRadians(90), 0, 0) })
                .AddComponent(new MaterialComponent() { Material = this.material.Material })
                .AddComponent(new PlaneMesh())
                .AddComponent(new MeshRenderer())
                .AddComponent(new VideoPlayer
                {
                    Autoplay = true,
                    Loop = true,
                    HWDevice = VideoPlayer.DeviceType.DXVA2,
                    VideoPath = targetVideoPath,
                })
                .AddComponent(new VideoPlayerController(material));

            if (modelEntity != null)
            {
                result.IsValid = true;
                result.Entity = modelEntity;
                result.BoundingBox = new BoundingBox(new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));
            }
            return result;
        }

        private async Task<string> CopyVideoToContentAsync(string sourcePath)
        {
            var assetsDirectory = this.runtimeAssetManager.AssetsDirectory;
            if (assetsDirectory == null || string.IsNullOrWhiteSpace(assetsDirectory.RootPath))
            {
                return null;
            }

            var tempDirectory = Path.Combine(assetsDirectory.RootPath, "../tmp");
            Directory.CreateDirectory(tempDirectory);

            var extension = Path.GetExtension(sourcePath);
            var extensionToKeep = string.IsNullOrWhiteSpace(extension) ? ".mp4" : extension.ToLowerInvariant();
            var tempFileName = $"{Guid.NewGuid():N}{extensionToKeep}";
            var destinationPath = Path.Combine(tempDirectory, tempFileName);

            const int bufferSize = 81920;
            using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
            using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }

            return $"../tmp/{tempFileName}";
        }

        private class VideoPlayerController : Behavior
        {
            private const float MaxPlaneWidth = 2.0f;
            private const float MaxPlaneHeight = 1.2f;

            [BindComponent]
            private VideoPlayer videoPlayer;

            [BindComponent]
            private PlaneMesh planeMesh;

            private readonly StandardMaterial material;
            private bool alreadyLoaded = false;

            public VideoPlayerController(StandardMaterial material)
            {
                this.material = material;
            }

            protected override void Update(TimeSpan gameTime)
            {
                if (this.videoPlayer.VideoTexture != null && !alreadyLoaded)
                {
                    var videoWidth = (float)this.videoPlayer.VideoTexture.Description.Width;
                    var videoHeight = (float)this.videoPlayer.VideoTexture.Description.Height;

                    // Keep the video ratio but fit it inside a reasonable size in meters.
                    var scale = MathF.Min(MaxPlaneWidth / videoWidth, MaxPlaneHeight / videoHeight);
                    this.planeMesh.Width = videoWidth * scale;
                    this.planeMesh.Height = videoHeight * scale;

                    this.material.BaseColorTexture = this.videoPlayer.VideoTexture;
                    alreadyLoaded = true;
                }
            }
        }
    }
}
