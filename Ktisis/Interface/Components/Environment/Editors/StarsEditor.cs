using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class StarsEditor : EditorBase {
	public override string Name { get; } = "星光";

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Stars);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("启用", EnvOverride.Stars, module);
		using var _ = this.Disable(module);

		ImGui.SliderFloat("星光", ref state.Stars.Stars, 0.0f, 20.0f);
		ImGui.SliderFloat("强度##1", ref state.Stars.StarIntensity, 0.0f, 2.5f);
		ImGui.Spacing();
		ImGui.SliderFloat("星座", ref state.Stars.Constellations, 0.0f, 10.0f);
		ImGui.SliderFloat("强度##2", ref state.Stars.ConstellationIntensity, 0.0f, 2.5f);
		ImGui.Spacing();
		ImGui.SliderFloat("星系强度", ref state.Stars.GalaxyIntensity, 0.0f, 10.0f);
		ImGui.Spacing();
		ImGui.ColorEdit4("月亮颜色", ref state.Stars.MoonColor);
		ImGui.SliderFloat("月亮亮度", ref state.Stars.MoonBrightness, 0.0f, 1.0f);
	}
}
