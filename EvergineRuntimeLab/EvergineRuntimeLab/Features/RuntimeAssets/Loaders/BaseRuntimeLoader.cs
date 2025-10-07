using Evergine.Components.Animation;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvergineRuntimeLab.Features.RuntimeAssets.Loaders
{
    public class RuntimeLoadResult
    {
        public bool IsValid;
        public Entity Entity;
        public BoundingBox? BoundingBox;
    }

    public abstract class BaseRuntimeLoader
    {
        internal abstract string[] SupportedExtensions { get; }

        internal RuntimeAssetManager runtimeAssetManager;
        public BaseRuntimeLoader(RuntimeAssetManager runtimeAssetManager)
        {
            this.runtimeAssetManager = runtimeAssetManager;
        }

        public virtual bool CanProcess(string filePath)
        {
            var fileExtension = System.IO.Path.GetExtension(filePath).ToLower();
            return SupportedExtensions.Contains(fileExtension);
        }

        public abstract Task<RuntimeLoadResult> LoadAsset(string path);
    }
}
