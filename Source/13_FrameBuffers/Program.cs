using SilkNetConvenience.Buffers;

var app = new HelloTriangleApplication_13();
app.Run();

public class HelloTriangleApplication_13 : HelloTriangleApplication_12
{
    protected VulkanFramebuffer[]? swapchainFramebuffers;

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

    protected virtual void CreateFramebuffers()
    {
        swapchainFramebuffers = new VulkanFramebuffer[swapchainImageViews!.Length];

        for(int i = 0; i < swapchainImageViews.Length; i++)
        {
            var attachment = swapchainImageViews[i];
            
            FramebufferCreateInformation framebufferInfo = new()
            {
                RenderPass = renderPass!,
                Attachments = new[]{attachment.ImageView},
                Width = swapchainExtent.Width,
                Height = swapchainExtent.Height,
                Layers = 1,
            };

            swapchainFramebuffers[i] = device!.CreateFramebuffer(framebufferInfo);
        }
    }
}