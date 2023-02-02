using Silk.NET.Vulkan;
using SilkNetConvenience.CommandBuffers;
using SilkNetConvenience.RenderPasses;

var app = new HelloTriangleApplication_14();
app.Run();

public class HelloTriangleApplication_14 : HelloTriangleApplication_13
{
    protected VulkanCommandPool? commandPool;
    protected VulkanCommandBuffer[]? commandBuffers;

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
        CreateCommandBuffers();
    }

    protected override void CleanUp()
    {
        commandPool?.Dispose();

        foreach (var framebuffer in swapchainFramebuffers!)
        {
            framebuffer.Dispose();
        }

        graphicsPipeline!.Dispose();
        pipelineLayout!.Dispose();
        renderPass!.Dispose();

        foreach (var imageView in swapchainImageViews!)
        {
            imageView.Dispose();
        }

        swapchain!.Dispose();

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

    protected void CreateCommandPool()
    {
        var queueFamilyIndices = FindQueueFamilies_05(physicalDevice!);

        CommandPoolCreateInformation poolInfo = new()
        {
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
        };

        commandPool = device!.CreateCommandPool(poolInfo);
    }

    protected virtual void CreateCommandBuffers()
    {
        commandBuffers = new VulkanCommandBuffer[swapchainFramebuffers!.Length];

        commandBuffers = commandPool!.AllocateCommandBuffers((uint)commandBuffers.Length, CommandBufferLevel.Primary);

        for (int i = 0; i < commandBuffers.Length; i++)
        {
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
                commandBuffers[i].Draw(3);
            commandBuffers[i].EndRenderPass();

            commandBuffers[i].End();

        }
    }
}