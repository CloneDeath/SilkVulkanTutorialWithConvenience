using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

var app = new HelloTriangleApplication_23();
app.Run();

public unsafe class HelloTriangleApplication_23 : HelloTriangleApplication_22
{
    protected Image textureImage;
    protected DeviceMemory textureImageMemory;

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapchain();
        CreateImageViews();
        CreateRenderPass();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateCommandPool();
        CreateTextureImage();
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    protected override void CleanUp()
    {
        CleanUpSwapchain();

        vk!.DestroyImage(device, textureImage, null);
        vk!.FreeMemory(device, textureImageMemory, null);

        vk!.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

        vk!.DestroyBuffer(device, indexBuffer, null);
        vk!.FreeMemory(device, indexBufferMemory, null);

        vertexBuffer!.Dispose();
        vertexBufferMemory!.Dispose();

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            renderFinishedSemaphores![i].Dispose();
            imageAvailableSemaphores![i].Dispose();
            inFlightFences![i].Dispose();
        }

        commandPool?.Dispose();

        device!.Dispose();

        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivalent to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugMessenger!.Dispose();
        }

        surface!.Dispose();
        instance!.Dispose();
        vk!.Dispose();

        window?.Dispose();
    }

    protected virtual void CreateTextureImage()
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>("Textures/texture.jpg");

        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,ref stagingBuffer, ref stagingBufferMemory );

        void* data;
        vk!.MapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
        vk!.UnmapMemory(device, stagingBufferMemory);

        CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, ref textureImage, ref textureImageMemory);

        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        vk!.DestroyBuffer(device, stagingBuffer, null);
        vk!.FreeMemory(device, stagingBufferMemory, null);
    }

    protected void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory)
    {
        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent =
            {
                Width = width,
                Height = height,
                Depth = 1,
            },
            MipLevels = 1,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Image* imagePtr = &image)
        {
            if(vk!.CreateImage(device,imageInfo,null, imagePtr) != Result.Success)
            {
                throw new Exception("failed to create image!");
            }
        }

        MemoryRequirements memRequirements;
        vk!.GetImageMemoryRequirements(device, image, out memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits,properties),
        };

        fixed(DeviceMemory* imageMemoryPtr = &imageMemory) 
        {
            if (vk!.AllocateMemory(device, allocInfo, null, imageMemoryPtr) != Result.Success)
            {
                throw new Exception("failed to allocate image memory!");
            }
        }

        vk!.BindImageMemory(device, image, imageMemory, 0);
    }

    // ReSharper disable once UnusedParameter.Local
    protected void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored, 
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
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

        vk!.CmdPipelineBarrier(commandBuffer,sourceStage,destinationStage,0,0,null,0,null,1,barrier);

        EndSingleTimeCommands(commandBuffer);

    }

    protected void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,                
            },
            ImageOffset = new Offset3D(0,0,0),
            ImageExtent = new Extent3D(width, height, 1),
            
        };

        vk!.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, region);

        EndSingleTimeCommands(commandBuffer);
    }

    protected CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1,
        };

        CommandBuffer commandBuffer;
        vk!.AllocateCommandBuffers(device, allocateInfo, out commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        vk!.BeginCommandBuffer(commandBuffer, beginInfo);

        return commandBuffer;
    }

    protected void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        vk!.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        vk!.QueueSubmit(graphicsQueue, 1, submitInfo, default);
        vk!.QueueWaitIdle(graphicsQueue);

        vk!.FreeCommandBuffers(device, commandPool, 1, commandBuffer);
    }

    protected override void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            Size = size,                
        };

        vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

        EndSingleTimeCommands(commandBuffer);
    }
}