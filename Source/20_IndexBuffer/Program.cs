using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SilkNetConvenience.Buffers;
using SilkNetConvenience.Memory;
using SilkNetConvenience.RenderPasses;

var app = new HelloTriangleApplication_20();
app.Run();

public class HelloTriangleApplication_20 : HelloTriangleApplication_19
{
    protected VulkanBuffer? indexBuffer;
    protected VulkanDeviceMemory? indexBufferMemory;

    protected override Vertex_17[] vertices { get; } = new Vertex_17[]
    {
        new Vertex_17 { pos = new Vector2D<float>(-0.5f,-0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f) },
        new Vertex_17 { pos = new Vector2D<float>(0.5f,-0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f) },
        new Vertex_17 { pos = new Vector2D<float>(0.5f,0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f) },
        new Vertex_17 { pos = new Vector2D<float>(-0.5f,0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f) },
    };

    protected virtual ushort[] indices { get; } = new ushort[]
    {
        0, 1, 2, 2, 3, 0
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
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateCommandPool();
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    protected override void CleanUp()
    {
        CleanUpSwapchain();

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

    protected virtual void CreateIndexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<ushort>() * indices.Length);

        var (stagingBuffer, stagingBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var data = stagingBufferMemory.MapMemory<ushort>();
            indices.AsSpan().CopyTo(data);
        stagingBufferMemory.UnmapMemory();

        (indexBuffer, indexBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit);

        CopyBuffer(stagingBuffer, indexBuffer, bufferSize);

        stagingBuffer.Dispose();
        stagingBufferMemory.Dispose();
    }

    protected override void CreateCommandBuffers() {
        commandBuffers = commandPool!.AllocateCommandBuffers((uint)swapchainFramebuffers!.Length);

        for (int i = 0; i < commandBuffers.Length; i++) {
            commandBuffers[i].Begin();

            RenderPassBeginInformation renderPassInfo = new()
            {
                RenderPass = renderPass!,
                Framebuffer = swapchainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 }, 
                    Extent = swapchainExtent,
                }
            };

            ClearValue clearColor = new()
            {
                Color = new (){ Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },                
            };

            renderPassInfo.ClearValues = new[]{clearColor};

            commandBuffers[i].BeginRenderPass(renderPassInfo, SubpassContents.Inline);
                commandBuffers[i].BindPipeline(PipelineBindPoint.Graphics, graphicsPipeline!);
                commandBuffers[i].BindVertexBuffer(0, vertexBuffer!);
                commandBuffers[i].BindIndexBuffer(indexBuffer!, 0, IndexType.Uint16);
                commandBuffers[i].DrawIndexed((uint)indices.Length);
            commandBuffers[i].EndRenderPass();
            commandBuffers[i].End();
        }
    }
}