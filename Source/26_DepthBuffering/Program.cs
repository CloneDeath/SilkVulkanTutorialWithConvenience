using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

var app = new HelloTriangleApplication_26();
app.Run();

public struct Vertex_26
{
    public Vector3D<float> pos;
    public Vector3D<float> color;
    public Vector2D<float> textCoord;

    public static VertexInputBindingDescription GetBindingDescription()
    {
        VertexInputBindingDescription bindingDescription = new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vertex_26>(),
            InputRate = VertexInputRate.Vertex,
        };

        return bindingDescription;
    }

    public static VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        var attributeDescriptions = new[]
        {
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex_26>(nameof(pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex_26>(nameof(color)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex_26>(nameof(textCoord)),
            }
        };

        return attributeDescriptions;
    }
}

public unsafe class HelloTriangleApplication_26 : HelloTriangleApplication_25
{
    protected Image depthImage;
    protected DeviceMemory depthImageMemory;
    protected ImageView depthImageView;

    protected virtual Vertex_26[] vertices_26 { get; } = new Vertex_26[]
    {
        new Vertex_26 { pos = new Vector3D<float>(-0.5f,-0.5f, 0.0f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f), textCoord = new Vector2D<float>(1.0f, 0.0f) },
        new Vertex_26 { pos = new Vector3D<float>(0.5f,-0.5f, 0.0f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f), textCoord = new Vector2D<float>(0.0f, 0.0f) },
        new Vertex_26 { pos = new Vector3D<float>(0.5f,0.5f, 0.0f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f), textCoord = new Vector2D<float>(0.0f, 1.0f) },
        new Vertex_26 { pos = new Vector3D<float>(-0.5f,0.5f, 0.0f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f), textCoord = new Vector2D<float>(1.0f, 1.0f) },

        new Vertex_26 { pos = new Vector3D<float>(-0.5f,-0.5f, -0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f), textCoord = new Vector2D<float>(1.0f, 0.0f) },
        new Vertex_26 { pos = new Vector3D<float>(0.5f,-0.5f, -0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f), textCoord = new Vector2D<float>(0.0f, 0.0f) },
        new Vertex_26 { pos = new Vector3D<float>(0.5f,0.5f, -0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f), textCoord = new Vector2D<float>(0.0f, 1.0f) },
        new Vertex_26 { pos = new Vector3D<float>(-0.5f,0.5f, -0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f), textCoord = new Vector2D<float>(1.0f, 1.0f) },
    };

    protected override ushort[] indices { get; } = new ushort[]
    {
        0, 1, 2, 2, 3, 0,
        4, 5, 6, 6, 7, 4
    };

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
        CreateCommandPool();
        CreateDepthResources();
        CreateFramebuffers();
        CreateTextureImage();
        CreateTextureImageView();
        CreateTextureSampler();
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    protected override void CleanUpSwapchain()
    {
        vk!.DestroyImageView(device, depthImageView, null);
        vk!.DestroyImage(device, depthImage, null);
        vk!.FreeMemory(device, depthImageMemory, null);

        foreach (var framebuffer in swapchainFramebuffers!)
        {
            framebuffer.Dispose();
        }

        foreach (var commandBuffer in commandBuffers!) {
            commandBuffer.Dispose();
        }

        graphicsPipeline!.Dispose();
        pipelineLayout!.Dispose();
        renderPass!.Dispose();

        foreach (var imageView in swapchainImageViews!)
        {
            imageView.Dispose();
        }

        swapchain!.Dispose();

        for (int i = 0; i < swapchainImages!.Length; i++)
        {
            uniformBuffers![i].Dispose();
            uniformBuffersMemory![i].Dispose();
        }

        descriptorPool!.Dispose();
    }

    protected override void RecreateSwapchain()
    {
        Vector2D<int> framebufferSize = window!.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = window.FramebufferSize;
            window.DoEvents();
        }

        device!.WaitIdle();

        CleanUpSwapchain();

        CreateSwapchain();
        CreateImageViews();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateDepthResources();
        CreateFramebuffers();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();

        imagesInFlight = new VulkanFence[swapchainImages!.Length];
    }
    
    protected override void CreateImageViews()
    {
        swapchainImageViews = new ImageView[swapchainImages!.Length];

        for (int i = 0; i < swapchainImages.Length; i++)
        {

            swapchainImageViews[i] = CreateImageView(swapchainImages[i], swapchainImageFormat, ImageAspectFlags.ColorBit);
        }
    }

    protected override void CreateRenderPass()
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = swapchainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr,
        };

        AttachmentDescription depthAttachment = new()
        {
            Format = FindDepthFormat(),
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout= ImageLayout.DepthStencilAttachmentOptimal,
        };

        AttachmentReference colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };

        AttachmentReference depthAttachmentRef = new()
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal,
        };

        SubpassDescription subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef,
        };

        SubpassDependency dependency = new()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
        };

        var attachments = new[] { colorAttachment, depthAttachment };

        fixed (AttachmentDescription* attachmentsPtr = attachments)
        {
            RenderPassCreateInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = attachmentsPtr,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency,
            };

            if (vk!.CreateRenderPass(device, renderPassInfo, null, out renderPass) != Result.Success)
            {
                throw new Exception("failed to create render pass!");
            } 
        }
    }

    protected override void CreateGraphicsPipeline()
    {
        var vertShaderCode = File.ReadAllBytes("shaders/vert.spv");
        var fragShaderCode = File.ReadAllBytes("shaders/frag.spv");

        using var vertShaderModule = CreateShaderModule(vertShaderCode);
        using var fragShaderModule = CreateShaderModule(fragShaderCode);

        PipelineShaderStageCreateInformation vertShaderStageInfo = new()
        {
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            Name = "main"
        };

        PipelineShaderStageCreateInformation fragShaderStageInfo = new()
        {
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            Name = "main"
        };

        var shaderStages = new []
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        var bindingDescription = Vertex_26.GetBindingDescription();
        var attributeDescriptions = Vertex_26.GetAttributeDescriptions();

        fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
        fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {

            PipelineVertexInputStateCreateInformation vertexInputInfo = new()
            {
                VertexBindingDescriptions = new[]{bindingDescription},
                VertexAttributeDescriptions = attributeDescriptions,
            };

            PipelineInputAssemblyStateCreateInformation inputAssembly = new()
            {
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false,
            };

            Viewport viewport = new()
            {
                X = 0,
                Y = 0,
                Width = swapchainExtent.Width,
                Height = swapchainExtent.Height,
                MinDepth = 0,
                MaxDepth = 1,
            };

            Rect2D scissor = new()
            {
                Offset = { X = 0, Y = 0 },
                Extent = swapchainExtent,
            };

            PipelineViewportStateCreateInformation viewportState = new()
            {
                Viewports = new[]{viewport},
                Scissors = new[]{scissor}
            };

            PipelineRasterizationStateCreateInformation rasterizer = new()
            {
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1,
                CullMode = CullModeFlags.BackBit,
                FrontFace = FrontFace.CounterClockwise,
                DepthBiasEnable = false,
            };

            PipelineMultisampleStateCreateInformation multisampling = new()
            {
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.Count1Bit,
            };

            PipelineDepthStencilStateCreateInfo depthStencil = new()
            {
                SType= StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.Less,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false,
            };

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                BlendEnable = false,
            };

            PipelineColorBlendStateCreateInformation colorBlending = new()
            {
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                Attachments = new[]{colorBlendAttachment}
            };

            colorBlending.BlendConstants.X = 0;
            colorBlending.BlendConstants.Y = 0;
            colorBlending.BlendConstants.Z = 0;
            colorBlending.BlendConstants.W = 0;

            PipelineLayoutCreateInformation pipelineLayoutInfo = new()
            {
                SetLayouts = new[]{descriptorSetLayout!.DescriptorSetLayout}
            };

            pipelineLayout = device!.CreatePipelineLayout(pipelineLayoutInfo);

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
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                Layout = pipelineLayout,
                RenderPass = renderPass,
                Subpass = 0,
                BasePipelineHandle = default
            };

            graphicsPipeline = device.CreateGraphicsPipeline(pipelineInfo);
        }

        
    }

    protected override void CreateFramebuffers()
    {
        swapchainFramebuffers = new Framebuffer[swapchainImageViews!.Length];

        for(int i = 0; i < swapchainImageViews.Length; i++)
        {
            var attachments = new[] { swapchainImageViews[i], depthImageView };

            fixed(ImageView* attachmentsPtr = attachments)
            {
                FramebufferCreateInfo framebufferInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = renderPass,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentsPtr,
                    Width = swapchainExtent.Width,
                    Height = swapchainExtent.Height,
                    Layers = 1,
                };

                if (vk!.CreateFramebuffer(device, framebufferInfo, null, out swapchainFramebuffers[i]) != Result.Success)
                {
                    throw new Exception("failed to create framebuffer!");
                } 
            }
        }
    }

    protected virtual void CreateDepthResources()
    {
        Format depthFormat = FindDepthFormat();

        CreateImage(swapchainExtent.Width, swapchainExtent.Height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref depthImage, ref depthImageMemory);
        depthImageView = CreateImageView(depthImage, depthFormat, ImageAspectFlags.DepthBit);
    }

    protected Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            vk!.GetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);
            
            if(tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
            {
                return format;
            }
            else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
            {
                return format;
            }
        }

        throw new Exception("failed to find supported format!");
    }

    protected Format FindDepthFormat()
    {
        return FindSupportedFormat(new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint }, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
    }

    protected override void CreateTextureImageView()
    {
        textureImageView = CreateImageView(textureImage, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);
    }

    protected ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags)
    {
        ImageViewCreateInfo createInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
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
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }

        };

        ImageView imageView;

        if (vk!.CreateImageView(device, createInfo, null, out imageView) != Result.Success)
        {
            throw new Exception("failed to create image views!");
        }

        return imageView;
    }

    protected override void CreateVertexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex_26>() * vertices_26.Length);

        var (stagingBuffer, stagingBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        
        var data = stagingBufferMemory.MapMemory<Vertex_17>();
            vertices_26.AsSpan().CopyTo(new Span<Vertex_26>(data, vertices_26.Length));
        stagingBufferMemory.UnmapMemory();

        (vertexBuffer, vertexBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit);

        CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

        stagingBuffer.Dispose();
        stagingBufferMemory.Dispose();
    }

    protected override void CreateCommandBuffers()
    {
        commandBuffers = commandPool!.AllocateCommandBuffers((uint)swapchainFramebuffers!.Length);

        for (int i = 0; i < commandBuffers.Length; i++)
        {
            commandBuffers[i].Begin();

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType= StructureType.RenderPassBeginInfo,
                RenderPass = renderPass,
                Framebuffer = swapchainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 }, 
                    Extent = swapchainExtent,
                }
            };

            var clearValues = new ClearValue[]
            {
                new()
                {
                    Color = new (){ Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },
                },
                new()
                {
                    DepthStencil = new () { Depth = 1, Stencil = 0 }
                }
            };


            fixed(ClearValue* clearValuesPtr = clearValues)
            {
                renderPassInfo.ClearValueCount = (uint)clearValues.Length;
                renderPassInfo.PClearValues = clearValuesPtr;

                commandBuffers[i].BeginRenderPass(renderPassInfo, SubpassContents.Inline);
            }

            commandBuffers[i].BindPipeline(PipelineBindPoint.Graphics, graphicsPipeline!);

                commandBuffers[i].BindVertexBuffer(0, vertexBuffer!);

                commandBuffers[i].BindIndexBuffer(indexBuffer!, 0, IndexType.Uint16);

                vk!.CmdBindDescriptorSets(commandBuffers[i], PipelineBindPoint.Graphics, pipelineLayout, 0, 1, descriptorSets![i], 0, null);

                commandBuffers[i].DrawIndexed((uint)indices.Length);

            commandBuffers[i].EndRenderPass();
            
            if (vk!.EndCommandBuffer(commandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }

        }
    }
}