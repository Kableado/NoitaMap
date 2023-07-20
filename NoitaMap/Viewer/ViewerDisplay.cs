﻿using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NoitaMap.Graphics;
using NoitaMap.Map;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Veldrid;
using Veldrid.SPIRV;

namespace NoitaMap.Viewer;

public class ViewerDisplay : IDisposable
{
    private readonly IWindow Window;

    private readonly GraphicsDevice GraphicsDevice;

    private readonly CommandList MainCommandList;

    private Framebuffer MainFrameBuffer;

    private Pipeline MainPipeline;

    private readonly DeviceBuffer TestVertexBuffer;

    private ResourceSet TestResourceSet;

    private readonly StagingResourcePool StagingResourcePool;

    private readonly Texture TestTexture;

    private readonly MaterialProvider MaterialProvider;

    private bool Disposed;

    public ViewerDisplay()
    {
        WindowOptions windowOptions = new WindowOptions()
        {
            API = GraphicsAPI.None,
            Title = "Noita Map Viewer",
            Size = new Vector2D<int>(1280, 720)
        };

        GraphicsDeviceOptions graphicsOptions = new GraphicsDeviceOptions()
        {
#if DEBUG
            Debug = true,
#endif
            SyncToVerticalBlank = true,
            HasMainSwapchain = true
        };

        VeldridWindow.CreateWindowAndGraphicsDevice(windowOptions, graphicsOptions, out Window, out GraphicsDevice);

        MainCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        StagingResourcePool = new StagingResourcePool(GraphicsDevice);

        (Shader[] shaders, VertexElementDescription[] vertexElements, ResourceLayoutDescription[] resourceLayout) = ShaderLoader.Load(GraphicsDevice, "PixelShader", "VertexShader");

        MainPipeline = CreatePipeline(shaders, vertexElements, resourceLayout);

        MaterialProvider = new MaterialProvider();

        Material brick = MaterialProvider.GetMaterial("brick");

        TextureDescription desc = new TextureDescription()
        {
            Type = TextureType.Texture2D,
            Format = PixelFormat.R8_G8_B8_A8_UNorm,
            Width = (uint)brick.MaterialTexture.Width,
            Height = (uint)brick.MaterialTexture.Height,
            Usage = TextureUsage.Sampled,
            MipLevels = 1,

            // Nececessary
            Depth = 1,
            ArrayLayers = 1,
            SampleCount = TextureSampleCount.Count1,
        };

        TestTexture = GraphicsDevice.ResourceFactory.CreateTexture(desc);

        GraphicsDevice.UpdateTexture(TestTexture, MemoryMarshal.CreateSpan(ref brick.MaterialTexture.Span.DangerousGetReference(), (int)brick.MaterialTexture.Length), 0, 0, 0, (uint)brick.MaterialTexture.Width, (uint)brick.MaterialTexture.Height, 1, 0, 0);

        TestResourceSet = CreateTestResourceSet(resourceLayout.Single());

        Vertex[] quadVertices = new Vertex[]
        {
            new Vertex() { Position = new Vector3(-0.5f, -0.5f, 0f), UV = new Vector2(0f, 0f) }, // Bottom-left vertex
            new Vertex() { Position = new Vector3(0.5f, -0.5f, 0f), UV = new Vector2(1f, 0f) },  // Bottom-right vertex
            new Vertex() { Position = new Vector3(-0.5f, 0.5f, 0f), UV = new Vector2(0f, 1f) },  // Top-left vertex

            new Vertex() { Position = new Vector3(-0.5f, 0.5f, 0f), UV = new Vector2(0f, 1f) },  // Top-left vertex
            new Vertex() { Position = new Vector3(0.5f, -0.5f, 0f), UV = new Vector2(1f, 0f) },  // Bottom-right vertex
            new Vertex() { Position = new Vector3(0.5f, 0.5f, 0f), UV = new Vector2(1f, 1f) }    // Top-right vertex
        };

        TestVertexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
        {
            SizeInBytes = (uint)(Unsafe.SizeOf<Vertex>() * quadVertices.Length),
            Usage = BufferUsage.VertexBuffer | BufferUsage.Dynamic,
        });

        unsafe
        {
            MappedResource mapped = GraphicsDevice.Map(TestVertexBuffer, MapMode.Write);

            fixed (void* vertexPointer = &quadVertices[0])
            {
                Unsafe.CopyBlock((void*)mapped.Data, vertexPointer, (uint)(Unsafe.SizeOf<Vertex>() * quadVertices.Length));
            }

            GraphicsDevice.Unmap(TestVertexBuffer);
        }

        Window.Center();

        Window.Render += x => Render();

        Window.Resize += HandleResize;
    }

    public void Start()
    {
        Window.Run();
    }

    private void Render()
    {
        MainCommandList.Begin();

        MainCommandList.SetFramebuffer(MainFrameBuffer);

        MainCommandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

        MainCommandList.SetPipeline(MainPipeline);

        MainCommandList.SetGraphicsResourceSet(0, TestResourceSet);

        MainCommandList.SetVertexBuffer(0, TestVertexBuffer);

        MainCommandList.Draw(6);

        MainCommandList.End();

        GraphicsDevice.SubmitCommands(MainCommandList);

        GraphicsDevice.SwapBuffers();
    }

    private Pipeline CreatePipeline(Shader[] shaders, VertexElementDescription[] vertexElements, ResourceLayoutDescription[] resourceLayout)
    {
        return GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription()
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription()
            {
                DepthComparison = ComparisonKind.Less,
                DepthTestEnabled = true,
                DepthWriteEnabled = true
            },
            Outputs = MainFrameBuffer.OutputDescription,
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            RasterizerState = new RasterizerStateDescription()
            {
                CullMode = FaceCullMode.None,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise
            },
            ShaderSet = new ShaderSetDescription()
            {
                Shaders = shaders,
                VertexLayouts = new VertexLayoutDescription[] { new VertexLayoutDescription(vertexElements) }
            },
            ResourceLayouts = resourceLayout.Select(x => GraphicsDevice.ResourceFactory.CreateResourceLayout(x)).ToArray()
        });
    }

    private ResourceSet CreateTestResourceSet(ResourceLayoutDescription resourceLayout)
    {
        return GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription()
        {
            BoundResources = new BindableResource[] { GraphicsDevice.ResourceFactory.CreateTextureView(TestTexture), GraphicsDevice.PointSampler },
            Layout = GraphicsDevice.ResourceFactory.CreateResourceLayout(resourceLayout)
        });
    }

    private void HandleResize(Vector2D<int> size)
    {
        GraphicsDevice.ResizeMainWindow((uint)size.X, (uint)size.Y);

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        // We call render to be more responsive when resizing.. or something like that
        Render();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            MainCommandList.Dispose();

            GraphicsDevice.Dispose();

            Window.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private struct Vertex
    {
        public Vector3 Position;

        public Vector2 UV;
    }
}
