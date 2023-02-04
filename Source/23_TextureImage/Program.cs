using Silk.NET.Vulkan;
using SilkNetConvenience.Barriers;
using SilkNetConvenience.CommandBuffers;
using SilkNetConvenience.Images;
using SilkNetConvenience.Memory;
using Buffer = Silk.NET.Vulkan.Buffer;

var app = new HelloTriangleApplication_23();
app.Run();

public class HelloTriangleApplication_23 : HelloTriangleApplication_22
{
    protected VulkanImage? textureImage;
    protected VulkanDeviceMemory? textureImageMemory;

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

        textureImage!.Dispose();
        textureImageMemory!.Dispose();

        descriptorSetLayout!.Dispose();

        indexBuffer?.Dispose();
        indexBufferMemory?.Dispose();

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

        var (stagingBuffer, stagingBufferMemory) = CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        using (stagingBuffer)
        using (stagingBufferMemory) {
            var data = stagingBufferMemory.MapMemory();
            img.CopyPixelDataTo(data);
            stagingBufferMemory.UnmapMemory();

            (textureImage, textureImageMemory) = CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal,
                ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit);

            TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined,
                ImageLayout.TransferDstOptimal);
            CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
            TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal,
                ImageLayout.ShaderReadOnlyOptimal);
        }
    }

    protected (VulkanImage, VulkanDeviceMemory) CreateImage(uint width, uint height, Format format, ImageTiling tiling, 
                                                            ImageUsageFlags usage, MemoryPropertyFlags properties)
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
            MipLevels = 1,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive,
        };

        var image = device!.CreateImage(imageInfo);
        var imageMemory = device.AllocateMemoryFor(image, properties);

        image.BindMemory(imageMemory);

        return (image, imageMemory);
    }

    // ReSharper disable once UnusedParameter.Local
    protected void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    {
        VulkanCommandBuffer commandBuffer = BeginSingleTimeCommands();

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

        commandBuffer.PipelineBarrier(sourceStage,destinationStage, DependencyFlags.None, barrier);

        EndSingleTimeCommands(commandBuffer);

    }

    protected void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    {
        var commandBuffer = BeginSingleTimeCommands();

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
            ImageExtent = new Extent3D(width, height, 1)
        };
        commandBuffer.CopyBufferToImage(buffer, image, ImageLayout.TransferDstOptimal, region);

        EndSingleTimeCommands(commandBuffer);
    }

    protected VulkanCommandBuffer BeginSingleTimeCommands()
    {
        var commandBuffer = commandPool!.AllocateCommandBuffer();
        commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmitBit);
        return commandBuffer;
    }

    protected void EndSingleTimeCommands(VulkanCommandBuffer commandBuffer)
    {
        commandBuffer.End();
        graphicsQueue!.Submit(commandBuffer);
        graphicsQueue!.WaitIdle();
        commandBuffer.Dispose();
    }

    protected override void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        var commandBuffer = BeginSingleTimeCommands();
        commandBuffer.CopyBuffer(srcBuffer, dstBuffer, size);
        EndSingleTimeCommands(commandBuffer);
    }
}