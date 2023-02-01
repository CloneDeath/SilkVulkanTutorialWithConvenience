using Silk.NET.Vulkan;

var app = new HelloTriangleApplication_13();
app.Run();

public unsafe class HelloTriangleApplication_13 : HelloTriangleApplication_12
{
    protected Framebuffer[]? swapchainFramebuffers;

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
    }

    protected override void CleanUp()
    {
        foreach (var framebuffer in swapchainFramebuffers!)
        {
            vk!.DestroyFramebuffer(device, framebuffer, null);
        }

        vk!.DestroyPipeline(device, graphicsPipeline, null);
        vk!.DestroyPipelineLayout(device, pipelineLayout, null);
        vk!.DestroyRenderPass(device, renderPass, null);

        foreach (var imageView in swapchainImageViews!)
        {
            vk!.DestroyImageView(device, imageView, null);
        }

        khrSwapchain!.DestroySwapchain(device, swapchain, null);

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

    protected virtual void CreateFramebuffers()
    {
        swapchainFramebuffers = new Framebuffer[swapchainImageViews!.Length];

        for(int i = 0; i < swapchainImageViews.Length; i++)
        {
            var attachment = swapchainImageViews[i];
            
            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = renderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = swapchainExtent.Width,
                Height = swapchainExtent.Height,
                Layers = 1,
            };

            if(vk!.CreateFramebuffer(device,framebufferInfo, null,out swapchainFramebuffers[i]) != Result.Success)
            {
                throw new Exception("failed to create framebuffer!");
            }
        }
    }
}