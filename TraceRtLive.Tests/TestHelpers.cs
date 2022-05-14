using Spectre.Console;
using Spectre.Console.Rendering;

namespace TraceRtLive.Tests
{
    public static class TestHelpers
    {

        public static RenderContext RenderContext { get; } = new RenderContext(new StringOutputCapabilities());

        private class StringOutputCapabilities : IReadOnlyCapabilities
        {
            public ColorSystem ColorSystem => ColorSystem.NoColors;
            public bool Ansi => false;
            public bool Links => false;
            public bool Legacy => false;
            public bool IsTerminal => false;
            public bool Interactive => false;
            public bool Unicode => true;
        }

        public static string RenderToString(this IRenderable renderable)
        {
            var segments = renderable.Render(RenderContext, maxWidth: 1024);
            return string.Concat(segments.Select(x => x.Text));
        }
    }
}
