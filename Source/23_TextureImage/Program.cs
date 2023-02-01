﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using Buffer = Silk.NET.Vulkan.Buffer;

var app = new HelloTriangleApplication_23();
app.Run();



public struct UniformBufferObject
{
    public Matrix4X4<float> model;
    public Matrix4X4<float> view;
    public Matrix4X4<float> proj;
}

public unsafe class HelloTriangleApplication_23 : HelloTriangleApplication_22
{
    protected DescriptorSetLayout descriptorSetLayout;

    protected Image textureImage;
    protected DeviceMemory textureImageMemory;






    protected Buffer[]? uniformBuffers;
    protected DeviceMemory[]? uniformBuffersMemory;

    protected DescriptorPool descriptorPool;
    protected DescriptorSet[]? descriptorSets;

    

    

    

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapChain();
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

    

    protected void CleanUpSwapChain()
    {
        foreach (var framebuffer in swapChainFramebuffers!)
        {
            vk!.DestroyFramebuffer(device, framebuffer, null);
        }

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            vk!.FreeCommandBuffers(device, commandPool, (uint)commandBuffers!.Length, commandBuffersPtr);
        }

        vk!.DestroyPipeline(device, graphicsPipeline, null);
        vk!.DestroyPipelineLayout(device, pipelineLayout, null);
        vk!.DestroyRenderPass(device, renderPass, null);

        foreach (var imageView in swapChainImageViews!)
        {
            vk!.DestroyImageView(device, imageView, null);
        }

        khrSwapChain!.DestroySwapchain(device, swapChain, null);

        for (int i = 0; i < swapChainImages!.Length; i++)
        {
            vk!.DestroyBuffer(device, uniformBuffers![i], null);
            vk!.FreeMemory(device, uniformBuffersMemory![i], null);
        }

        vk!.DestroyDescriptorPool(device, descriptorPool, null);
    }

    protected override void CleanUp()
    {
        CleanUpSwapChain();

        vk!.DestroyImage(device, textureImage, null);
        vk!.FreeMemory(device, textureImageMemory, null);

        vk!.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

        vk!.DestroyBuffer(device, indexBuffer, null);
        vk!.FreeMemory(device, indexBufferMemory, null);

        vk!.DestroyBuffer(device, vertexBuffer, null);
        vk!.FreeMemory(device,vertexBufferMemory, null);

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            vk!.DestroySemaphore(device, renderFinishedSemaphores![i], null);
            vk!.DestroySemaphore(device, imageAvailableSemaphores![i], null);
            vk!.DestroyFence(device, inFlightFences![i], null);
        }

        vk!.DestroyCommandPool(device, commandPool, null);

        vk!.DestroyDevice(device, null);

        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivalent to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
        }

        khrSurface!.DestroySurface(instance, surface, null);
        vk!.DestroyInstance(instance, null);
        vk!.Dispose();

        window?.Dispose();
    }

    protected void RecreateSwapChain()
    {
        Vector2D<int> framebufferSize = window!.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = window.FramebufferSize;
            window.DoEvents();
        }

        vk!.DeviceWaitIdle(device);

        CleanUpSwapChain();

        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();

        imagesInFlight = new Fence[swapChainImages!.Length];
    }

    

    

    

    

    

    protected void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBinding uboLayoutBinding = new()
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            PImmutableSamplers = null,
            StageFlags = ShaderStageFlags.VertexBit,
        };

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &uboLayoutBinding,
        };

        fixed(DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {
            if(vk!.CreateDescriptorSetLayout(device, layoutInfo, null, descriptorSetLayoutPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor set layout!");
            }
        }
    }

    protected void CreateGraphicsPipeline()
    {
        var vertShaderCode = File.ReadAllBytes("shaders/vert.spv");
        var fragShaderCode = File.ReadAllBytes("shaders/frag.spv");

        var vertShaderModule = CreateShaderModule(vertShaderCode);
        var fragShaderModule = CreateShaderModule(fragShaderCode);

        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        PipelineShaderStageCreateInfo fragShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc []
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        var bindingDescription = Vertex_17.GetBindingDescription();
        var attributeDescriptions = Vertex_17.GetAttributeDescriptions();

        fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
        fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {

            PipelineVertexInputStateCreateInfo vertexInputInfo = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                PVertexBindingDescriptions = &bindingDescription,
                PVertexAttributeDescriptions = attributeDescriptionsPtr,
            };

            PipelineInputAssemblyStateCreateInfo inputAssembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false,
            };

            Viewport viewport = new()
            {
                X = 0,
                Y = 0,
                Width = swapChainExtent.Width,
                Height = swapChainExtent.Height,
                MinDepth = 0,
                MaxDepth = 1,
            };

            Rect2D scissor = new()
            {
                Offset = { X = 0, Y = 0 },
                Extent = swapChainExtent,
            };

            PipelineViewportStateCreateInfo viewportState = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                PViewports = &viewport,
                ScissorCount = 1,
                PScissors = &scissor,
            };

            PipelineRasterizationStateCreateInfo rasterizer = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1,
                CullMode = CullModeFlags.BackBit,
                FrontFace = FrontFace.CounterClockwise,
                DepthBiasEnable = false,
            };

            PipelineMultisampleStateCreateInfo multisampling = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.Count1Bit,
            };

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                BlendEnable = false,
            };

            PipelineColorBlendStateCreateInfo colorBlending = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment,
            };

            colorBlending.BlendConstants[0] = 0;
            colorBlending.BlendConstants[1] = 0;
            colorBlending.BlendConstants[2] = 0;
            colorBlending.BlendConstants[3] = 0;

            PipelineLayoutCreateInfo pipelineLayoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 0,                
                SetLayoutCount = 1,
                PSetLayouts = descriptorSetLayoutPtr
            };

            if (vk!.CreatePipelineLayout(device, pipelineLayoutInfo, null, out pipelineLayout) != Result.Success)
            {
                throw new Exception("failed to create pipeline layout!");
            }

            GraphicsPipelineCreateInfo pipelineInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PColorBlendState = &colorBlending,
                Layout = pipelineLayout,
                RenderPass = renderPass,
                Subpass = 0,
                BasePipelineHandle = default
            };

            if (vk!.CreateGraphicsPipelines(device, default, 1, pipelineInfo, null, out graphicsPipeline) != Result.Success)
            {
                throw new Exception("failed to create graphics pipeline!");
            }
        }

        vk!.DestroyShaderModule(device, fragShaderModule, null);
        vk!.DestroyShaderModule(device, vertShaderModule, null);

        SilkMarshal.Free((nint)vertShaderStageInfo.PName);
        SilkMarshal.Free((nint)fragShaderStageInfo.PName);
    }

    

    

    protected void CreateTextureImage()
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



    

    

    protected void CreateUniformBuffers()
    {
        ulong bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

        uniformBuffers = new Buffer[swapChainImages!.Length];
        uniformBuffersMemory = new DeviceMemory[swapChainImages!.Length];

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            CreateBuffer(bufferSize, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref uniformBuffers[i], ref uniformBuffersMemory[i]);   
        }

    }

    protected void CreateDescriptorPool()
    {
        DescriptorPoolSize poolSize = new()
        {
            Type = DescriptorType.UniformBuffer,
            DescriptorCount = (uint)swapChainImages!.Length,
        };


        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
            MaxSets = (uint)swapChainImages!.Length,
        };

        fixed (DescriptorPool* descriptorPoolPtr = &descriptorPool)
        {
            if(vk!.CreateDescriptorPool(device, poolInfo, null, descriptorPoolPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }

        }
    }

    protected void CreateDescriptorSets()
    {
        var layouts = new DescriptorSetLayout[swapChainImages!.Length];
        Array.Fill(layouts, descriptorSetLayout); 

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = (uint)swapChainImages!.Length,
                PSetLayouts = layoutsPtr,
            };

            descriptorSets = new DescriptorSet[swapChainImages.Length];
            fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
            {
                if (vk!.AllocateDescriptorSets(device, allocateInfo, descriptorSetsPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate descriptor sets!");
                }
            }
        }
        

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = uniformBuffers![i],
                Offset = 0,
                Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),

            };

            WriteDescriptorSet descriptorWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = descriptorSets[i],
                DstBinding = 0,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                PBufferInfo = &bufferInfo,
            };

            vk!.UpdateDescriptorSets(device, 1, descriptorWrite, 0, null);
        }

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

    protected void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            Size = size,                
        };

        vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

        EndSingleTimeCommands(commandBuffer);
    }

    

    protected void CreateCommandBuffers()
    {
        commandBuffers = new CommandBuffer[swapChainFramebuffers!.Length];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)commandBuffers.Length,
        };

        fixed(CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            if (vk!.AllocateCommandBuffers(device, allocInfo, commandBuffersPtr) != Result.Success)
            {
                throw new Exception("failed to allocate command buffers!");
            }
        }
        

        for (int i = 0; i < commandBuffers.Length; i++)
        {
            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
            };

            if(vk!.BeginCommandBuffer(commandBuffers[i], beginInfo ) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType= StructureType.RenderPassBeginInfo,
                RenderPass = renderPass,
                Framebuffer = swapChainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 }, 
                    Extent = swapChainExtent,
                }
            };

            ClearValue clearColor = new()
            {
                Color = new (){ Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },                
            };

            renderPassInfo.ClearValueCount = 1;
            renderPassInfo.PClearValues = &clearColor;

            vk!.CmdBeginRenderPass(commandBuffers[i], &renderPassInfo, SubpassContents.Inline);

                vk!.CmdBindPipeline(commandBuffers[i], PipelineBindPoint.Graphics, graphicsPipeline);

                var vertexBuffers = new[] { vertexBuffer };
                var offsets = new ulong[] { 0 };

                fixed (ulong* offsetsPtr = offsets)
                fixed (Buffer* vertexBuffersPtr = vertexBuffers)
                {
                    vk!.CmdBindVertexBuffers(commandBuffers[i], 0, 1, vertexBuffersPtr, offsetsPtr);
                }

                vk!.CmdBindIndexBuffer(commandBuffers[i], indexBuffer, 0, IndexType.Uint16);

                vk!.CmdBindDescriptorSets(commandBuffers[i], PipelineBindPoint.Graphics, pipelineLayout, 0, 1, descriptorSets![i], 0, null);

                vk!.CmdDrawIndexed(commandBuffers[i], (uint)indices.Length, 1, 0, 0, 0);

            vk!.CmdEndRenderPass(commandBuffers[i]);

            if(vk!.EndCommandBuffer(commandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }

        }
    }

    

    protected void UpdateUniformBuffer(uint currentImage)
    {
        //Silk Window has timing information so we are skipping the time code.
        var time = (float)window!.Time;

        UniformBufferObject ubo = new()
        {
            model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle(new Vector3D<float>(0,0,1), time * Scalar.DegreesToRadians(90.0f)),
            view = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1)),
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), swapChainExtent.Width * 1f / swapChainExtent.Height, 0.1f, 10.0f),
        };
        ubo.proj.M22 *= -1;


        void* data;
        vk!.MapMemory(device, uniformBuffersMemory![currentImage], 0, (ulong)Unsafe.SizeOf<UniformBufferObject>(), 0, &data);
            new Span<UniformBufferObject>(data, 1)[0] = ubo;
        vk!.UnmapMemory(device, uniformBuffersMemory![currentImage]);

    }

    protected void DrawFrame(double delta)
    {
        vk!.WaitForFences(device, 1, inFlightFences![currentFrame], true, ulong.MaxValue);

        uint imageIndex = 0;
        var result = khrSwapChain!.AcquireNextImage(device, swapChain, ulong.MaxValue, imageAvailableSemaphores![currentFrame], default, ref imageIndex);

        if(result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return;
        }
        else if(result != Result.Success && result != Result.SuboptimalKhr)
        {
            throw new Exception("failed to acquire swap chain image!");
        }

        UpdateUniformBuffer(imageIndex);

        if(imagesInFlight![imageIndex].Handle != default)
        {
            vk!.WaitForFences(device, 1, imagesInFlight[imageIndex], true, ulong.MaxValue);
        }
        imagesInFlight[imageIndex] = inFlightFences[currentFrame];

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
        };

        var waitSemaphores = stackalloc [] {imageAvailableSemaphores[currentFrame]};
        var waitStages = stackalloc [] { PipelineStageFlags.ColorAttachmentOutputBit };

        var buffer = commandBuffers![imageIndex];

        submitInfo = submitInfo with 
        { 
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            
            CommandBufferCount = 1,
            PCommandBuffers = &buffer
        };

        var signalSemaphores = stackalloc[] { renderFinishedSemaphores![currentFrame] };
        submitInfo = submitInfo with
        {
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        vk!.ResetFences(device, 1,inFlightFences[currentFrame]);

        if(vk!.QueueSubmit(graphicsQueue, 1, submitInfo, inFlightFences[currentFrame]) != Result.Success)
        {
            throw new Exception("failed to submit draw command buffer!");
        }

        var swapChains = stackalloc[] { swapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,

            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,

            SwapchainCount = 1,
            PSwapchains = swapChains,

            PImageIndices = &imageIndex
        };

        result = khrSwapChain.QueuePresent(presentQueue, presentInfo);

        if(result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || frameBufferResized)
        {
            frameBufferResized = false;
            RecreateSwapChain();
        }
        else if(result != Result.Success)
        {
            throw new Exception("failed to present swap chain image!");
        }

        currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;

    }

    

    

    

    

    

    

    

    

    

    

    
}