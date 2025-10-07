using Evergine.Components.Animation;
using Evergine.Components.Graphics3D;
using Evergine.Framework.Graphics;
using Evergine.Runtimes.CAD;
using Evergine.Runtimes.CAD.CustomLineBatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvergineRuntimeLab.Features.RuntimeAssets.Loaders
{
    public class CADRuntimeLoader : BaseRuntimeLoader
    {
        internal override string[] SupportedExtensions { get; } = new[] { ".dxf", ".dwg" };

        public CADRuntimeLoader(RuntimeAssetManager runtimeAssetManager)
            : base(runtimeAssetManager)
        {
        }

        public override async Task<RuntimeLoadResult> LoadAsset(string path)
        {
            RuntimeLoadResult result = new RuntimeLoadResult();

            using var fileStream = File.OpenRead(path);
            if (fileStream != null)
            {
                var modelEntity = await CADRuntime.Instance.Read(fileStream);

                if (modelEntity != null)
                {
                    result.IsValid = true;
                    result.Entity = modelEntity;

                    var drawable = modelEntity.FindComponentInChildren<DrawableLineBatch>(skipOwner: false);
                    if (drawable != null)
                    {
                        result.BoundingBox = drawable.BoundingBox; 
                    }
                }
            }

            return result;
        }
    }
}
