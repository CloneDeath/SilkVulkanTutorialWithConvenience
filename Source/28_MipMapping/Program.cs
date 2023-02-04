using Silk.NET.Vulkan;
using SilkNetConvenience.Barriers;
using SilkNetConvenience.Images;
using SilkNetConvenience.Memory;

var app = new HelloTriangleApplication_28();
app.Run();

public class HelloTriangleApplication_28 : HelloTriangleApplication_27
{
    protected uint mipLevels;
    
    protected override void CreateImageViews()
    {
        swapchainImageViews = new VulkanImageView[swapchainImages!.Length];

        for (int i = 0; i < swapchainImages.Length; i++)
        {

            swapchainImageViews[i] = CreateImageView(swapchainImages[i], swapchainImageFormat, ImageAspectFlags.ColorBit, 1);
        }
    }

    protected override void CreateDepthResources()
    {
        Format depthFormat = FindDepthFormat();

        (depthImage, depthImageMemory) = CreateImage(swapchainExtent.Width, swapchainExtent.Height, 1, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit);
        depthImageView = CreateImageView(depthImage, depthFormat, ImageAspectFlags.DepthBit, 1);
    }

    protected override void CreateTextureImage()
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(TEXTURE_PATH);

        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
        mipLevels = (uint)(Math.Floor(Math.Log2(Math.Max(img.Width, img.Height))) + 1);

        var (stagingBuffer, stagingBufferMemory) = CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var data = stagingBufferMemory.MapMemory();
        img.CopyPixelDataTo(data);
        stagingBufferMemory.UnmapMemory();

        (textureImage, textureImageMemory) = CreateImage((uint)img.Width, (uint)img.Height, mipLevels, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit);

        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, mipLevels);
        CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
        //Transitioned to VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL while generating mipmaps

        stagingBuffer.Dispose();
        stagingBufferMemory.Dispose();

        GenerateMipMaps(textureImage, Format.R8G8B8A8Srgb, (uint)img.Width, (uint)img.Height, mipLevels);
    }

    protected void GenerateMipMaps(Image image, Format imageFormat, uint width, uint height, uint withMipLevels) {
        var formatProperties = physicalDevice!.GetFormatProperties(imageFormat);
        
        if((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.SampledImageFilterLinearBit) == 0)
        {
            throw new Exception("texture image format does not support linear blitting!");
        }

        var commandBuffer = BeginSingleTimeCommands();

        ImageMemoryBarrierInformation barrier = new()
        {
            Image = image,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseArrayLayer = 0,
                LayerCount = 1,
                LevelCount = 1,
            }
        };

        var mipWidth = width;
        var mipHeight = height;

        for (uint i = 1; i < withMipLevels; i++)
        {
            barrier.SubresourceRange.BaseMipLevel = i - 1;
            barrier.OldLayout = ImageLayout.TransferDstOptimal;
            barrier.NewLayout = ImageLayout.TransferSrcOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.TransferReadBit;

            commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, DependencyFlags.None, barrier);

            ImageBlit blit = new()
            {
                SrcOffsets =
                {
                    Element0 = new Offset3D(0,0,0),
                    Element1 = new Offset3D((int)mipWidth, (int)mipHeight, 1),
                },
                SrcSubresource =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = i - 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,                    
                },
                DstOffsets =
                {
                    Element0 = new Offset3D(0,0,0),
                    Element1 = new Offset3D((int)(mipWidth > 1 ? mipWidth / 2 : 1), (int)(mipHeight > 1 ? mipHeight / 2 : 1),1),
                },
                DstSubresource =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = i,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },

            };

            commandBuffer.BlitImage(image, ImageLayout.TransferSrcOptimal,
                image, ImageLayout.TransferDstOptimal,
                new[] {blit}, Filter.Linear);

            barrier.OldLayout = ImageLayout.TransferSrcOptimal;
            barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferReadBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, DependencyFlags.None, barrier);

            if (mipWidth > 1) mipWidth /= 2;
            if (mipHeight > 1) mipHeight /= 2;
        }

        barrier.SubresourceRange.BaseMipLevel = withMipLevels - 1;
        barrier.OldLayout = ImageLayout.TransferDstOptimal;
        barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
        barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier.DstAccessMask = AccessFlags.ShaderReadBit;

        commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, DependencyFlags.None, barrier);

        EndSingleTimeCommands(commandBuffer);
    }

    protected override void CreateTextureImageView()
    {
        textureImageView = CreateImageView(textureImage!, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit, mipLevels);
    }

    protected override void CreateTextureSampler()
    {
        PhysicalDeviceProperties properties = physicalDevice!.GetProperties();

        SamplerCreateInformation samplerInfo = new()
        {
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = true,
            MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
            MinLod = 0,
            MaxLod = mipLevels,
            MipLodBias = 0,
        };

        textureSampler = device!.CreateSampler(samplerInfo);
    }

    protected VulkanImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags, uint withMipLevels)
    {
        ImageViewCreateInformation createInfo = new()
        {
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            //Components =
            //    {
            //        R = ComponentSwizzle.Identity,
            //        G = ComponentSwizzle.Identity,
            //        B = ComponentSwizzle.Identity,
            //        A = ComponentSwizzle.Identity,
            //    },
            SubresourceRange =
                {
                    AspectMask = aspectFlags,
                    BaseMipLevel = 0,
                    LevelCount = withMipLevels,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }

        };

        return device!.CreateImageView(createInfo);
    }

    protected (VulkanImage, VulkanDeviceMemory) CreateImage(uint width, uint height, uint withMipLevels, Format format, 
                                                            ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties)
    {
        ImageCreateInformation imageInfo = new()
        {
            ImageType = ImageType.Type2D,
            Extent =
            {
                Width = width,
                Height = height,
                Depth = 1,
            },
            MipLevels = withMipLevels,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive,
        };

        var image = device!.CreateImage(imageInfo);
        var imageMemory = device!.AllocateMemoryFor(image, properties);

        image.BindMemory(imageMemory);
        return (image, imageMemory);
    }

    // ReSharper disable once UnusedParameter.Local
    protected void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout, uint withMipLevels)
    {
        var commandBuffer = BeginSingleTimeCommands();

        ImageMemoryBarrierInformation barrier = new()
        {
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = withMipLevels,
                BaseArrayLayer = 0,
                LayerCount = 1,
            }
        };

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        if(oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;

            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else
        {
            throw new Exception("unsupported layout transition!");
        }

        commandBuffer.PipelineBarrier(sourceStage, destinationStage, DependencyFlags.None, barrier);

        EndSingleTimeCommands(commandBuffer);

    }
}