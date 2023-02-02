using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Silk.NET.Assimp;

var app = new HelloTriangleApplication_27();
app.Run();

public unsafe class HelloTriangleApplication_27 : HelloTriangleApplication_26
{
    protected const string MODEL_PATH = @"Assets/viking_room.obj";
    protected const string TEXTURE_PATH = @"Assets/viking_room.png";

    private Vertex_26[] _vertices = Array.Empty<Vertex_26>();
    protected override Vertex_26[] vertices_26 => _vertices;

    protected uint[]? indices_27;

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
        LoadModel();
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    protected override void CreateTextureImage()
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(TEXTURE_PATH);

        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,ref stagingBuffer, ref stagingBufferMemory );

        void* data;
        vk!.MapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
        stagingBufferMemory.UnmapMemory();

        CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, ref textureImage, ref textureImageMemory);

        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        stagingBuffer.Dispose();
        stagingBufferMemory.Dispose();
    }

    protected void LoadModel()
    {
        using var assimp = Assimp.GetApi();
        var scene = assimp.ImportFile(MODEL_PATH, (uint)PostProcessPreset.TargetRealTimeMaximumQuality);

        var vertexMap = new Dictionary<Vertex_26, uint>();
        var localVertices = new List<Vertex_26>();
        var localIndices = new List<uint>();

        VisitSceneNode(scene->MRootNode);

        assimp.ReleaseImport(scene);

        _vertices = localVertices.ToArray();
        indices_27 = localIndices.ToArray();

        void VisitSceneNode(Node* node)
        {
            for (int m = 0; m < node->MNumMeshes; m++)
            {
                var mesh = scene->MMeshes[node->MMeshes[m]];

                for (int f = 0; f < mesh->MNumFaces; f++)
                {
                    var face = mesh->MFaces[f];
                    
                    for (int i = 0; i < face.MNumIndices; i++)
                    {
                        uint index = face.MIndices[i];                      

                        var position = mesh->MVertices[index];
                        var texture = mesh->MTextureCoords[0][(int)index];

                        Vertex_26 vertex = new Vertex_26
                        {
                            pos = new Vector3D<float>(position.X, position.Y, position.Z),
                            color = new Vector3D<float>(1, 1, 1),
                            //Flip Y for OBJ in Vulkan
                            textCoord = new Vector2D<float>(texture.X, 1.0f - texture.Y)
                        };

                        if(vertexMap.TryGetValue(vertex, out var meshIndex))
                        {
                            localIndices.Add(meshIndex);
                        }
                        else
                        {
                            localIndices.Add((uint)localVertices.Count);
                            vertexMap[vertex] = (uint)localVertices.Count;
                            localVertices.Add(vertex);
                        }                        
                    }
                }
            }

            for (int c = 0; c < node->MNumChildren; c++)
            {
                VisitSceneNode(node->MChildren[c]);
            }
        }
    }

    protected override void CreateIndexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<uint>() * indices_27!.Length);

        var (stagingBuffer, stagingBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var data = stagingBufferMemory.MapMemory<Vertex_17>();
            indices_27.AsSpan().CopyTo(new Span<uint>(data, indices_27.Length));
        stagingBufferMemory.UnmapMemory();

        (indexBuffer, indexBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit);

        CopyBuffer(stagingBuffer, indexBuffer, bufferSize);

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

                vk!.CmdBindIndexBuffer(commandBuffers[i], indexBuffer, 0, IndexType.Uint32);

                vk!.CmdBindDescriptorSets(commandBuffers[i], PipelineBindPoint.Graphics, pipelineLayout, 0, 1, descriptorSets![i], 0, null);

                vk!.CmdDrawIndexed(commandBuffers[i], (uint)indices_27!.Length, 1, 0, 0, 0);

            commandBuffers[i].EndRenderPass();
            
            if (vk!.EndCommandBuffer(commandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }

        }
    }
}