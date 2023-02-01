using Silk.NET.Vulkan;

var app = new HelloTriangleApplication_07();
app.Run();

public unsafe class HelloTriangleApplication_07 : HelloTriangleApplication_06
{
    protected ImageView[]? swapchainImageViews;

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapchain();
        CreateImageViews();
    }

    protected override void CleanUp()
    {
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

    protected virtual void CreateImageViews()
    {
        swapchainImageViews = new ImageView[swapchainImages!.Length];

        for (int i = 0; i < swapchainImages.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = swapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = swapchainImageFormat,
                Components =
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity,
                },
                SubresourceRange =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }

            };

            if(vk!.CreateImageView(device, createInfo, null, out swapchainImageViews[i]) != Result.Success)
            {
                throw new Exception("failed to create image views!");
            }
        }
    }
}