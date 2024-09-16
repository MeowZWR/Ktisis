using System.Numerics;

using ImGuiNET;

using Ktisis.Structs.Actor;
using Ktisis.Interop.Hooks;
using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Interface.Windows.Workspace.Tabs;

namespace Ktisis.Interface.Windows.Toolbar {
	public static class AdvancedWindow {
		private static bool Visible = false;

		/* FIXME: This seems unused? */
		public static TransformTable Transform = new();

		// Toggle visibility
		public static void Toggle() => Visible = !Visible;

		// Draw window
		public unsafe static void Draw() {
			if (!Visible || !Ktisis.IsInGPose)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(new Vector2(ImGui.GetFontSize() * 16, 1), new Vector2(50000, 50000));
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("高级", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {

				var target = Ktisis.GPoseTarget;
				var actor = (Actor*)target!.Address;

				if (actor->Model != null) {
					// Animation Controls
					AnimationControls.Draw(target);

					// Gaze Controls
					if (ImGui.CollapsingHeader("视线控制")) {
						if (PoseHooks.PosingEnabled)
							ImGui.TextWrapped("姿势模式已启用时无法使用视线控制。");
						else
							EditGaze.Draw(actor);
					}
					
					// Advanced
					if (ImGui.CollapsingHeader("高级（调试）")) {
						PoseTab.DrawAdvancedDebugOptions(actor);
					}
				}
			}

			ImGui.PopStyleVar();
			ImGui.End();
		}
	}
}
