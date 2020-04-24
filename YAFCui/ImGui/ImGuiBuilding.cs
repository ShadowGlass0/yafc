using System;
using System.Collections.Generic;
using System.Numerics;
using SDL2;

namespace YAFC.UI
{
    public partial class ImGui
    {
        private readonly List<(Rect, SchemeColor)> rects = new List<(Rect, SchemeColor)>();
        private readonly List<(Rect, RectangleBorder)> borders = new List<(Rect, RectangleBorder)>();
        private readonly List<(Rect, Icon, SchemeColor)> icons = new List<(Rect, Icon, SchemeColor)>();
        private readonly List<(Rect, IRenderable, SchemeColor)> renderables = new List<(Rect, IRenderable, SchemeColor)>();
        private readonly List<(Rect, IGuiPanel)> panels = new List<(Rect, IGuiPanel)>();

        public void DrawRectangle(Rect rect, SchemeColor color, RectangleBorder border = RectangleBorder.None)
        {
            CheckBuilding();
            rects.Add((rect, color));
            if (border != RectangleBorder.None)
                borders.Add((rect, border));
        }

        public void DrawIcon(Rect rect, Icon icon, SchemeColor color)
        {
            CheckBuilding();
            if (icon == Icon.None)
                return;
            icons.Add((rect, icon, color));
        }

        public void DrawRenderable(Rect rect, IRenderable renderable, SchemeColor color)
        {
            CheckBuilding();
            renderables.Add((rect, renderable, color));
        }

        public void DrawPanel(Rect rect, IGuiPanel panel)
        {
            CheckBuilding();
            panels.Add((rect, panel));
            panel.Build(rect + screenOffset, this, pixelsPerUnit);
        }
        
        public readonly ImGuiCache<TextCache, (FontFile.FontSize size, string text, uint wrapWidth)>.Cache textCache = new ImGuiCache<TextCache, (FontFile.FontSize size, string text, uint wrapWidth)>.Cache();

        public FontFile.FontSize GetFontSize(Font font = null) => (font ?? Font.text).GetFontSize(pixelsPerUnit);

        public void BuildText(string text, Font font = null, SchemeColor color = SchemeColor.BackgroundText, bool wrap = false, RectAlignment align = RectAlignment.MiddleLeft)
        {
            var fontSize = GetFontSize(font);
            var cache = textCache.GetCached((fontSize, text, wrap ? (uint) UnitsToPixels(width) : uint.MaxValue));
            var rect = AllocateRect(cache.texRect.w / pixelsPerUnit, cache.texRect.h / pixelsPerUnit, align);
            if (action == ImGuiAction.Build)
            {
                DrawRenderable(rect, cache, color);
            }
        }

        private ImGuiTextInputHelper textInputHelper;
        public bool BuildTextInput(string text, out string newText, string placeholder)
        {
            if (textInputHelper == null)
                textInputHelper = new ImGuiTextInputHelper(this);
            return textInputHelper.BuildTextInput(text, out newText, placeholder, GetFontSize());
        }
        
        public void BuildIcon(Icon icon, SchemeColor color, float size = 1f)
        {
            var rect = AllocateRect(size, size, RectAlignment.Middle);
            if (action == ImGuiAction.Build)
                DrawIcon(rect, icon, color);
        }

        public Vector2 mousePosition { get; private set; }
        public bool mousePresent { get; private set; }
        private Rect mouseDownRect;
        private Rect mouseOverRect = Rect.VeryBig;
        private readonly RectAllocator defaultAllocator;
        public event Action CollectCustomCache;

        private bool DoGui(ImGuiAction action)
        {
            this.action = action;
            ResetLayout();
            gui.Build(this);
            eventArg = 0;
            var consumed = this.action == ImGuiAction.Consumed;
            if (IsRebuildRequired())
                BuildGui(buildWidth);
            this.action = ImGuiAction.Consumed;
            return consumed;
        }

        private void BuildGui(float width)
        {
            buildWidth = width;
            nextRebuildTimer = long.MaxValue;
            rebuildRequested = false;
            rects.Clear();
            borders.Clear();
            icons.Clear();
            renderables.Clear();
            panels.Clear();
            DoGui(ImGuiAction.Build);
            contentSize = layoutSize;
            textCache.PurgeUnused();
            CollectCustomCache?.Invoke();
            Repaint();
        }

        public void MouseMove(Vector2 mousePosition, int mouseDownButton)
        {
            eventArg = mouseDownButton;
            mousePresent = true;
            this.mousePosition = mousePosition - screenOffset;
            if (!mouseOverRect.Contains(this.mousePosition))
            {
                mouseOverRect = Rect.VeryBig;
                rebuildRequested = true;
                SDL.SDL_SetCursor(RenderingUtils.cursorArrow);
            }

            DoGui(ImGuiAction.MouseMove);
        }

        public void MouseDown(int button)
        {
            eventArg = button;
            DoGui(ImGuiAction.MouseDown);
        }

        public void MouseUp(int button)
        {
            eventArg = button;
            DoGui(ImGuiAction.MouseUp);
        }

        public void MouseScroll(int delta)
        {
            eventArg = delta;
            DoGui(ImGuiAction.MouseScroll);
        }

        public void MouseExit()
        {
            mousePosition = default;
            mousePresent = false;
            if (mouseOverRect != Rect.VeryBig)
            {
                SDL.SDL_SetCursor(RenderingUtils.cursorArrow);
                BuildGui(buildWidth);
            }
        }

        public bool ConsumeMouseDown(Rect rect)
        {
            if (action == ImGuiAction.MouseDown && mousePresent && rect.Contains(mousePosition))
            {
                action = ImGuiAction.Consumed;
                rebuildRequested = true;
                mouseDownRect = rect;
                return true;
            }

            return false;
        }

        public bool ConsumeMouseOver(Rect rect, IntPtr cursor = default, bool rebuild = true)
        {
            if (action == ImGuiAction.MouseMove && mousePresent && rect.Contains(mousePosition))
            {
                action = ImGuiAction.Consumed;
                if (mouseOverRect != rect)
                {
                    if (rebuild)
                        rebuildRequested = true;
                    mouseOverRect = rect;
                    SDL.SDL_SetCursor(cursor == default ? RenderingUtils.cursorArrow : cursor);
                }
                return true;
            }

            return false;
        }

        public bool ConsumeMouseUp(Rect rect, bool inside = true)
        {
            if (action == ImGuiAction.MouseUp && rect == mouseDownRect && (!inside || rect.Contains(mousePosition)))
            {
                action = ImGuiAction.Consumed;
                return true;
            }

            return false;
        }

        public void ConsumeEvent(Rect rect)
        {
            if (action == ImGuiAction.MouseScroll && rect.Contains(mousePosition))
                action = ImGuiAction.Consumed;
        }

        public bool IsMouseOver(Rect rect) => rect == mouseOverRect;
        public bool IsMouseDown(Rect rect) => rect == mouseDownRect;
    }
}