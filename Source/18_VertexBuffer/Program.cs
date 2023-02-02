using Silk.NET.Vulkan;
using SilkNetConvenience.Buffers;
using SilkNetConvenience.Memory;
using SilkNetConvenience.RenderPasses;

var app = new HelloTriangleApplication_18();
app.Run();

public unsafe class HelloTriangleApplication_18 : HelloTriangleApplication_17
{
    protected VulkanBuffer? vertexBuffer;
    protected VulkanDeviceMemory? vertexBufferMemory;

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
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    protected override void CleanUp()
    {
        CleanUpSwapchain();

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

    protected virtual void CreateVertexBuffer()
    {
        BufferCreateInformation bufferInfo = new()
        {
            Size = (ulong)(sizeof(Vertex_17) * vertices.Length),
            Usage = BufferUsageFlags.VertexBufferBit,
            SharingMode = SharingMode.Exclusive,
        };

        vertexBuffer = device!.CreateBuffer(bufferInfo);

        vertexBufferMemory = device!.AllocateMemoryFor(vertexBuffer,
                                                       MemoryPropertyFlags.HostVisibleBit |
                                                       MemoryPropertyFlags.HostCoherentBit);

        vertexBuffer.BindMemory(vertexBufferMemory);

        var data = vertexBufferMemory.MapMemory<Vertex_17>();
        vertices.AsSpan().CopyTo(data);
        vertexBufferMemory.UnmapMemory();
    }

    protected override void CreateCommandBuffers()
    {
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
                commandBuffers[i].Draw((uint)vertices.Length);
            commandBuffers[i].EndRenderPass();

            commandBuffers[i].End();

        }
    }
}