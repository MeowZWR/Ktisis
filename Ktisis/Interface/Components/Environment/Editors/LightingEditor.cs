using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class LightingEditor : EditorBase {
	public override string Name { get; } = "光";

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Lighting);

	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("启用", EnvOverride.Lighting, module);
		using var _ = this.Disable(module);

		using var _disable = ImRaii.Disabled(!module.Override.HasFlag(EnvOverride.Lighting));
		ImGui.ColorEdit3("阳光", ref state.Lighting.SunLightColor);
		ImGui.ColorEdit3("月光", ref state.Lighting.MoonLightColor);
		ImGui.ColorEdit3("环境光", ref state.Lighting.Ambient);
		ImGui.SliderFloat("未知 #1", ref state.Lighting._unk1, 0.0f, 10.0f);
		ImGui.SliderFloat("饱和度", ref state.Lighting.AmbientSaturation, 0.0f, 5.0f);
		ImGui.SliderFloat("温度", ref state.Lighting.Temperature, -2.5f, 2.5f);
		ImGui.SliderFloat("未知 #2", ref state.Lighting._unk2, 0.0f, 100.0f);
		ImGui.SliderFloat("未知 #3", ref state.Lighting._unk3, 0.0f, 100.0f);
		ImGui.SliderFloat("未知 #4", ref state.Lighting._unk4, 0.0f, 1.0f);
	}
}