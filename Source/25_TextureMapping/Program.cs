using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SilkNetConvenience.Descriptors;

var app = new HelloTriangleApplication_25();
app.Run();

public struct Vertex_25
{
    public Vector2D<float> pos;
    public Vector3D<float> color;
    public Vector2D<float> textCoord;

    public static VertexInputBindingDescription GetBindingDescription()
    {
        VertexInputBindingDescription bindingDescription = new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vertex_25>(),
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
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex_25>(nameof(pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex_25>(nameof(color)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex_25>(nameof(textCoord)),
            }
        };

        return attributeDescriptions;
    }
}

public unsafe class HelloTriangleApplication_25 : HelloTriangleApplication_24
{
    protected Vertex_25[] vertices_25 = new Vertex_25[]
    {
        new Vertex_25 { pos = new Vector2D<float>(-0.5f,-0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f), textCoord = new Vector2D<float>(1.0f, 0.0f) },
        new Vertex_25 { pos = new Vector2D<float>(0.5f,-0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f), textCoord = new Vector2D<float>(0.0f, 0.0f) },
        new Vertex_25 { pos = new Vector2D<float>(0.5f,0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f), textCoord = new Vector2D<float>(0.0f, 1.0f) },
        new Vertex_25 { pos = new Vector2D<float>(-0.5f,0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f), textCoord = new Vector2D<float>(1.0f, 1.0f) },
    };

    protected override void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBindingInformation uboLayoutBinding = new()
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            StageFlags = ShaderStageFlags.VertexBit,
        };

        DescriptorSetLayoutBindingInformation samplerLayoutBinding = new()
        {
            Binding = 1,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            StageFlags = ShaderStageFlags.FragmentBit,
        };

        var bindings = new[] { uboLayoutBinding, samplerLayoutBinding };

        DescriptorSetLayoutCreateInformation layoutInfo = new() {
            Bindings = bindings
        };

        if (vk!.CreateDescriptorSetLayout(device, layoutInfo, null, descriptorSetLayoutPtr) != Result.Success) {
            throw new Exception("failed to create descriptor set layout!");
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

        var bindingDescription = Vertex_25.GetBindingDescription();
        var attributeDescriptions = Vertex_25.GetAttributeDescriptions();

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

            GraphicsPipelineCreateInformation pipelineInfo = new()
            {
                Stages = shaderStages,
                VertexInputState = vertexInputInfo,
                InputAssemblyState = inputAssembly,
                ViewportState = viewportState,
                RasterizationState = rasterizer,
                MultisampleState = multisampling,
                ColorBlendState = colorBlending,
                Layout = pipelineLayout,
                RenderPass = renderPass!,
                Subpass = 0,
            };

            graphicsPipeline = device.CreateGraphicsPipeline(pipelineInfo);
        }

        
    }

    protected override void CreateVertexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex_25>() * vertices_25.Length);

        var (stagingBuffer, stagingBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        
        var data = stagingBufferMemory.MapMemory<Vertex_17>();
            vertices_25.AsSpan().CopyTo(new Span<Vertex_25>(data, vertices_25.Length));
        stagingBufferMemory.UnmapMemory();

        (vertexBuffer, vertexBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit);

        CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

        stagingBuffer.Dispose();
        stagingBufferMemory.Dispose();
    }

    protected override void CreateDescriptorPool()
    {
        var poolSizes = new[]
        {
            new DescriptorPoolSize()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = (uint)swapchainImages!.Length,
            },
            new DescriptorPoolSize()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)swapchainImages!.Length,
            }
        };
        
        fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
        fixed (DescriptorPool* descriptorPoolPtr = &descriptorPool)
        {

            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = poolSizesPtr,
                MaxSets = (uint)swapchainImages!.Length,
            };

            if (vk!.CreateDescriptorPool(device, poolInfo, null, descriptorPoolPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }

        }
    }

    protected override void CreateDescriptorSets()
    {
        var layouts = new DescriptorSetLayout[swapchainImages!.Length];
        Array.Fill(layouts, descriptorSetLayout); 

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = (uint)swapchainImages!.Length,
                PSetLayouts = layoutsPtr,
            };

            descriptorSets = new DescriptorSet[swapchainImages.Length];
            fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
            {
                if (vk!.AllocateDescriptorSets(device, allocateInfo, descriptorSetsPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate descriptor sets!");
                }
            }
        }
        

        for (int i = 0; i < swapchainImages.Length; i++)
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = uniformBuffers![i],
                Offset = 0,
                Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),

            };

            DescriptorImageInfo imageInfo = new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = textureImageView,
                Sampler = textureSampler,
            };

            var descriptorWrites = new WriteDescriptorSet[]
            {
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                }
            };

            fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
            {
                vk!.UpdateDescriptorSets(device, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, null);
            }
        }

    }
}