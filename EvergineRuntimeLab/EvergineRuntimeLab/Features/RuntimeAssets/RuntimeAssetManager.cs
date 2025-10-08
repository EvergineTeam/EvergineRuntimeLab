using Evergine.Common.Attributes;
using Evergine.Common.Attributes.Converters;
using Evergine.Common.Graphics;
using Evergine.Components.Animation;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using EvergineRuntimeLab.Features.Camera;
using EvergineRuntimeLab.Features.RuntimeAssets.Loaders;
using EvergineRuntimeLab.Features.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvergineRuntimeLab.Features.RuntimeAssets
{
    public class RuntimeAssetManager : UpdatableSceneManager
    {
        [BindService]
        internal AssetsService AssetsService;

        [BindSceneManager]
        private RenderManager renderManager;

        private List<BaseRuntimeLoader> runtimeLoaders = new List<BaseRuntimeLoader>();
        private RuntimeLoadResult currentLoad;

        private OrbitCameraBehavior orbitCameraBehavior;

        private DirectionalLight light;

        private UIComponent uIComponent;

        public float CameraResetZoom = 2f;

        [RenderPropertyAsFInput(typeof(FloatRadianToDegreeConverter), MinLimit = -90, MaxLimit = 90, AsSlider = true, DesiredChange = 1, DesiredLargeChange = 5)]
        public float CameraResetLambda = MathHelper.ToRadians(25);

        [RenderPropertyAsFInput(typeof(FloatRadianToDegreeConverter), MinLimit = -180, MaxLimit = 180, AsSlider = true, DesiredChange = 1, DesiredLargeChange = 5)]
        public float cameraResetTheta = MathHelper.ToRadians(-25);

        public BoundingBox BBox { get; private set; }

        protected override bool OnAttached()
        {
            // Register runtimes
            this.runtimeLoaders.Add(new GLBRuntimeLoader(this));
            this.runtimeLoaders.Add(new STLRuntimeLoader(this));
            this.runtimeLoaders.Add(new OBJRuntimeLoader(this));
            this.runtimeLoaders.Add(new USDRuntimeLoader(this));
            this.runtimeLoaders.Add(new IMGRuntimeLoader(this));
            this.runtimeLoaders.Add(new IFCRuntimeLoader(this));
            //this.runtimeLoaders.Add(new CADRuntimeLoader(this));

            this.orbitCameraBehavior = this.Managers.EntityManager.FindFirstComponentOfType<OrbitCameraBehavior>();
            this.light = this.Managers.EntityManager.FindFirstComponentOfType<DirectionalLight>(isExactType: false);
            this.uIComponent = this.Managers.EntityManager.FindFirstComponentOfType<UIComponent>();

            var extensionsByType = this.runtimeLoaders
                .GroupBy(l => l.LoaderType)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(l => l.SupportedExtensions)
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .OrderBy(e => e)
                          .ToArray());

            this.uIComponent.SetSuportedFiles(extensionsByType);


            // Register to application events
            MyApplication.OnNewRuntimeAsset += OnNewRuntimeAsset;
            MyApplication.IsRuntimeAssetValid = IsRuntimeAssetValid;

            return base.OnAttached();
        }

        protected override void Start()
        {
            base.Start();

            this.uIComponent.IsEnabled = true;

            this.CenterCamera();
        }

        public override void Update(TimeSpan gameTime)
        {
            var keyboard = this.Managers.RenderManager.ActiveCamera3D?.Display?.KeyboardDispatcher;
            if (keyboard?.ReadKeyState(Evergine.Common.Input.Keyboard.Keys.F1) == Evergine.Common.Input.ButtonState.Pressing)
            {
                this.renderManager.DebugLines = !this.renderManager.DebugLines;
            }

            if (renderManager.DebugLines && this.currentLoad?.IsValid == true && this.currentLoad.BoundingBox.HasValue)
            {
                var lb = renderManager.LineBatch3D;
                lb.DrawBoundingBox(this.BBox, Color.Red);
                lb.DrawPoint(this.BBox.Center, 0.5f, Color.Blue);
                lb.DrawPoint(Vector3.Zero, 1, Color.Black);
            }
        }

        protected override void OnDetached()
        {
            base.OnDetached();

            this.runtimeLoaders.Clear();

            // Unregister from application events
            MyApplication.OnNewRuntimeAsset -= OnNewRuntimeAsset;
            MyApplication.IsRuntimeAssetValid = null;

        }

        private bool IsRuntimeAssetValid(string filePath)
        {
            return this.runtimeLoaders.Any(loader => loader.CanProcess(filePath));
        }

        private async void OnNewRuntimeAsset(object sender, string path)
        {    
            var loader = runtimeLoaders.FirstOrDefault(l => l.CanProcess(path));
            if (loader != null)
            {
                try
                {
                    var result = await loader.LoadAsset(path);
                 
                    if (result.IsValid && result.Entity != null)
                    {
                        Debug.WriteLine($"[RuntimeAssetManager] Loaded asset from {path}");
                        this.RuntimeAssetLoaded(result);
                    }
                    else
                    {
                        Debug.WriteLine($"[RuntimeAssetManager] Failed to load asset from {path}");
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"[RuntimeAssetManager] Exception loading asset from {path}: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"[RuntimeAssetManager] No loader available for asset from {path}");
            }
        }

        private void RuntimeAssetLoaded(RuntimeLoadResult result)
        {
            if (this.currentLoad?.IsValid == true)
            {
                this.Managers.EntityManager.Remove(this.currentLoad.Entity);
                this.currentLoad= null;
            }

            var animation = result.Entity.FindComponent<Animation3D>();
            if (animation != null)
            {
                animation.PlayAutomatically = true;
                animation.Loop = true;
            }

            this.currentLoad = result;
            this.Managers.EntityManager.Add(this.currentLoad.Entity);

            this.CenterCamera();
            this.uIComponent.IsEnabled = false;
        }

        private void CenterCamera()
        {
            this.orbitCameraBehavior.ResetCameraToInit();

            if (this.currentLoad?.BoundingBox.HasValue == true)
            {
                this.BBox = BoundingBox.Transform(this.currentLoad.BoundingBox.Value, this.currentLoad.Entity.FindComponent<Transform3D>().WorldTransform);

                var aspectRatio = Math.Max(1 / this.orbitCameraBehavior.Camera.AspectRatio, 1.5f);
                var zoom = this.BBox.HalfExtent.Length() * this.CameraResetZoom * aspectRatio;

                this.orbitCameraBehavior.ResetPosition(this.BBox.Center);
                this.orbitCameraBehavior.ResetZoom(zoom);
                this.light.ShadowDistance = zoom * 2;
            }

            this.orbitCameraBehavior.ResetOrbit(this.cameraResetTheta, this.CameraResetLambda);
        }
    }
}
