using Evergine.Components.Animation;
using Evergine.Runtimes.USD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvergineRuntimeLab.Features.RuntimeAssets.Loaders
{
    public class USDRuntimeLoader : BaseRuntimeLoader
    {
        internal override string[] SupportedExtensions { get; } = new[] { ".usdz" };

        public USDRuntimeLoader(RuntimeAssetManager runtimeAssetManager) 
            : base(runtimeAssetManager)
        {
        }

        public override async Task<RuntimeLoadResult> LoadAsset(string path)
        {
            RuntimeLoadResult result = new RuntimeLoadResult();

            using var fileStream = File.OpenRead(path);
            if (fileStream != null)
            {
                var model = await USDRuntime.Instance.Read(fileStream);

                if (model != null)
                {
                    var modelEntity = model.InstantiateModelHierarchy(this.runtimeAssetManager.AssetsService);

                    if (modelEntity != null)
                    {
                        result.IsValid = true;
                        result.Entity = modelEntity;
                        result.BoundingBox = model.BoundingBox;
                    }
                }
            }

            return result;
        }
    }
}
