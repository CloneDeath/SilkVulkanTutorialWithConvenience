using Silk.NET.Vulkan;
using SilkNetConvenience.CreateInfo.Images;
using SilkNetConvenience.Wrappers;

var app = new HelloTriangleApplication_07();
app.Run();

public class HelloTriangleApplication_07 : HelloTriangleApplication_06
{
    protected VulkanImageView[]? swapchainImageViews;

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

    protected virtual void CreateImageViews()
    {
        swapchainImageViews = new VulkanImageView[swapchainImages!.Length];

        for (int i = 0; i < swapchainImages.Length; i++)
        {
            ImageViewCreateInformation createInfo = new()
            {
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

            swapchainImageViews[i] = device!.CreateImageView(createInfo);
        }
    }
}