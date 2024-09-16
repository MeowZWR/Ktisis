using System;
using System.Numerics;

using ImGuiNET;

using Ktisis.Structs.Actor;
using Ktisis.Structs.Actor.State;

namespace Ktisis.Interface.Windows.ActorEdit {
	public static class EditActor {
		// Properties

		public static bool Visible = false;

		public unsafe static Actor* Target
			=> Ktisis.GPoseTarget != null ? (Actor*)Ktisis.GPoseTarget.Address : null;

		// Toggle visibility

		public static void Show() => Visible = true;
		public static void Hide() => Visible = false;

		// Display

		public unsafe static void Draw() {
			if (!Visible)
				return;

			if (Target == null)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			// Create window

			if (ImGui.Begin("角色外观", ref Visible, ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				if (ImGui.BeginTabBar("设置")) {
					if (ImGui.BeginTabItem("容貌"))
						EditCustomize.Draw();
					if (ImGui.BeginTabItem("装备"))
						EditEquip.Draw();
					if (ImGui.BeginTabItem("高级"))
						AdvancedEdit();

					ImGui.EndTabBar();
				}

				ImGui.PopStyleVar(1);
				ImGui.End();
			}
		}

		public unsafe static void AdvancedEdit() {
            ImGui.Spacing();
			var modelId = (int)Target->ModelId;
			if (ImGui.InputInt("模型 ID", ref modelId)) {
				Target->ModelId = (uint)modelId;
				Target->Redraw();
			}
			
			ImGui.Spacing();
			ImGui.SliderFloat("不透明度", ref Target->Transparency, 0.0f, 1.0f);

			if (Target->Model != null) {
				if (!ActorWetnessOverride.Instance.WetnessOverrides.TryGetValue((IntPtr)Target, out var wetness))
					wetness = (Target->Model->WeatherWetness, Target->Model->SwimmingWetness, Target->Model->WetnessDepth);

				if (!ActorWetnessOverride.Instance.WetnessOverridesEnabled.TryGetValue((IntPtr)Target, out var enabled))
					enabled = false;

				ImGui.Checkbox("启用湿度覆盖", ref enabled);

				ImGui.SliderFloat("天气湿度", ref wetness.WeatherWetness, 0.0f, 1.0f);
				ImGui.SliderFloat("游泳湿度", ref wetness.SwimmingWetness, 0.0f, 1.0f);
				ImGui.SliderFloat("湿度深度", ref wetness.WetnessDepth, 0.0f, 3.0f);

				ActorWetnessOverride.Instance.WetnessOverrides[(IntPtr)Target] = wetness;
				ActorWetnessOverride.Instance.WetnessOverridesEnabled[(IntPtr)Target] = enabled;
			}

			ImGui.EndTabItem();
		}
	}
}
