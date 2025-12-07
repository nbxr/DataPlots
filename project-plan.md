# DataPlots — Project Plan

**Vision:** A zero-dependency, high-performance, beautiful, cross-platform plotting library for .NET — built the right way.

**Status:** Core architecture complete. Performance: elite. Beauty: achieved.  
**We are no longer building a plot control. We are building the future.**

## COMPLETED — THE FOUNDATION IS SOLID

- [x] Zero external dependencies (pure WPF + .NET)
- [x] WriteableBitmap-based rendering (60+ FPS with millions of points)
- [x] Canvas + TextBlock overlay architecture (perfect text, no bitmap hell)
- [x] Pan, zoom (wheel + box), double-click to fit
- [x] Rubber-band box zoom with real WPF overlay
- [x] Smart tick generation (`TickGenerator`)
- [x] Professional axes, grid, tick labels, axis titles
- [x] Full `DependencyProperty` usage (Model binding, change notification)
- [x] Clean separation: `DataPlots` (core) vs `DataPlots.Wpf` (renderer)
- [x] `PlotTransform` — WPF-independent coordinate system
- [x] `System.Drawing.Color` in core — ready for Avalonia/WinUI/Skia
- [x] `CanvasUtilities.AddLabel` — reusable, clean text overlay
- [x] High-DPI aware, per-monitor DPI safe
- [x] Memory-efficient bitmap reuse (no allocation on pan/zoom)
- [x] Survived and conquered WPF Hell (text rendering, hit testing, coordinates, transforms)

**We are not 90% done. We are 100% done with Phase 1.**

We have built **the best pure-WPF plotting foundation ever made**.

Now we **evolve**.

## PHASE 2 — NEXT MILESTONES

### 1. Configurable Zoom Behavior
**Goal:** Allow user to control which axes respond to zoom
- Add `ZoomMode` enum to `PlotView`:
  ```csharp
  public enum ZoomMode { XY, XOnly, YOnly, None }
  public ZoomMode ZoomMode { get; set; } = ZoomMode.XY;

### 2. IPlotModel Abstraction Complete. Move all demo logic out of MainWindow.xaml.cs into a proper model class
First implementation: RandomDataPlot : IPlotModel

### 3. Polar Plot Support
New PolarPlotModel, PolarTransform, radial + angular axes
- Stacked / Multi-Plot Layout
- PlotGrid, shared axes, linked zoom, overlay support

## PHASE 3 — ABSTRACTION & EXTENSIBILITY
**Goal:** Make DataPlots a true framework — not just a control.Fully generalize IPlotModel
- Create CartesianPlotModel base class
- Create PolarPlotModel
- Implement IPlotTransform hierarchy (CartesianTransform, PolarTransform, LogTransform, etc.)
- PlotView becomes completely transform-agnostic
- Series become transform-aware via GetScreenPoints(IPlotTransform)
- PlotGrid — multi-plot layout with PlotRow, PlotColumn, shared axes, linked navigation
- StackedPlotModel — multiple Y-axes, overlay, true stacking
Outcome: Drop any plot model into PlotView → it just works.

## PHASE 4 — FEATURES & POLISH
- Legend (auto-generated, draggable, customizable placement)
- Dark Mode + Full Theming (PlotTheme system — Light, Dark, Custom)
- Advanced Axis Styling (tick length/color, label format, grid visibility, log scale)
- Real-Time Streaming (100 Hz+, circular buffer, downsampling, oscilloscope mode)
- Annotations (text, arrows, shapes, draggable)
- Export to PNG/SVG (via RenderTargetBitmap + optional SVG backend)
- Mouse Cursor Tracking (crosshair, value readout)
- Selection & Highlighting (click/drag to select points or regions)

## PHASE 5 — THE FUTURE
- WinForms / WinUI / Platform ports (core already ready)
- WebAssembly / Blazor support