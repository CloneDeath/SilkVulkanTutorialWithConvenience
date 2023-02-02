using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using SilkNetConvenience.Buffers;
using SilkNetConvenience.Memory;
using Buffer = Silk.NET.Vulkan.Buffer;

var app = new HelloTriangleApplication_19();
app.Run();

public class HelloTriangleApplication_19 : HelloTriangleApplication_18
{
    protected override void CreateVertexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex_17>() * vertices.Length);

        var (stagingBuffer, stagingBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, 
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var data = stagingBufferMemory.MapMemory<Vertex_17>();
            vertices.AsSpan().CopyTo(data);
        stagingBufferMemory.UnmapMemory();

        (vertexBuffer, vertexBufferMemory) = CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit);

        CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

        stagingBuffer.Dispose();
        stagingBufferMemory.Dispose();
    }

    protected (VulkanBuffer, VulkanDeviceMemory) CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties)
    {
        BufferCreateInformation bufferInfo = new()
        { 
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };

        var buffer = device!.CreateBuffer(bufferInfo);
        var bufferMemory = device!.AllocateMemoryFor(buffer, properties);
        buffer.BindMemory(bufferMemory);
        return (buffer, bufferMemory);
    }

    protected virtual void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        using var commandBuffer = commandPool!.AllocateCommandBuffer();

        commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmitBit);
            commandBuffer.CopyBuffer(srcBuffer, dstBuffer, size);
        commandBuffer.End();

        graphicsQueue!.Submit(commandBuffer);
        graphicsQueue!.WaitIdle();
    }
}