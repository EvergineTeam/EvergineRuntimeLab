using Evergine.Framework;
using Evergine.Mathematics;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace EvergineRuntimeLab.Features.RuntimeAssets.Loaders
{
    public enum RuntimeLoaderType
    {
        [Description("3D Models")]
        Model,
        [Description("CAD files")]
        CAD,
        [Description("Images")]
        Image,
        [Description("Videos")]
        Video,
        Unknown
    }

    public class RuntimeLoadResult
    {
        public bool IsValid;
        public Entity Entity;
        public BoundingBox? BoundingBox;
    }

    public abstract class BaseRuntimeLoader
    {
        public abstract RuntimeLoaderType LoaderType { get; }

        public abstract string[] SupportedExtensions { get; }

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
